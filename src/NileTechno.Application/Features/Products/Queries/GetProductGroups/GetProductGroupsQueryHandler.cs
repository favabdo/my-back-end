using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetProductGroups;

public class GetProductGroupsQueryHandler
    : IRequestHandler<GetProductGroupsQuery, IReadOnlyList<ProductGroupDto>>
{
    private readonly IItemStockQuery _itemStockQuery;

    public GetProductGroupsQueryHandler(IItemStockQuery itemStockQuery)
    {
        _itemStockQuery = itemStockQuery;
    }

    public Task<IReadOnlyList<ProductGroupDto>> Handle(
        GetProductGroupsQuery request,
        CancellationToken cancellationToken)
        => _itemStockQuery.GetGroupsAsync(cancellationToken);
}
