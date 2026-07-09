using PersonalFinanceTracker.Models;
using PersonalFinanceTracker.Repositories;

namespace PersonalFinanceTracker.Services;

public class ReportService : IReportService
{
    private readonly ITransactionRepository _repository;

    public ReportService(ITransactionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetSpendingByCategoryAsync(
        DateTime? from = null, DateTime? to = null)
    {
        var transactions = await _repository.GetAllAsync();

        return FilterByDate(transactions, from, to)
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
    }

    public async Task<FinancialSummary> GetSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        var transactions = await _repository.GetAllAsync();
        var filtered = FilterByDate(transactions, from, to).ToList();

        var income = filtered.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = filtered.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        return new FinancialSummary(
            TotalIncome: income,
            TotalExpenses: expenses,
            NetBalance: income - expenses,
            From: from,
            To: to,
            TransactionCount: filtered.Count
        );
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByPeriodAsync(DateTime from, DateTime to)
    {
        if (from > to)
            throw new ArgumentException("'from' must be earlier than 'to'.");

        var transactions = await _repository.GetAllAsync();
        return transactions
            .Where(t => t.Date >= from && t.Date <= to)
            .OrderBy(t => t.Date);
    }

    private static IEnumerable<Transaction> FilterByDate(
        IEnumerable<Transaction> transactions, DateTime? from, DateTime? to)
    {
        if (from.HasValue)
            transactions = transactions.Where(t => t.Date >= from.Value);
        if (to.HasValue)
            transactions = transactions.Where(t => t.Date <= to.Value);
        return transactions;
    }
}
