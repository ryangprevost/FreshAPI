using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price,
    string Currency) : IRequest<Result>;
