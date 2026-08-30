using NileTechno.Application.Features.Auth.DTOs;
using NileTechno.Domain.Entities;

namespace NileTechno.Application.Common.Interfaces;

public interface IAuthSessionService
{
    Task<AuthResponseDto> IssueAsync(LoginAccount account, IList<string> roles, CancellationToken cancellationToken = default);
}
