using MediatR;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(string Email, string Token) : IRequest<Result>;
