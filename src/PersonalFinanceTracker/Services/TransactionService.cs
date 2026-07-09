using PersonalFinanceTracker.Models;
using PersonalFinanceTracker.Repositories;

namespace PersonalFinanceTracker.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Transaction> AddTransactionAsync(
        TransactionType type,
        decimal amount,
        string description,
        string category,
        DateTime? date = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        var transaction = new Transaction
        {
            Type = type,
            Amount = amount,
            Description = description.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "Uncategorized" : category.Trim(),
            Date = date ?? DateTime.Now
        };

        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();
        return transaction;
    }

    public Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
        => _repository.GetAllAsync();

    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (deleted)
            await _repository.SaveChangesAsync();
        return deleted;
    }

    public async Task<decimal> GetBalanceAsync()
    {
        var transactions = await _repository.GetAllAsync();
        return transactions.Sum(t => t.SignedAmount);
    }
}
