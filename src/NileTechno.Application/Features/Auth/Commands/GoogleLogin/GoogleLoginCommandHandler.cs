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
    private readonly IAuthSessionService _sessions;

    public GoogleLoginCommandHandler(
        IGoogleTokenValidator googleTokens,
        ILoginAccountStore loginAccounts,
        ILoginSecretHasher secretHasher,
        IAuthSessionService sessions)
    {
        _googleTokens = googleTokens;
        _loginAccounts = loginAccounts;
        _secretHasher = secretHasher;
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
            account = CreateGoogleAccount(google);
            await _loginAccounts.AddAsync(account, cancellationToken);
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
        }

        var response = await _sessions.IssueAsync(account, new List<string> { "User" }, cancellationToken);
        return Result<AuthResponseDto>.Success(response);
    }

    private LoginAccount CreateGoogleAccount(GoogleIdentity google)
    {
        var account = new LoginAccount
        {
            Id = Guid.NewGuid(),
            Email = google.Email,
            NormalizedEmail = google.Email,
            FullName = google.Name,
            AuthProvider = LoginAuthProvider.Google,
            GoogleSubject = google.Subject,
            EmailConfirmed = true,
            LoyaltyPoints = 100,
            CreatedAt = DateTime.UtcNow
        };
        var passwordStandIn = $"google:{google.Subject}:{Convert.ToHexString(RandomNumberGenerator.GetBytes(32))}";
        account.PasswordHash = _secretHasher.Hash(account, passwordStandIn);
        account.GoogleSignInToken = account.PasswordHash;
        return account;
    }
}
