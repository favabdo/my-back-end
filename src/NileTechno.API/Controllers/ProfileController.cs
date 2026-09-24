using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.API.Controllers;

[Route("api/auth/profile")]
[Authorize]
public class ProfileController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var account = await FindAsync(ct);
        if (account is null)
            return NotFound(new { error = "الحساب غير موجود" });

        return Ok(new
        {
            userId = account.Id,
            email = account.Email,
            fullName = account.FullName,
            phone = account.Phone,
            loyaltyPoints = account.LoyaltyPoints,
            authProvider = account.AuthProvider,
            emailConfirmed = account.EmailConfirmed,
            createdAt = account.CreatedAt,
            lastLoginAt = account.LastLoginAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest body, CancellationToken ct)
    {
        var account = await FindAsync(ct);
        if (account is null)
            return NotFound(new { error = "الحساب غير موجود" });

        if (body.FullName is not null)
            account.FullName = body.FullName.Trim();
        if (body.Phone is not null)
            account.Phone = body.Phone.Trim();
        account.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    private async Task<Domain.Entities.LoginAccount?> FindAsync(CancellationToken ct)
    {
        if (_currentUser.AccountId is not int id)
            return null;
        return await _db.LoginAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);
    }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
}
