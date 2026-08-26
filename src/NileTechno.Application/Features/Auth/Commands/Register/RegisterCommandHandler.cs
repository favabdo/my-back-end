using MediatR;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RegisterCommandHandler(IIdentityService identityService, IEmailService emailService, IConfiguration configuration)
    {
        _identityService = identityService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _identityService.EmailExistsAsync(request.Email))
            return Result<Guid>.Failure("البريد الإلكتروني مستخدم بالفعل، جرّب تسجيل الدخول.");

        var createResult = await _identityService.CreateUserAsync(request.Email, request.Password, request.FullName);
        if (!createResult.Succeeded)
            return Result<Guid>.Failure(createResult.Errors);

        var token = await _identityService.GenerateEmailConfirmationTokenAsync(request.Email);
        var clientUrl = _configuration["ClientAppUrl"] ?? "http://localhost:5173";
        var encodedToken = Uri.EscapeDataString(token);
        var verificationLink = $"{clientUrl}/verify-email?email={Uri.EscapeDataString(request.Email)}&token={encodedToken}";

        await _emailService.SendVerificationEmailAsync(request.Email, request.FullName, verificationLink, cancellationToken);

        return Result<Guid>.Success(createResult.UserId!.Value);
    }
}
