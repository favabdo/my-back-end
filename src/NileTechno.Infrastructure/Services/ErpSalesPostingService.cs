using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Services;

/// <summary>
/// Posts storefront orders into the ERP as sales invoices
/// (wh_TransHeader TransType=3 + wh_TransDetails), which the stock
/// procedure picks up on its next run so EC_Products stock decrements.
/// Ec_ErpPostings is the outbox that tracks success/failure per order.
/// </summary>
public class ErpSalesPostingService : IErpSalesPostingService
{
    private const int OrderStatusCanceled = 6;
    private const int OrderStatusRefunded = 7;

    private readonly string _connectionString;
    private readonly ILogger<ErpSalesPostingService> _logger;

    private readonly bool _enabled;
    private readonly int _storeId;
    private readonly int _branchId;
    private readonly byte _transType;
    private readonly byte _payType;
    private readonly byte _priceType;
    private readonly int _maxAttempts;

    public ErpSalesPostingService(IConfiguration configuration, ILogger<ErpSalesPostingService> logger)
    {
        _logger = logger;
        _connectionString = SqlConnectionString.Resolve(configuration);

        var section = configuration.GetSection("ErpPosting");
        _enabled = !bool.TryParse(section["Enabled"], out var enabled) || enabled;
        _storeId = int.TryParse(section["StoreId"], out var storeId) ? storeId : 1;
        _branchId = int.TryParse(section["BranchId"], out var branchId) ? branchId : 100;
        _transType = byte.TryParse(section["TransType"], out var transType) ? transType : (byte)3;
        _payType = byte.TryParse(section["PayType"], out var payType) ? payType : (byte)1;
        _priceType = byte.TryParse(section["PriceType"], out var priceType) ? priceType : (byte)5;
        _maxAttempts = int.TryParse(section["MaxAttempts"], out var maxAttempts) && maxAttempts > 0 ? maxAttempts : 10;
    }

