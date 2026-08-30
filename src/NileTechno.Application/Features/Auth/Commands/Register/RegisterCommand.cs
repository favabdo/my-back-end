using MediatR;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Email, string Password, string FullName) : IRequest<Result<int>>;
