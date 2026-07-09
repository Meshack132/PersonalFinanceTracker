using PersonalFinanceTracker.Models;

namespace PersonalFinanceTracker.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<Transaction?> GetByIdAsync(Guid id);
    Task AddAsync(Transaction transaction);
    Task<bool> DeleteAsync(Guid id);
    Task SaveChangesAsync();
}
