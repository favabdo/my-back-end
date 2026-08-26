using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ConfirmEmailCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var confirmed = await _identityService.ConfirmEmailAsync(request.Email, request.Token);
        return confirmed
            ? Result.Success()
            : Result.Failure("رابط التفعيل غير صحيح أو منتهي الصلاحية.");
    }
}
