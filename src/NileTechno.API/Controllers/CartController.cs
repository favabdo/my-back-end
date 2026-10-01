using Microsoft.AspNetCore.Mvc;
using NileTechno.Infrastructure.Services;

namespace NileTechno.API.Controllers;

[Route("api/cart")]
public class CartController : ApiControllerBase
{
    private readonly CartStore _cart;

    public CartController(CartStore cart) => _cart = cart;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Ok(new List<object>());

        var rows = await _cart.GetByUserAsync(userId.Trim(), ct);

        return Ok(rows.Select(r => (object)new
        {
            id = r.Id,
            productId = r.Code,
            quantity = r.Qty,
            color = r.Color,
            size = r.Size,
            product = new
            {
                id = r.Code,
                itemCode = r.Code,
                name = r.Name,
                title = r.Name,
                price = r.UnitPrice,
                image = r.Image ?? "",
                stock = r.Stock,
                groupId = r.GroupCode,
                category = r.GroupName
            }
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
            .Select(i => new CartStore.Incoming(i.ProductId!.Trim(), i.Quantity, i.Color ?? i.SelectedColor, i.Size ?? i.SelectedSize))
            .ToList();

        var (replaced, removed) = await _cart.ReplaceAllAsync(userId, incoming, ct);
        return Ok(new { success = true, replaced, removed });
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] CartItemRequest body, CancellationToken ct)
    {
        var userId = (body.UserId ?? "").Trim();
        var productId = (body.ProductId ?? "").Trim();
        if (userId.Length == 0 || productId.Length == 0)
            return BadRequest(new { error = "userId و productId مطلوبان" });

        var (ok, error) = await _cart.UpsertAsync(userId,
            new CartStore.Incoming(productId, body.Quantity, body.Color ?? body.SelectedColor, body.Size ?? body.SelectedSize), ct);

        return ok ? Ok(new { success = true }) : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var removed = await _cart.DeleteAsync(id, ct);
        return removed > 0 ? Ok(new { success = true }) : NotFound(new { error = "العنصر غير موجود" });
    }

    [HttpDelete]
    public async Task<IActionResult> Clear([FromQuery] string userId, CancellationToken ct)
    {
        var removed = await _cart.ClearAsync(userId.Trim(), ct);
        return Ok(new { success = true, removed });
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
