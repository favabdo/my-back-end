using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/custom-reviews")]
public class CustomReviewsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public CustomReviewsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? productId, [FromQuery] string? all, CancellationToken ct)
    {
        var query = _db.Reviews.AsNoTracking().AsQueryable();
        if (!string.Equals(all, "true", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Approved);

        if (!string.IsNullOrWhiteSpace(productId))
            query = query.Where(r => r.ProductId == productId);

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return Ok(reviews.Select(Map));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.ProductId) || body.Rating is null)
            return BadRequest(new { error = "productId و rating مطلوبان" });

        var key = $"{body.OrderId ?? "gen"}-{body.ProductId}";
        var existing = await _db.Reviews.FirstOrDefaultAsync(
            r => r.ExternalId == key || (body.OrderId != null && r.OrderId == body.OrderId && r.ProductId == body.ProductId),
            ct);

        var review = existing ?? new Review { ExternalId = $"{key}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" };
        review.ProductId = body.ProductId;
        review.OrderId = body.OrderId;
        review.Rating = body.Rating.Value;
        review.Comment = body.Comment ?? "";
        review.CustomerName = string.IsNullOrWhiteSpace(body.CustomerName) ? "عميل مميز" : body.CustomerName;
        review.Date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        review.Approved = false;
        review.UpdatedAt = DateTime.UtcNow;

        if (existing is null)
            _db.Reviews.Add(review);

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, review = Map(review) });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id, CancellationToken ct)
    {
        var review = await FindAsync(id, ct);
        if (review is null)
            return NotFound(new { error = "التقييم المطلوب غير موجود" });

        review.Approved = true;
        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var review = await FindAsync(id, ct);
        if (review is null)
            return NotFound(new { error = "التقييم المطلوب غير موجود" });

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    private async Task<Review?> FindAsync(string id, CancellationToken ct) =>
        await _db.Reviews.FirstOrDefaultAsync(
            r => r.ExternalId == id || r.Id.ToString() == id,
            ct);

    private static object Map(Review r) => new
    {
        id = string.IsNullOrWhiteSpace(r.ExternalId) ? r.Id.ToString() : r.ExternalId,
        productId = r.ProductId,
        orderId = r.OrderId,
        rating = r.Rating,
        comment = r.Comment,
        customerName = r.CustomerName,
        date = r.Date,
        approved = r.Approved
    };
}

public class CreateReviewRequest
{
    public string? ProductId { get; set; }
    public string? OrderId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public string? CustomerName { get; set; }
}
