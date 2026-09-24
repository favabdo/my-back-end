using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Services;

/// <summary>
/// Executes the ERP stock procedure on an interval and mirrors its result into
/// EC_Groups / EC_Products, which are the source the storefront catalog reads from.
/// </summary>
public class ProductCatalogSyncService : BackgroundService
{
    private static readonly Regex SafeSqlName = new(@"^[\[\]a-zA-Z0-9_\.]+$", RegexOptions.Compiled);

    private readonly string _connectionString;
    private readonly string _procedureName;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;
    private readonly ILogger<ProductCatalogSyncService> _logger;

    public ProductCatalogSyncService(IConfiguration configuration, ILogger<ProductCatalogSyncService> logger)
    {
        _logger = logger;
        _connectionString = SqlConnectionString.Resolve(configuration);

        var section = configuration.GetSection("CatalogSync");
        _procedureName = section["ProcedureName"] ?? "dbo.wh_ItemStockWatcherNew";
        if (!SafeSqlName.IsMatch(_procedureName))
            throw new InvalidOperationException("CatalogSync:ProcedureName قيمة غير مسموحة.");

        var seconds = int.TryParse(section["IntervalSeconds"], out var v) && v > 0 ? v : 120;
        _interval = TimeSpan.FromSeconds(seconds);
        _enabled = !bool.TryParse(section["Enabled"], out var enabled) || enabled;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
            return;

        // Give the schema bootstrapper a moment before the first EXEC.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Catalog sync failed; EC_Products keeps its last good data.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = string.Format(SyncSqlTemplate, _procedureName);
        command.CommandTimeout = 120;

        var touched = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Catalog sync finished ({Touched} EC_Products rows touched).", touched);
    }

    private const string SyncSqlTemplate = """
        DECLARE @rows table (
            itemid bigint,
            itemcode nvarchar(200),
            itemname nvarchar(500),
            transpkgqty1 decimal(38,6),
            ReorderQty decimal(38,6),
            liveReorderQty decimal(38,6),
            ItemPrice decimal(38,6),
            storeid bigint,
            storecode nvarchar(200),
            storename nvarchar(500),
            groupid bigint,
            groupname nvarchar(500),
            mystoreid bigint,
            mystorecode nvarchar(200),
            mystorename nvarchar(500),
            mystoreqty decimal(38,6),
            storetype nvarchar(100),
            orderstore decimal(38,6),
            subgroupid bigint,
            subgroupname nvarchar(500)
        );

        INSERT INTO @rows
        EXEC {0};

        -- إجراء فاضي (عطل مؤقت في المصدر) لا يجب أن يفرّغ الكتالوج
        IF (SELECT COUNT(*) FROM @rows) = 0
            RETURN;

        MERGE dbo.EC_Groups AS t
        USING (
            SELECT LEFT(CONVERT(nvarchar(50), r.groupid), 50) AS Code,
                   MAX(LEFT(r.groupname, 200)) AS Name
            FROM @rows AS r
            WHERE r.groupid IS NOT NULL
              AND ISNULL(r.groupname, N'') <> N''
            GROUP BY LEFT(CONVERT(nvarchar(50), r.groupid), 50)
        ) AS s
        ON t.Code = s.Code
        WHEN MATCHED THEN UPDATE SET
            Name = s.Name,
            Status = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT (ServerId, Code, Name, Status)
            VALUES (0, s.Code, s.Name, 1);

        MERGE dbo.EC_Products AS t
        USING (
            SELECT LEFT(LTRIM(RTRIM(r.itemcode)), 50) AS Code,
                   MAX(r.itemid) AS ItemId,
                   MAX(LEFT(LTRIM(RTRIM(r.itemname)), 200)) AS Name,
                   SUM(r.transpkgqty1) AS StockQty,
                   MAX(r.ItemPrice) AS Price,
                   MAX(g.Id) AS GroupRowId
            FROM @rows AS r
            LEFT JOIN dbo.EC_Groups AS g ON g.Code = LEFT(CONVERT(nvarchar(50), r.groupid), 50)
            WHERE LTRIM(RTRIM(ISNULL(r.itemcode, N''))) <> N''
            GROUP BY LEFT(LTRIM(RTRIM(r.itemcode)), 50)
        ) AS s
        ON t.Code = s.Code
        WHEN MATCHED THEN UPDATE SET
            ItemId = s.ItemId,
            Name = s.Name,
            Stock = CAST(ROUND(s.StockQty, 0) AS int),
            Price = CAST(ROUND(ISNULL(s.Price, 0), 2) AS decimal(18,2)),
            GroupID = s.GroupRowId,
            Status = 1,
            LastUpdate = SYSUTCDATETIME(),
            UpdatedAt = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT
            (ServerId, Code, ItemId, Name, GroupID, Stock, Price, AvgRate, Status, LastUpdate)
            VALUES (0, s.Code, s.ItemId, s.Name, s.GroupRowId,
                    CAST(ROUND(s.StockQty, 0) AS int),
                    CAST(ROUND(ISNULL(s.Price, 0), 2) AS decimal(18,2)),
                    0, 1, SYSUTCDATETIME());

        -- الأصناف التي اختفت من البروسيدير (موقوفة/محذوفة في ERP) تُخرج من الكتالوج
        UPDATE p
        SET p.Status = 0,
            p.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.EC_Products AS p
        WHERE p.Status = 1
          AND p.Code NOT IN (
              SELECT LEFT(LTRIM(RTRIM(r.itemcode)), 50)
              FROM @rows AS r
              WHERE LTRIM(RTRIM(ISNULL(r.itemcode, N''))) <> N''
          );

        SELECT @@ROWCOUNT + @@ROWCOUNT;
        """;
}
