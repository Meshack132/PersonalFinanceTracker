using System.Text.Json;
using PersonalFinanceTracker.Models;

namespace PersonalFinanceTracker.Repositories;

/// <summary>
/// Persists transactions to a JSON file on disk.
/// Simple, no DB dependencies; good enough for a personal tracker.
/// </summary>
public class JsonTransactionRepository : ITransactionRepository
{
    private readonly string _filePath;
    private readonly List<Transaction> _transactions;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
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
            return JsonSerializer.Deserialize<List<Transaction>>(json, _jsonOptions)
                   ?? new List<Transaction>();
        }
        catch (JsonException)
        {
            // Corrupted file — start clean rather than crash
            Console.WriteLine($"Warning: {_filePath} is corrupted. Starting with empty data.");
            return new List<Transaction>();
        }
    }

    public Task<IEnumerable<Transaction>> GetAllAsync()
        => Task.FromResult<IEnumerable<Transaction>>(_transactions.ToList());

    public Task<Transaction?> GetByIdAsync(Guid id)
        => Task.FromResult(_transactions.FirstOrDefault(t => t.Id == id));

    public Task AddAsync(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var transaction = _transactions.FirstOrDefault(t => t.Id == id);
        if (transaction is null)
            return Task.FromResult(false);

        _transactions.Remove(transaction);
        return Task.FromResult(true);
    }

    public async Task SaveChangesAsync()
    {
        var json = JsonSerializer.Serialize(_transactions, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
