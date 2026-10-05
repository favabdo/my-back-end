using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace NileTechno.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected bool IsPrivileged => User.IsInRole("Admin") || User.IsInRole("MainAdmin");

    protected string TokenUserId => (User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "").Trim();

    /// <summary>Identity always comes from the token; only Admin/MainAdmin may act on another user's id.</summary>
    protected string EffectiveUserId(string? requestedUserId)
        => IsPrivileged && !string.IsNullOrWhiteSpace(requestedUserId)
            ? requestedUserId.Trim()
            : TokenUserId;
}
