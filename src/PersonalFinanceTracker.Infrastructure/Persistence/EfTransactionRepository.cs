using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed implementation of <see cref="ITransactionRepository"/> via EF Core.
///
/// Swapping this in for <see cref="JsonTransactionRepository"/> required zero changes
/// to Domain, Application, or any consumer of the interface — that's the payoff of
/// depending on an abstraction rather than a concrete persistence technology.
/// </summary>
public class EfTransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public EfTransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default)
        => await _context.Transactions.AsNoTracking().ToListAsync(ct);

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        await _context.Transactions.AddAsync(transaction, ct);
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken ct = default)
        => await _context.Transactions.AddRangeAsync(transactions, ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return false;

        _context.Transactions.Remove(entity);
        return true;
    }

    public async Task<HashSet<string>> GetExistingDedupeKeysAsync(CancellationToken ct = default)
    {
        // DedupeKey is a computed, unmapped property — pull the handful of columns
        // it depends on and compute client-side rather than trying to translate
        // string formatting into SQL.
        var rows = await _context.Transactions
            .AsNoTracking()
            .Select(t => new { t.Date, t.Amount, t.Description })
            .ToListAsync(ct);

        return rows
            .Select(r => $"{r.Date:yyyyMMdd}|{r.Amount.Amount:F2}|{r.Description.Trim().ToLowerInvariant()}")
            .ToHashSet();
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
