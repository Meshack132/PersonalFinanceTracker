using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Domain.ValueObjects;

namespace PersonalFinanceTracker.Domain.Entities;

/// <summary>
/// A single financial transaction.
/// Uses <see cref="Money"/> for currency-safe amounts and <see cref="TaxCategory"/>
/// for SARS deduction tracking.
/// </summary>
public class Transaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Date { get; init; }
    public Money Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "Uncategorized";
    public TransactionType Type { get; init; }
    public TaxCategory TaxCategory { get; init; } = TaxCategory.NotApplicable;

    /// <summary>Origin bank if the transaction was imported (e.g., "StandardBank", "FNB").</summary>
    public string? SourceBank { get; init; }

    /// <summary>
    /// Deterministic hash used for deduplication when re-importing statements.
    /// Two transactions with the same date, amount, and description are considered duplicates.
    /// </summary>
    public string DedupeKey =>
        $"{Date:yyyyMMdd}|{Amount.Amount:F2}|{Description.Trim().ToLowerInvariant()}";

    public override string ToString()
    {
        var sign = Type == TransactionType.Income ? "+" : "-";
        var taxTag = TaxCategory == TaxCategory.NotApplicable ? "" : $" [{TaxCategory}]";
        return $"{Date:yyyy-MM-dd} | {sign}{Amount} | {Category,-20}{taxTag} | {Description}";
    }
}
