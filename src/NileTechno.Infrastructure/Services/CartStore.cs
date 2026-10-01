using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Services;

/// <summary>
/// Cart storage on the legacy dbo.Ec_Cart table:
/// ProductID = EC_Products.Id, ProductServerId = ERP ItemId, UserId = buyer account id,
/// color/size ride in Notes as JSON.
/// </summary>
public class CartStore
{
    private readonly string _connectionString;

    public CartStore(IConfiguration configuration)
        => _connectionString = SqlConnectionString.Resolve(configuration);

    public record CartRow(int Id, int DbId, long ItemId, string Code, string Name, decimal UnitPrice,
        int Qty, string? Color, string? Size, decimal Stock, string GroupCode, string GroupName,
        string? Image, string? Description, DateTime CreatedAt);

    public record Incoming(string Code, int Qty, string? Color, string? Size);

    public async Task<List<CartRow>> GetByUserAsync(string userId, CancellationToken ct)
    {
        await using var connection = Open();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT c.Id, c.ProductID, c.ProductServerId, c.Qty, c.Price, c.Notes, c.CreatedAt,
                   p.Code, p.Name, CONVERT(decimal(18,2), p.Stock) AS stock,
                   ISNULL(g.Code, N'') AS groupcode, ISNULL(g.Name, N'') AS groupname,
                   p.ProductImg, p.Description
            FROM dbo.Ec_Cart AS c
            INNER JOIN dbo.EC_Products AS p ON p.Id = c.ProductID
            LEFT JOIN dbo.EC_Groups AS g ON g.Id = p.GroupID
            WHERE c.UserId = @userId
              AND c.Status = 1
              AND c.OrderID IS NULL
              AND p.Status = 1
              AND (p.GroupID IS NULL OR g.Status = 1)
            ORDER BY c.CreatedAt;
            """;
        cmd.Parameters.AddWithValue("@userId", userId);

        var rows = new List<CartRow>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var (color, size) = ReadNotes(reader.IsDBNull(5) ? null : reader.GetString(5));
            rows.Add(new CartRow(
                Id: Convert.ToInt32(reader.GetValue(0)),
                DbId: Convert.ToInt32(reader.GetValue(1)),
                ItemId: Convert.ToInt64(reader.GetValue(2)),
                Qty: Convert.ToInt32(reader.GetValue(3)),
                UnitPrice: Convert.ToDecimal(reader.GetValue(4)),
                CreatedAt: reader.GetDateTime(6),
                Code: reader.GetString(7),
                Name: reader.GetString(8),
                Stock: Convert.ToDecimal(reader.GetValue(9)),
                GroupCode: reader.GetString(10),
                GroupName: reader.GetString(11),
                Image: reader.IsDBNull(12) ? null : reader.GetString(12),
                Description: reader.IsDBNull(13) ? null : reader.GetString(13),
                Color: color,
                Size: size));
        }

        return rows;
    }

    public async Task<(bool Ok, string? Error)> UpsertAsync(string userId, Incoming item, CancellationToken ct)
    {
        await using var connection = Open();

        var product = await ResolveAsync(connection, item.Code, ct);
        if (product is null)
            return (false, $"منتج '{item.Code}' غير موجود أو غير متاح");

        var (dbId, itemId, price) = product.Value;
        var qty = item.Qty <= 0 ? 1 : item.Qty;
        var notes = WriteNotes(item.Color, item.Size);

        int? existingId;
        await using (var find = connection.CreateCommand())
        {
            find.CommandText = """
                SELECT TOP 1 Id FROM dbo.Ec_Cart
                WHERE UserId = @userId AND ProductID = @dbId AND OrderID IS NULL
                  AND ISNULL(Notes, N'') = ISNULL(@notes, N'')
                ORDER BY Id;
                """;
            find.Parameters.AddWithValue("@userId", userId);
            find.Parameters.AddWithValue("@dbId", dbId);
            find.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
            existingId = await find.ExecuteScalarAsync(ct) as int?;
        }

        if (existingId is int id)
        {
            await using var update = connection.CreateCommand();
            update.CommandText = """
                UPDATE dbo.Ec_Cart
                SET Qty = @qty, Price = @price, Total = @total, Status = 1, UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @id;
                """;
            update.Parameters.AddWithValue("@qty", qty);
            update.Parameters.AddWithValue("@price", price);
            update.Parameters.AddWithValue("@total", price * qty);
            update.Parameters.AddWithValue("@id", id);
            await update.ExecuteNonQueryAsync(ct);
            return (true, null);
        }

        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO dbo.Ec_Cart (UserId, ProductID, ProductServerId, Qty, Price, Total, Notes)
                VALUES (@userId, @dbId, @itemId, @qty, @price, @total, @notes);
                """;
            insert.Parameters.AddWithValue("@userId", userId);
            insert.Parameters.AddWithValue("@dbId", dbId);
            insert.Parameters.AddWithValue("@itemId", itemId);
            insert.Parameters.AddWithValue("@qty", qty);
            insert.Parameters.AddWithValue("@price", price);
            insert.Parameters.AddWithValue("@total", price * qty);
            insert.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
            await insert.ExecuteNonQueryAsync(ct);
        }

