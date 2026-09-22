using FreshApi.Domain.Common;
using FreshApi.Domain.Events;
using FreshApi.Domain.ValueObjects;

namespace FreshApi.Domain.Entities;

/// <summary>
/// Sample aggregate root. All state changes go through factory/behavior methods
/// so invariants are enforced in one place.
/// </summary>
public sealed class Product : Entity
{
    public const int MaxNameLength = 200;
    public const int MaxSkuLength = 50;

    public string Name { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public Money Price { get; private set; } = null!;

    public int StockQuantity { get; private set; }

    // Required by EF Core.
    private Product()
    {
    }

    private Product(string name, string sku, Money price, int stockQuantity)
    {
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public static Result<Product> Create(string name, string sku, Money price, int initialStock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Product>.Failure(ProductErrors.InvalidName);
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Result<Product>.Failure(ProductErrors.NameTooLong);
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result<Product>.Failure(ProductErrors.InvalidSku);
        }

        if (sku.Trim().Length > MaxSkuLength)
        {
            return Result<Product>.Failure(ProductErrors.SkuTooLong);
        }

        if (initialStock < 0)
        {
            return Result<Product>.Failure(ProductErrors.NegativeStock);
        }

        var product = new Product(name.Trim(), sku.Trim().ToUpperInvariant(), price, initialStock);
        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));

        return product;
    }

    public Result UpdateDetails(string name, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(ProductErrors.InvalidName);
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Result.Failure(ProductErrors.NameTooLong);
        }

        ArgumentNullException.ThrowIfNull(price);

        Name = name.Trim();
        Price = price;

        return Result.Success();
    }

    /// <summary>
    /// Adjusts stock by <paramref name="delta"/> (positive to add, negative to remove).
    /// Fails instead of letting stock go negative.
    /// </summary>
    public Result AdjustStock(int delta)
    {
        if (StockQuantity + delta < 0)
        {
            return Result.Failure(ProductErrors.InsufficientStock);
        }

        StockQuantity += delta;

        return Result.Success();
    }
}
