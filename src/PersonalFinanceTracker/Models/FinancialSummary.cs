namespace PersonalFinanceTracker.Models;

public record FinancialSummary(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    DateTime? From,
    DateTime? To,
    int TransactionCount
);
