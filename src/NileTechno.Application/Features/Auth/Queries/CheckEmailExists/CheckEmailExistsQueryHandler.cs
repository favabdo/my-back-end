using MediatR;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Application.Features.Auth.Queries.CheckEmailExists;

public class CheckEmailExistsQueryHandler : IRequestHandler<CheckEmailExistsQuery, bool>
{
    private readonly IIdentityService _identityService;
    private readonly ILoginAccountStore _loginAccounts;

    public CheckEmailExistsQueryHandler(IIdentityService identityService, ILoginAccountStore loginAccounts)
    {
        _identityService = identityService;
        _loginAccounts = loginAccounts;
    }

    public async Task<bool> Handle(CheckEmailExistsQuery request, CancellationToken cancellationToken) =>
        await _loginAccounts.EmailExistsAsync(request.Email, cancellationToken)
        || await _identityService.EmailExistsAsync(request.Email);
}
