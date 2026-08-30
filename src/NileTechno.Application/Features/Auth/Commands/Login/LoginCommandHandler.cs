using MediatR;
using NileTechno.Application.Common;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Auth.DTOs;
using NileTechno.Domain.Entities;
using NileTechno.Domain.Enums;

namespace NileTechno.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ILoginAccountStore _loginAccounts;
    private readonly ILoginSecretHasher _secretHasher;
    private readonly IAuthSessionService _sessions;

    public LoginCommandHandler(
        IIdentityService identityService,
        ILoginAccountStore loginAccounts,
        ILoginSecretHasher secretHasher,
        IAuthSessionService sessions)
    {
        _identityService = identityService;
        _loginAccounts = loginAccounts;
        _secretHasher = secretHasher;
        _sessions = sessions;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        const string genericError = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";

        var account = await _loginAccounts.FindByEmailAsync(email, cancellationToken);
        if (account is null)
        {
            account = await TryBackfillFromIdentityAsync(email, request.Password, cancellationToken);
            if (account is null)
                return Result<AuthResponseDto>.Failure(genericError);
        }

        if (account.AuthProvider == LoginAuthProvider.Google)
            return Result<AuthResponseDto>.Failure("الحساب ده مسجّل بجوجل. استخدم متابعة باستخدام Google.");

        if (account.IsBlocked)
            return Result<AuthResponseDto>.Failure("تم حظر هذا الحساب، تواصل مع الدعم الفني.");

        if (!account.EmailConfirmed)
            return Result<AuthResponseDto>.Failure("من فضلك فعّل بريدك الإلكتروني أولاً عن طريق الرابط المرسل إليك.");

        if (!_secretHasher.Verify(account, request.Password))
            return Result<AuthResponseDto>.Failure(genericError);

        var identity = await _identityService.FindUserAsync(email);
        if (identity is not null && identity.IsBlocked)
            return Result<AuthResponseDto>.Failure("تم حظر هذا الحساب، تواصل مع الدعم الفني.");

        if (identity is null)
        {
            var created = await _identityService.CreateUserAsync(email, request.Password, account.FullName, account.Id);
            if (!created.Succeeded)
                return Result<AuthResponseDto>.Failure(created.Errors);
        }

        var roles = await _identityService.GetRolesAsync(account.Id);
        var response = await _sessions.IssueAsync(account, roles, cancellationToken);
        return Result<AuthResponseDto>.Success(response);
    }

    private async Task<LoginAccount?> TryBackfillFromIdentityAsync(string email, string password, CancellationToken cancellationToken)
    {
        var identity = await _identityService.FindUserAsync(email);
        if (identity is null)
            return null;

        if (!await _identityService.CheckPasswordAsync(email, password))
            return null;

        var account = new LoginAccount
        {
            Id = identity.UserId,
            Email = email,
            NormalizedEmail = email,
            FullName = identity.FullName,
            AuthProvider = LoginAuthProvider.Password,
            EmailConfirmed = identity.EmailConfirmed,
            IsBlocked = identity.IsBlocked,
            LoyaltyPoints = 100,
            CreatedAt = DateTime.UtcNow
        };
        account.PasswordHash = _secretHasher.Hash(account, password);
        await _loginAccounts.AddAsync(account, cancellationToken);
        return account;
    }
}
