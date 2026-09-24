using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.API.Controllers;

[Route("api/auth/change-password")]
[Authorize]
public class ChangePasswordController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ILoginSecretHasher _hasher;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordController(IApplicationDbContext db, ILoginSecretHasher hasher, ICurrentUserService currentUser)
    {
        _db = db;
        _hasher = hasher;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Change([FromBody] ChangePasswordRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.CurrentPassword) || string.IsNullOrWhiteSpace(body.NewPassword))
            return BadRequest(new { errors = new[] { "أدخل كلمة المرور الحالية والجديدة." } });
        if (body.NewPassword.Trim().Length < 6)
            return BadRequest(new { errors = new[] { "كلمة المرور الجديدة قصيرة جدًا." } });

        if (_currentUser.AccountId is not int id)
            return Unauthorized(new { errors = new[] { "جلسة غير صالحة." } });

        var account = await _db.LoginAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (account is null)
            return NotFound(new { errors = new[] { "الحساب غير موجود." } });

        if (account.AuthProvider == Domain.Enums.LoginAuthProvider.Google)
            return BadRequest(new { errors = new[] { "حساب Google لا يحتاج كلمة مرور." } });

        if (!_hasher.Verify(account, body.CurrentPassword))
            return BadRequest(new { errors = new[] { "كلمة المرور الحالية غير صحيحة." } });

        account.PasswordHash = _hasher.Hash(account, body.NewPassword.Trim());
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }
}

public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
