using MediatR;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Auth.DTOs;

namespace NileTechno.Application.Features.Auth.Commands.GoogleLogin;

public record GoogleLoginCommand(string? IdToken, string? AccessToken) : IRequest<Result<AuthResponseDto>>;
