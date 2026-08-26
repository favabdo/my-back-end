using Microsoft.AspNetCore.Identity;
using NileTechno.Domain.Enums;

namespace NileTechno.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public bool IsBlocked { get; set; } = false;
    public DateTime? BlockedAt { get; set; }

    public int LoyaltyPoints { get; set; } = 100;
    public DateTime? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
