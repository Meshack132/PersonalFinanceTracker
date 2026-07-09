using PersonalFinanceTracker.Models;
using PersonalFinanceTracker.Repositories;

namespace PersonalFinanceTracker.Tests;

/// <summary>
/// Fake repository used by the unit tests so we don't touch the file system.
/// </summary>
internal class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _transactions = new();

    public Task<IEnumerable<Transaction>> GetAllAsync()
        => Task.FromResult<IEnumerable<Transaction>>(_transactions.ToList());

    public Task<Transaction?> GetByIdAsync(Guid id)
        => Task.FromResult(_transactions.FirstOrDefault(t => t.Id == id));

    public Task AddAsync(Transaction transaction)
    {
        _transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var t = _transactions.FirstOrDefault(x => x.Id == id);
        if (t is null) return Task.FromResult(false);
        _transactions.Remove(t);
        return Task.FromResult(true);
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}
