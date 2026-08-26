using MediatR;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(IIdentityService identityService, IEmailService emailService, IConfiguration configuration)
    {
        _identityService = identityService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindUserAsync(request.Email);

        if (user is null)
            return Result.Success();

        var token = await _identityService.GeneratePasswordResetTokenAsync(request.Email);
        var clientUrl = _configuration["ClientAppUrl"] ?? "http://localhost:5173";
        var link = $"{clientUrl}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendPasswordResetEmailAsync(request.Email, user.FullName, link, cancellationToken);
        return Result.Success();
    }
}
