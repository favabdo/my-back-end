namespace NileTechno.Application.Common.Interfaces;

public record CreateUserResult(bool Succeeded, Guid? UserId, string[] Errors);

public record UserLookupDto(Guid UserId, string Email, string FullName, bool EmailConfirmed, bool IsBlocked);

public interface IIdentityService
{
    Task<CreateUserResult> CreateUserAsync(string email, string password, string fullName, Guid? userId = null, bool emailConfirmed = false);

    Task<UserLookupDto?> FindUserAsync(string email);
    Task<bool> EmailExistsAsync(string email);

    Task<bool> CheckPasswordAsync(string email, string password);

    Task<IList<string>> GetRolesAsync(Guid userId);

    Task<string> GenerateEmailConfirmationTokenAsync(string email);
    Task<bool> ConfirmEmailAsync(string email, string token);

    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

    Task SaveRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAtUtc);
    Task<UserLookupDto?> GetUserByValidRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(Guid userId);

    Task UpdateLastLoginAsync(Guid userId);
}
