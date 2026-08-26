using Microsoft.Data.SqlClient;

namespace NileTechno.Infrastructure.Configuration;

public static class SqlConnectionString
{
    public static string Normalize(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        // Microsoft.Data.SqlClient 4+ defaults Encrypt=True. Linux containers then fail
        // pre-login TLS against many hosted/on-prem SQL Server instances (EOF / error 31).
        var encryptOverride = Environment.GetEnvironmentVariable("SQL_ENCRYPT");
        builder.Encrypt = bool.TryParse(encryptOverride, out var encrypt) ? encrypt : false;

        var trustOverride = Environment.GetEnvironmentVariable("SQL_TRUST_SERVER_CERTIFICATE");
        builder.TrustServerCertificate = !bool.TryParse(trustOverride, out var trust) || trust;

        if (builder.ConnectTimeout < 30)
            builder.ConnectTimeout = 30;

        return builder.ConnectionString;
    }
}
