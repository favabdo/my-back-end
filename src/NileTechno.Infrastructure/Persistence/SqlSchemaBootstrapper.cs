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
        await EnsureLoginAccountsAsync(connection, cancellationToken);
        await EnsureBusinessTablesAsync(connection, cancellationToken);
    }

    private async Task EnsureIdentityAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // ترقية الجداول القديمة (AspNet*) لاصطلاح تسمية Ec_... مع الحفاظ على البيانات
        await RenameTableIfNeededAsync(connection, "AspNetUsers", "Ec_AspNetUsers", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AspNetRoles", "Ec_AspNetRoles", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AspNetUserRoles", "Ec_AspNetUserRoles", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AspNetUserClaims", "Ec_AspNetUserClaims", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AspNetUserLogins", "Ec_AspNetUserLogins", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AspNetUserTokens", "Ec_AspNetUserTokens", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AspNetRoleClaims", "Ec_AspNetRoleClaims", cancellationToken);

        if (!await TableExistsAsync(connection, "Ec_AspNetUsers", cancellationToken))
        {
            _logger.LogInformation("Creating table dbo.Ec_AspNetUsers");
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_AspNetUsers (
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
                CREATE UNIQUE INDEX UserNameIndex ON dbo.Ec_AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
                CREATE INDEX EmailIndex ON dbo.Ec_AspNetUsers (NormalizedEmail);
                """, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Table dbo.Ec_AspNetUsers already exists; checking missing columns only");
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "UserName", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "NormalizedUserName", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "Email", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "NormalizedEmail", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "EmailConfirmed", "bit NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "PasswordHash", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "SecurityStamp", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "ConcurrencyStamp", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "PhoneNumber", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "PhoneNumberConfirmed", "bit NOT NULL CONSTRAINT DF_AspNetUsers_PhoneNumberConfirmed DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "TwoFactorEnabled", "bit NOT NULL CONSTRAINT DF_AspNetUsers_TwoFactorEnabled DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "LockoutEnd", "datetimeoffset NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "LockoutEnabled", "bit NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (1)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "AccessFailedCount", "int NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailedCount DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "FullName", "nvarchar(200) NOT NULL CONSTRAINT DF_AspNetUsers_FullName DEFAULT ('')", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "Role", "int NOT NULL CONSTRAINT DF_AspNetUsers_Role DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "IsBlocked", "bit NOT NULL CONSTRAINT DF_AspNetUsers_IsBlocked DEFAULT (0)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "BlockedAt", "datetime2 NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "LoyaltyPoints", "int NOT NULL CONSTRAINT DF_AspNetUsers_LoyaltyPoints DEFAULT (100)", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "LastLoginAt", "datetime2 NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "RefreshToken", "nvarchar(max) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetUsers", "RefreshTokenExpiresAtUtc", "datetime2 NULL", cancellationToken);

            await ExecuteAsync(connection, """
                UPDATE dbo.AspNetUsers
                SET NormalizedEmail = UPPER(Email)
                WHERE NormalizedEmail IS NULL AND Email IS NOT NULL;
                UPDATE dbo.AspNetUsers
                SET NormalizedUserName = UPPER(UserName)
                WHERE NormalizedUserName IS NULL AND UserName IS NOT NULL;
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "Ec_AspNetRoles", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_AspNetRoles (
                    Id uniqueidentifier NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
                    Name nvarchar(256) NULL,
                    NormalizedName nvarchar(256) NULL,
                    ConcurrencyStamp nvarchar(max) NULL
                );
                CREATE UNIQUE INDEX RoleNameIndex ON dbo.Ec_AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
                """, cancellationToken);
        }
        else
        {
            await EnsureColumnAsync(connection, "Ec_AspNetRoles", "Name", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetRoles", "NormalizedName", "nvarchar(256) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "Ec_AspNetRoles", "ConcurrencyStamp", "nvarchar(max) NULL", cancellationToken);
        }

        if (!await TableExistsAsync(connection, "Ec_AspNetUserRoles", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_AspNetUserRoles (
                    UserId uniqueidentifier NOT NULL,
                    RoleId uniqueidentifier NOT NULL,
                    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId)
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "Ec_AspNetUserClaims", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_AspNetUserClaims (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
                    UserId uniqueidentifier NOT NULL,
                    ClaimType nvarchar(max) NULL,
                    ClaimValue nvarchar(max) NULL
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "Ec_AspNetUserLogins", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_AspNetUserLogins (
                    LoginProvider nvarchar(450) NOT NULL,
                    ProviderKey nvarchar(450) NOT NULL,
                    ProviderDisplayName nvarchar(max) NULL,
                    UserId uniqueidentifier NOT NULL,
                    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey)
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "Ec_AspNetUserTokens", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_AspNetUserTokens (
                    UserId uniqueidentifier NOT NULL,
                    LoginProvider nvarchar(450) NOT NULL,
                    Name nvarchar(450) NOT NULL,
                    Value nvarchar(max) NULL,
                    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name)
                );
                """, cancellationToken);
        }

        if (!await TableExistsAsync(connection, "Ec_AspNetRoleClaims", cancellationToken))
        {
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_AspNetRoleClaims (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
                    RoleId uniqueidentifier NOT NULL,
                    ClaimType nvarchar(max) NULL,
                    ClaimValue nvarchar(max) NULL
                );
                """, cancellationToken);
        }
    }

    private async Task EnsureLoginAccountsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string table = "Ec_LoginAccounts";
        await RenameTableIfNeededAsync(connection, "LoginAccounts", table, cancellationToken);

        if (await TableExistsAsync(connection, table, cancellationToken)
            && !await HasIntIdentityIdAsync(connection, table, cancellationToken)
            && await TableRowCountAsync(connection, table, cancellationToken) == 0)
        {
            _logger.LogInformation("Recreating empty dbo.{Table} with IDENTITY(1,1) Id", table);
            await ExecuteAsync(connection, "DROP TABLE dbo.Ec_LoginAccounts;", cancellationToken);
        }

        if (!await TableExistsAsync(connection, table, cancellationToken))
        {
            _logger.LogInformation("Creating table dbo.{Table}", table);
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Ec_LoginAccounts (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ec_LoginAccounts PRIMARY KEY,
                    Email nvarchar(256) NOT NULL,
                    NormalizedEmail nvarchar(256) NOT NULL,
                    PasswordHash nvarchar(max) NOT NULL,
                    AuthProvider nvarchar(32) NOT NULL,
                    GoogleSubject nvarchar(128) NULL,
                    GoogleSignInToken nvarchar(max) NULL,
                    FullName nvarchar(200) NOT NULL,
                    EmailConfirmed bit NOT NULL CONSTRAINT DF_Ec_LoginAccounts_EmailConfirmed DEFAULT (0),
                    IsBlocked bit NOT NULL CONSTRAINT DF_Ec_LoginAccounts_IsBlocked DEFAULT (0),
                    BlockedAt datetime2 NULL,
                    LoyaltyPoints int NOT NULL CONSTRAINT DF_Ec_LoginAccounts_LoyaltyPoints DEFAULT (100),
                    Phone nvarchar(32) NULL,
                    RefreshToken nvarchar(max) NULL,
                    RefreshTokenExpiresAtUtc datetime2 NULL,
                    LastLoginAt datetime2 NULL,
                    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Ec_LoginAccounts_CreatedAt DEFAULT (SYSUTCDATETIME()),
                    UpdatedAt datetime2 NULL
                );
                CREATE UNIQUE INDEX IX_Ec_LoginAccounts_NormalizedEmail
                    ON dbo.Ec_LoginAccounts (NormalizedEmail);
                CREATE UNIQUE INDEX IX_Ec_LoginAccounts_GoogleSubject
                    ON dbo.Ec_LoginAccounts (GoogleSubject)
                    WHERE GoogleSubject IS NOT NULL;
                """, cancellationToken);
            return;
        }

        _logger.LogInformation("Table dbo.{Table} already exists; checking missing columns only", table);

        await EnsureColumnAsync(connection, table, "Email", "nvarchar(256) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "NormalizedEmail", "nvarchar(256) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "PasswordHash", "nvarchar(max) NOT NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "AuthProvider", "nvarchar(32) NOT NULL CONSTRAINT DF_Ec_LoginAccounts_AuthProvider DEFAULT ('Password')", cancellationToken);
        await EnsureColumnAsync(connection, table, "GoogleSubject", "nvarchar(128) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "GoogleSignInToken", "nvarchar(max) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "FullName", "nvarchar(200) NOT NULL CONSTRAINT DF_Ec_LoginAccounts_FullName DEFAULT ('')", cancellationToken);
        await EnsureColumnAsync(connection, table, "EmailConfirmed", "bit NOT NULL CONSTRAINT DF_Ec_LoginAccounts_EmailConfirmed DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync(connection, table, "IsBlocked", "bit NOT NULL CONSTRAINT DF_Ec_LoginAccounts_IsBlocked DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync(connection, table, "BlockedAt", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "LoyaltyPoints", "int NOT NULL CONSTRAINT DF_Ec_LoginAccounts_LoyaltyPoints DEFAULT (100)", cancellationToken);
        await EnsureColumnAsync(connection, table, "Phone", "nvarchar(32) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "RefreshToken", "nvarchar(max) NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "RefreshTokenExpiresAtUtc", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "LastLoginAt", "datetime2 NULL", cancellationToken);
        await EnsureColumnAsync(connection, table, "CreatedAt", "datetime2 NOT NULL CONSTRAINT DF_Ec_LoginAccounts_CreatedAt DEFAULT (SYSUTCDATETIME())", cancellationToken);
        await EnsureColumnAsync(connection, table, "UpdatedAt", "datetime2 NULL", cancellationToken);

        await EnsureIndexAsync(connection, table, "IX_Ec_LoginAccounts_NormalizedEmail", """
            CREATE UNIQUE INDEX IX_Ec_LoginAccounts_NormalizedEmail
                ON dbo.Ec_LoginAccounts (NormalizedEmail);
            """, cancellationToken);

        await EnsureIndexAsync(connection, table, "IX_Ec_LoginAccounts_GoogleSubject", """
            CREATE UNIQUE INDEX IX_Ec_LoginAccounts_GoogleSubject
                ON dbo.Ec_LoginAccounts (GoogleSubject)
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

    private async Task EnsureBusinessTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // ترقية الجداول القديمة (أسماء بدون Ec_...) لاصطلاح التسمية الجديد مع الحفاظ على البيانات
        await RenameTableIfNeededAsync(connection, "Orders", "Ec_Orders", cancellationToken);
        await RenameTableIfNeededAsync(connection, "OrderItems", "Ec_OrderItems", cancellationToken);
        await RenameTableIfNeededAsync(connection, "OrderHistoryEntries", "Ec_OrderHistoryEntries", cancellationToken);
        await RenameTableIfNeededAsync(connection, "Categories", "Ec_Categories", cancellationToken);
        await RenameTableIfNeededAsync(connection, "Products", "Ec_Products", cancellationToken);
        await RenameTableIfNeededAsync(connection, "Coupons", "Ec_Coupons", cancellationToken);
        await RenameTableIfNeededAsync(connection, "ShippingZones", "Ec_ShippingZones", cancellationToken);
        await RenameTableIfNeededAsync(connection, "Reviews", "Ec_Reviews", cancellationToken);
        await RenameTableIfNeededAsync(connection, "CartItems", "Ec_CartItems", cancellationToken);
        await RenameTableIfNeededAsync(connection, "WishlistItems", "Ec_WishlistItems", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AbandonedCarts", "Ec_AbandonedCarts", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AbandonedCartItems", "Ec_AbandonedCartItems", cancellationToken);
        await RenameTableIfNeededAsync(connection, "ActivityLogs", "Ec_ActivityLogs", cancellationToken);
        await RenameTableIfNeededAsync(connection, "StoreSettingsList", "Ec_StoreSettingsList", cancellationToken);
        await RenameTableIfNeededAsync(connection, "UserAddresses", "Ec_UserAddresses", cancellationToken);
        await RenameTableIfNeededAsync(connection, "StockOverrides", "Ec_StockOverrides", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AnalyticsSearches", "Ec_AnalyticsSearches", cancellationToken);
        await RenameTableIfNeededAsync(connection, "AnalyticsProductViews", "Ec_AnalyticsProductViews", cancellationToken);

        // ====== Orders subsystem ======
        await CreateTableIfMissingAsync(connection, "Ec_Orders", cancellationToken, """
            CREATE TABLE dbo.Ec_Orders (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Orders PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                OrderNumber nvarchar(50) NOT NULL,
                UserId uniqueidentifier NULL,
                Status int NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (0),
                CancelReason nvarchar(500) NULL,
                InternalNote nvarchar(1000) NULL,
                CustomerName nvarchar(200) NOT NULL,
                CustomerPhone nvarchar(50) NOT NULL,
                CustomerEmail nvarchar(256) NULL,
                Governorate nvarchar(100) NULL,
                AddressDetails nvarchar(1000) NULL,
                Latitude float NULL,
                Longitude float NULL,
                PaymentMethod nvarchar(20) NOT NULL CONSTRAINT DF_Orders_PaymentMethod DEFAULT ('cod'),
                CouponCode nvarchar(50) NULL,
                DiscountAmount decimal(18,2) NOT NULL CONSTRAINT DF_Orders_DiscountAmount DEFAULT (0),
                ShippingCost decimal(18,2) NOT NULL CONSTRAINT DF_Orders_ShippingCost DEFAULT (0),
                Subtotal decimal(18,2) NOT NULL CONSTRAINT DF_Orders_Subtotal DEFAULT (0),
                Total decimal(18,2) NOT NULL CONSTRAINT DF_Orders_Total DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE UNIQUE INDEX IX_Orders_OrderNumber ON dbo.Ec_Orders (OrderNumber);
            CREATE INDEX IX_Orders_CreatedAt ON dbo.Ec_Orders (CreatedAt DESC);
            CREATE INDEX IX_Orders_Status ON dbo.Ec_Orders (Status);
            CREATE INDEX IX_Orders_UserId ON dbo.Ec_Orders (UserId);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_OrderItems", cancellationToken, """
            CREATE TABLE dbo.Ec_OrderItems (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                OrderId uniqueidentifier NOT NULL,
                ProductId uniqueidentifier NOT NULL,
                ProductName nvarchar(200) NOT NULL,
                ProductImage nvarchar(500) NULL,
                UnitPrice decimal(18,2) NOT NULL,
                Quantity int NOT NULL,
                SelectedColor nvarchar(50) NULL,
                SelectedSize nvarchar(50) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_OrderItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL,
                CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Ec_Orders(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_OrderItems_OrderId ON dbo.Ec_OrderItems (OrderId);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_OrderHistoryEntries", cancellationToken, """
            CREATE TABLE dbo.Ec_OrderHistoryEntries (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_OrderHistoryEntries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                OrderId uniqueidentifier NOT NULL,
                Action nvarchar(500) NOT NULL,
                Actor nvarchar(200) NOT NULL,
                Type nvarchar(100) NOT NULL,
                NewStatus nvarchar(50) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_OrderHistoryEntries_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL,
                CONSTRAINT FK_OrderHistoryEntries_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Ec_Orders(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_OrderHistoryEntries_OrderId ON dbo.Ec_OrderHistoryEntries (OrderId);
            """);

        // ====== Catalog ======
        await CreateTableIfMissingAsync(connection, "Ec_Categories", cancellationToken, """
            CREATE TABLE dbo.Ec_Categories (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Categories PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                Name nvarchar(200) NOT NULL,
                NameEn nvarchar(200) NULL,
                IconUrl nvarchar(500) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            """);

        await CreateTableIfMissingAsync(connection, "Ec_Products", cancellationToken, """
            CREATE TABLE dbo.Ec_Products (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Products PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                Name nvarchar(200) NOT NULL,
                NameEn nvarchar(200) NULL,
                Description nvarchar(max) NULL,
                DescriptionEn nvarchar(max) NULL,
                Price decimal(18,2) NOT NULL CONSTRAINT DF_Products_Price DEFAULT (0),
                DiscountPrice decimal(18,2) NULL,
                Stock int NOT NULL CONSTRAINT DF_Products_Stock DEFAULT (0),
                Featured bit NOT NULL CONSTRAINT DF_Products_Featured DEFAULT (0),
                IsNew bit NOT NULL CONSTRAINT DF_Products_IsNew DEFAULT (0),
                ImageUrl nvarchar(500) NULL,
                GalleryUrls nvarchar(max) NULL,
                Rating float NOT NULL CONSTRAINT DF_Products_Rating DEFAULT (0),
                ReviewsCount int NOT NULL CONSTRAINT DF_Products_ReviewsCount DEFAULT (0),
                Colors nvarchar(max) NULL,
                Sizes nvarchar(max) NULL,
                CategoryId uniqueidentifier NOT NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL,
                CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Ec_Categories(Id)
            );
            CREATE INDEX IX_Products_CategoryId ON dbo.Ec_Products (CategoryId);
            """);

        // ====== Marketing & fulfilment ======
        await CreateTableIfMissingAsync(connection, "Ec_Coupons", cancellationToken, """
            CREATE TABLE dbo.Ec_Coupons (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Coupons PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                Code nvarchar(50) NOT NULL,
                DiscountPercent decimal(5,2) NOT NULL CONSTRAINT DF_Coupons_DiscountPercent DEFAULT (0),
                IsActive bit NOT NULL CONSTRAINT DF_Coupons_IsActive DEFAULT (1),
                ExpiresAt datetime2 NULL,
                MaxUses int NULL,
                UsedCount int NOT NULL CONSTRAINT DF_Coupons_UsedCount DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Coupons_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE UNIQUE INDEX IX_Coupons_Code ON dbo.Ec_Coupons (Code);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_ShippingZones", cancellationToken, """
            CREATE TABLE dbo.Ec_ShippingZones (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ShippingZones PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                Code nvarchar(50) NOT NULL,
                Name nvarchar(200) NOT NULL,
                Price decimal(18,2) NOT NULL CONSTRAINT DF_ShippingZones_Price DEFAULT (0),
                Active bit NOT NULL CONSTRAINT DF_ShippingZones_Active DEFAULT (1),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_ShippingZones_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE UNIQUE INDEX IX_ShippingZones_Code ON dbo.Ec_ShippingZones (Code);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_Reviews", cancellationToken, """
            CREATE TABLE dbo.Ec_Reviews (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                ExternalId nvarchar(200) NOT NULL,
                ProductId nvarchar(100) NOT NULL,
                OrderId nvarchar(100) NULL,
                Rating int NOT NULL CONSTRAINT DF_Reviews_Rating DEFAULT (0),
                Comment nvarchar(max) NULL,
                CustomerName nvarchar(200) NOT NULL CONSTRAINT DF_Reviews_CustomerName DEFAULT (N'عميل مميز'),
                Approved bit NOT NULL CONSTRAINT DF_Reviews_Approved DEFAULT (0),
                Date nvarchar(50) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE INDEX IX_Reviews_ExternalId ON dbo.Ec_Reviews (ExternalId);
            CREATE INDEX IX_Reviews_ProductId ON dbo.Ec_Reviews (ProductId);
            """);

        // ====== Customer baskets ======
        await CreateTableIfMissingAsync(connection, "Ec_CartItems", cancellationToken, """
            CREATE TABLE dbo.Ec_CartItems (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_CartItems PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                UserId uniqueidentifier NOT NULL,
                ProductId uniqueidentifier NOT NULL,
                Quantity int NOT NULL CONSTRAINT DF_CartItems_Quantity DEFAULT (1),
                SelectedColor nvarchar(50) NULL,
                SelectedSize nvarchar(50) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_CartItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE INDEX IX_CartItems_UserId_ProductId ON dbo.Ec_CartItems (UserId, ProductId);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_WishlistItems", cancellationToken, """
            CREATE TABLE dbo.Ec_WishlistItems (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_WishlistItems PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                UserId uniqueidentifier NOT NULL,
                ProductId uniqueidentifier NOT NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_WishlistItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE UNIQUE INDEX IX_WishlistItems_UserId_ProductId ON dbo.Ec_WishlistItems (UserId, ProductId);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_AbandonedCarts", cancellationToken, """
            CREATE TABLE dbo.Ec_AbandonedCarts (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_AbandonedCarts PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                UserId uniqueidentifier NULL,
                CustomerName nvarchar(200) NOT NULL CONSTRAINT DF_AbandonedCarts_CustomerName DEFAULT (N'زائر المتجر'),
                CustomerPhone nvarchar(50) NULL,
                CustomerEmail nvarchar(256) NULL,
                Governorate nvarchar(100) NOT NULL CONSTRAINT DF_AbandonedCarts_Governorate DEFAULT (N'غير محدد'),
                Total decimal(18,2) NOT NULL CONSTRAINT DF_AbandonedCarts_Total DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_AbandonedCarts_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            """);

        await CreateTableIfMissingAsync(connection, "Ec_AbandonedCartItems", cancellationToken, """
            CREATE TABLE dbo.Ec_AbandonedCartItems (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_AbandonedCartItems PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                AbandonedCartId uniqueidentifier NOT NULL,
                ProductName nvarchar(200) NOT NULL,
                Price decimal(18,2) NOT NULL CONSTRAINT DF_AbandonedCartItems_Price DEFAULT (0),
                Quantity int NOT NULL,
                Image nvarchar(500) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_AbandonedCartItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL,
                CONSTRAINT FK_AbandonedCartItems_AbandonedCarts FOREIGN KEY (AbandonedCartId) REFERENCES dbo.Ec_AbandonedCarts(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_AbandonedCartItems_AbandonedCartId ON dbo.Ec_AbandonedCartItems (AbandonedCartId);
            """);

        // ====== Admin / settings / analytics ======
        await CreateTableIfMissingAsync(connection, "Ec_ActivityLogs", cancellationToken, """
            CREATE TABLE dbo.Ec_ActivityLogs (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ActivityLogs PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                ActorName nvarchar(200) NOT NULL,
                Action nvarchar(500) NOT NULL,
                EntityType nvarchar(100) NULL,
                EntityId nvarchar(100) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_ActivityLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            """);

        await CreateTableIfMissingAsync(connection, "Ec_StoreSettingsList", cancellationToken, """
            CREATE TABLE dbo.Ec_StoreSettingsList (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_StoreSettingsList PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                StoreName nvarchar(200) NOT NULL,
                StoreTitle nvarchar(200) NOT NULL,
                PromoTagline nvarchar(500) NULL,
                LogoUrl nvarchar(500) NULL,
                PrimaryColor nvarchar(50) NULL,
                FreeShippingMin decimal(18,2) NOT NULL CONSTRAINT DF_StoreSettingsList_FreeShippingMin DEFAULT (0),
                AnnouncementText nvarchar(max) NULL,
                AnnouncementEnabled bit NOT NULL CONSTRAINT DF_StoreSettingsList_AnnouncementEnabled DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_StoreSettingsList_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            """);

        await CreateTableIfMissingAsync(connection, "Ec_UserAddresses", cancellationToken, """
            CREATE TABLE dbo.Ec_UserAddresses (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_UserAddresses PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                UserId uniqueidentifier NOT NULL,
                Label nvarchar(100) NOT NULL,
                Governorate nvarchar(100) NOT NULL,
                Details nvarchar(500) NULL,
                Latitude float NULL,
                Longitude float NULL,
                IsDefault bit NOT NULL CONSTRAINT DF_UserAddresses_IsDefault DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_UserAddresses_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE INDEX IX_UserAddresses_UserId ON dbo.Ec_UserAddresses (UserId);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_StockOverrides", cancellationToken, """
            CREATE TABLE dbo.Ec_StockOverrides (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_StockOverrides PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                ItemCode nvarchar(100) NOT NULL,
                Quantity decimal(18,2) NOT NULL CONSTRAINT DF_StockOverrides_Quantity DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_StockOverrides_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE UNIQUE INDEX IX_StockOverrides_ItemCode ON dbo.Ec_StockOverrides (ItemCode);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_AnalyticsSearches", cancellationToken, """
            CREATE TABLE dbo.Ec_AnalyticsSearches (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_AnalyticsSearches PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                Term nvarchar(200) NOT NULL,
                Count int NOT NULL CONSTRAINT DF_AnalyticsSearches_Count DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_AnalyticsSearches_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE UNIQUE INDEX IX_AnalyticsSearches_Term ON dbo.Ec_AnalyticsSearches (Term);
            """);

        await CreateTableIfMissingAsync(connection, "Ec_AnalyticsProductViews", cancellationToken, """
            CREATE TABLE dbo.Ec_AnalyticsProductViews (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_AnalyticsProductViews PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                ProductId nvarchar(100) NOT NULL,
                Count int NOT NULL CONSTRAINT DF_AnalyticsProductViews_Count DEFAULT (0),
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_AnalyticsProductViews_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt datetime2 NULL
            );
            CREATE UNIQUE INDEX IX_AnalyticsProductViews_ProductId ON dbo.Ec_AnalyticsProductViews (ProductId);
            """);
    }

    private async Task CreateTableIfMissingAsync(SqlConnection connection, string table, CancellationToken cancellationToken, string createSql)
    {
        if (await TableExistsAsync(connection, table, cancellationToken))
        {
            _logger.LogInformation("Table dbo.{Table} already exists; skipping.", table);
            return;
        }

        _logger.LogInformation("Creating table dbo.{Table}", table);
        await ExecuteAsync(connection, createSql, cancellationToken);
    }
}
