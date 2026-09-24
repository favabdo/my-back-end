using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Products.DTOs;
using NileTechno.Application.Features.Stock.DTOs;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Services;

/// <summary>
/// IItemStockQuery implementation backed by the EC_Products / EC_Groups tables
/// (kept fresh by ProductCatalogSyncService). Stock management for admins still
/// reads the live ERP procedure through ItemStockQuery.
/// </summary>
public class EcProductCatalogQuery : IItemStockQuery
{
    private const string CatalogSelect = """
        SELECT
            p.Code AS itemcode,
            CONVERT(nvarchar(100), p.ItemId) AS itemid,
            p.Name AS itemname,
            ISNULL(g.Code, '') AS groupid,
            ISNULL(g.Name, '') AS groupname,
            CONVERT(decimal(18,2), p.Stock) AS stock,
            p.Price AS price,
            p.ProductImg AS image,
            p.Description AS description
        FROM dbo.EC_Products AS p
        LEFT JOIN dbo.EC_Groups AS g ON g.Id = p.GroupID
        WHERE p.Status = 1
          AND (p.GroupID IS NULL OR g.Status = 1)
          AND (@groupId IS NULL OR g.Code = @groupId)
          AND (@search IS NULL OR p.Name LIKE @search OR p.Code LIKE @search)
        """;

    private readonly string _connectionString;
    private readonly ItemStockQuery _liveStock;

    public EcProductCatalogQuery(IConfiguration configuration, ItemStockQuery liveStock)
    {
        _connectionString = SqlConnectionString.Resolve(configuration);
        _liveStock = liveStock;
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
        const string sql = CatalogSelect + " ORDER BY p.Name";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;
        AddFilterParameters(command, groupId, search);

        return await ReadCatalogAsync(command, cancellationToken);
    }

