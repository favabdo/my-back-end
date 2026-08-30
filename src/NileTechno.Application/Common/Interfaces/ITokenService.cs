namespace NileTechno.Application.Common.Interfaces;

public record AuthTokenResult(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);

public interface ITokenService
{
    AuthTokenResult GenerateTokens(int userId, string email, string fullName, IList<string> roles);
}
