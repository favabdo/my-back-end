using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Products.DTOs;
using NileTechno.Application.Features.Stock.DTOs;

namespace NileTechno.Application.Common.Interfaces;

public interface IItemStockQuery
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerProductCardDto>> GetCustomerCatalogAsync(
        string? groupId,
        string? search,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<CustomerProductCardDto>> GetCustomerCatalogPageAsync(
        string? groupId,
        string? search,
        int pageNumber,
        CancellationToken cancellationToken = default);

    Task<CustomerProductCardDto?> GetCustomerProductByCodeAsync(
        string itemCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default);

    Task<PaginatedList<AdminStockItemDto>> GetAdminStockAsync(
        int pageNumber,
        int pageSize,
        string? groupId,
        string? storeCode,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, decimal>> GetQuantitiesByItemCodeAsync(
        CancellationToken cancellationToken = default);
}
