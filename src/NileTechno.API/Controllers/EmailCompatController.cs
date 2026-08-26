using Microsoft.AspNetCore.Mvc;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.API.Controllers;

[Route("api/email")]
public class EmailCompatController : ApiControllerBase
{
    private readonly IEmailService _email;

    public EmailCompatController(IEmailService email)
    {
        _email = email;
    }

    [HttpPost("welcome")]
    public async Task<IActionResult> Welcome([FromBody] EmailNameRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email))
            return BadRequest(new { error = "البريد الإلكتروني مطلوب" });

        var result = await _email.SendWelcomeEmailAsync(body.Email, body.Name ?? "", ct);
        return Ok(new { success = result.Success, error = result.Error });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] EmailLinkRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email))
            return BadRequest(new { error = "البريد الإلكتروني مطلوب" });

        var result = await _email.SendVerificationEmailAsync(body.Email, body.Name ?? "", body.VerificationLink ?? "", ct);
        return Ok(new { success = result.Success, error = result.Error });
    }

    [HttpPost("password-reset")]
    public async Task<IActionResult> PasswordReset([FromBody] EmailLinkRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email))
            return BadRequest(new { error = "البريد الإلكتروني مطلوب" });

        var result = await _email.SendPasswordResetEmailAsync(body.Email, body.Name ?? "", body.ResetLink ?? "", ct);
        return Ok(new { success = result.Success, error = result.Error });
    }

    [HttpPost("login-notification")]
    public async Task<IActionResult> LoginNotification([FromBody] EmailNameRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email))
            return BadRequest(new { error = "البريد الإلكتروني مطلوب" });

        var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var result = await _email.SendLoginNotificationEmailAsync(body.Email, body.Name ?? "", ip, ct);
        return Ok(new { success = result.Success, error = result.Error });
    }

    [HttpPost("order-status")]
    public async Task<IActionResult> OrderStatus([FromBody] OrderStatusEmailRequest body, CancellationToken ct)
    {
        var orderNumber = body.Order?.OrderNumber ?? body.Order?.Id ?? "";
        if (string.IsNullOrWhiteSpace(orderNumber) && string.IsNullOrWhiteSpace(body.Email))
            return BadRequest(new { error = "بيانات الطلب مطلوبة" });

        var result = await _email.SendOrderStatusEmailAsync(
            body.Email ?? "",
            body.Name ?? "العميل",
            orderNumber,
            body.NewStatus ?? "",
            ct);
        return Ok(new { success = result.Success, error = result.Error });
    }

    [HttpPost("generic")]
    public async Task<IActionResult> Generic([FromBody] GenericEmailRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.To))
            return BadRequest(new { error = "عنوان المستلم مطلوب" });

        var result = await _email.SendGenericEmailAsync(
            body.To,
            body.Subject ?? body.Title ?? "إشعار",
            body.Title ?? "",
            body.MessageHtml ?? "",
            ct);
        return Ok(new { success = result.Success, error = result.Error });
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        var result = await _email.TestSmtpAsync(ct);
        return Ok(new { success = result.Success, error = result.Error });
    }
}

public class EmailNameRequest
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public class EmailLinkRequest : EmailNameRequest
{
    public string? VerificationLink { get; set; }
    public string? ResetLink { get; set; }
}

public class OrderStatusEmailRequest
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? NewStatus { get; set; }
    public OrderEmailPayload? Order { get; set; }
}

public class OrderEmailPayload
{
    public string? Id { get; set; }
    public string? OrderNumber { get; set; }
}

public class GenericEmailRequest
{
    public string? To { get; set; }
    public string? Subject { get; set; }
    public string? Title { get; set; }
    public string? MessageHtml { get; set; }
}
