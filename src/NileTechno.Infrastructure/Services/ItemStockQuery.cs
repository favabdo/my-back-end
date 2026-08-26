using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Products.DTOs;
using NileTechno.Application.Features.Stock.DTOs;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Services;

public class ItemStockQuery : IItemStockQuery
{
    private static readonly Regex SafeSqlName = new(@"^[\[\]a-zA-Z0-9_\.]+$", RegexOptions.Compiled);

    private readonly string _connectionString;
    private readonly string _objectName;
    private readonly string _sourceKind;
    private readonly string _joinSql;
    private readonly string _reorderQtySql;

    public ItemStockQuery(IConfiguration configuration)
    {
        _connectionString = SqlConnectionString.Resolve(configuration);

        var section = configuration.GetSection("StockCatalog");
        _objectName = section["ObjectName"] ?? "dbo.wh_ItemStockWatcherNew";
        _sourceKind = section["SourceKind"] ?? "StoredProcedure";
        _joinSql = section["JoinSql"] ?? string.Empty;
        _reorderQtySql = section["ReorderQtySql"] ?? "MAX(a.ReorderQty)";

        if (!SafeSqlName.IsMatch(_objectName))
            throw new InvalidOperationException("StockCatalog:ObjectName قيمة غير مسموحة.");
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<CustomerProductCardDto>> GetCustomerCatalogAsync(
        string? groupId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (IsStoredProcedure)
        {
            var rows = await LoadProcedureRowsAsync(cancellationToken);
            var items = AggregateCustomer(rows, groupId, search, itemCode: null).ToList();
            await AttachCustomerPricesAsync(items, cancellationToken);
            return items;
        }

        const string sql = """
            SELECT
                MAX(a.itemcode) AS itemcode,
                MAX(a.itemname) AS itemname,
                MAX(a.groupid) AS groupid,
                MAX(a.groupname) AS groupname,
                SUM(a.transpkgqty1) AS stock
            FROM {0} AS a
            {1}
            WHERE (@groupId IS NULL OR CONVERT(nvarchar(100), a.groupid) = @groupId)
              AND (
                    @search IS NULL
                    OR a.itemname LIKE @search
                    OR CONVERT(nvarchar(100), a.itemcode) LIKE @search
                  )
            GROUP BY a.itemcode
            ORDER BY MAX(a.itemname)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, string.Format(sql, _objectName, _joinSql));
        AddFilterParameters(command, groupId, storeCode: null, search, itemCode: null);

        var sqlItems = (await ReadCustomerListAsync(command, cancellationToken)).ToList();
        await AttachCustomerPricesAsync(sqlItems, cancellationToken);
        return sqlItems;
    }

    public async Task<PaginatedList<CustomerProductCardDto>> GetCustomerCatalogPageAsync(
        string? groupId,
        string? search,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 50;
        var page = pageNumber < 1 ? 1 : pageNumber;

        if (IsStoredProcedure)
        {
            var rows = await LoadProcedureRowsAsync(cancellationToken);
            var all = AggregateCustomer(rows, groupId, search, itemCode: null);
            var count = all.Count;
            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            await AttachCustomerPricesAsync(items, cancellationToken);
            return new PaginatedList<CustomerProductCardDto>(items, count, page, pageSize);
        }

        var fromSql = $"""
            FROM {_objectName} AS a
            {_joinSql}
            WHERE (@groupId IS NULL OR CONVERT(nvarchar(100), a.groupid) = @groupId)
              AND (
                    @search IS NULL
                    OR a.itemname LIKE @search
                    OR CONVERT(nvarchar(100), a.itemcode) LIKE @search
                  )
            """;

        var countSql = $"""
            SELECT COUNT(*) FROM (
                SELECT a.itemcode
                {fromSql}
                GROUP BY a.itemcode
            ) AS catalog_groups
            """;

        var dataSql = $"""
            SELECT
                MAX(a.itemcode) AS itemcode,
                MAX(a.itemname) AS itemname,
                MAX(a.groupid) AS groupid,
                MAX(a.groupname) AS groupname,
                SUM(a.transpkgqty1) AS stock
            {fromSql}
            GROUP BY a.itemcode
            ORDER BY MAX(a.itemname)
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var countCommand = CreateCommand(connection, countSql);
        AddFilterParameters(countCommand, groupId, storeCode: null, search, itemCode: null);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var dataCommand = CreateCommand(connection, dataSql);
        AddFilterParameters(dataCommand, groupId, storeCode: null, search, itemCode: null);
        dataCommand.Parameters.Add(new SqlParameter("@skip", SqlDbType.Int) { Value = (page - 1) * pageSize });
        dataCommand.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = pageSize });

