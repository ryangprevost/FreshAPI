using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;
