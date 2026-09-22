using FreshApi.Domain.Entities;

namespace FreshApi.Application.Products.Mappings;

/// <summary>
/// Manual mapping keeps the dependency graph small. Swap for Mapster/AutoMapper
/// if the read model grows beyond a handful of DTOs.
/// </summary>
public static class ProductMapping
{
    public static ProductDto ToDto(this Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductDto(
            product.Id,
            product.Name,
            product.Sku,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity,
            product.CreatedAt);
    }
}
