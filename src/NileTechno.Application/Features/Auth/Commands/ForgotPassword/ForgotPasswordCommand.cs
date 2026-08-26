using MediatR;
using NileTechno.Application.Common.Models;

namespace NileTechno.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result>;