        var pageItems = (await ReadCustomerListAsync(dataCommand, cancellationToken)).ToList();
        await AttachCustomerPricesAsync(pageItems, cancellationToken);
        return new PaginatedList<CustomerProductCardDto>(pageItems, totalCount, page, pageSize);
    }

    public async Task<CustomerProductCardDto?> GetCustomerProductByCodeAsync(
        string itemCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemCode))
            return null;

        if (IsStoredProcedure)
        {
            var rows = await LoadProcedureRowsAsync(cancellationToken);
            var item = AggregateCustomer(rows, groupId: null, search: null, itemCode).FirstOrDefault();
            if (item is not null)
                await AttachCustomerPricesAsync(new[] { item }, cancellationToken);
            return item;
        }

        const string sql = """
            SELECT
                MAX(a.itemcode) AS itemcode,
                MAX(a.itemname) AS itemname,
                MAX(a.groupid) AS groupid,
                MAX(a.groupname) AS groupname,
                SUM(a.transpkgqty1) AS stock
            FROM {0} AS a
            {1}
            WHERE CONVERT(nvarchar(100), a.itemcode) = @itemCode
            GROUP BY a.itemcode
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, string.Format(sql, _objectName, _joinSql));
        AddFilterParameters(command, groupId: null, storeCode: null, search: null, itemCode);

        var list = (await ReadCustomerListAsync(command, cancellationToken)).ToList();
        await AttachCustomerPricesAsync(list, cancellationToken);
        return list.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ProductGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        if (IsStoredProcedure)
        {
            var rows = await LoadProcedureRowsAsync(cancellationToken);
            return rows
                .Where(r => !string.IsNullOrWhiteSpace(r.GroupId) || !string.IsNullOrWhiteSpace(r.GroupName))
                .GroupBy(r => r.GroupId)
                .Select(g => new ProductGroupDto
                {
                    GroupId = g.Key,
                    GroupName = g.Select(x => x.GroupName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty,
                    ItemCount = g.Select(x => x.ItemCode).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .OrderBy(g => g.GroupName)
                .ToList();
        }

        const string sql = """
            SELECT
                MAX(a.groupid) AS groupid,
                MAX(a.groupname) AS groupname,
                COUNT(DISTINCT a.itemcode) AS itemcount
            FROM {0} AS a
            {1}
            WHERE a.groupid IS NOT NULL
            GROUP BY a.groupid
            ORDER BY MAX(a.groupname)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, string.Format(sql, _objectName, _joinSql));

        var groups = new List<ProductGroupDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            groups.Add(new ProductGroupDto
            {
                GroupId = ReadString(reader, "groupid"),
                GroupName = ReadString(reader, "groupname"),
                ItemCount = (int)ReadDecimal(reader, "itemcount")
            });
        }

        return groups;
    }

    public async Task<PaginatedList<AdminStockItemDto>> GetAdminStockAsync(
        int pageNumber,
        int pageSize,
        string? groupId,
        string? storeCode,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (IsStoredProcedure)
        {
            var rows = await LoadProcedureRowsAsync(cancellationToken);
            var aggregated = AggregateAdmin(rows, groupId, storeCode, search);
            var count = aggregated.Count;
            var page = aggregated
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PaginatedList<AdminStockItemDto>(page, count, pageNumber, pageSize);
        }

        var fromSql = $"""
            FROM {_objectName} AS a
            {_joinSql}
            WHERE (@groupId IS NULL OR CONVERT(nvarchar(100), a.groupid) = @groupId)
              AND (@storeCode IS NULL OR CONVERT(nvarchar(100), a.storecode) = @storeCode)
              AND (
                    @search IS NULL
                    OR a.itemname LIKE @search
                    OR CONVERT(nvarchar(100), a.itemcode) LIKE @search
                  )
            """;

        var countSql = $"""
            SELECT COUNT(*) FROM (
                SELECT a.itemcode, a.storecode
                {fromSql}
                GROUP BY a.itemcode, a.storecode
            ) AS stock_groups
            """;

        var dataSql = $"""
            SELECT
                MAX(a.itemcode) AS itemcode,
                MAX(a.itemname) AS itemname,
                SUM(a.transpkgqty1) AS transpkgqty1,
                {_reorderQtySql} AS ReorderQty,
                MAX(a.storecode) AS storecode,
                MAX(a.storename) AS storename,
                MAX(a.groupid) AS groupid,
                MAX(a.groupname) AS groupname
            {fromSql}
            GROUP BY a.itemcode, a.storecode
            ORDER BY MAX(a.itemname), MAX(a.storename)
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var countCommand = CreateCommand(connection, countSql);
        AddFilterParameters(countCommand, groupId, storeCode, search, itemCode: null);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var dataCommand = CreateCommand(connection, dataSql);
        AddFilterParameters(dataCommand, groupId, storeCode, search, itemCode: null);
        dataCommand.Parameters.Add(new SqlParameter("@skip", SqlDbType.Int) { Value = (pageNumber - 1) * pageSize });
        dataCommand.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = pageSize });

        var items = new List<AdminStockItemDto>();
        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(MapAdmin(reader));

        return new PaginatedList<AdminStockItemDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetQuantitiesByItemCodeAsync(
        CancellationToken cancellationToken = default)
    {
        var admin = await GetAdminStockAsync(1, 10_000, null, null, null, cancellationToken);
        return admin.Items
            .GroupBy(x => x.ItemCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TransPkgQty1), StringComparer.OrdinalIgnoreCase);
    }

    private bool IsStoredProcedure =>
        string.Equals(_sourceKind, "StoredProcedure", StringComparison.OrdinalIgnoreCase);

    private static SqlCommand CreateCommand(SqlConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 60;
        return command;
    }

    private static void AddFilterParameters(
        SqlCommand command,
        string? groupId,
        string? storeCode,
        string? search,
        string? itemCode)
    {
        command.Parameters.Add(new SqlParameter("@groupId", SqlDbType.NVarChar, 100)
        {
            Value = string.IsNullOrWhiteSpace(groupId) ? DBNull.Value : groupId.Trim()
        });
        command.Parameters.Add(new SqlParameter("@storeCode", SqlDbType.NVarChar, 100)
        {
            Value = string.IsNullOrWhiteSpace(storeCode) ? DBNull.Value : storeCode.Trim()
        });
        command.Parameters.Add(new SqlParameter("@search", SqlDbType.NVarChar, 200)
        {
            Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%"
        });
        command.Parameters.Add(new SqlParameter("@itemCode", SqlDbType.NVarChar, 100)
        {
            Value = string.IsNullOrWhiteSpace(itemCode) ? DBNull.Value : itemCode.Trim()
        });
    }

    private async Task<List<RawStockRow>> LoadProcedureRowsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = _objectName;
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = 60;

        var rows = new List<RawStockRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new RawStockRow(
                ReadString(reader, "itemcode"),
                ReadString(reader, "itemname"),
                ReadDecimal(reader, "transpkgqty1"),
                ReadDecimal(reader, "ReorderQty"),
                ReadString(reader, "storecode"),
                ReadString(reader, "storename"),
                ReadString(reader, "groupid"),
                ReadString(reader, "groupname")));
        }

        return rows;
    }

    private async Task AttachCustomerPricesAsync(
        IList<CustomerProductCardDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var prices = await GetPricesByItemIdAsync(items.Select(x => x.ItemCode), cancellationToken);
        foreach (var item in items)
        {
            if (prices.TryGetValue(item.ItemCode, out var price))
                item.Price = price;
        }
    }

    private async Task<Dictionary<string, decimal>> GetPricesByItemIdAsync(
        IEnumerable<string> itemCodes,
        CancellationToken cancellationToken)
    {
        var codes = itemCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (codes.Count == 0)
            return prices;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const int batchSize = 200;
        for (var offset = 0; offset < codes.Count; offset += batchSize)
        {
            var batch = codes.Skip(offset).Take(batchSize).ToList();
            var paramNames = batch.Select((_, index) => $"@itemId{index}").ToArray();
            var sql = $"""
                SELECT
                    LTRIM(RTRIM(CONVERT(nvarchar(100), ID))) AS id,
                    Pkg1Price5 AS price
                FROM dbo.wh_Items
                WHERE LTRIM(RTRIM(CONVERT(nvarchar(100), ID))) IN ({string.Join(", ", paramNames)})
                """;

            await using var command = CreateCommand(connection, sql);
            for (var i = 0; i < batch.Count; i++)
            {
                command.Parameters.Add(new SqlParameter(paramNames[i], SqlDbType.NVarChar, 100)
                {
                    Value = batch[i]
                });
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = ReadString(reader, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                prices[id] = ReadDecimal(reader, "price");
            }
        }

        return prices;
    }

    private static IReadOnlyList<CustomerProductCardDto> AggregateCustomer(
        IEnumerable<RawStockRow> rows,
        string? groupId,
        string? search,
        string? itemCode)
    {
        var query = rows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(itemCode))
            query = query.Where(r => string.Equals(r.ItemCode, itemCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(groupId))
            query = query.Where(r => string.Equals(r.GroupId, groupId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.ItemCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .GroupBy(r => r.ItemCode)
            .Select(g => new CustomerProductCardDto
            {
                ItemCode = g.Key,
                ItemName = g.Select(x => x.ItemName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty,
                GroupId = g.Select(x => x.GroupId).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty,
                GroupName = g.Select(x => x.GroupName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty,
                Stock = g.Sum(x => x.TransPkgQty1)
            })
            .OrderBy(p => p.ItemName)
            .ToList();
    }

    private static List<AdminStockItemDto> AggregateAdmin(
        IEnumerable<RawStockRow> rows,
        string? groupId,
        string? storeCode,
        string? search)
    {
        var query = rows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(groupId))
            query = query.Where(r => string.Equals(r.GroupId, groupId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(storeCode))
            query = query.Where(r => string.Equals(r.StoreCode, storeCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.ItemCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .GroupBy(r => new { r.ItemCode, r.StoreCode })
            .Select(g => new AdminStockItemDto
            {
                ItemCode = g.Key.ItemCode,
                ItemName = g.Max(x => x.ItemName) ?? string.Empty,
                TransPkgQty1 = g.Sum(x => x.TransPkgQty1),
                ReorderQty = g.Max(x => x.ReorderQty),
                StoreCode = g.Key.StoreCode,
                StoreName = g.Max(x => x.StoreName) ?? string.Empty,
                GroupId = g.Max(x => x.GroupId) ?? string.Empty,
                GroupName = g.Max(x => x.GroupName) ?? string.Empty
            })
            .OrderBy(x => x.ItemName)
            .ThenBy(x => x.StoreName)
            .ToList();
    }

    private static async Task<IReadOnlyList<CustomerProductCardDto>> ReadCustomerListAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        var items = new List<CustomerProductCardDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CustomerProductCardDto
            {
                ItemCode = ReadString(reader, "itemcode"),
                ItemName = ReadString(reader, "itemname"),
                GroupId = ReadString(reader, "groupid"),
                GroupName = ReadString(reader, "groupname"),
                Stock = ReadDecimal(reader, "stock")
            });
        }

        return items;
    }

    private static AdminStockItemDto MapAdmin(SqlDataReader reader) => new()
    {
        ItemCode = ReadString(reader, "itemcode"),
        ItemName = ReadString(reader, "itemname"),
        TransPkgQty1 = ReadDecimal(reader, "transpkgqty1"),
        ReorderQty = ReadDecimal(reader, "ReorderQty"),
        StoreCode = ReadString(reader, "storecode"),
        StoreName = ReadString(reader, "storename"),
        GroupId = ReadString(reader, "groupid"),
        GroupName = ReadString(reader, "groupname")
    };

    private static string ReadString(SqlDataReader reader, string column)
    {
        var ordinal = FindOrdinal(reader, column);
        if (ordinal is null || reader.IsDBNull(ordinal.Value))
            return string.Empty;

        return Convert.ToString(reader.GetValue(ordinal.Value))?.Trim() ?? string.Empty;
    }

    private static decimal ReadDecimal(SqlDataReader reader, string column)
    {
        var ordinal = FindOrdinal(reader, column);
        if (ordinal is null || reader.IsDBNull(ordinal.Value))
            return 0;

        return Convert.ToDecimal(reader.GetValue(ordinal.Value));
    }

    private static int? FindOrdinal(SqlDataReader reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return null;
    }

    private sealed record RawStockRow(
        string ItemCode,
        string ItemName,
        decimal TransPkgQty1,
        decimal ReorderQty,
        string StoreCode,
        string StoreName,
        string GroupId,
        string GroupName);
}
