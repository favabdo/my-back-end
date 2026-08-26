using MediatR;
using NileTechno.Application.Common.Models;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetCustomerProducts;

public sealed record GetCustomerProductsQuery(string? GroupId, string? Search, int Page = 1)
    : IRequest<PaginatedList<CustomerProductCardDto>>;
