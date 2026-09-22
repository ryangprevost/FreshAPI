using FreshApi.Application.Common.Interfaces;
using FreshApi.Application.Products.Mappings;
using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FreshApi.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return Result<ProductDto>.Failure(ProductErrors.NotFound(request.Id));
        }

        return product.ToDto();
    }
}
