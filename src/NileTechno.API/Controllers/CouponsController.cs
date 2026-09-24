using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/coupons")]
public class CouponsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public CouponsController(IApplicationDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var list = await _db.Coupons.AsNoTracking()
            .Where(c => c.IsActive && (c.ExpiresAt == null || c.ExpiresAt > now))
                .OrderBy(c => c.Code)
            .ToListAsync(ct);
        return Ok(list.Select(Map));
    }

    [AllowAnonymous]
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateCouponRequest body, CancellationToken ct)
    {
        var code = (body.Code ?? "").Trim();
        if (code.Length == 0)
            return Ok(new { valid = false, error = "أدخل كود الخصم" });

        var coupon = await _db.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code, ct);

        var now = DateTime.UtcNow;
        if (coupon is null || !coupon.IsActive || (coupon.ExpiresAt != null && coupon.ExpiresAt <= now)
            || (coupon.MaxUses != null && coupon.UsedCount >= coupon.MaxUses))
            return Ok(new { valid = false, error = "كود الخصم غير صالح أو منتهي" });

        var discount = Math.Round((body.Subtotal ?? 0) * coupon.DiscountPercent / 100m, 2);
        return Ok(new { valid = true, code = coupon.Code, discountPercent = coupon.DiscountPercent, discount });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok((await _db.Coupons.AsNoTracking().OrderBy(c => c.Code).ToListAsync(ct)).Select(Map));

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] CouponRequest body, CancellationToken ct)
    {
        var code = (body.Code ?? "").Trim().ToUpperInvariant();
        if (code.Length == 0)
            return BadRequest(new { error = "الكود مطلوب" });

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);
        if (coupon is null)
        {
            coupon = new Coupon { Code = code };
            _db.Coupons.Add(coupon);
        }

        coupon.DiscountPercent = body.DiscountPercent ?? coupon.DiscountPercent;
        coupon.IsActive = body.IsActive ?? coupon.IsActive;
        coupon.ExpiresAt = body.ExpiresAt ?? coupon.ExpiresAt;
        coupon.MaxUses = body.MaxUses ?? coupon.MaxUses;
        coupon.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, coupon = Map(coupon) });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (coupon is null)
            return NotFound(new { error = "الكوبون غير موجود" });

        _db.Coupons.Remove(coupon);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    private static object Map(Coupon c) => new
    {
        id = c.Id,
        code = c.Code,
        discountPercent = c.DiscountPercent,
        isActive = c.IsActive,
        expiresAt = c.ExpiresAt,
        maxUses = c.MaxUses,
        usedCount = c.UsedCount
    };
}

public class ValidateCouponRequest
{
    public string? Code { get; set; }
    public decimal? Subtotal { get; set; }
}

public class CouponRequest
{
    public string? Code { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
}
