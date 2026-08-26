using MediatR;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Stock.DTOs;

namespace NileTechno.Application.Features.Stock.Queries.GetAdminStock;

public class GetAdminStockQueryHandler
    : IRequestHandler<GetAdminStockQuery, PaginatedList<AdminStockItemDto>>
{
    private readonly IItemStockQuery _itemStockQuery;

    public GetAdminStockQueryHandler(IItemStockQuery itemStockQuery)
    {
        _itemStockQuery = itemStockQuery;
    }

    public Task<PaginatedList<AdminStockItemDto>> Handle(
        GetAdminStockQuery request,
        CancellationToken cancellationToken)
        => _itemStockQuery.GetAdminStockAsync(
            request.PageNumber,
            request.PageSize,
            request.GroupId,
            request.StoreCode,
            request.Search,
            cancellationToken);
}
