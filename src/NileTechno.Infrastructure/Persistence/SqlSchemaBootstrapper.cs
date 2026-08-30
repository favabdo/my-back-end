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

        await EnsureIdentityAsync(connection, cancellationToken);
        await EnsureLoginAccountsByAAsync(connection, cancellationToken);
    }

    private async Task EnsureIdentityAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "AspNetUsers", cancellationToken))
        {
            _logger.LogInformation("Creating table dbo.AspNetUsers");
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.AspNetUsers (
                    Id uniqueidentifier NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
                    UserName nvarchar(256) NULL,
                    NormalizedUserName nvarchar(256) NULL,
                    Email nvarchar(256) NULL,
                    NormalizedEmail nvarchar(256) NULL,
                    EmailConfirmed bit NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0),
                    PasswordHash nvarchar(max) NULL,
                    SecurityStamp nvarchar(max) NULL,
                    ConcurrencyStamp nvarchar(max) NULL,
                    PhoneNumber nvarchar(max) NULL,
                    PhoneNumberConfirmed bit NOT NULL CONSTRAINT DF_AspNetUsers_PhoneNumberConfirmed DEFAULT (0),
                    TwoFactorEnabled bit NOT NULL CONSTRAINT DF_AspNetUsers_TwoFactorEnabled DEFAULT (0),
                    LockoutEnd datetimeoffset NULL,
                    LockoutEnabled bit NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (1),
                    AccessFailedCount int NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailedCount DEFAULT (0),
                    FullName nvarchar(200) NOT NULL CONSTRAINT DF_AspNetUsers_FullName DEFAULT (''),
                    Role int NOT NULL CONSTRAINT DF_AspNetUsers_Role DEFAULT (0),
                    IsBlocked bit NOT NULL CONSTRAINT DF_AspNetUsers_IsBlocked DEFAULT (0),
                    BlockedAt datetime2 NULL,
                    LoyaltyPoints int NOT NULL CONSTRAINT DF_AspNetUsers_LoyaltyPoints DEFAULT (100),
                    LastLoginAt datetime2 NULL,
                    RefreshToken nvarchar(max) NULL,
                    RefreshTokenExpiresAtUtc datetime2 NULL
                );
                CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
                CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
                """, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Table dbo.AspNetUsers already exists; checking missing columns only");
            await EnsureColumnAsync(connection, "AspNetUsers", "UserName", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "NormalizedUserName", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "Email", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "NormalizedEmail", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "EmailConfirmed", "bit NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "PasswordHash", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "SecurityStamp", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "ConcurrencyStamp", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "PhoneNumber", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "PhoneNumberConfirmed", "bit NOT NULL CONSTRAINT DF_AspNetUsers_PhoneNumberConfirmed DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "TwoFactorEnabled", "bit NOT NULL CONSTRAINT DF_AspNetUsers_TwoFactorEnabled DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "LockoutEnd", "datetimeoffset NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "LockoutEnabled", "bit NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (1)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "AccessFailedCount", "int NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailedCount DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "FullName", "nvarchar(200) NOT NULL CONSTRAINT DF_AspNetUsers_FullName DEFAULT ('')", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "Role", "int NOT NULL CONSTRAINT DF_AspNetUsers_Role DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "IsBlocked", "bit NOT NULL CONSTRAINT DF_AspNetUsers_IsBlocked DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "BlockedAt", "datetime2 NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "LoyaltyPoints", "int NOT NULL CONSTRAINT DF_AspNetUsers_LoyaltyPoints DEFAULT (100)", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "LastLoginAt", "datetime2 NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "RefreshToken", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetUsers", "RefreshTokenExpiresAtUtc", "datetime2 NULL", cancellationToken);

            await ExecuteAsync(connection, """
                UPDATE dbo.AspNetUsers
                SET NormalizedEmail = UPPER(Email)
                WHERE NormalizedEmail IS NULL AND Email IS NOT NULL;
                UPDATE dbo.AspNetUsers
                SET NormalizedUserName = UPPER(UserName)
                WHERE NormalizedUserName IS NULL AND UserName IS NOT NULL;
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "AspNetRoles", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.AspNetRoles (
                    Id uniqueidentifier NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
                    Name nvarchar(256) NULL,
                    NormalizedName nvarchar(256) NULL,
                    ConcurrencyStamp nvarchar(max) NULL
                );
                CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
                """, cancellationToken);
        }
        else
        {
            await EnsureColumnAsync(connection, "AspNetRoles", "Name", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetRoles", "NormalizedName", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "AspNetRoles", "ConcurrencyStamp", "nvarchar(max) NULL", cancellationToken);
        }

        if (!await TableExistsAsync(connection, "AspNetUserRoles", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.AspNetUserRoles (
                    UserId uniqueidentifier NOT NULL,
                    RoleId uniqueidentifier NOT NULL,
                    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId)
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "AspNetUserClaims", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.AspNetUserClaims (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
                    UserId uniqueidentifier NOT NULL,
                    ClaimType nvarchar(max) NULL,
                    ClaimValue nvarchar(max) NULL
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "AspNetUserLogins", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.AspNetUserLogins (
                    LoginProvider nvarchar(450) NOT NULL,
                    ProviderKey nvarchar(450) NOT NULL,
                    ProviderDisplayName nvarchar(max) NULL,
                    UserId uniqueidentifier NOT NULL,
                    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey)
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "AspNetUserTokens", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.AspNetUserTokens (
                    UserId uniqueidentifier NOT NULL,
                    LoginProvider nvarchar(450) NOT NULL,
                    Name nvarchar(450) NOT NULL,
                    Value nvarchar(max) NULL,
                    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name)
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "AspNetRoleClaims", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.AspNetRoleClaims (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
                    RoleId uniqueidentifier NOT NULL,
                    ClaimType nvarchar(max) NULL,
                    ClaimValue nvarchar(max) NULL
                );
                """, cancellationToken);
        }
    }

    private async Task EnsureLoginAccountsByAAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string table = "Eco_LoginAccounts_byA";
        await RenameTableIfNeededAsync(connection, "LoginAccounts_byA", table, cancellationToken);

        if (await TableExistsAsync(connection, table, cancellationToken)
            && !await HasIntIdentityIdAsync(connection, table, cancellationToken)
            && await TableRowCountAsync(connection, table, cancellationToken) == 0)
        {
            _logger.LogInformation("Recreating empty dbo.{Table} with IDENTITY(1,1) Id", table);
            await ExecuteAsync(connection, "DROP TABLE dbo.Eco_LoginAccounts_byA;", cancellationToken);
        }

        if (!await TableExistsAsync(connection, table, cancellationToken))
        {
            _logger.LogInformation("Creating table dbo.{Table}", table);
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Eco_LoginAccounts_byA (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Eco_LoginAccounts_byA PRIMARY KEY,
                    Email nvarchar(256) NOT NULL,
                    NormalizedEmail nvarchar(256) NOT NULL,
                    PasswordHash nvarchar(max) NOT NULL,
                    AuthProvider nvarchar(32) NOT NULL,
                    GoogleSubject nvarchar(128) NULL,
                    GoogleSignInToken nvarchar(max) NULL,
                    FullName nvarchar(200) NOT NULL,
                    EmailConfirmed bit NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_EmailConfirmed DEFAULT (0),
                    IsBlocked bit NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_IsBlocked DEFAULT (0),
                    BlockedAt datetime2 NULL,
                    LoyaltyPoints int NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_LoyaltyPoints DEFAULT (100),
                    Phone nvarchar(32) NULL,
                    RefreshToken nvarchar(max) NULL,
                    RefreshTokenExpiresAtUtc datetime2 NULL,
                    LastLoginAt datetime2 NULL,
                    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_CreatedAt DEFAULT (SYSUTCDATETIME()),
                    UpdatedAt datetime2 NULL
                );
                CREATE UNIQUE INDEX IX_Eco_LoginAccounts_byA_NormalizedEmail
                    ON dbo.Eco_LoginAccounts_byA (NormalizedEmail);
                CREATE UNIQUE INDEX IX_Eco_LoginAccounts_byA_GoogleSubject
                    ON dbo.Eco_LoginAccounts_byA (GoogleSubject)
                    WHERE GoogleSubject IS NOT NULL;
                """, cancellationToken);
            return;
        }

        _logger.LogInformation("Table dbo.{Table} already exists; checking missing columns only", table);

        await EnsureColumnAsync(connection, table, "Email", "nvarchar(256) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "NormalizedEmail", "nvarchar(256) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "PasswordHash", "nvarchar(max) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "AuthProvider", "nvarchar(32) NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_AuthProvider DEFAULT ('Password')", cancellationToken);
        await EnsureColumnAsync(connection, table, "GoogleSubject", "nvarchar(128) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "GoogleSignInToken", "nvarchar(max) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "FullName", "nvarchar(200) NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_FullName DEFAULT ('')", cancellationToken);
        await EnsureColumnAsync(connection, table, "EmailConfirmed", "bit NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_EmailConfirmed DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync(connection, table, "IsBlocked", "bit NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_IsBlocked DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync(connection, table, "BlockedAt", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "LoyaltyPoints", "int NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_LoyaltyPoints DEFAULT (100)", cancellationToken);
        await EnsureColumnAsync(connection, table, "Phone", "nvarchar(32) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "RefreshToken", "nvarchar(max) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "RefreshTokenExpiresAtUtc", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "LastLoginAt", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "CreatedAt", "datetime2 NOT NULL CONSTRAINT DF_Eco_LoginAccounts_byA_CreatedAt DEFAULT (SYSUTCDATETIME())", cancellationToken);
        await EnsureColumnAsync(connection, table, "UpdatedAt", "datetime2 NULL", cancellationToken);

        await EnsureIndexAsync(connection, table, "IX_Eco_LoginAccounts_byA_NormalizedEmail", """
            CREATE UNIQUE INDEX IX_Eco_LoginAccounts_byA_NormalizedEmail
                ON dbo.Eco_LoginAccounts_byA (NormalizedEmail);
            """, cancellationToken);

        await EnsureIndexAsync(connection, table, "IX_Eco_LoginAccounts_byA_GoogleSubject", """
            CREATE UNIQUE INDEX IX_Eco_LoginAccounts_byA_GoogleSubject
                ON dbo.Eco_LoginAccounts_byA (GoogleSubject)
                WHERE GoogleSubject IS NOT NULL;
            """, cancellationToken);
    }

    private async Task RenameTableIfNeededAsync(
        SqlConnection connection,
        string oldName,
        string newName,
        CancellationToken cancellationToken)
    {
        if (await TableExistsAsync(connection, newName, cancellationToken))
            return;
        if (!await TableExistsAsync(connection, oldName, cancellationToken))
            return;

        _logger.LogInformation("Renaming dbo.{Old} to dbo.{New}", oldName, newName);
        await ExecuteAsync(connection, $"EXEC sp_rename N'dbo.{oldName}', N'{newName}';", cancellationToken);
    }

    private static async Task<bool> HasIntIdentityIdAsync(
        SqlConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT 1
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            WHERE t.name = @table
              AND SCHEMA_NAME(t.schema_id) = N'dbo'
              AND c.name = N'Id'
              AND ty.name = N'int'
              AND c.is_identity = 1;
            """;
        cmd.Parameters.AddWithValue("@table", table);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task<int> TableRowCountAsync(
        SqlConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT_BIG(*) FROM dbo.{table};";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
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
