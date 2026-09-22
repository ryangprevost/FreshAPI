using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using FreshApi.Domain.ValueObjects;
using Xunit;

namespace FreshApi.Domain.Tests;

public sealed class ProductTests
{
    private static Money ValidPrice() =>
        Money.Create(19.99m, "USD").Value;

    [Fact]
    public void Create_WithValidData_ReturnsSuccessWithNormalizedValues()
    {
        var result = Product.Create("  Widget  ", "wgt-001", ValidPrice(), 10);

        Assert.True(result.IsSuccess);
        Assert.Equal("Widget", result.Value.Name);
        Assert.Equal("WGT-001", result.Value.Sku);
        Assert.Equal(10, result.Value.StockQuantity);
        Assert.Single(result.Value.DomainEvents);
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailure()
    {
        var result = Product.Create("   ", "WGT-001", ValidPrice(), 10);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidName", result.Error.Code);
    }

    [Fact]
    public void Create_WithNegativeStock_ReturnsFailure()
    {
        var result = Product.Create("Widget", "WGT-001", ValidPrice(), -1);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NegativeStock", result.Error.Code);
    }

    [Fact]
    public void AdjustStock_WhenResultWouldGoNegative_ReturnsFailureAndKeepsStock()
    {
        var product = Product.Create("Widget", "WGT-001", ValidPrice(), 5).Value;

        var result = product.AdjustStock(-10);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InsufficientStock", result.Error.Code);
        Assert.Equal(5, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_WithValidDelta_UpdatesStock()
    {
        var product = Product.Create("Widget", "WGT-001", ValidPrice(), 5).Value;

        Assert.True(product.AdjustStock(3).IsSuccess);
        Assert.True(product.AdjustStock(-8).IsSuccess);
        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ReturnsFailureAndKeepsValues()
    {
        var product = Product.Create("Widget", "WGT-001", ValidPrice(), 5).Value;

        var result = product.UpdateDetails("  ", ValidPrice());

        Assert.True(result.IsFailure);
        Assert.Equal("Widget", product.Name);
    }

    [Fact]
    public void Money_Create_WithNegativeAmount_ReturnsFailure()
    {
        var result = Money.Create(-1m, "USD");

        Assert.True(result.IsFailure);
        Assert.Equal("Money.NegativeAmount", result.Error.Code);
    }

    [Fact]
    public void Money_Create_NormalizesCurrencyToUpperInvariant()
    {
        var result = Money.Create(10m, "usd");

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.Value.Currency);
    }

    [Fact]
    public void Money_WithSameAmountAndCurrency_AreEqual()
    {
        var left = Money.Create(10m, "USD").Value;
        var right = Money.Create(10m, "USD").Value;

        Assert.Equal(left, right);
        Assert.True(left == right);
    }
}
