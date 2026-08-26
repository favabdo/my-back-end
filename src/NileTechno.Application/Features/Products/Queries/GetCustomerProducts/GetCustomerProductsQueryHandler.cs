using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetCustomerProducts;

public class GetCustomerProductsQueryHandler
    : IRequestHandler<GetCustomerProductsQuery, IReadOnlyList<CustomerProductCardDto>>
{
    private readonly IItemStockQuery _itemStockQuery;

    public GetCustomerProductsQueryHandler(IItemStockQuery itemStockQuery)
    {
        _itemStockQuery = itemStockQuery;
    }

    public Task<IReadOnlyList<CustomerProductCardDto>> Handle(
        GetCustomerProductsQuery request,
        CancellationToken cancellationToken)
        => _itemStockQuery.GetCustomerCatalogAsync(request.GroupId, request.Search, cancellationToken);
}
