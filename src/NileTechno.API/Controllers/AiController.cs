using Microsoft.AspNetCore.Mvc;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.API.Controllers;

[Route("api/ai")]
public class AiController : ApiControllerBase
{
    private readonly IItemStockQuery _stockQuery;

    public AiController(IItemStockQuery stockQuery)
    {
        _stockQuery = stockQuery;
    }

    [HttpPost("smart-search")]
    public async Task<IActionResult> SmartSearch([FromBody] SmartSearchRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Query))
            return BadRequest(new { error = "استعلام البحث مطلوب" });

        var products = body.Products is { Count: > 0 }
            ? body.Products
            : (await _stockQuery.GetCustomerCatalogAsync(null, body.Query, ct))
                .Select(p => new SmartSearchProduct
                {
                    Id = p.ItemCode,
                    Title = p.ItemName,
                    Name = p.ItemName,
                    Category = p.GroupName
                })
                .ToList();

        if (products.Count == 0)
        {
            return Ok(new
            {
                aiAdvice = "الكتالوج خالٍ حالياً من المنتجات.",
                recommendations = Array.Empty<object>()
            });
        }

        var query = body.Query.Trim();
        var matches = products
            .Select(p => new
            {
                product = p,
                score = Score(query, p)
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(4)
            .ToList();

        if (matches.Count == 0)
        {
            return Ok(new
            {
                aiAdvice = "لا توجد نتائج مطابقة لطلبك.",
                recommendations = Array.Empty<object>()
            });
        }

        return Ok(new
        {
            aiAdvice = $"بناءً على بحثك عن (\"{query}\"):",
            recommendations = matches.Select(m => new
            {
                productId = m.product.Id ?? m.product.ItemCode,
                matchReason = "منتج مطابق لمواصفات وكلمات البحث",
                confidenceScore = 0.9
            })
        });
    }

    private static int Score(string query, SmartSearchProduct product)
    {
        var haystack = $"{product.Title} {product.Name} {product.Category} {product.Id}".ToLowerInvariant();
        var tokens = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Count(t => haystack.Contains(t));
    }
}

public class SmartSearchRequest
{
    public string? Query { get; set; }
    public List<SmartSearchProduct>? Products { get; set; }
}

public class SmartSearchProduct
{
    public string? Id { get; set; }
    public string? ItemCode { get; set; }
    public string? Title { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
}
