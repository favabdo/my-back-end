using MediatR;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ResendVerificationEmail;

public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ResendVerificationEmailCommandHandler(IIdentityService identityService, IEmailService emailService, IConfiguration configuration)
    {
        _identityService = identityService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindUserAsync(request.Email);

        if (user is null || user.EmailConfirmed)
            return Result.Success();

        var token = await _identityService.GenerateEmailConfirmationTokenAsync(request.Email);
        var clientUrl = _configuration["ClientAppUrl"] ?? "http://localhost:5173";
        var link = $"{clientUrl}/verify-email?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendVerificationEmailAsync(request.Email, user.FullName, link, cancellationToken);
        return Result.Success();
    }
}
