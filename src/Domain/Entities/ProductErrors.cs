using FreshApi.Domain.Common;

namespace FreshApi.Domain.Entities;

public static class ProductErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Product.NotFound", $"Product with id '{id}' was not found.");

    public static Error DuplicateSku(string sku) =>
        Error.Conflict("Product.DuplicateSku", $"A product with SKU '{sku}' already exists.");

    public static Error InvalidName =>
        Error.Validation("Product.InvalidName", "Product name must not be empty.");

    public static Error NameTooLong =>
        Error.Validation("Product.NameTooLong", $"Product name must not exceed {Product.MaxNameLength} characters.");

    public static Error InvalidSku =>
        Error.Validation("Product.InvalidSku", "Product SKU must not be empty.");

    public static Error SkuTooLong =>
        Error.Validation("Product.SkuTooLong", $"Product SKU must not exceed {Product.MaxSkuLength} characters.");

    public static Error NegativeStock =>
        Error.Validation("Product.NegativeStock", "Initial stock quantity cannot be negative.");

    public static Error InsufficientStock =>
        Error.Validation("Product.InsufficientStock", "Not enough stock for the requested adjustment.");
}
