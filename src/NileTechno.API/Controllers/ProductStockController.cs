using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/product-stock")]
public class ProductStockController : ApiControllerBase
{
    private readonly IItemStockQuery _stockQuery;
    private readonly IApplicationDbContext _db;

    public ProductStockController(IItemStockQuery stockQuery, IApplicationDbContext db)
    {
        _stockQuery = stockQuery;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var map = await BuildMapAsync(ct);
        return Ok(map);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var map = await BuildMapAsync(ct);
        var stock = map.TryGetValue(id, out var qty) ? qty : 0;
        return Ok(new { productId = id, stock });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost("decrement")]
    public async Task<IActionResult> Decrement([FromBody] DecrementStockRequest body, CancellationToken ct)
    {
        if (body.Items is null)
            return BadRequest(new { error = "قائمة المنتجات (items) مطلوبة" });

        var map = await BuildMapAsync(ct);
        foreach (var item in body.Items)
        {
            var code = item.ProductId ?? "";
            var qty = item.Quantity <= 0 ? 1 : item.Quantity;
            var current = map.TryGetValue(code, out var value) ? value : 0;
            await UpsertOverrideAsync(code, Math.Max(0, current - qty), ct);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, stocks = await BuildMapAsync(ct) });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateStockRequest body, CancellationToken ct)
    {
        if (body.Stock is null)
            return BadRequest(new { error = "حقل المخزون مطلوب" });

        var qty = Math.Max(0, body.Stock.Value);
        await UpsertOverrideAsync(id, qty, ct);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, productId = id, stock = qty });
    }

    private async Task<Dictionary<string, decimal>> BuildMapAsync(CancellationToken ct)
    {
        var map = (await _stockQuery.GetQuantitiesByItemCodeAsync(ct)).ToDictionary(
            kv => kv.Key,
            kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);

        var overrides = await _db.StockOverrides.AsNoTracking().ToListAsync(ct);
        foreach (var item in overrides)
            map[item.ItemCode] = item.Quantity;

        return map;
    }

    private async Task UpsertOverrideAsync(string itemCode, decimal quantity, CancellationToken ct)
    {
        var existing = await _db.StockOverrides.FirstOrDefaultAsync(x => x.ItemCode == itemCode, ct);
        if (existing is null)
            _db.StockOverrides.Add(new StockOverride { ItemCode = itemCode, Quantity = quantity });
        else
        {
            existing.Quantity = quantity;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}

public class UpdateStockRequest
{
    public decimal? Stock { get; set; }
}

public class DecrementStockRequest
{
    public List<DecrementStockItem>? Items { get; set; }
}

public class DecrementStockItem
{
    public string? ProductId { get; set; }
    public decimal Quantity { get; set; }
}
