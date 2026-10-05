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

    /// <summary>
    /// Identity always comes from the token; only Admin/MainAdmin may act on another user's id.
    /// Returns null when a non-privileged caller names someone else, so the request is refused
    /// instead of silently being filed under the caller's own account.
    /// </summary>
    protected string? EffectiveUserId(string? requestedUserId)
    {
        var requested = (requestedUserId ?? "").Trim();
        if (IsPrivileged)
            return requested.Length > 0 ? requested : TokenUserId;
        return requested.Length == 0 || requested == TokenUserId ? TokenUserId : null;
    }

    protected IActionResult UserIdMismatch() => StatusCode(StatusCodes.Status403Forbidden, new
    {
        error = "userId لا يطابق حساب التوكن — لشغل على حسابك شيل القيمة أو ابعت رقم حسابك، والكتابة لحساب تاني متاحة للأدمن فقط"
    });
}
