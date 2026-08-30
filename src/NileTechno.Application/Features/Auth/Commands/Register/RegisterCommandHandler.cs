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
        try
        {
            var email = EmailNormalizer.Normalize(request.Email);
            var fullName = request.FullName.Trim();
            var existingAccount = await _loginAccounts.FindByEmailAsync(email, cancellationToken);
            var existingIdentity = await _identityService.FindUserAsync(email);

            if (existingAccount is not null && existingIdentity is not null)
                return Result<Guid>.Failure("البريد الإلكتروني مستخدم بالفعل، جرّب تسجيل الدخول.");

            Guid userId;
            if (existingIdentity is not null)
            {
                userId = existingIdentity.UserId;
            }
            else
            {
                userId = existingAccount?.Id ?? Guid.NewGuid();
                var createResult = await _identityService.CreateUserAsync(
                    email, request.Password, fullName, userId, emailConfirmed: true);
                if (!createResult.Succeeded)
                    return Result<Guid>.Failure(createResult.Errors);
                userId = createResult.UserId ?? userId;
            }

            if (existingAccount is null)
            {
                var account = new LoginAccount
                {
                    Id = userId,
                    Email = email,
                    NormalizedEmail = email,
                    FullName = fullName,
                    AuthProvider = LoginAuthProvider.Password,
                    EmailConfirmed = true,
                    LoyaltyPoints = 100,
                    CreatedAt = DateTime.UtcNow
                };
                account.PasswordHash = _secretHasher.Hash(account, request.Password);
                await _loginAccounts.AddAsync(account, cancellationToken);
            }
            else if (!existingAccount.EmailConfirmed)
            {
                existingAccount.EmailConfirmed = true;
                await _loginAccounts.UpdateAsync(existingAccount, cancellationToken);
            }

            TrySendActivationEmails(email, fullName);
            return Result<Guid>.Success(userId);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"تعذر إنشاء الحساب: {ex.Message}");
        }
    }

    private void TrySendActivationEmails(string email, string fullName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var token = await _identityService.GenerateEmailConfirmationTokenAsync(email);
                var clientUrl = _configuration["ClientAppUrl"] ?? "http://localhost:5173";
                var link = $"{clientUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
                await _emailService.SendVerificationEmailAsync(email, fullName, link);
                await _emailService.SendWelcomeEmailAsync(email, fullName);
            }
            catch
            {
                /* SMTP must never block or fail registration */
            }
        });
    }
}
