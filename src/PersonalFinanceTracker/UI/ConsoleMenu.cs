using PersonalFinanceTracker.Models;
using PersonalFinanceTracker.Services;

namespace PersonalFinanceTracker.UI;

public class ConsoleMenu
{
    private readonly ITransactionService _transactionService;
    private readonly IReportService _reportService;

    public ConsoleMenu(ITransactionService transactionService, IReportService reportService)
    {
        _transactionService = transactionService;
        _reportService = reportService;
    }

    public async Task RunAsync()
    {
        PrintHeader();

        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1": await AddTransactionAsync(TransactionType.Income); break;
                    case "2": await AddTransactionAsync(TransactionType.Expense); break;
                    case "3": await ListTransactionsAsync(); break;
                    case "4": await ShowBalanceAsync(); break;
                    case "5": await ShowSummaryReportAsync(); break;
                    case "6": await ShowSpendingByCategoryAsync(); break;
                    case "7": await DeleteTransactionAsync(); break;
                    case "0":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}\n");
                Console.ResetColor();
            }
        }
    }

    private static void PrintHeader()
    {
        Console.WriteLine("=====================================");
        Console.WriteLine("   Personal Finance Tracker v1.0");
        Console.WriteLine("=====================================\n");
    }

    private static void PrintMenu()
    {
        Console.WriteLine("Menu:");
        Console.WriteLine("  1. Add Income");
        Console.WriteLine("  2. Add Expense");
        Console.WriteLine("  3. List Transactions");
        Console.WriteLine("  4. Show Balance");
        Console.WriteLine("  5. Summary Report");
        Console.WriteLine("  6. Spending by Category");
        Console.WriteLine("  7. Delete Transaction");
        Console.WriteLine("  0. Exit");
        Console.Write("Choice > ");
    }

    private async Task AddTransactionAsync(TransactionType type)
    {
        Console.Write($"\n{type} amount: ");
        if (!decimal.TryParse(Console.ReadLine(), out var amount))
        {
            Console.WriteLine("Invalid amount.\n");
            return;
        }

        Console.Write("Description: ");
        var description = Console.ReadLine() ?? string.Empty;

        Console.Write("Category (or press Enter for 'Uncategorized'): ");
        var category = Console.ReadLine() ?? string.Empty;

        var transaction = await _transactionService.AddTransactionAsync(type, amount, description, category);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✔ Added: {transaction.Type} of {transaction.Amount:C} — '{transaction.Description}'\n");
        Console.ResetColor();
    }

    private async Task ListTransactionsAsync()
    {
        var transactions = (await _transactionService.GetAllTransactionsAsync())
            .OrderByDescending(t => t.Date)
            .ToList();

        Console.WriteLine("\n--- Transactions ---");
        if (transactions.Count == 0)
        {
            Console.WriteLine("(none)\n");
            return;
        }

        foreach (var t in transactions)
        {
            Console.WriteLine(t);
            Console.WriteLine($"    id: {t.Id}");
        }
        Console.WriteLine();
    }

    private async Task ShowBalanceAsync()
    {
        var balance = await _transactionService.GetBalanceAsync();
        Console.ForegroundColor = balance >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"\nCurrent Balance: {balance:C}\n");
        Console.ResetColor();
    }

    private async Task ShowSummaryReportAsync()
    {
        var summary = await _reportService.GetSummaryAsync();
        Console.WriteLine("\n--- Summary Report ---");
        Console.WriteLine($"Transactions:   {summary.TransactionCount,12}");
        Console.WriteLine($"Total Income:   {summary.TotalIncome,12:C}");
        Console.WriteLine($"Total Expenses: {summary.TotalExpenses,12:C}");
        Console.WriteLine($"Net Balance:    {summary.NetBalance,12:C}\n");
    }

    private async Task ShowSpendingByCategoryAsync()
    {
        var spending = await _reportService.GetSpendingByCategoryAsync();
        Console.WriteLine("\n--- Spending by Category ---");
        if (spending.Count == 0)
        {
            Console.WriteLine("(no expenses recorded yet)\n");
            return;
        }

        foreach (var (category, amount) in spending.OrderByDescending(kv => kv.Value))
        {
            Console.WriteLine($"  {category,-20} {amount,12:C}");
        }
        Console.WriteLine();
    }

    private async Task DeleteTransactionAsync()
    {
        Console.Write("\nTransaction ID: ");
        if (!Guid.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID format.\n");
            return;
        }

        var deleted = await _transactionService.DeleteTransactionAsync(id);
        Console.WriteLine(deleted ? "✔ Transaction deleted.\n" : "Transaction not found.\n");
    }
}
