using MediatR;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Auth.DTOs;

namespace NileTechno.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(IIdentityService identityService, ITokenService tokenService, IConfiguration configuration)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByValidRefreshTokenAsync(request.RefreshToken);
        if (user is null)
            return Result<AuthResponseDto>.Failure("جلسة الدخول انتهت، برجاء تسجيل الدخول مرة أخرى.");

        if (user.IsBlocked)
            return Result<AuthResponseDto>.Failure("تم حظر هذا الحساب، تواصل مع الدعم الفني.");

        var roles = await _identityService.GetRolesAsync(user.UserId);
        var tokens = _tokenService.GenerateTokens(user.UserId, user.Email, user.FullName, roles);

        var refreshExpiryDays = _configuration.GetSection("Jwt").GetValue<int>("RefreshTokenExpiryDays", 30);
        await _identityService.SaveRefreshTokenAsync(user.UserId, tokens.RefreshToken, DateTime.UtcNow.AddDays(refreshExpiryDays));

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            AccessToken = tokens.AccessToken,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshToken = tokens.RefreshToken
        });
    }
}
