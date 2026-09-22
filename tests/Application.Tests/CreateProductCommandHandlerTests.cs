using System.Linq.Expressions;
using FreshApi.Application.Common.Interfaces;
using FreshApi.Application.Products.Commands.CreateProduct;
using FreshApi.Domain.Entities;
using NSubstitute;
using Xunit;

namespace FreshApi.Application.Tests;

public sealed class CreateProductCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Product> _repository;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _repository = Substitute.For<IRepository<Product>>();
        _unitOfWork.Repository<Product>().Returns(_repository);
        _handler = new CreateProductCommandHandler(_unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesProductAndSaves()
    {
        _repository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var result = await _handler.Handle(
            new CreateProductCommand("Widget", "WGT-001", 19.99m, "USD", 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateSku_ReturnsFailureWithoutSaving()
    {
        var existing = Product.Create("Old Widget", "WGT-001",
            Domain.ValueObjects.Money.Create(9.99m, "USD").Value, 1).Value;

        _repository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(
            new CreateProductCommand("New Widget", "wgt-001", 19.99m, "USD", 10),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.DuplicateSku", result.Error.Code);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPrice_ReturnsFailureWithoutSaving()
    {
        _repository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var result = await _handler.Handle(
            new CreateProductCommand("Widget", "WGT-001", -5m, "USD", 10),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Money.NegativeAmount", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
