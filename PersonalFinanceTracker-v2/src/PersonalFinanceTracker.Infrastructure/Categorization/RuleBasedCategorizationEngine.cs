using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Infrastructure.Categorization;

/// <summary>
/// Walks through registered rules in order. First rule to match sets the category.
/// If no rule matches, returns <see cref="CategoryResult.Uncategorized"/>.
///
/// This is a classic Chain of Responsibility: adding a new rule = adding a new class,
/// no changes to this engine (Open/Closed principle).
/// </summary>
public class RuleBasedCategorizationEngine : ICategorizationEngine
{
    private readonly IReadOnlyList<ICategorizationRule> _rules;

    public RuleBasedCategorizationEngine(IEnumerable<ICategorizationRule> rules)
    {
        // Preserve registration order — more specific rules should be registered first.
        _rules = rules.ToList();
    }

    public CategoryResult Categorize(Transaction transaction)
    {
        foreach (var rule in _rules)
        {
            var result = rule.Match(transaction);
            if (result.HasValue)
                return result.Value;
        }

        return CategoryResult.Uncategorized;
    }
}
