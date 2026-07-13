using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Infrastructure.Persistence;

/// <summary>
/// JSON-file-backed transaction repository.
/// Load-once, mutate in memory, save on demand. Good enough for a personal
/// tracker; swap for SQLite/EF Core when concurrency matters.
/// </summary>
public class JsonTransactionRepository : ITransactionRepository
{
    private readonly string _filePath;
    private readonly List<Transaction> _transactions;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public JsonTransactionRepository(string filePath = "transactions.json")
    {
        _filePath = filePath;
        _transactions = LoadFromFile();
    }

    private List<Transaction> LoadFromFile()
    {
        if (!File.Exists(_filePath))
            return new List<Transaction>();

        try
        {
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<Transaction>();

            return JsonSerializer.Deserialize<List<Transaction>>(json, JsonOptions)
                   ?? new List<Transaction>();
        }
        catch (JsonException)
        {
            Console.WriteLine($"Warning: '{_filePath}' is corrupted. Starting with empty data.");
            return new List<Transaction>();
        }
    }

    public Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Transaction>>(_transactions.ToList());

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_transactions.FirstOrDefault(t => t.Id == id));

    public Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken ct = default)
    {
        _transactions.AddRange(transactions);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var t = _transactions.FirstOrDefault(x => x.Id == id);
        if (t is null) return Task.FromResult(false);
        _transactions.Remove(t);
        return Task.FromResult(true);
    }

    public Task<HashSet<string>> GetExistingDedupeKeysAsync(CancellationToken ct = default)
        => Task.FromResult(_transactions.Select(t => t.DedupeKey).ToHashSet());

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(_transactions, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }
}
