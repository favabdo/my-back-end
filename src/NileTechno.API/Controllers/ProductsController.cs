using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileTechno.Application.Features.Products.Queries.GetCustomerProductByCode;
using NileTechno.Application.Features.Products.Queries.GetCustomerProducts;

namespace NileTechno.API.Controllers;

public class ProductsController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? groupId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 0,
        [FromQuery] string? deviceType = null,
        [FromHeader(Name = "X-Device-Type")] string? headerDeviceType = null)
    {
        // Use header deviceType if query param not provided
        if (string.IsNullOrEmpty(deviceType) && !string.IsNullOrEmpty(headerDeviceType))
            deviceType = headerDeviceType;

        var result = await Mediator.Send(new GetCustomerProductsQuery(groupId, search, page, pageSize, deviceType));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{itemCode}")]
    public async Task<IActionResult> GetByCode(string itemCode)
    {
        var item = await Mediator.Send(new GetCustomerProductByCodeQuery(itemCode));
        if (item is null)
            return NotFound(new { title = "الصنف غير موجود", status = 404 });

        return Ok(item);
    }
}
