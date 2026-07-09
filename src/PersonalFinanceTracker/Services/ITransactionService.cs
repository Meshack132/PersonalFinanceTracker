using PersonalFinanceTracker.Models;

namespace PersonalFinanceTracker.Services;

public interface ITransactionService
{
    Task<Transaction> AddTransactionAsync(
        TransactionType type,
        decimal amount,
        string description,
        string category,
        DateTime? date = null);

    Task<IEnumerable<Transaction>> GetAllTransactionsAsync();
    Task<bool> DeleteTransactionAsync(Guid id);
    Task<decimal> GetBalanceAsync();
}
