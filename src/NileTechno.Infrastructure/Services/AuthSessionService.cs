using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Features.Auth.DTOs;
using NileTechno.Domain.Entities;

namespace NileTechno.Infrastructure.Services;

public class AuthSessionService : IAuthSessionService
{
    private readonly ITokenService _tokenService;
    private readonly ILoginAccountStore _loginAccounts;
    private readonly IConfiguration _configuration;

    public AuthSessionService(
        ITokenService tokenService,
        ILoginAccountStore loginAccounts,
        IConfiguration configuration)
    {
        _tokenService = tokenService;
        _loginAccounts = loginAccounts;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> IssueAsync(LoginAccount account, IList<string> roles, CancellationToken cancellationToken = default)
    {
        var tokens = _tokenService.GenerateTokens(account.Id, account.Email, account.FullName, roles);
        var refreshExpiryDays = _configuration.GetSection("Jwt").GetValue<int>("RefreshTokenExpiryDays", 30);

        account.RefreshToken = tokens.RefreshToken;
        account.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(refreshExpiryDays);
        account.LastLoginAt = DateTime.UtcNow;
        await _loginAccounts.UpdateAsync(account, cancellationToken);

        return new AuthResponseDto
        {
            UserId = account.Id,
            Email = account.Email,
            FullName = account.FullName,
            Roles = roles,
            AccessToken = tokens.AccessToken,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshToken = tokens.RefreshToken,
            LoyaltyPoints = account.LoyaltyPoints,
            Phone = account.Phone,
            CreatedAt = account.CreatedAt,
            AuthProvider = account.AuthProvider
        };
    }
}
