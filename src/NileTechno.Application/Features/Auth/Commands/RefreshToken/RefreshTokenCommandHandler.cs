using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Auth.DTOs;

namespace NileTechno.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IAuthSessionService _sessions;
    private readonly ILoginAccountStore _loginAccounts;

    public RefreshTokenCommandHandler(IAuthSessionService sessions, ILoginAccountStore loginAccounts)
    {
        _sessions = sessions;
        _loginAccounts = loginAccounts;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var account = await _loginAccounts.FindByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (account is null || account.RefreshTokenExpiresAtUtc is null || account.RefreshTokenExpiresAtUtc < DateTime.UtcNow)
            return Result<AuthResponseDto>.Failure("جلسة الدخول انتهت، برجاء تسجيل الدخول مرة أخرى.");

        if (account.IsBlocked)
            return Result<AuthResponseDto>.Failure("تم حظر هذا الحساب، تواصل مع الدعم الفني.");

        var response = await _sessions.IssueAsync(account, new List<string> { "User" }, cancellationToken);
        return Result<AuthResponseDto>.Success(response);
    }
}
