using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;

namespace NileTechno.API.Controllers;

[Route("api")]
public class ShopCompatController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public ShopCompatController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("payment-methods")]
    public IActionResult PaymentMethods() => Ok(new[]
    {
        new
        {
            id = "cod",
            name = "الدفع عند الاستلام كاش",
            description = "ادفع نقداً عند استلام طلبك من مندوب التوصيل بعد فحصه بالكامل."
        }
    });

    [HttpGet("shipping-methods")]
    public async Task<IActionResult> ShippingMethods(CancellationToken ct)
    {
        var zones = await _db.ShippingZones.AsNoTracking()
            .Where(z => z.Active)
            .OrderBy(z => z.Name)
            .ToListAsync(ct);

        if (zones.Count == 0)
            return Ok(Array.Empty<object>());

        return Ok(zones.Select(z => new
        {
            id = z.Code,
            name = z.Name,
            price = z.Price,
            description = $"التوصيل إلى {z.Name}"
        }));
    }

    [HttpGet("admin/config")]
    public async Task<IActionResult> AdminConfig(CancellationToken ct)
    {
        var settings = await _db.StoreSettingsList.AsNoTracking().FirstOrDefaultAsync(ct);
        return Ok(new
        {
            storeName = settings?.StoreName ?? "المتجر الإلكتروني",
            storeTitle = settings?.StoreTitle ?? "متجرك الإلكتروني",
            promoTagline = settings?.PromoTagline ?? "",
            logoUrl = settings?.LogoUrl ?? "",
            primaryColor = settings?.PrimaryColor ?? "",
            freeShippingMin = settings?.FreeShippingMin ?? 0,
            announcementText = settings?.AnnouncementText ?? "",
            announcementEnabled = settings?.AnnouncementEnabled ?? false,
            extraJson = settings?.ExtraJson
        });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPut("admin/config")]
    public async Task<IActionResult> SaveAdminConfig([FromBody] StoreSettingsRequest body, CancellationToken ct)
    {
        var settings = await _db.StoreSettingsList.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new StoreSettings();
            _db.StoreSettingsList.Add(settings);
        }

        if (body.StoreName is not null) settings.StoreName = body.StoreName;
        if (body.StoreTitle is not null) settings.StoreTitle = body.StoreTitle;
        if (body.PromoTagline is not null) settings.PromoTagline = body.PromoTagline;
        if (body.LogoUrl is not null) settings.LogoUrl = body.LogoUrl;
        if (body.PrimaryColor is not null) settings.PrimaryColor = body.PrimaryColor;
        if (body.FreeShippingMin is not null) settings.FreeShippingMin = body.FreeShippingMin.Value;
        if (body.AnnouncementText is not null) settings.AnnouncementText = body.AnnouncementText;
        if (body.AnnouncementEnabled is not null) settings.AnnouncementEnabled = body.AnnouncementEnabled.Value;
        if (body.ExtraJson is not null) settings.ExtraJson = body.ExtraJson;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }
}

public class StoreSettingsRequest
{
    public string? StoreName { get; set; }
    public string? StoreTitle { get; set; }
    public string? PromoTagline { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public decimal? FreeShippingMin { get; set; }
    public string? AnnouncementText { get; set; }
    public bool? AnnouncementEnabled { get; set; }
    public string? ExtraJson { get; set; }
}
