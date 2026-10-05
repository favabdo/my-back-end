using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;
using NileTechno.Infrastructure.Services;

namespace NileTechno.API.Controllers;

[Authorize]
[Route("api/wishlist")]
public class WishlistController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly EcProductCatalogQuery _catalog;

    public WishlistController(IApplicationDbContext db, EcProductCatalogQuery catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? userId, CancellationToken ct)
    {
        var owner = EffectiveUserId(userId);

        var items = await _db.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == owner)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

        var products = await _catalog.GetProductsByCodesAsync(
            items.Select(i => i.ProductId).Distinct().ToList(), ct);

        return Ok(items
            .Where(w => products.ContainsKey(w.ProductId))
            .Select(w =>
            {
                var p = products[w.ProductId];
                return new
                {
                    id = w.Id,
                    productId = w.ProductId,
                    product = new
                    {
                        id = p.ItemCode,
                        itemCode = p.ItemCode,
                        name = p.ItemName,
                        title = p.ItemName,
                        price = p.Price,
                        image = p.Image ?? "",
                        stock = p.Stock,
                        groupId = p.GroupId,
                        category = p.GroupName
                    }
                };
            }));
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] WishlistSyncRequest body, CancellationToken ct)
    {
        var userId = EffectiveUserId(body.UserId);

        var incoming = (body.ProductIds ?? new List<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var current = await _db.WishlistItems.Where(w => w.UserId == userId).ToListAsync(ct);
        foreach (var item in current.Where(item => !incoming.Contains(item.ProductId, StringComparer.OrdinalIgnoreCase)))
            _db.WishlistItems.Remove(item);

        foreach (var code in incoming.Where(code => !current.Any(c => string.Equals(c.ProductId, code, StringComparison.OrdinalIgnoreCase))))
            _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = code });

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] WishlistRequest body, CancellationToken ct)
    {
        var userId = EffectiveUserId(body.UserId);
        var productId = (body.ProductId ?? "").Trim();
        if (productId.Length == 0)
            return BadRequest(new { error = "productId مطلوب" });

        var exists = await _db.WishlistItems.AnyAsync(
            w => w.UserId == userId && w.ProductId == productId, ct);
        if (!exists)
            _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] string? userId, [FromQuery] string? productId, CancellationToken ct)
    {
        var owner = EffectiveUserId(userId);
        var code = (productId ?? "").Trim();
        if (code.Length == 0)
            return BadRequest(new { error = "productId مطلوب" });

        var items = await _db.WishlistItems
            .Where(w => w.UserId == owner && w.ProductId == code)
            .ToListAsync(ct);
        _db.WishlistItems.RemoveRange(items);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, removed = items.Count });
    }
}

public class WishlistSyncRequest
{
    public string? UserId { get; set; }
    public List<string>? ProductIds { get; set; }
}

public class WishlistRequest
{
    public string? UserId { get; set; }
    public string? ProductId { get; set; }
}
