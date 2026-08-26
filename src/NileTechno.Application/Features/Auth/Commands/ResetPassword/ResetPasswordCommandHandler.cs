using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var succeeded = await _identityService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        return succeeded
            ? Result.Success()
            : Result.Failure("رابط استعادة كلمة المرور غير صحيح أو منتهي الصلاحية.");
    }
}
