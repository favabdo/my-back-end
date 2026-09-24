using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Infrastructure.Services;

/// <summary>
/// Admin identity lives in configuration (Admins section / ADMIN_MAIN_EMAILS + ADMIN_EMAILS env),
/// never in the buyer accounts table.
/// </summary>
public class AdminRoleProvider : IAdminRoleProvider
{
    private readonly HashSet<string> _mainAdmins;
    private readonly HashSet<string> _admins;

    public AdminRoleProvider(IConfiguration configuration)
    {
        _mainAdmins = Parse(configuration["Admins:MainAdminEmails"]
                            ?? Environment.GetEnvironmentVariable("ADMIN_MAIN_EMAILS"));
        _admins = Parse(configuration["Admins:AdminEmails"]
                        ?? Environment.GetEnvironmentVariable("ADMIN_EMAILS"));
    }

    private static HashSet<string> Parse(string? csv) =>
        (csv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();

    public IList<string> ResolveRoles(string email)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        if (_mainAdmins.Contains(normalized))
            return new List<string> { "MainAdmin", "Admin", "User" };
        if (_admins.Contains(normalized))
            return new List<string> { "Admin", "User" };
        return new List<string> { "User" };
    }

    public string ResolveRole(string email)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        if (_mainAdmins.Contains(normalized))
            return "main_admin";
        if (_admins.Contains(normalized))
            return "admin";
        return "user";
    }
}
