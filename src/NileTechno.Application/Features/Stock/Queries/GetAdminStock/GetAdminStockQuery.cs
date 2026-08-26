using MediatR;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Stock.DTOs;

namespace NileTechno.Application.Features.Stock.Queries.GetAdminStock;

public sealed record GetAdminStockQuery(
    int PageNumber = 1,
    int PageSize = 50,
    string? GroupId = null,
    string? StoreCode = null,
    string? Search = null) : IRequest<PaginatedList<AdminStockItemDto>>;
