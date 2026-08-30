using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ILoginAccountStore _loginAccounts;

    public LogoutCommandHandler(ILoginAccountStore loginAccounts)
    {
        _loginAccounts = loginAccounts;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var account = await _loginAccounts.FindByIdAsync(request.UserId, cancellationToken);
        if (account is not null)
        {
            account.RefreshToken = null;
            account.RefreshTokenExpiresAtUtc = null;
            await _loginAccounts.UpdateAsync(account, cancellationToken);
        }

        return Result.Success();
    }
}
