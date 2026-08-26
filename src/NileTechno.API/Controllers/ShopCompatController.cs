using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;

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
    public IActionResult ShippingMethods() => Ok(Array.Empty<object>());

    [HttpGet("admin/config")]
    public async Task<IActionResult> AdminConfig(CancellationToken ct)
    {
        var settings = await _db.StoreSettingsList.AsNoTracking().FirstOrDefaultAsync(ct);
        return Ok(new
        {
            storeName = settings?.StoreName ?? "المتجر الإلكتروني",
            storeTitle = settings?.StoreTitle ?? "متجرك الإلكتروني",
            promoTagline = settings?.PromoTagline ?? ""
        });
    }
}
