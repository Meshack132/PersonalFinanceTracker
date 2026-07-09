using System.Text.RegularExpressions;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Infrastructure.Categorization;

/// <summary>
/// Base class for regex-driven merchant categorization.
/// Subclass, provide keywords + category + tax bucket, done.
/// </summary>
public abstract class KeywordRule : ICategorizationRule
{
    protected abstract IReadOnlyList<string> Keywords { get; }
    protected abstract string Category { get; }
    protected virtual TaxCategory TaxCategory => TaxCategory.PersonalExpense;
    protected virtual string RuleName => GetType().Name;

    public CategoryResult? Match(Transaction transaction)
    {
        var desc = transaction.Description.ToLowerInvariant();
        foreach (var keyword in Keywords)
        {
            if (desc.Contains(keyword.ToLowerInvariant()))
                return new CategoryResult(Category, TaxCategory, RuleName);
        }
        return null;
    }
}

// ─── SARS-relevant categories (tax-deductible or credit-eligible) ────────────

/// <summary>Section 6A — medical scheme fees tax credit.</summary>
public class MedicalAidRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "discovery health", "momentum health", "bonitas", "medihelp",
        "gems", "polmed", "bestmed", "fedhealth", "medical aid", "medshield"
    };
    protected override string Category => "Medical Aid";
    protected override TaxCategory TaxCategory => TaxCategory.MedicalSchemeFees;
}

/// <summary>Section 11F — retirement fund contributions.</summary>
public class RetirementContributionRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "retirement annuity", "provident fund", "pension fund",
        "10x", "sygnia", "allan gray ra", "coronation ra"
    };
    protected override string Category => "Retirement Annuity";
    protected override TaxCategory TaxCategory => TaxCategory.RetirementContribution;
}

/// <summary>Section 18A — donations to approved PBOs.</summary>
public class CharitableDonationRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "gift of the givers", "smile foundation", "unicef", "red cross",
        "salvation army", "world wildlife", "wwf", "rise against hunger"
    };
    protected override string Category => "Charitable Donation";
    protected override TaxCategory TaxCategory => TaxCategory.CharitableDonation;
}

// ─── Non-deductible but budget-useful categories ─────────────────────────────

public class GroceriesRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "checkers", "pick n pay", "pnp", "woolworths", "spar",
        "shoprite", "food lover", "boxer", "makro", "game stores"
    };
    protected override string Category => "Groceries";
}

public class FuelRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "engen", "shell", "bp ", "sasol", "total energies", "caltex", "astron"
    };
    protected override string Category => "Fuel";
}

public class TransportRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "uber", "bolt", "gautrain", "prasa", "e-toll", "sanral"
    };
    protected override string Category => "Transport";
}

public class UtilitiesRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "eskom", "city of joburg", "city of cape town", "ekurhuleni",
        "tshwane", "vodacom", "mtn", "cell c", "telkom", "rain",
        "afrihost", "webafrica", "vumatel"
    };
    protected override string Category => "Utilities";
}

public class BankingFeesRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "monthly fee", "service fee", "atm fee", "sms notification",
        "cash withdrawal fee", "card replacement"
    };
    protected override string Category => "Banking Fees";
}

public class InvestmentRule : KeywordRule
{
    protected override IReadOnlyList<string> Keywords => new[]
    {
        "easyequities", "satrix", "coronation", "allan gray", "ninety one",
        "sanlam investments", "old mutual invest", "psg wealth"
    };
    protected override string Category => "Investment";
}

/// <summary>
/// Regex-based salary detector — catches "SALARY XXX", "PAYROLL", or the recurring
/// month-end deposit pattern.
/// </summary>
public class SalaryRule : ICategorizationRule
{
    private static readonly Regex SalaryPattern = new(
        @"\b(salary|salaris|payroll|wages|remuneration)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CategoryResult? Match(Transaction transaction)
    {
        return SalaryPattern.IsMatch(transaction.Description)
            ? new CategoryResult("Salary", TaxCategory.NotApplicable, nameof(SalaryRule))
            : null;
    }
}
