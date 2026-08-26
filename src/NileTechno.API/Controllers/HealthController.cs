using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Infrastructure.Persistence;

namespace NileTechno.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public HealthController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        bool canConnect;
        try
        {
            canConnect = await _db.Database.CanConnectAsync();
        }
        catch
        {
            canConnect = false;
        }

        return Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow.ToString("o"),
            timeUtc = DateTime.UtcNow,
            database = canConnect ? "connected" : "not-connected"
        });
    }
}
