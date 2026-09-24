using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api/activity-logs")]
public class ActivityLogsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public ActivityLogsController(IApplicationDbContext db) => _db = db;

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 200, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 1000);
        var logs = await _db.ActivityLogs.AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        return Ok(logs.Select(l => new
        {
            id = l.Id,
            actorName = l.ActorName,
            action = l.Action,
            entityType = l.EntityType,
            entityId = l.EntityId,
            createdAt = l.CreatedAt
        }));
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost]
    public async Task<IActionResult> Log([FromBody] ActivityLogRequest body, CancellationToken ct)
    {
        var action = (body.Action ?? "").Trim();
        if (action.Length == 0)
            return BadRequest(new { error = "نص الحركة مطلوب" });

        _db.ActivityLogs.Add(new ActivityLog
        {
            ActorName = string.IsNullOrWhiteSpace(body.ActorName) ? (User.Identity?.Name ?? "مدير") : body.ActorName.Trim(),
            Action = action,
            EntityType = body.EntityType,
            EntityId = body.EntityId
        });
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        var all = await _db.ActivityLogs.ToListAsync(ct);
        _db.ActivityLogs.RemoveRange(all);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, removed = all.Count });
    }
}

public class ActivityLogRequest
{
    public string? ActorName { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}
