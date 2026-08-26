using MediatR;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Auth.DTOs;

namespace NileTechno.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(IIdentityService identityService, ITokenService tokenService, IConfiguration configuration)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindUserAsync(request.Email);

        const string genericError = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";

        if (user is null)
            return Result<AuthResponseDto>.Failure(genericError);

        if (user.IsBlocked)
            return Result<AuthResponseDto>.Failure("تم حظر هذا الحساب، تواصل مع الدعم الفني.");

        if (!user.EmailConfirmed)
            return Result<AuthResponseDto>.Failure("من فضلك فعّل بريدك الإلكتروني أولاً عن طريق الرابط المرسل إليك.");

        var passwordValid = await _identityService.CheckPasswordAsync(request.Email, request.Password);
        if (!passwordValid)
            return Result<AuthResponseDto>.Failure(genericError);

        var roles = await _identityService.GetRolesAsync(user.UserId);
        var tokens = _tokenService.GenerateTokens(user.UserId, user.Email, user.FullName, roles);

        var refreshExpiryDays = _configuration.GetSection("Jwt").GetValue<int>("RefreshTokenExpiryDays", 30);
        await _identityService.SaveRefreshTokenAsync(user.UserId, tokens.RefreshToken, DateTime.UtcNow.AddDays(refreshExpiryDays));
        await _identityService.UpdateLastLoginAsync(user.UserId);

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
