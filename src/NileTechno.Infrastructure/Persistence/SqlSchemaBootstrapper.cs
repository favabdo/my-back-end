using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Persistence;

public class SqlSchemaBootstrapper : ISqlSchemaBootstrapper
{
    private readonly string _connectionString;
    private readonly ILogger<SqlSchemaBootstrapper> _logger;

    public SqlSchemaBootstrapper(ILogger<SqlSchemaBootstrapper> logger)
    {
        _connectionString = SqlConnectionString.Resolve();
        _logger = logger;
    }

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureLoginAccountsByAAsync(connection, cancellationToken);
    }

    private async Task EnsureLoginAccountsByAAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string table = "LoginAccounts_byA";

        if (!await TableExistsAsync(connection, table, cancellationToken))
        {
            _logger.LogInformation("Creating table dbo.{Table}", table);
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.LoginAccounts_byA (
                    Id uniqueidentifier NOT NULL CONSTRAINT PK_LoginAccounts_byA PRIMARY KEY,
                    Email nvarchar(256) NOT NULL,
                    NormalizedEmail nvarchar(256) NOT NULL,
                    PasswordHash nvarchar(max) NOT NULL,
                    AuthProvider nvarchar(32) NOT NULL,
                    GoogleSubject nvarchar(128) NULL,
                    GoogleSignInToken nvarchar(max) NULL,
                    FullName nvarchar(200) NOT NULL,
                    EmailConfirmed bit NOT NULL CONSTRAINT DF_LoginAccounts_byA_EmailConfirmed DEFAULT (0),
                    IsBlocked bit NOT NULL CONSTRAINT DF_LoginAccounts_byA_IsBlocked DEFAULT (0),
                    BlockedAt datetime2 NULL,
                    LoyaltyPoints int NOT NULL CONSTRAINT DF_LoginAccounts_byA_LoyaltyPoints DEFAULT (100),
                    Phone nvarchar(32) NULL,
                    RefreshToken nvarchar(max) NULL,
                    RefreshTokenExpiresAtUtc datetime2 NULL,
                    LastLoginAt datetime2 NULL,
                    CreatedAt datetime2 NOT NULL,
                    UpdatedAt datetime2 NULL
                );
                CREATE UNIQUE INDEX IX_LoginAccounts_byA_NormalizedEmail
                    ON dbo.LoginAccounts_byA (NormalizedEmail);
                CREATE UNIQUE INDEX IX_LoginAccounts_byA_GoogleSubject
                    ON dbo.LoginAccounts_byA (GoogleSubject)
                    WHERE GoogleSubject IS NOT NULL;
                """, cancellationToken);
            return;
        }

        _logger.LogInformation("Table dbo.{Table} already exists; checking missing columns only", table);

        await EnsureColumnAsync(connection, table, "Email", "nvarchar(256) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "NormalizedEmail", "nvarchar(256) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "PasswordHash", "nvarchar(max) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "AuthProvider", "nvarchar(32) NOT NULL CONSTRAINT DF_LoginAccounts_byA_AuthProvider DEFAULT ('Password')", cancellationToken);
        await EnsureColumnAsync(connection, table, "GoogleSubject", "nvarchar(128) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "GoogleSignInToken", "nvarchar(max) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "FullName", "nvarchar(200) NOT NULL CONSTRAINT DF_LoginAccounts_byA_FullName DEFAULT ('')", cancellationToken);
        await EnsureColumnAsync(connection, table, "EmailConfirmed", "bit NOT NULL CONSTRAINT DF_LoginAccounts_byA_EmailConfirmed DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync(connection, table, "IsBlocked", "bit NOT NULL CONSTRAINT DF_LoginAccounts_byA_IsBlocked DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync(connection, table, "BlockedAt", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "LoyaltyPoints", "int NOT NULL CONSTRAINT DF_LoginAccounts_byA_LoyaltyPoints DEFAULT (100)", cancellationToken);
        await EnsureColumnAsync(connection, table, "Phone", "nvarchar(32) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "RefreshToken", "nvarchar(max) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "RefreshTokenExpiresAtUtc", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "LastLoginAt", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "CreatedAt", "datetime2 NOT NULL CONSTRAINT DF_LoginAccounts_byA_CreatedAt DEFAULT (SYSUTCDATETIME())", cancellationToken);
        await EnsureColumnAsync(connection, table, "UpdatedAt", "datetime2 NULL", cancellationToken);

        await EnsureIndexAsync(connection, table, "IX_LoginAccounts_byA_NormalizedEmail", """
            CREATE UNIQUE INDEX IX_LoginAccounts_byA_NormalizedEmail
                ON dbo.LoginAccounts_byA (NormalizedEmail);
            """, cancellationToken);

        await EnsureIndexAsync(connection, table, "IX_LoginAccounts_byA_GoogleSubject", """
            CREATE UNIQUE INDEX IX_LoginAccounts_byA_GoogleSubject
                ON dbo.LoginAccounts_byA (GoogleSubject)
                WHERE GoogleSubject IS NOT NULL;
            """, cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string table, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT 1
            FROM sys.tables
            WHERE name = @table AND schema_id = SCHEMA_ID(N'dbo');
            """;
        cmd.Parameters.AddWithValue("@table", table);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private async Task EnsureColumnAsync(
        SqlConnection connection,
        string table,
        string column,
        string sqlType,
        CancellationToken cancellationToken)
    {
        await using var check = connection.CreateCommand();
        check.CommandText = """
            SELECT 1
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            WHERE t.name = @table
              AND SCHEMA_NAME(t.schema_id) = N'dbo'
              AND c.name = @column;
            """;
        check.Parameters.AddWithValue("@table", table);
        check.Parameters.AddWithValue("@column", column);
        var exists = await check.ExecuteScalarAsync(cancellationToken);
        if (exists is not null)
            return;

        _logger.LogInformation("Adding column {Column} to dbo.{Table}", column, table);
        await ExecuteAsync(connection, $"ALTER TABLE dbo.{table} ADD {column} {sqlType};", cancellationToken);
    }

    private static async Task EnsureIndexAsync(
        SqlConnection connection,
        string table,
        string indexName,
        string createSql,
        CancellationToken cancellationToken)
    {
        await using var check = connection.CreateCommand();
        check.CommandText = """
            SELECT 1
            FROM sys.indexes
            WHERE name = @index AND object_id = OBJECT_ID(@table);
            """;
        check.Parameters.AddWithValue("@index", indexName);
        check.Parameters.AddWithValue("@table", $"dbo.{table}");
        var result = await check.ExecuteScalarAsync(cancellationToken);
        if (result is not null)
            return;

        await ExecuteAsync(connection, createSql, cancellationToken);
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
