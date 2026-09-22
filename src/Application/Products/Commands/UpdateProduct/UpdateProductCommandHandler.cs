using FreshApi.Application.Common.Interfaces;
using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using FreshApi.Domain.ValueObjects;
using MediatR;

namespace FreshApi.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Product>();

        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(request.Id));
        }

        var priceResult = Money.Create(request.Price, request.Currency);
        if (priceResult.IsFailure)
        {
            return Result.Failure(priceResult.Error);
        }

        var updateResult = product.UpdateDetails(request.Name, priceResult.Value);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        repository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
