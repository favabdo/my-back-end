using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/abandoned-carts")]
public class AbandonedCartsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public AbandonedCartsController(IApplicationDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Capture([FromBody] AbandonedCartRequest body, CancellationToken ct)
    {
        var key = !string.IsNullOrWhiteSpace(body.UserId) ? body.UserId.Trim()
            : (!string.IsNullOrWhiteSpace(body.Id) ? body.Id.Trim() : null);

        var existingId = key is null
            ? Guid.Empty
            : await _db.AbandonedCarts
                .Where(a => a.UserId == key)
                .Select(a => a.Id)
                .FirstOrDefaultAsync(ct);

        if (existingId != Guid.Empty)
        {
            await _db.AbandonedCartItems
                .Where(i => i.AbandonedCartId == existingId)
                .ExecuteDeleteAsync(ct);

            await _db.AbandonedCarts
                .Where(a => a.Id == existingId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.CustomerName, body.CustomerName ?? "زائر المتجر")
                    .SetProperty(a => a.CustomerPhone, body.CustomerPhone)
                    .SetProperty(a => a.CustomerEmail, body.CustomerEmail)
                    .SetProperty(a => a.Governorate, body.Governorate ?? "غير محدد")
                    .SetProperty(a => a.Total, body.Total)
                    .SetProperty(a => a.UpdatedAt, DateTime.UtcNow), ct);

            if (body.Items is not null)
            {
                foreach (var item in body.Items)
                    _db.AbandonedCartItems.Add(NewItem(existingId, item));
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { success = true, id = existingId });
        }

        var cart = new AbandonedCart
        {
            UserId = key,
            CustomerName = body.CustomerName ?? "زائر المتجر",
            CustomerPhone = body.CustomerPhone,
            CustomerEmail = body.CustomerEmail,
            Governorate = body.Governorate ?? "غير محدد",
            Total = body.Total
        };

        if (body.Items is not null)
        {
            foreach (var item in body.Items)
                cart.Items.Add(new AbandonedCartItem
                {
                    ProductName = item.Name ?? "",
                    Price = item.Price ?? 0,
                    Quantity = item.Quantity <= 0 ? 1 : item.Quantity,
                    Image = item.Image
                });
        }

        _db.AbandonedCarts.Add(cart);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, id = cart.Id });
    }

    private static AbandonedCartItem NewItem(Guid cartId, AbandonedCartItemRequest item) => new()
    {
        AbandonedCartId = cartId,
        ProductName = item.Name ?? "",
        Price = item.Price ?? 0,
        Quantity = item.Quantity <= 0 ? 1 : item.Quantity,
        Image = item.Image
    };

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var carts = await _db.AbandonedCarts.AsNoTracking()
            .Include(a => a.Items)
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        return Ok(carts.Select(a => new
        {
            id = a.Id,
            userId = a.UserId,
            customerName = a.CustomerName,
            customerPhone = a.CustomerPhone,
            customerEmail = a.CustomerEmail,
            governorate = a.Governorate,
            total = a.Total,
            createdAt = a.CreatedAt,
            items = a.Items.Select(i => new { name = i.ProductName, price = i.Price, quantity = i.Quantity, image = i.Image })
        }));
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var cart = await _db.AbandonedCarts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (cart is null)
            return NotFound(new { error = "العربة غير موجودة" });

        _db.AbandonedCarts.Remove(cart);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }
}

public class AbandonedCartRequest
{
    public string? Id { get; set; }
    public string? UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Governorate { get; set; }
    public decimal Total { get; set; }
    public List<AbandonedCartItemRequest>? Items { get; set; }
}

public class AbandonedCartItemRequest
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public int Quantity { get; set; }
    public string? Image { get; set; }
}
