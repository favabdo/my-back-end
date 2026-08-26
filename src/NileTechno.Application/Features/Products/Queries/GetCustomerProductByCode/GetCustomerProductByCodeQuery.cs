using MediatR;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetCustomerProductByCode;

public sealed record GetCustomerProductByCodeQuery(string ItemCode)
    : IRequest<CustomerProductCardDto?>;
