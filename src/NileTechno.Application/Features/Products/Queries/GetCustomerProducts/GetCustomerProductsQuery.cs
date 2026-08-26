using MediatR;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetCustomerProducts;

public sealed record GetCustomerProductsQuery(string? GroupId, string? Search)
    : IRequest<IReadOnlyList<CustomerProductCardDto>>;
