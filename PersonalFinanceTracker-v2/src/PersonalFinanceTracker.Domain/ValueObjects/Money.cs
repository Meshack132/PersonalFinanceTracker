using System.Globalization;

namespace PersonalFinanceTracker.Domain.ValueObjects;

/// <summary>
/// Value object representing a monetary amount in a specific currency.
/// Enforces currency safety — you cannot add ZAR to USD without an explicit conversion.
/// Immutable. All arithmetic returns a new Money instance.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static readonly string DefaultCurrency = "ZAR";

    public static Money Zar(decimal amount) => new(amount, "ZAR");
    public static Money Zero(string currency = "ZAR") => new(0m, currency);

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator -(Money a) => new(-a.Amount, a.Currency);

    public bool IsNegative => Amount < 0;
    public bool IsPositive => Amount > 0;
    public bool IsZero => Amount == 0;

    public Money Abs() => new(Math.Abs(Amount), Currency);

    /// <summary>
    /// Format as a South African-style currency string (R 1 234.56).
    /// </summary>
    public override string ToString()
    {
        if (Currency == "ZAR")
        {
            var culture = new CultureInfo("en-ZA");
            return string.Format(culture, "R {0:N2}", Amount);
        }
        return $"{Currency} {Amount:N2}";
    }

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot mix currencies: {a.Currency} vs {b.Currency}. Convert first.");
    }
}
