namespace NileTechno.Application.Features.Auth.DTOs;

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();

    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;

    public int LoyaltyPoints { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AuthProvider { get; set; } = "Password";
}
