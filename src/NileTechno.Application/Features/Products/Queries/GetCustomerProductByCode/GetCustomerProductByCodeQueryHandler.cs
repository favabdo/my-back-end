using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetCustomerProductByCode;

public class GetCustomerProductByCodeQueryHandler
    : IRequestHandler<GetCustomerProductByCodeQuery, CustomerProductCardDto?>
{
    private readonly IItemStockQuery _itemStockQuery;

    public GetCustomerProductByCodeQueryHandler(IItemStockQuery itemStockQuery)
    {
        _itemStockQuery = itemStockQuery;
    }

    public Task<CustomerProductCardDto?> Handle(
        GetCustomerProductByCodeQuery request,
        CancellationToken cancellationToken)
        => _itemStockQuery.GetCustomerProductByCodeAsync(request.ItemCode, cancellationToken);
}
