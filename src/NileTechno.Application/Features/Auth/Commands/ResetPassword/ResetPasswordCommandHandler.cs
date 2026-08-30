using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Domain.Enums;

namespace NileTechno.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ILoginAccountStore _loginAccounts;
    private readonly ILoginSecretHasher _secretHasher;

    public ResetPasswordCommandHandler(
        IIdentityService identityService,
        ILoginAccountStore loginAccounts,
        ILoginSecretHasher secretHasher)
    {
        _identityService = identityService;
        _loginAccounts = loginAccounts;
        _secretHasher = secretHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var succeeded = await _identityService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        if (!succeeded)
            return Result.Failure("رابط استعادة كلمة المرور غير صحيح أو منتهي الصلاحية.");

        var account = await _loginAccounts.FindByEmailAsync(request.Email, cancellationToken);
        if (account is not null)
        {
            account.PasswordHash = _secretHasher.Hash(account, request.NewPassword);
            if (account.AuthProvider == LoginAuthProvider.Google)
                account.AuthProvider = LoginAuthProvider.Linked;
            await _loginAccounts.UpdateAsync(account, cancellationToken);
        }

        return Result.Success();
    }
}
