using PersonalFinanceTracker.Models;
using PersonalFinanceTracker.Services;
using Xunit;

namespace PersonalFinanceTracker.Tests;

public class TransactionServiceTests
{
    [Fact]
    public async Task AddTransactionAsync_ValidInput_ReturnsTransactionWithId()
    {
        var service = new TransactionService(new InMemoryTransactionRepository());

        var t = await service.AddTransactionAsync(TransactionType.Income, 1000m, "Salary", "Work");

        Assert.NotEqual(Guid.Empty, t.Id);
        Assert.Equal(1000m, t.Amount);
        Assert.Equal("Salary", t.Description);
        Assert.Equal(TransactionType.Income, t.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task AddTransactionAsync_NonPositiveAmount_ThrowsArgumentException(decimal amount)
    {
        var service = new TransactionService(new InMemoryTransactionRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddTransactionAsync(TransactionType.Expense, amount, "Test", "Test"));
    }

    [Fact]
    public async Task AddTransactionAsync_EmptyDescription_ThrowsArgumentException()
    {
        var service = new TransactionService(new InMemoryTransactionRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddTransactionAsync(TransactionType.Expense, 50m, "   ", "Test"));
    }

    [Fact]
    public async Task AddTransactionAsync_EmptyCategory_DefaultsToUncategorized()
    {
        var service = new TransactionService(new InMemoryTransactionRepository());

        var t = await service.AddTransactionAsync(TransactionType.Expense, 50m, "Coffee", "");

        Assert.Equal("Uncategorized", t.Category);
    }

    [Fact]
    public async Task GetBalanceAsync_MixedTransactions_ReturnsCorrectBalance()
    {
        var service = new TransactionService(new InMemoryTransactionRepository());

        await service.AddTransactionAsync(TransactionType.Income, 1000m, "Salary", "Work");
        await service.AddTransactionAsync(TransactionType.Expense, 300m, "Rent", "Housing");
        await service.AddTransactionAsync(TransactionType.Expense, 100m, "Groceries", "Food");

        var balance = await service.GetBalanceAsync();

        Assert.Equal(600m, balance);
    }

    [Fact]
    public async Task DeleteTransactionAsync_ExistingTransaction_ReturnsTrue()
    {
        var service = new TransactionService(new InMemoryTransactionRepository());
        var t = await service.AddTransactionAsync(TransactionType.Income, 100m, "Test", "Test");

        var deleted = await service.DeleteTransactionAsync(t.Id);

        Assert.True(deleted);
        var remaining = await service.GetAllTransactionsAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteTransactionAsync_NonExistentTransaction_ReturnsFalse()
    {
        var service = new TransactionService(new InMemoryTransactionRepository());

        var deleted = await service.DeleteTransactionAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TransactionService(null!));
    }
}
