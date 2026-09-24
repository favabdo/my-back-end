using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/admin/users")]
[Authorize(Roles = "Admin,MainAdmin")]
public class AdminUsersController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IAdminRoleProvider _adminRoles;

    public AdminUsersController(IApplicationDbContext db, IAdminRoleProvider adminRoles)
    {
        _db = db;
        _adminRoles = adminRoles;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct)
    {
        var query = _db.LoginAccounts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(a => a.Email.Contains(search.Trim()) || a.FullName.Contains(search.Trim()));
        }

        var users = await query.OrderByDescending(a => a.CreatedAt).Take(500).ToListAsync(ct);
        return Ok(users.Select(u => new
        {
            uid = u.Id.ToString(),
            userId = u.Id,
            email = u.Email,
            name = u.FullName,
            fullName = u.FullName,
            phone = u.Phone,
            role = _adminRoles.ResolveRole(u.Email),
            points = u.LoyaltyPoints,
            isBlocked = u.IsBlocked,
            authProvider = u.AuthProvider,
            emailConfirmed = u.EmailConfirmed,
            createdAt = u.CreatedAt,
            lastLoginAt = u.LastLoginAt
        }));
    }

    [HttpPost("{id:int}/block")]
    public async Task<IActionResult> Block(int id, [FromQuery] bool blocked = true, CancellationToken ct = default)
    {
        var user = await _db.LoginAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (user is null)
            return NotFound(new { error = "الحساب غير موجود" });

        user.IsBlocked = blocked;
        user.BlockedAt = blocked ? DateTime.UtcNow : null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, isBlocked = user.IsBlocked });
    }

    [HttpPost("{id:int}/points")]
    public async Task<IActionResult> SetPoints(int id, [FromBody] PointsRequest body, CancellationToken ct)
    {
        var user = await _db.LoginAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (user is null)
            return NotFound(new { error = "الحساب غير موجود" });

        user.LoyaltyPoints = Math.Max(0, body.Points ?? user.LoyaltyPoints);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, points = user.LoyaltyPoints });
    }
}

public class PointsRequest
{
    public int? Points { get; set; }
}
