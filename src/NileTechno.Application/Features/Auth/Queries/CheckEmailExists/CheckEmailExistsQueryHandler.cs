using MediatR;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Application.Features.Auth.Queries.CheckEmailExists;

public class CheckEmailExistsQueryHandler : IRequestHandler<CheckEmailExistsQuery, bool>
{
    private readonly ILoginAccountStore _loginAccounts;

    public CheckEmailExistsQueryHandler(ILoginAccountStore loginAccounts)
    {
        _loginAccounts = loginAccounts;
    }

    public Task<bool> Handle(CheckEmailExistsQuery request, CancellationToken cancellationToken) =>
        _loginAccounts.EmailExistsAsync(request.Email, cancellationToken);
}
