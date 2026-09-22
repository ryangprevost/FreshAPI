using FreshApi.Domain.Common;

namespace FreshApi.Domain.ValueObjects;

public static class MoneyErrors
{
    public static Error NegativeAmount =>
        Error.Validation("Money.NegativeAmount", "Amount cannot be negative.");

    public static Error InvalidCurrency =>
        Error.Validation("Money.InvalidCurrency", "Currency must be a 3-letter ISO code (e.g. USD).");
}
