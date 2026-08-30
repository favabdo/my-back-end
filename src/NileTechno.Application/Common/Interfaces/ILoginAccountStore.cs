using NileTechno.Domain.Entities;

namespace NileTechno.Application.Common.Interfaces;

public interface ILoginAccountStore
{
    Task<LoginAccount?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LoginAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<LoginAccount?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<LoginAccount?> FindByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(LoginAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(LoginAccount account, CancellationToken cancellationToken = default);
}
