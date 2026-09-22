using FreshApi.Application.Common.Models;
using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    string? SearchTerm,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<ProductDto>>>;
