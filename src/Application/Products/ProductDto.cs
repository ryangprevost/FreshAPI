namespace FreshApi.Application.Products;

/// <summary>
/// Read model for products. Kept as a record: immutable, cheap to construct.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    DateTimeOffset CreatedAt);
