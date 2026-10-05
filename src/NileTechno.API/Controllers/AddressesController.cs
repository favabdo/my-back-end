using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Authorize]
[Route("api/addresses")]
public class AddressesController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public AddressesController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? userId, CancellationToken ct)
    {
        var owner = EffectiveUserId(userId);

        var list = await _db.UserAddresses.AsNoTracking()
            .Where(a => a.UserId == owner)
            .OrderByDescending(a => a.IsDefault)
            .ToListAsync(ct);

        return Ok(list.Select(Map));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddressInput body, CancellationToken ct)
    {
        var userId = EffectiveUserId(body.UserId);
        var address = new UserAddress { UserId = userId };
        _db.UserAddresses.Add(address);
        Apply(address, body);

        if (body.IsDefault)
        {
            await ClearOtherDefaultsAsync(userId, address.Id, ct);
            address.IsDefault = true;
        }
        else
        {
            address.IsDefault = !await _db.UserAddresses.AnyAsync(a => a.UserId == userId && a.Id != address.Id, ct);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, address = Map(address) });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AddressInput body, CancellationToken ct)
    {
        var userId = EffectiveUserId(body.UserId);
        var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (address is null)
            return NotFound(new { error = "العنوان غير موجود" });

        Apply(address, body);

        if (body.IsDefault)
        {
            await ClearOtherDefaultsAsync(userId, address.Id, ct);
            address.IsDefault = true;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, address = Map(address) });
    }

    private static void Apply(UserAddress address, AddressInput body)
    {
        address.Label = body.Label ?? "";
        address.Governorate = body.Governorate ?? "";
        address.Details = body.Details ?? "";
        address.Latitude = body.Latitude;
        address.Longitude = body.Longitude;
        address.UpdatedAt = DateTime.UtcNow;
    }

    private async Task ClearOtherDefaultsAsync(string userId, Guid keepId, CancellationToken ct)
    {
        var others = await _db.UserAddresses
            .Where(a => a.UserId == userId && a.Id != keepId && a.IsDefault)
            .ToListAsync(ct);
        foreach (var other in others)
            other.IsDefault = false;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string? userId, CancellationToken ct)
    {
        var owner = EffectiveUserId(userId);
        var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == owner, ct);
        if (address is null)
            return NotFound(new { error = "العنوان غير موجود" });

        _db.UserAddresses.Remove(address);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }


    [HttpPut("sync")]
    public async Task<IActionResult> Sync([FromQuery] string? userId, [FromBody] List<AddressRequest> addresses, CancellationToken ct)
        => await SaveAll(userId, addresses, ct);

    [HttpPost("sync")]
    public async Task<IActionResult> SyncPost([FromBody] AddressSyncRequest body, CancellationToken ct)
        => await SaveAll(body.UserId, body.Addresses, ct);

    private async Task<IActionResult> SaveAll(string? userIdRaw, List<AddressRequest>? addresses, CancellationToken ct)
    {
        var userId = EffectiveUserId(userIdRaw);

        var incoming = addresses ?? new List<AddressRequest>();
        var keepIds = incoming.Where(a => a.Id is Guid g && g != Guid.Empty).Select(a => a.Id!.Value).ToHashSet();
        var current = await _db.UserAddresses.Where(a => a.UserId == userId).ToListAsync(ct);

        foreach (var stale in current.Where(c => !keepIds.Contains(c.Id)))
            _db.UserAddresses.Remove(stale);

        var anyDefault = incoming.Any(a => a.IsDefault);
        var first = true;
        foreach (var req in incoming)
        {
            var rid = req.Id ?? Guid.Empty;
            var update = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == rid && a.UserId == userId, ct);
            var isDefault = anyDefault ? req.IsDefault : first;
            first = false;
            if (update is null)
            {
                _db.UserAddresses.Add(new UserAddress
                {
                    UserId = userId,
                    Label = req.Label ?? "",
                    Governorate = req.Governorate ?? "",
                    Details = req.Details ?? "",
                    Latitude = req.Latitude,
                    Longitude = req.Longitude,
                    IsDefault = isDefault
                });
            }
            else
            {
                update.Label = req.Label ?? update.Label;
                update.Governorate = req.Governorate ?? update.Governorate;
                update.Details = req.Details ?? update.Details;
                update.Latitude = req.Latitude ?? update.Latitude;
                update.Longitude = req.Longitude ?? update.Longitude;
                update.IsDefault = isDefault;
                update.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    private static object Map(UserAddress a) => new
    {
        id = a.Id,
        userId = a.UserId,
        label = a.Label,
        governorate = a.Governorate,
        details = a.Details,
        latitude = a.Latitude,
        longitude = a.Longitude,
        isDefault = a.IsDefault,
        createdAt = a.CreatedAt
    };
}

public class AddressSyncRequest
{
    public string? UserId { get; set; }
    public List<AddressRequest>? Addresses { get; set; }
}

public class AddressInput
{
    public string? UserId { get; set; }
    public string? Label { get; set; }
    public string? Governorate { get; set; }
    public string? Details { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
}

public class AddressRequest : AddressInput
{
    public Guid? Id { get; set; }
}

