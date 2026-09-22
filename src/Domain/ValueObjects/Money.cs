using FreshApi.Domain.Common;

namespace FreshApi.Domain.ValueObjects;

/// <summary>
/// Money value object: an amount plus an ISO 4217 currency code.
/// Amounts are always non-negative; currency codes are normalized to upper case.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    public string Currency { get; }

    // Required by EF Core.
    private Money()
    {
        Currency = string.Empty;
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return Result<Money>.Failure(MoneyErrors.NegativeAmount);
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result<Money>.Failure(MoneyErrors.InvalidCurrency);
        }

        return new Money(decimal.Round(amount, 2), currency.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