        return (true, null);
    }

    public async Task<(int Replaced, int Removed)> ReplaceAllAsync(string userId, IReadOnlyList<Incoming> items, CancellationToken ct)
    {
        var incomingKeys = new Dictionary<string, Incoming>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in items.Where(i => !string.IsNullOrWhiteSpace(i.Code)))
            incomingKeys.TryAdd(Key(i.Code, i.Color, i.Size), i with { Code = i.Code.Trim(), Qty = i.Qty <= 0 ? 1 : i.Qty });

        await using var connection = Open();

        var current = new List<(int Id, int DbId, string? Notes)>();
        await using (var list = connection.CreateCommand())
        {
            list.CommandText = "SELECT Id, ProductID, Notes FROM dbo.Ec_Cart WHERE UserId = @userId AND Status = 1 AND OrderID IS NULL;";
            list.Parameters.AddWithValue("@userId", userId);
            await using var reader = await list.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                current.Add((reader.GetInt32(0), reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        var codeByDbId = new Dictionary<int, string>();
        var distinctDbIds = current.Select(c => c.DbId).Distinct().ToList();
        if (distinctDbIds.Count > 0)
        {
            await using var codes = connection.CreateCommand();
            var placeholders = new List<string>();
            for (var i = 0; i < distinctDbIds.Count; i++)
            {
                placeholders.Add($"@d{i}");
                codes.Parameters.Add($"@d{i}", System.Data.SqlDbType.Int).Value = distinctDbIds[i];
            }
            codes.CommandText = $"SELECT Id, Code FROM dbo.EC_Products WHERE Id IN ({string.Join(",", placeholders)});";
            await using var reader = await codes.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                codeByDbId[reader.GetInt32(0)] = reader.GetString(1);
        }

        var removed = 0;
        foreach (var (rowId, dbId, notes) in current)
        {
            var (color, size) = ReadNotes(notes);
            var key = Key(codeByDbId.TryGetValue(dbId, out var code) ? code : "", color, size);
            if (incomingKeys.ContainsKey(key)) continue;

            await using var del = connection.CreateCommand();
            del.CommandText = "UPDATE dbo.Ec_Cart SET Status = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @id;";
            del.Parameters.AddWithValue("@id", rowId);
            removed += await del.ExecuteNonQueryAsync(ct);
        }

        var replaced = 0;
        foreach (var item in incomingKeys.Values)
        {
            var (ok, _) = await UpsertAsync(userId, item, ct);
            if (ok) replaced++;
        }

        return (replaced, removed);
    }

    public async Task<int> DeleteAsync(int id, CancellationToken ct)
    {
        await using var connection = Open();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE dbo.Ec_Cart SET Status = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @id AND Status = 1;";
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> ClearAsync(string userId, CancellationToken ct)
    {
        await using var connection = Open();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.Ec_Cart SET Status = 0, UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @userId AND Status = 1 AND OrderID IS NULL;
            """;
        cmd.Parameters.AddWithValue("@userId", userId);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> StampActiveRowsAsOrdered(string userId, int orderId, CancellationToken ct)
    {
        await using var connection = Open();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.Ec_Cart SET OrderID = @orderId, UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @userId AND Status = 1 AND OrderID IS NULL;
            """;
        cmd.Parameters.AddWithValue("@orderId", orderId);
        cmd.Parameters.AddWithValue("@userId", userId);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<(int DbId, long ItemId, decimal Price)?> ResolveAsync(
        SqlConnection connection, string code, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT TOP 1 p.Id, ISNULL(p.ItemId, 0), p.Price
            FROM dbo.EC_Products AS p
            LEFT JOIN dbo.EC_Groups AS g ON g.Id = p.GroupID
            WHERE LTRIM(RTRIM(p.Code)) = @code
              AND p.Status = 1
              AND (p.GroupID IS NULL OR g.Status = 1);
            """;
        cmd.Parameters.AddWithValue("@code", code.Trim());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return (Convert.ToInt32(reader.GetValue(0)), Convert.ToInt64(reader.GetValue(1)), Convert.ToDecimal(reader.GetValue(2)));
    }

    private static string Key(string code, string? color, string? size) =>
        $"{code.Trim()}|{color ?? ""}|{size ?? ""}";

    private static string? WriteNotes(string? color, string? size)
    {
        if (string.IsNullOrWhiteSpace(color) && string.IsNullOrWhiteSpace(size)) return null;
        return JsonSerializer.Serialize(new Dictionary<string, string> { ["c"] = color ?? "", ["s"] = size ?? "" });
    }

    private static (string? Color, string? Size) ReadNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return (null, null);
        try
        {
            using var doc = JsonDocument.Parse(notes);
            var c = doc.RootElement.TryGetProperty("c", out var cv) ? cv.GetString() : null;
            var s = doc.RootElement.TryGetProperty("s", out var sv) ? sv.GetString() : null;
            return (string.IsNullOrEmpty(c) ? null : c, string.IsNullOrEmpty(s) ? null : s);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private SqlConnection Open()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
