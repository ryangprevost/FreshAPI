using FreshApi.Application.Common.Interfaces;
using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using MediatR;

namespace FreshApi.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Product>();

        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(request.Id));
        }

        repository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
