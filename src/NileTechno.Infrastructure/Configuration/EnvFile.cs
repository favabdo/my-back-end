namespace NileTechno.Infrastructure.Configuration;

public static class EnvFile
{
    public const string Connection = "DB_CONNECTION";

    public static void Load()
    {
        var path = Find();
        if (path is not null)
            Apply(File.ReadAllLines(path));

        Alias(Connection, "ConnectionStrings__DefaultConnection");
        Alias("SMTP_HOST", "Smtp__Host");
        Alias("SMTP_PORT", "Smtp__Port");
        Alias("SMTP_USER", "Smtp__User");
        Alias("SMTP_PASS", "Smtp__Password");
        Alias("SMTP_PASSWORD", "Smtp__Password");
        Alias("SMTP_FROM_EMAIL", "Smtp__FromEmail");
        Alias("SMTP_FROM_NAME", "Smtp__FromName");
        Alias("CORS_ALLOWED_ORIGINS", "Cors__AllowedOrigins");
        Alias("JWT_KEY", "Jwt__Key");
    }

    public static string? Find()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(Path.GetFullPath(start));
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, ".env");
                if (File.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }
        }

        return null;
    }

    private static void Apply(IEnumerable<string> lines)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                line = line[7..].Trim();

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = Unquote(line[(separator + 1)..].Trim());
            if (key.Length == 0)
                continue;

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static void Alias(string from, string to)
    {
        var value = Environment.GetEnvironmentVariable(from);
        if (!string.IsNullOrWhiteSpace(value))
            Environment.SetEnvironmentVariable(to, value);
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }
}
