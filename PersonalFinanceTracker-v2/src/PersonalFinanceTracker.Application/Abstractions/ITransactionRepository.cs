using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<HashSet<string>> GetExistingDedupeKeysAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
