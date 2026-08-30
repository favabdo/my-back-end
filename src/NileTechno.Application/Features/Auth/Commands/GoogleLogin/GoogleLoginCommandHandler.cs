using System.Security.Cryptography;
using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Auth.DTOs;
using NileTechno.Domain.Entities;
using NileTechno.Domain.Enums;

namespace NileTechno.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<AuthResponseDto>>
{
    private readonly IGoogleTokenValidator _googleTokens;
    private readonly ILoginAccountStore _loginAccounts;
    private readonly ILoginSecretHasher _secretHasher;
    private readonly IIdentityService _identityService;
    private readonly IAuthSessionService _sessions;

    public GoogleLoginCommandHandler(
        IGoogleTokenValidator googleTokens,
        ILoginAccountStore loginAccounts,
        ILoginSecretHasher secretHasher,
        IIdentityService identityService,
        IAuthSessionService sessions)
    {
        _googleTokens = googleTokens;
        _loginAccounts = loginAccounts;
        _secretHasher = secretHasher;
        _identityService = identityService;
        _sessions = sessions;
    }

    public async Task<Result<AuthResponseDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var google = await _googleTokens.ValidateAsync(request.IdToken, request.AccessToken, cancellationToken);
        if (google is null)
            return Result<AuthResponseDto>.Failure("فشل التحقق من حساب جوجل. لازم تكون عامل تسجيل دخول على جيميل.");

        if (!google.EmailVerified)
            return Result<AuthResponseDto>.Failure("إيميل جوجل ده مش مفعّل.");

        var account = await _loginAccounts.FindByGoogleSubjectAsync(google.Subject, cancellationToken)
            ?? await _loginAccounts.FindByEmailAsync(google.Email, cancellationToken);

        if (account is null)
        {
            account = await CreateGoogleAccountAsync(google, cancellationToken);
            if (account is null)
                return Result<AuthResponseDto>.Failure("تعذر إنشاء الحساب من جوجل.");
        }
        else
        {
            if (account.IsBlocked)
                return Result<AuthResponseDto>.Failure("تم حظر هذا الحساب، تواصل مع الدعم الفني.");

            account.GoogleSubject ??= google.Subject;
            if (string.IsNullOrWhiteSpace(account.FullName))
                account.FullName = google.Name;
            account.EmailConfirmed = true;
            if (account.AuthProvider == LoginAuthProvider.Password)
                account.AuthProvider = LoginAuthProvider.Linked;
            await _loginAccounts.UpdateAsync(account, cancellationToken);
        }

        if (await _identityService.FindUserAsync(account.Email) is null)
        {
            await _identityService.CreateUserAsync(
                account.Email,
                CreateRandomPassword(),
                account.FullName,
                account.Id,
                emailConfirmed: true);
        }

        var roles = await _identityService.GetRolesAsync(account.Id);
        if (roles.Count == 0)
            roles = new List<string> { "User" };
        var response = await _sessions.IssueAsync(account, roles, cancellationToken);
        return Result<AuthResponseDto>.Success(response);
    }

    private async Task<LoginAccount?> CreateGoogleAccountAsync(GoogleIdentity google, CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();
        var passwordStandIn = CreateGooglePasswordToken(google.Subject);
        var identityPassword = CreateRandomPassword();

        var created = await _identityService.CreateUserAsync(google.Email, identityPassword, google.Name, userId, emailConfirmed: true);
        _ = created;

        var account = new LoginAccount
        {
            Id = userId,
            Email = google.Email,
            NormalizedEmail = google.Email,
            FullName = google.Name,
            AuthProvider = LoginAuthProvider.Google,
            GoogleSubject = google.Subject,
            EmailConfirmed = true,
            LoyaltyPoints = 100,
            CreatedAt = DateTime.UtcNow
        };
        account.PasswordHash = _secretHasher.Hash(account, passwordStandIn);
        account.GoogleSignInToken = account.PasswordHash;
        await _loginAccounts.AddAsync(account, cancellationToken);
        return account;
    }

    private static string CreateGooglePasswordToken(string subject) =>
        $"google:{subject}:{Convert.ToHexString(RandomNumberGenerator.GetBytes(32))}";

    private static string CreateRandomPassword() =>
        $"Gg{Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))}9!";
}