    public async Task<PaginatedList<CustomerProductCardDto>> GetCustomerCatalogPageAsync(
        string? groupId,
        string? search,
        int pageNumber,
        int pageSize,
        string? deviceType,
        CancellationToken cancellationToken = default)
    {
        int actualPageSize = pageSize;
        if (pageSize == 0)
        {
            if (deviceType?.ToLower() == "mobile")
                actualPageSize = 20;
            else if (deviceType?.ToLower() == "tablet")
                actualPageSize = 30;
            else
                actualPageSize = 50;
        }
        else
        {
            actualPageSize = Math.Min(Math.Max(pageSize, 1), 100);
        }

        var page = pageNumber < 1 ? 1 : pageNumber;

        var countSql = $"""
            SELECT COUNT(*)
            FROM dbo.EC_Products AS p
            LEFT JOIN dbo.EC_Groups AS g ON g.Id = p.GroupID
            WHERE p.Status = 1
              AND (p.GroupID IS NULL OR g.Status = 1)
              AND (@groupId IS NULL OR g.Code = @groupId)
              AND (@search IS NULL OR p.Name LIKE @search OR p.Code LIKE @search)
            """;

        var dataSql = CatalogSelect + " ORDER BY p.Name OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = countSql;
        countCommand.CommandTimeout = 60;
        AddFilterParameters(countCommand, groupId, search);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var dataCommand = connection.CreateCommand();
        dataCommand.CommandText = dataSql;
        dataCommand.CommandTimeout = 60;
        AddFilterParameters(dataCommand, groupId, search);
        dataCommand.Parameters.Add(new SqlParameter("@skip", SqlDbType.Int) { Value = (page - 1) * actualPageSize });
        dataCommand.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = actualPageSize });

        var items = await ReadCatalogAsync(dataCommand, cancellationToken);
        return new PaginatedList<CustomerProductCardDto>(items, totalCount, page, actualPageSize);
    }

    public async Task<CustomerProductCardDto?> GetCustomerProductByCodeAsync(
        string itemCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemCode))
            return null;

        const string sql = """
            SELECT
                p.Code AS itemcode,
                CONVERT(nvarchar(100), p.ItemId) AS itemid,
                p.Name AS itemname,
                ISNULL(g.Code, '') AS groupid,
                ISNULL(g.Name, '') AS groupname,
                CONVERT(decimal(18,2), p.Stock) AS stock,
                p.Price AS price,
                p.ProductImg AS image,
                p.Description AS description
            FROM dbo.EC_Products AS p
            LEFT JOIN dbo.EC_Groups AS g ON g.Id = p.GroupID
            WHERE p.Status = 1 AND (p.GroupID IS NULL OR g.Status = 1) AND p.Code = @itemCode
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;
        command.Parameters.Add(new SqlParameter("@itemCode", SqlDbType.NVarChar, 100) { Value = itemCode.Trim() });

        var items = await ReadCatalogAsync(command, cancellationToken);
        return items.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ProductGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                g.Code AS groupid,
                g.Name AS groupname,
                COUNT(DISTINCT p.Code) AS itemcount
            FROM dbo.EC_Products AS p
            INNER JOIN dbo.EC_Groups AS g ON g.Id = p.GroupID
            WHERE p.Status = 1 AND g.Status = 1
            GROUP BY g.Code, g.Name
            ORDER BY g.Name
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;

        var groups = new List<ProductGroupDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            groups.Add(new ProductGroupDto
            {
                GroupId = ItemStockQuery.ReadString(reader, "groupid"),
                GroupName = ItemStockQuery.ReadString(reader, "groupname"),
                ItemCount = (int)ItemStockQuery.ReadDecimal(reader, "itemcount")
            });
        }

        return groups;
    }

    public Task<PaginatedList<AdminStockItemDto>> GetAdminStockAsync(
        int pageNumber,
        int pageSize,
        string? groupId,
        string? storeCode,
        string? search,
        CancellationToken cancellationToken = default)
        => _liveStock.GetAdminStockAsync(pageNumber, pageSize, groupId, storeCode, search, cancellationToken);

    public async Task<IReadOnlyDictionary<string, decimal>> GetQuantitiesByItemCodeAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.Code AS itemcode, CONVERT(decimal(18,2), p.Stock) AS stock
            FROM dbo.EC_Products AS p
            WHERE p.Status = 1
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;

        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var code = ItemStockQuery.ReadString(reader, "itemcode");
            if (!string.IsNullOrWhiteSpace(code))
                map[code] = ItemStockQuery.ReadDecimal(reader, "stock");
        }

        return map;
    }


    public async Task<IReadOnlyDictionary<string, CustomerProductCardDto>> GetProductsByCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken = default)
    {
        var wanted = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var map = new Dictionary<string, CustomerProductCardDto>(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
            return map;

        var paramNames = new List<string>();
        var sql = CatalogSelect + " AND p.Code IN (";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        for (var i = 0; i < wanted.Count; i++)
        {
            paramNames.Add($"@code{i}");
            command.Parameters.Add(new SqlParameter($"@code{i}", SqlDbType.NVarChar, 50) { Value = wanted[i] });
        }

        command.CommandText = sql + string.Join(", ", paramNames) + ")";
        command.CommandTimeout = 60;
        AddFilterParameters(command, groupId: null, search: null);

        foreach (var item in await ReadCatalogAsync(command, cancellationToken))
            map[item.ItemCode] = item;
        return map;
    }

    private static void AddFilterParameters(SqlCommand command, string? groupId, string? search)
    {
        command.Parameters.Add(new SqlParameter("@groupId", SqlDbType.NVarChar, 100)
        {
            Value = string.IsNullOrWhiteSpace(groupId) ? DBNull.Value : groupId.Trim()
        });
        command.Parameters.Add(new SqlParameter("@search", SqlDbType.NVarChar, 200)
        {
            Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%"
        });
    }

    private static async Task<List<CustomerProductCardDto>> ReadCatalogAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        var items = new List<CustomerProductCardDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CustomerProductCardDto
            {
                ItemCode = ItemStockQuery.ReadString(reader, "itemcode"),
                ItemId = ItemStockQuery.ReadString(reader, "itemid"),
                ItemName = ItemStockQuery.ReadString(reader, "itemname"),
                GroupId = ItemStockQuery.ReadString(reader, "groupid"),
                GroupName = ItemStockQuery.ReadString(reader, "groupname"),
                Stock = ItemStockQuery.ReadDecimal(reader, "stock"),
                Price = ItemStockQuery.ReadDecimal(reader, "price"),
                Image = ItemStockQuery.ReadString(reader, "image"),
                Description = ItemStockQuery.ReadString(reader, "description")
            });
        }

        return items;
    }
}
