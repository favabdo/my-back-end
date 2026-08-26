using MediatR;
using NileTechno.Application.Features.Products.DTOs;

namespace NileTechno.Application.Features.Products.Queries.GetProductGroups;

public sealed record GetProductGroupsQuery : IRequest<IReadOnlyList<ProductGroupDto>>;
