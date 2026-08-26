using MediatR;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Application.Features.Auth.Queries.CheckEmailExists;

public class CheckEmailExistsQueryHandler : IRequestHandler<CheckEmailExistsQuery, bool>
{
    private readonly IIdentityService _identityService;

    public CheckEmailExistsQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<bool> Handle(CheckEmailExistsQuery request, CancellationToken cancellationToken) =>
        _identityService.EmailExistsAsync(request.Email);
}
