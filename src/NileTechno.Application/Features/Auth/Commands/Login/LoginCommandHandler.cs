using MediatR;
using NileTechno.Application.Common;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Auth.DTOs;
using NileTechno.Domain.Enums;

namespace NileTechno.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly ILoginAccountStore _loginAccounts;
    private readonly ILoginSecretHasher _secretHasher;
    private readonly IAuthSessionService _sessions;

    public LoginCommandHandler(
        ILoginAccountStore loginAccounts,
        ILoginSecretHasher secretHasher,
        IAuthSessionService sessions)
    {
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
            return Result<AuthResponseDto>.Failure(genericError);

        if (account.AuthProvider == LoginAuthProvider.Google)
            return Result<AuthResponseDto>.Failure("الحساب ده مسجّل بجوجل. استخدم متابعة باستخدام Google.");

        if (account.IsBlocked)
            return Result<AuthResponseDto>.Failure("تم حظر هذا الحساب، تواصل مع الدعم الفني.");

        if (!_secretHasher.Verify(account, request.Password))
            return Result<AuthResponseDto>.Failure(genericError);

        account.EmailConfirmed = true;
        var response = await _sessions.IssueAsync(account, new List<string> { "User" }, cancellationToken);
        return Result<AuthResponseDto>.Success(response);
    }
}
