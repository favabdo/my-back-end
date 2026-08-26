using MediatR;

namespace NileTechno.Application.Features.Auth.Queries.CheckEmailExists;

public record CheckEmailExistsQuery(string Email) : IRequest<bool>;
