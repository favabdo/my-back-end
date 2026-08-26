using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileTechno.Application.Features.Products.Queries.GetProductGroups;

namespace NileTechno.API.Controllers;

public class CategoriesController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var groups = await Mediator.Send(new GetProductGroupsQuery());
        return Ok(groups);
    }
}
