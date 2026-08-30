using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Features.Auth.DTOs;
using NileTechno.Domain.Entities;

namespace NileTechno.Infrastructure.Services;

public class AuthSessionService : IAuthSessionService
{
    private readonly ITokenService _tokenService;
    private readonly IIdentityService _identityService;
    private readonly ILoginAccountStore _loginAccounts;
    private readonly IConfiguration _configuration;

    public AuthSessionService(
        ITokenService tokenService,
        IIdentityService identityService,
        ILoginAccountStore loginAccounts,
        IConfiguration configuration)
    {
        _tokenService = tokenService;
        _identityService = identityService;
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

        await _identityService.SaveRefreshTokenAsync(account.Id, tokens.RefreshToken, account.RefreshTokenExpiresAtUtc.Value);
        await _identityService.UpdateLastLoginAsync(account.Id);

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
