using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    int InitialStock) : IRequest<Result<Guid>>;
