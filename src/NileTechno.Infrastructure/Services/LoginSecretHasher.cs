using Microsoft.AspNetCore.Identity;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.Infrastructure.Services;

public class LoginSecretHasher : ILoginSecretHasher
{
    private readonly PasswordHasher<LoginAccount> _hasher = new();

    public string Hash(LoginAccount account, string secret) =>
        _hasher.HashPassword(account, secret);

    public bool Verify(LoginAccount account, string secret)
    {
        if (string.IsNullOrEmpty(account.PasswordHash))
            return false;

        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, secret);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
