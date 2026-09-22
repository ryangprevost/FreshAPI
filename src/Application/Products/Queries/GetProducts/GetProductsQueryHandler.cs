using FreshApi.Application.Common.Interfaces;
using FreshApi.Application.Common.Models;
using FreshApi.Application.Products.Mappings;
using FreshApi.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FreshApi.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? 20 : request.PageSize;

        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ProductDto>(
            items.Select(p => p.ToDto()).ToList(),
            page,
            pageSize,
            totalCount);

        return Result<PagedResult<ProductDto>>.Success(result);
    }
}
