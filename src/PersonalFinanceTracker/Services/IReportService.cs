using PersonalFinanceTracker.Models;

namespace PersonalFinanceTracker.Services;

public interface IReportService
{
    Task<IReadOnlyDictionary<string, decimal>> GetSpendingByCategoryAsync(
        DateTime? from = null, DateTime? to = null);

    Task<FinancialSummary> GetSummaryAsync(DateTime? from = null, DateTime? to = null);

    Task<IEnumerable<Transaction>> GetTransactionsByPeriodAsync(DateTime from, DateTime to);
}