    public async Task PostOrderAsync(int orderId, CancellationToken ct = default)
    {
        if (!_enabled) return;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            var (status, erpHeaderId) = await ReadPostingAsync(connection, orderId, ct);
            if (status is "Posted" or "Reversed")
                return;

            var orderStatus = await ExecuteScalarAsync<int?>(connection,
                "SELECT Status FROM dbo.Ec_Orders WHERE Id = @orderId;",
                ("@orderId", orderId), ct: ct);
            if (orderStatus is null)
            {
                await UpsertPostingAsync(connection, orderId, null, "Skipped", 0, "الطلب غير موجود", ct);
                return;
            }

            if (orderStatus is OrderStatusCanceled or OrderStatusRefunded)
            {
                // لم تُفتح فاتورة بعد والإلغاء سبق → لا نرسل شيئًا
                await UpsertPostingAsync(connection, orderId, erpHeaderId, "Skipped", 0, null, ct);
                return;
            }

            var items = await LoadItemsAsync(connection, orderId, ct);
            if (items.Count == 0)
            {
                await UpsertPostingAsync(connection, orderId, erpHeaderId, "Failed", 1, "الأورد بدون أصناف", ct);
                return;
            }

            var total = await ExecuteScalarAsync<decimal?>(connection,
                "SELECT Total FROM dbo.Ec_Orders WHERE Id = @orderId;",
                ("@orderId", orderId), ct: ct) ?? items.Sum(i => i.Price * i.Quantity);

            decimal sumOfItems = items.Sum(i => i.Price * i.Quantity);
            var orderNumber = await ExecuteScalarAsync<string?>(connection,
                "SELECT OrderNumber FROM dbo.Ec_Orders WHERE Id = @orderId;",
                ("@orderId", orderId), ct: ct) ?? orderId.ToString();

            long headerId;
            using (var transaction = connection.BeginTransaction())
            {
                var egyptNow = EgyptNow();
                var transDate = int.Parse(egyptNow.ToString("yyyyMMdd"));
                var insertDateTime = long.Parse(egyptNow.ToString("yyyyMMddHHmmss"));

                using (var header = connection.CreateCommand())
                {
                    header.Transaction = transaction;
                    header.CommandText = """
                        INSERT INTO dbo.wh_TransHeader
                            (TransType, DocTransNo, TransDate, BranchID, CustomerId, StoreId,
                             ReferenceNo, Notes, PriceType, PayType,
                             Total, Paid, Balance, Status, InsertDateTime)
                        VALUES
                            (@transType, @docTransNo, @transDate, @branchId, 0, @storeId,
                             @docTransNo, N'طلب من المتجر الإلكتروني', @priceType, @payType,
                             @total, 0, @total, 1, @insertDateTime);
                        SELECT CAST(SCOPE_IDENTITY() AS bigint);
                        """;
                    header.Parameters.AddWithValue("@transType", _transType);
                    header.Parameters.AddWithValue("@docTransNo", Truncate(orderNumber, 100));
                    header.Parameters.AddWithValue("@transDate", transDate);
                    header.Parameters.AddWithValue("@branchId", _branchId);
                    header.Parameters.AddWithValue("@storeId", _storeId);
                    header.Parameters.AddWithValue("@priceType", _priceType);
                    header.Parameters.AddWithValue("@payType", _payType);
                    header.Parameters.AddWithValue("@total", (double)total);
                    header.Parameters.AddWithValue("@insertDateTime", insertDateTime);
                    headerId = (long)(await header.ExecuteScalarAsync(ct) ?? 0);
                }

                using (var setCode = connection.CreateCommand())
                {
                    // رقم فاتورة مميز وفريد يسهل البحث عنه في شاشات الـ ERP
                    setCode.Transaction = transaction;
                    setCode.CommandText = "UPDATE dbo.wh_TransHeader SET Code = @id WHERE HeaderId = @id;";
                    setCode.Parameters.AddWithValue("@id", headerId);
                    await setCode.ExecuteNonQueryAsync(ct);
                }

                foreach (var item in items)
                {
                    using var detail = connection.CreateCommand();
                    detail.Transaction = transaction;
                    detail.CommandText = """
                        INSERT INTO dbo.wh_TransDetails
                            (HeaderId, ItemID, ItemBarCode, Package, Qty, TransPkgQty1,
                             Price, Value, DiamonQty, Status, InsertDateTime)
                        VALUES
                            (@headerId, @itemId, @itemBarCode, 1, @qty, @qty,
                             @price, @value, 0, 1, @insertDateTime);
                        """;
                    detail.Parameters.AddWithValue("@headerId", headerId);
                    detail.Parameters.AddWithValue("@itemId", item.ItemId);
                    detail.Parameters.AddWithValue("@itemBarCode", item.BarCode);
                    detail.Parameters.AddWithValue("@qty", (double)item.Quantity);
                    detail.Parameters.AddWithValue("@price", (double)item.Price);
                    detail.Parameters.AddWithValue("@value", (double)(item.Price * item.Quantity));
                    detail.Parameters.AddWithValue("@insertDateTime", insertDateTime);
                    await detail.ExecuteNonQueryAsync(ct);
                }

                transaction.Commit();
            }

            await UpsertPostingAsync(connection, orderId, headerId, "Posted", 1, null, ct);
            _logger.LogInformation("ERP invoice {HeaderId} posted for order {OrderId} (sum={Sum})", headerId, orderId, sumOfItems);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ERP posting failed for order {OrderId}; retry worker will pick it up.", orderId);
            await TryMarkFailedAsync(orderId, ex.Message, ct);
        }
    }

    public async Task ReverseOrderAsync(int orderId, CancellationToken ct = default)
    {
        if (!_enabled) return;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            var (status, erpHeaderId) = await ReadPostingAsync(connection, orderId, ct);
            if (status == "Skipped") return;

            if (erpHeaderId is long headerId)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    UPDATE dbo.wh_TransHeader SET Status = 0 WHERE HeaderId = @headerId;
                    UPDATE dbo.wh_TransDetails SET Status = 0 WHERE HeaderId = @headerId;
                    """;
                cmd.Parameters.AddWithValue("@headerId", headerId);
                await cmd.ExecuteNonQueryAsync(ct);
                await UpsertPostingAsync(connection, orderId, headerId, "Reversed", 0, null, ct);
                _logger.LogInformation("ERP invoice {HeaderId} reversed for order {OrderId}", headerId, orderId);
            }
            else
            {
                // لم تُفتح الفاتورة بعد: نمنع الـ worker من إرسالها لاحقًا
                await UpsertPostingAsync(connection, orderId, null, "Skipped", 0, null, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ERP reversal failed for order {OrderId}", orderId);
        }
    }

    public async Task RestoreOrderAsync(int orderId, CancellationToken ct = default)
    {
        if (!_enabled) return;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            var (status, erpHeaderId) = await ReadPostingAsync(connection, orderId, ct);
            if (status is not ("Reversed" or "Skipped")) return;

            if (erpHeaderId is long headerId)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    UPDATE dbo.wh_TransHeader SET Status = 1 WHERE HeaderId = @headerId;
                    UPDATE dbo.wh_TransDetails SET Status = 1 WHERE HeaderId = @headerId;
                    """;
                cmd.Parameters.AddWithValue("@headerId", headerId);
                await cmd.ExecuteNonQueryAsync(ct);
                await UpsertPostingAsync(connection, orderId, headerId, "Posted", 0, null, ct);
            }
            else
            {
                await UpsertPostingAsync(connection, orderId, null, "Pending", 0, null, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ERP restore failed for order {OrderId}", orderId);
        }
    }

    private record struct ErpItem(long ItemId, string BarCode, decimal Quantity, decimal Price);

    private async Task<List<ErpItem>> LoadItemsAsync(SqlConnection connection, int orderId, CancellationToken ct)
    {
        var result = new List<ErpItem>();
        var rows = new List<(string ProductId, decimal Quantity, decimal Price)>();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT ProductId, Quantity, UnitPrice FROM dbo.Ec_OrderItems WHERE OrderId = @orderId;";
            cmd.Parameters.AddWithValue("@orderId", orderId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add((reader.GetString(0), Convert.ToDecimal(reader.GetValue(1)), Convert.ToDecimal(reader.GetValue(2))));
        }

        foreach (var (productId, quantity, price) in rows)
        {
            var itemId = await ExecuteScalarAsync<long?>(connection, """
                SELECT TOP 1 p.ItemId FROM dbo.EC_Products p
                WHERE LTRIM(RTRIM(p.Code)) = @code AND p.ItemId IS NOT NULL;
                """, ("@code", productId.Trim()), ct: ct)
                ?? await ExecuteScalarAsync<long?>(connection,
                "SELECT TOP 1 w.id FROM dbo.wh_items w WHERE LTRIM(RTRIM(w.code)) = @code ORDER BY w.id;",
                ("@code", productId.Trim()), ct: ct);

            if (itemId is null)
                throw new InvalidOperationException($"الصنف '{productId}' غير مرتبط بصنف ERP (ItemId)");

            var barCode = await ExecuteScalarAsync<string?>(connection, """
                SELECT TOP 1 b.ItemBarCode FROM dbo.wh_ItemBarCodes b
                WHERE b.ItemId = @itemId AND b.Package = 1 ORDER BY b.ID;
                """, ("@itemId", itemId.Value), ct: ct)
                ?? await ExecuteScalarAsync<string?>(connection,
                "SELECT TOP 1 w.code FROM dbo.wh_items w WHERE w.id = @itemId;",
                ("@itemId", itemId.Value), ct: ct)
                ?? string.Empty;

            result.Add(new ErpItem(itemId.Value, barCode, quantity, price));
        }

        return result;
    }

    private async Task<(string Status, long? ErpHeaderId)> ReadPostingAsync(SqlConnection connection, int orderId, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Status, ErpHeaderId FROM dbo.Ec_ErpPostings WHERE OrderId = @orderId;";
        cmd.Parameters.AddWithValue("@orderId", orderId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return ("None", null);
        return (reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetInt64(1));
    }

    private static async Task UpsertPostingAsync(SqlConnection connection, int orderId, long? erpHeaderId,
        string status, int incrementAttempts, string? error, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.Ec_ErpPostings
            SET ErpHeaderId = ISNULL(@headerId, ErpHeaderId),
                Status = @status,
                Attempts = Attempts + @increment,
                LastError = @error,
                UpdatedAt = SYSUTCDATETIME()
            WHERE OrderId = @orderId;
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.Ec_ErpPostings (OrderId, ErpHeaderId, Status, Attempts, LastError)
                VALUES (@orderId, @headerId, @status, @increment, @error);
            """;
        cmd.Parameters.AddWithValue("@orderId", orderId);
        cmd.Parameters.AddWithValue("@headerId", (object?)erpHeaderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@increment", incrementAttempts);
        cmd.Parameters.AddWithValue("@error", (object?)error ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task TryMarkFailedAsync(int orderId, string message, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await UpsertPostingAsync(connection, orderId, null, "Failed", 1, Truncate(message, 2000), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not record ERP posting failure for order {OrderId}", orderId);
        }
    }

    internal async Task<List<int>> GetRetryableOrderIdsAsync(CancellationToken ct)
    {
        var ids = new List<int>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT OrderId FROM dbo.Ec_ErpPostings
            WHERE Status IN ('Pending', 'Failed') AND Attempts < @max
            ORDER BY Id;
            """;
        cmd.Parameters.AddWithValue("@max", _maxAttempts);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            ids.Add(reader.GetInt32(0));
        return ids;
    }

    private static async Task<T?> ExecuteScalarAsync<T>(SqlConnection connection, string sql,
        (string, object) p1, (string, object)? p2 = null, CancellationToken ct = default)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue(p1.Item1, p1.Item2);
        if (p2 is not null)
            cmd.Parameters.AddWithValue(p2.Value.Item1, p2.Value.Item2);
        var value = await cmd.ExecuteScalarAsync(ct);
        if (value is null || value is DBNull)
            return default;
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(value, target);
    }

    private static DateTime EgyptNow()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow;
        }
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
