using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;
using NileTechno.Infrastructure.Services;

namespace NileTechno.API.Controllers;

[Route("api/cart")]
public class CartController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly EcProductCatalogQuery _catalog;

    public CartController(IApplicationDbContext db, EcProductCatalogQuery catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Ok(new List<object>());

        var items = await _db.CartItems.AsNoTracking()
            .Where(c => c.UserId == userId.Trim())
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var products = await _catalog.GetProductsByCodesAsync(
            items.Select(i => i.ProductId).Distinct().ToList(), ct);

        return Ok(items.Select(c =>
        {
            products.TryGetValue(c.ProductId, out var p);
            return new
            {
                id = c.Id,
                productId = c.ProductId,
                quantity = c.Quantity,
                color = c.SelectedColor,
                size = c.SelectedSize,
                product = p is null ? (object?)new { id = c.ProductId } : new
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
    public async Task<IActionResult> Sync([FromBody] CartSyncRequest body, CancellationToken ct)
    {
        var userId = (body.UserId ?? "").Trim();
        if (userId.Length == 0)
            return BadRequest(new { error = "userId مطلوب" });

        var incoming = (body.Items ?? new List<CartItemRequest>())
            .Where(i => !string.IsNullOrWhiteSpace(i.ProductId))
            .ToList();

        var current = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync(ct);
        var incomingByKey = incoming
            .GroupBy(i => Key(i.ProductId, i.Color ?? i.SelectedColor, i.Size ?? i.SelectedSize), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in current)
        {
            var key = Key(item.ProductId, item.SelectedColor, item.SelectedSize);
            if (!incomingByKey.TryGetValue(key, out var match))
            {
                _db.CartItems.Remove(item);
                continue;
            }
            existingKeys.Add(key);
            var qty = match.Quantity <= 0 ? 1 : match.Quantity;
            if (item.Quantity != qty)
            {
                item.Quantity = qty;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        foreach (var req in incoming)
        {
            var key = Key(req.ProductId, req.Color ?? req.SelectedColor, req.Size ?? req.SelectedSize);
            if (existingKeys.Contains(key))
                continue;
            _db.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = req.ProductId!.Trim(),
                Quantity = req.Quantity <= 0 ? 1 : req.Quantity,
                SelectedColor = req.Color ?? req.SelectedColor,
                SelectedSize = req.Size ?? req.SelectedSize
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    private static string Key(string? productId, string? color, string? size) =>
        $"{productId?.Trim()}|{color ?? ""}|{size ?? ""}";

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] CartItemRequest body, CancellationToken ct)
    {
        var userId = (body.UserId ?? "").Trim();
        var productId = (body.ProductId ?? "").Trim();
        if (userId.Length == 0 || productId.Length == 0)
            return BadRequest(new { error = "userId و productId مطلوبان" });

        var qty = body.Quantity <= 0 ? 1 : body.Quantity;
        var color = body.Color ?? body.SelectedColor;
        var size = body.Size ?? body.SelectedSize;

        var existing = await _db.CartItems.FirstOrDefaultAsync(
            c => c.UserId == userId && c.ProductId == productId
                 && (c.SelectedColor ?? "") == (color ?? "")
                 && (c.SelectedSize ?? "") == (size ?? ""), ct);

        if (existing is null)
            _db.CartItems.Add(new CartItem { UserId = userId, ProductId = productId, Quantity = qty, SelectedColor = color, SelectedSize = size });
        else
        {
            existing.Quantity = qty;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (item is null)
            return NotFound(new { error = "العنصر غير موجود" });

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpDelete]
    public async Task<IActionResult> Clear([FromQuery] string userId, CancellationToken ct)
    {
        var items = await _db.CartItems.Where(c => c.UserId == userId.Trim()).ToListAsync(ct);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, removed = items.Count });
    }
}

public class CartSyncRequest
{
    public string? UserId { get; set; }
    public List<CartItemRequest>? Items { get; set; }
}

public class CartItemRequest
{
    public string? UserId { get; set; }
    public string? ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }
}
