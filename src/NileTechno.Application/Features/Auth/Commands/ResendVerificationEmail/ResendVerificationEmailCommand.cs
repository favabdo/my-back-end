using MediatR;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ResendVerificationEmail;

public record ResendVerificationEmailCommand(string Email) : IRequest<Result>;
