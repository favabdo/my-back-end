using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NileTechno.Infrastructure.Configuration;

/// <summary>
/// Same shape as Nile Chat server/src/database/connection.js (mssql / tedious):
/// discrete DB_SERVER, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD plus
/// encrypt: false and trustServerCertificate: true.
/// </summary>
public static class SqlConnectionString
{
    public static string Resolve(IConfiguration? configuration = null)
    {
        var fromNileChat = FromNileChatEnv();
        if (!string.IsNullOrWhiteSpace(fromNileChat))
            return Normalize(fromNileChat);

        var fromConfig = configuration?.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return Normalize(fromConfig);

        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable(EnvFile.Connection);

        if (string.IsNullOrWhiteSpace(fromEnv))
        {
            throw new InvalidOperationException(
                "SQL is not configured. Set Nile Chat vars (DB_SERVER, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD) or DB_CONNECTION.");
        }

        return Normalize(fromEnv);
    }

    public static string? FromNileChatEnv()
    {
        var server = Get("DB_SERVER");
        var user = Get("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var database = Get("DB_NAME");

        if (string.IsNullOrWhiteSpace(server)
            || string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        var port = 1433;
        var portRaw = Get("DB_PORT");
        if (!string.IsNullOrWhiteSpace(portRaw) && int.TryParse(portRaw, out var parsedPort) && parsedPort > 0)
            port = parsedPort;

        var dataSource = server.Contains(',') || server.Contains('\\')
            ? server
            : $"{server},{port}";

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = database,
            UserID = user,
            Password = password
        };

        return builder.ConnectionString;
    }

    public static string Normalize(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        // Match mssql options.encrypt / options.trustServerCertificate.
        var encryptOverride = Environment.GetEnvironmentVariable("SQL_ENCRYPT");
        builder.Encrypt = bool.TryParse(encryptOverride, out var encrypt) && encrypt;
        builder.TrustServerCertificate = true;

        if (builder.ConnectTimeout < 30)
            builder.ConnectTimeout = 30;

        // SqlClient defaults ConnectRetryCount=1 (TDS resiliency). Nile Chat / tedious
        // does not; older SQL Server then fails with TCP error 35.
        builder.ConnectRetryCount = 0;

        return builder.ConnectionString;
    }

    private static string? Get(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
