using FreshApi.Application.Common.Interfaces;
using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using FreshApi.Domain.ValueObjects;
using MediatR;

namespace FreshApi.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Product>();

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var existing = await repository.FirstOrDefaultAsync(p => p.Sku == normalizedSku, cancellationToken);
        if (existing is not null)
        {
            return Result<Guid>.Failure(ProductErrors.DuplicateSku(normalizedSku));
        }

        var priceResult = Money.Create(request.Price, request.Currency);
        if (priceResult.IsFailure)
        {
            return Result<Guid>.Failure(priceResult.Error);
        }

        var productResult = Product.Create(request.Name, request.Sku, priceResult.Value, request.InitialStock);
        if (productResult.IsFailure)
        {
            return Result<Guid>.Failure(productResult.Error);
        }

        await repository.AddAsync(productResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productResult.Value.Id;
    }
}
