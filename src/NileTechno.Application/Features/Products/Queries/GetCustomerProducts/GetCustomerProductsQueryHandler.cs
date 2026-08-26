using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetCustomerProducts;

public class GetCustomerProductsQueryHandler
    : IRequestHandler<GetCustomerProductsQuery, PaginatedList<CustomerProductCardDto>>
{
    private readonly IItemStockQuery _itemStockQuery;

    public GetCustomerProductsQueryHandler(IItemStockQuery itemStockQuery)
    {
        _itemStockQuery = itemStockQuery;
    }

    public Task<PaginatedList<CustomerProductCardDto>> Handle(
        GetCustomerProductsQuery request,
        CancellationToken cancellationToken)
        => _itemStockQuery.GetCustomerCatalogPageAsync(
            request.GroupId,
            request.Search,
            request.Page,
            cancellationToken);
}
