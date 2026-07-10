using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.Abstractions;

/// <summary>
/// Applies a chain of rules to determine the best category and SARS tax bucket
/// for a transaction. First rule to match wins (Chain of Responsibility).
/// </summary>
public interface ICategorizationEngine
{
    CategoryResult Categorize(Transaction transaction);
}

/// <summary>
/// One rule in the categorization chain.
/// Rules are ordered by specificity — most specific first.
/// </summary>
public interface ICategorizationRule
{
    /// <summary>Returns null if this rule doesn't apply; otherwise the result.</summary>
    CategoryResult? Match(Transaction transaction);
}

public readonly record struct CategoryResult(string Category, TaxCategory TaxCategory, string MatchedBy)
{
    public static readonly CategoryResult Uncategorized =
        new("Uncategorized", TaxCategory.NotApplicable, "default");
}
