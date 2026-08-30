using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.Infrastructure.Persistence;

public class LoginAccountStore : ILoginAccountStore
{
    private readonly ApplicationDbContext _db;

    public LoginAccountStore(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<LoginAccount?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.LoginAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<LoginAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = EmailNormalizer.Normalize(email);
        return _db.LoginAccounts.FirstOrDefaultAsync(a => a.NormalizedEmail == normalized, cancellationToken);
    }

    public Task<LoginAccount?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        _db.LoginAccounts.FirstOrDefaultAsync(a => a.RefreshToken == refreshToken, cancellationToken);

    public Task<LoginAccount?> FindByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default) =>
        _db.LoginAccounts.FirstOrDefaultAsync(a => a.GoogleSubject == googleSubject, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = EmailNormalizer.Normalize(email);
        return _db.LoginAccounts.AnyAsync(a => a.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task AddAsync(LoginAccount account, CancellationToken cancellationToken = default)
    {
        account.Email = EmailNormalizer.Normalize(account.Email);
        account.NormalizedEmail = account.Email;
        _db.LoginAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoginAccount account, CancellationToken cancellationToken = default)
    {
        account.Email = EmailNormalizer.Normalize(account.Email);
        account.NormalizedEmail = account.Email;
        account.UpdatedAt = DateTime.UtcNow;
        _db.LoginAccounts.Update(account);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
