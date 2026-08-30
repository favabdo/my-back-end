using NileTechno.Domain.Common;
using NileTechno.Domain.Enums;

namespace NileTechno.Domain.Entities;

public class LoginAccount : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = LoginAuthProvider.Password;
    public string? GoogleSubject { get; set; }
    public string? GoogleSignInToken { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? BlockedAt { get; set; }
    public int LoyaltyPoints { get; set; } = 100;
    public string? Phone { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
