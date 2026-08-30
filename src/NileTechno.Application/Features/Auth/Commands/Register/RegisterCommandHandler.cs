using MediatR;
using Microsoft.Extensions.Configuration;
using NileTechno.Application.Common;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Domain.Entities;
using NileTechno.Domain.Enums;

namespace NileTechno.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IIdentityService _identityService;
    private readonly ILoginAccountStore _loginAccounts;
    private readonly ILoginSecretHasher _secretHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RegisterCommandHandler(
        IIdentityService identityService,
        ILoginAccountStore loginAccounts,
        ILoginSecretHasher secretHasher,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _identityService = identityService;
        _loginAccounts = loginAccounts;
        _secretHasher = secretHasher;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        if (await _loginAccounts.EmailExistsAsync(email, cancellationToken) ||
            await _identityService.EmailExistsAsync(email))
            return Result<Guid>.Failure("البريد الإلكتروني مستخدم بالفعل، جرّب تسجيل الدخول.");

        var userId = Guid.NewGuid();
        var createResult = await _identityService.CreateUserAsync(email, request.Password, request.FullName.Trim(), userId);
        if (!createResult.Succeeded)
            return Result<Guid>.Failure(createResult.Errors);

        var account = new LoginAccount
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email,
            FullName = request.FullName.Trim(),
            AuthProvider = LoginAuthProvider.Password,
            EmailConfirmed = false,
            LoyaltyPoints = 100,
            CreatedAt = DateTime.UtcNow
        };
        account.PasswordHash = _secretHasher.Hash(account, request.Password);
        await _loginAccounts.AddAsync(account, cancellationToken);

        var token = await _identityService.GenerateEmailConfirmationTokenAsync(email);
        var clientUrl = _configuration["ClientAppUrl"] ?? "http://localhost:5173";
        var encodedToken = Uri.EscapeDataString(token);
        var verificationLink = $"{clientUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        await _emailService.SendVerificationEmailAsync(email, request.FullName, verificationLink, cancellationToken);

        return Result<Guid>.Success(userId);
    }
}
