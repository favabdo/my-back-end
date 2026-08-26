using MediatR;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId) : IRequest<Result>;
