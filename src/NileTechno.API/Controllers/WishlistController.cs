using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;
using NileTechno.Infrastructure.Services;

namespace NileTechno.API.Controllers;

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
    public async Task<IActionResult> Get([FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Ok(new List<object>());

        var items = await _db.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId.Trim())
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

        var products = await _catalog.GetProductsByCodesAsync(
            items.Select(i => i.ProductId).Distinct().ToList(), ct);

        return Ok(items.Select(w =>
        {
            products.TryGetValue(w.ProductId, out var p);
            return new
            {
                id = w.Id,
                productId = w.ProductId,
                product = p is null ? (object?)new { id = w.ProductId } : new
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
        var userId = (body.UserId ?? "").Trim();
        if (userId.Length == 0)
            return BadRequest(new { error = "userId مطلوب" });

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
        var userId = (body.UserId ?? "").Trim();
        var productId = (body.ProductId ?? "").Trim();
        if (userId.Length == 0 || productId.Length == 0)
            return BadRequest(new { error = "userId و productId مطلوبان" });

        var exists = await _db.WishlistItems.AnyAsync(
            w => w.UserId == userId && w.ProductId == productId, ct);
        if (!exists)
            _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] string userId, [FromQuery] string productId, CancellationToken ct)
    {
        var items = await _db.WishlistItems
            .Where(w => w.UserId == (userId ?? "").Trim() && w.ProductId == (productId ?? "").Trim())
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
