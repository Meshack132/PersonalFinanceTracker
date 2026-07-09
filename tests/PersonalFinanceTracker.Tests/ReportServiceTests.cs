using PersonalFinanceTracker.Models;
using PersonalFinanceTracker.Services;
using Xunit;

namespace PersonalFinanceTracker.Tests;

public class ReportServiceTests
{
    private static async Task<(TransactionService tx, ReportService report)> BuildSystemUnderTestAsync()
    {
        var repo = new InMemoryTransactionRepository();
        var tx = new TransactionService(repo);
        var report = new ReportService(repo);

        await tx.AddTransactionAsync(TransactionType.Income, 5000m, "Salary", "Work", new DateTime(2025, 1, 15));
        await tx.AddTransactionAsync(TransactionType.Expense, 1200m, "Rent", "Housing", new DateTime(2025, 1, 20));
        await tx.AddTransactionAsync(TransactionType.Expense, 400m, "Groceries", "Food", new DateTime(2025, 1, 22));
        await tx.AddTransactionAsync(TransactionType.Expense, 150m, "Restaurant", "Food", new DateTime(2025, 2, 3));

        return (tx, report);
    }

    [Fact]
    public async Task GetSummaryAsync_NoDateFilter_ReturnsAllTransactionsSummary()
    {
        var (_, report) = await BuildSystemUnderTestAsync();

        var summary = await report.GetSummaryAsync();

        Assert.Equal(5000m, summary.TotalIncome);
        Assert.Equal(1750m, summary.TotalExpenses);
        Assert.Equal(3250m, summary.NetBalance);
        Assert.Equal(4, summary.TransactionCount);
    }

    [Fact]
    public async Task GetSummaryAsync_WithDateFilter_ReturnsFilteredSummary()
    {
        var (_, report) = await BuildSystemUnderTestAsync();

        var summary = await report.GetSummaryAsync(
            from: new DateTime(2025, 1, 1),
            to: new DateTime(2025, 1, 31));

        Assert.Equal(5000m, summary.TotalIncome);
        Assert.Equal(1600m, summary.TotalExpenses);
        Assert.Equal(3, summary.TransactionCount);
    }

    [Fact]
    public async Task GetSpendingByCategoryAsync_ReturnsGroupedExpenses()
    {
        var (_, report) = await BuildSystemUnderTestAsync();

        var spending = await report.GetSpendingByCategoryAsync();

        // Only expense categories should appear — "Work" is income and must be excluded.
        Assert.Equal(2, spending.Count);
        Assert.Equal(1200m, spending["Housing"]);
        Assert.Equal(550m, spending["Food"]);
        Assert.DoesNotContain("Work", spending.Keys);
    }

    [Fact]
    public async Task GetTransactionsByPeriodAsync_InvalidRange_Throws()
    {
        var (_, report) = await BuildSystemUnderTestAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            report.GetTransactionsByPeriodAsync(new DateTime(2025, 12, 1), new DateTime(2025, 1, 1)));
    }
}