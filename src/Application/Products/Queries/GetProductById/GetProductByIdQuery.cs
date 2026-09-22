using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;
