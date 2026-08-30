using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileTechno.Application.Features.Auth.Commands.ConfirmEmail;
using NileTechno.Application.Features.Auth.Commands.ForgotPassword;
using NileTechno.Application.Features.Auth.Commands.GoogleLogin;
using NileTechno.Application.Features.Auth.Commands.Login;
using NileTechno.Application.Features.Auth.Commands.Logout;
using NileTechno.Application.Features.Auth.Commands.RefreshToken;
using NileTechno.Application.Features.Auth.Commands.Register;
using NileTechno.Application.Features.Auth.Commands.ResendVerificationEmail;
using NileTechno.Application.Features.Auth.Commands.ResetPassword;
using NileTechno.Application.Features.Auth.Queries.CheckEmailExists;

namespace NileTechno.API.Controllers;

public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors });
        return Ok(new { message = "تم إنشاء الحساب بنجاح. تقدر تسجّل الدخول دلوقتي.", userId = result.Data, emailConfirmed = true });
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google(GoogleLoginCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded) return Unauthorized(new { errors = result.Errors });
        return Ok(result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded) return Unauthorized(new { errors = result.Errors });
        return Ok(result.Data);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded) return Unauthorized(new { errors = result.Errors });
        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await Mediator.Send(new LogoutCommand(userId));
        return Ok(new { message = "تم تسجيل الخروج بنجاح." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
    {
        var result = await Mediator.Send(new ConfirmEmailCommand(email, token));
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors });
        return Ok(new { message = "تم تفعيل الحساب بنجاح، تقدر تسجل الدخول دلوقتي." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailCommand command)
    {
        await Mediator.Send(command);
        return Ok(new { message = "لو الإيميل ده مسجل عندنا ولسه مش مفعّل، هيوصلك رابط تفعيل جديد." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await Mediator.Send(command);
        return Ok(new { message = "لو الإيميل ده مسجل عندنا، هيوصلك رابط استعادة كلمة المرور." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors });
        return Ok(new { message = "تم تغيير كلمة المرور بنجاح." });
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
    {
        var exists = await Mediator.Send(new CheckEmailExistsQuery(email));
        return Ok(new { exists });
    }
}
