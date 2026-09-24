using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/shipping-zones")]
public class ShippingZonesController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public ShippingZonesController(IApplicationDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok((await _db.ShippingZones.AsNoTracking().Where(z => z.Active).OrderBy(z => z.Name).ToListAsync(ct))
            .Select(Map));

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok((await _db.ShippingZones.AsNoTracking().OrderBy(z => z.Name).ToListAsync(ct)).Select(Map));

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] ShippingZoneRequest body, CancellationToken ct)
    {
        var name = (body.Name ?? "").Trim();
        if (name.Length == 0)
            return BadRequest(new { error = "اسم منطقة الشحن مطلوب" });

        var code = (string.IsNullOrWhiteSpace(body.Code) ? name : body.Code.Trim())
            .ToUpperInvariant().Replace(' ', '_');

        ShippingZone? zone = null;
        if (body.Id is Guid id && id != Guid.Empty)
            zone = await _db.ShippingZones.FirstOrDefaultAsync(z => z.Id == id, ct);
        if (zone is null)
            zone = await _db.ShippingZones.FirstOrDefaultAsync(z => z.Code == code, ct);

        if (zone is null)
        {
            zone = new ShippingZone { Code = code, Name = name };
            _db.ShippingZones.Add(zone);
        }

        zone.Name = name;
        zone.Price = body.Price ?? zone.Price;
        zone.Active = body.Active ?? zone.Active;
        zone.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, zone = Map(zone) });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var zone = await _db.ShippingZones.FirstOrDefaultAsync(z => z.Id == id, ct);
        if (zone is null)
            return NotFound(new { error = "المنطقة غير موجودة" });

        _db.ShippingZones.Remove(zone);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    private static object Map(ShippingZone z) => new
    {
        id = z.Id,
        code = z.Code,
        name = z.Name,
        price = z.Price,
        active = z.Active
    };
}

public class ShippingZoneRequest
{
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public bool? Active { get; set; }
}
