using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Domain.ValueObjects;
using PersonalFinanceTracker.Infrastructure.Categorization;
using Xunit;

namespace PersonalFinanceTracker.Infrastructure.Tests.Categorization;

public class RuleBasedCategorizationEngineTests
{
    private static ICategorizationEngine BuildEngineWithAllRules()
    {
        var rules = new ICategorizationRule[]
        {
            // Order matters: more specific first
            new MedicalAidRule(),
            new RetirementContributionRule(),
            new CharitableDonationRule(),
            new SalaryRule(),
            new GroceriesRule(),
            new FuelRule(),
            new TransportRule(),
            new UtilitiesRule(),
            new InvestmentRule(),
            new BankingFeesRule()
        };
        return new RuleBasedCategorizationEngine(rules);
    }

    private static Transaction TxWith(string description) => new()
    {
        Date = DateTime.Today,
        Amount = Money.Zar(100m),
        Description = description,
        Type = TransactionType.Expense
    };

    [Theory]
    [InlineData("DISCOVERY HEALTH DEBIT ORDER", "Medical Aid", TaxCategory.MedicalSchemeFees)]
    [InlineData("MOMENTUM HEALTH PLAN", "Medical Aid", TaxCategory.MedicalSchemeFees)]
    [InlineData("10X RETIREMENT ANNUITY", "Retirement Annuity", TaxCategory.RetirementContribution)]
    [InlineData("SYGNIA PROVIDENT FUND", "Retirement Annuity", TaxCategory.RetirementContribution)]
    [InlineData("GIFT OF THE GIVERS", "Charitable Donation", TaxCategory.CharitableDonation)]
    [InlineData("SALARY XYZ COMPANY", "Salary", TaxCategory.NotApplicable)]
    [InlineData("CHECKERS SANDTON", "Groceries", TaxCategory.PersonalExpense)]
    [InlineData("ENGEN 1 STOP", "Fuel", TaxCategory.PersonalExpense)]
    [InlineData("UBER TRIP", "Transport", TaxCategory.PersonalExpense)]
    [InlineData("ESKOM PREPAID", "Utilities", TaxCategory.PersonalExpense)]
    [InlineData("EASYEQUITIES DEPOSIT", "Investment", TaxCategory.PersonalExpense)]
    public void Categorize_MerchantPatterns_MatchedCorrectly(
        string description, string expectedCategory, TaxCategory expectedTax)
    {
        var engine = BuildEngineWithAllRules();

        var result = engine.Categorize(TxWith(description));

        Assert.Equal(expectedCategory, result.Category);
        Assert.Equal(expectedTax, result.TaxCategory);
    }

    [Fact]
    public void Categorize_NoRuleMatches_ReturnsUncategorized()
    {
        var engine = BuildEngineWithAllRules();

        var result = engine.Categorize(TxWith("SOME RANDOM MERCHANT XYZ"));

        Assert.Equal("Uncategorized", result.Category);
        Assert.Equal(TaxCategory.NotApplicable, result.TaxCategory);
    }

    [Fact]
    public void Categorize_CaseInsensitive()
    {
        var engine = BuildEngineWithAllRules();

        var lower = engine.Categorize(TxWith("discovery health"));
        var upper = engine.Categorize(TxWith("DISCOVERY HEALTH"));
        var mixed = engine.Categorize(TxWith("Discovery Health"));

        Assert.Equal(lower.Category, upper.Category);
        Assert.Equal(upper.Category, mixed.Category);
    }

    [Fact]
    public void Categorize_ChainOfResponsibility_FirstMatchWins()
    {
        // A description that could match multiple rules — first registered should win.
        var rules = new ICategorizationRule[]
        {
            new MedicalAidRule(),      // registered first — should win
            new CharitableDonationRule()
        };
        var engine = new RuleBasedCategorizationEngine(rules);

        // "Discovery health" matches MedicalAidRule; nothing here matches CharitableDonation
        // but if there was an overlap, the first rule wins.
        var result = engine.Categorize(TxWith("DISCOVERY HEALTH"));

        Assert.Equal("Medical Aid", result.Category);
    }

    [Fact]
    public void Categorize_RecordsWhichRuleMatched()
    {
        var engine = BuildEngineWithAllRules();

        var result = engine.Categorize(TxWith("DISCOVERY HEALTH"));

        Assert.Equal(nameof(MedicalAidRule), result.MatchedBy);
    }
}
