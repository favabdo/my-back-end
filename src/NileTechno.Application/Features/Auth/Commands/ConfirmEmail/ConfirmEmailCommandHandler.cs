using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ILoginAccountStore _loginAccounts;

    public ConfirmEmailCommandHandler(IIdentityService identityService, ILoginAccountStore loginAccounts)
    {
        _identityService = identityService;
        _loginAccounts = loginAccounts;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var confirmed = await _identityService.ConfirmEmailAsync(request.Email, request.Token);
        if (!confirmed)
            return Result.Failure("رابط التفعيل غير صحيح أو منتهي الصلاحية.");

        var account = await _loginAccounts.FindByEmailAsync(request.Email, cancellationToken);
        if (account is not null && !account.EmailConfirmed)
        {
            account.EmailConfirmed = true;
            await _loginAccounts.UpdateAsync(account, cancellationToken);
        }

        return Result.Success();
    }
}
