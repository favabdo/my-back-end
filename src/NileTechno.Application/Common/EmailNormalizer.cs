namespace NileTechno.Application.Common;

public static class EmailNormalizer
{
    public static string Normalize(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();
}
