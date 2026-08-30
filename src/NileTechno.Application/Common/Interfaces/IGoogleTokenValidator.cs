namespace NileTechno.Application.Common.Interfaces;

public record GoogleIdentity(string Email, string Name, string Subject, bool EmailVerified);

public interface IGoogleTokenValidator
{
    Task<GoogleIdentity?> ValidateAsync(string? idToken, string? accessToken, CancellationToken cancellationToken = default);
}
