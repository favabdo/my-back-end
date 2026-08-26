using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/analytics")]
public class AnalyticsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public AnalyticsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("search")]
    public async Task<IActionResult> TrackSearch([FromBody] TrackSearchRequest body, CancellationToken ct)
    {
        var term = (body.Query ?? "").Trim().ToLowerInvariant();
        if (term.Length > 0)
        {
            var row = await _db.AnalyticsSearches.FirstOrDefaultAsync(x => x.Term == term, ct);
            if (row is null)
                _db.AnalyticsSearches.Add(new AnalyticsSearch { Term = term, Count = 1 });
            else
                row.Count += 1;

            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { success = true });
    }

    [HttpPost("view/{productId}")]
    public async Task<IActionResult> TrackView(string productId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(productId))
        {
            var row = await _db.AnalyticsProductViews.FirstOrDefaultAsync(x => x.ProductId == productId, ct);
            if (row is null)
                _db.AnalyticsProductViews.Add(new AnalyticsProductView { ProductId = productId, Count = 1 });
            else
                row.Count += 1;

            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { success = true });
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report(CancellationToken ct)
    {
        var searches = await _db.AnalyticsSearches.AsNoTracking().ToListAsync(ct);
        var views = await _db.AnalyticsProductViews.AsNoTracking().ToListAsync(ct);
        return Ok(new
        {
            searches = searches.ToDictionary(x => x.Term, x => x.Count),
            productViews = views.ToDictionary(x => x.ProductId, x => x.Count)
        });
    }
}

public class TrackSearchRequest
{
    public string? Query { get; set; }
}
