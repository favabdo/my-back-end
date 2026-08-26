using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileTechno.Application.Features.Stock.Queries.GetAdminStock;

namespace NileTechno.API.Controllers;

[Authorize(Roles = "Admin,MainAdmin")]
public class StockController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? groupId = null,
        [FromQuery] string? storeCode = null,
        [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetAdminStockQuery(pageNumber, pageSize, groupId, storeCode, search));
        return Ok(result);
    }
}
