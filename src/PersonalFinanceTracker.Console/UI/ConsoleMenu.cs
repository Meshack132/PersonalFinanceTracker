using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.UseCases;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Console.UI;

public class ConsoleMenu
{
    private readonly ImportBankStatementUseCase _importUseCase;
    private readonly ITransactionRepository _repository;
    private readonly IReceiptOcrService _ocr;
    private readonly IBankParserFactory _parserFactory;

    public ConsoleMenu(
        ImportBankStatementUseCase importUseCase,
        ITransactionRepository repository,
        IReceiptOcrService ocr,
        IBankParserFactory parserFactory)
    {
        _importUseCase = importUseCase;
        _repository = repository;
        _ocr = ocr;
        _parserFactory = parserFactory;
    }

    public async Task RunAsync()
    {
        PrintHeader();

        while (true)
        {
            PrintMenu();
            var choice = System.Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1": await ImportBankStatementAsync(); break;
                    case "2": await ListTransactionsAsync(); break;
                    case "3": await ShowSarsSummaryAsync(); break;
                    case "4": await ShowSpendingByCategoryAsync(); break;
                    case "5": await ShowBalanceAsync(); break;
                    case "6": await ExtractReceiptAsync(); break;
                    case "7": ListSupportedBanks(); break;
                    case "0":
                        System.Console.WriteLine("Goodbye 🇿🇦");
                        return;
                    default:
                        System.Console.WriteLine("Invalid choice. Please try again.\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteColor(ConsoleColor.Red, $"Error: {ex.Message}\n");
            }
        }
    }

    // ── UI primitives ──────────────────────────────────────────────

    private static void PrintHeader()
    {
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.WriteLine("  🇿🇦 Personal Finance Tracker — SA edition v2.0");
        System.Console.WriteLine("  Clean Architecture · SARS-aware · Multi-bank import");
        System.Console.WriteLine("═══════════════════════════════════════════════════════\n");
    }

    private static void PrintMenu()
    {
        System.Console.WriteLine("What would you like to do?");
        System.Console.WriteLine("  1. Import a bank statement (CSV)");
        System.Console.WriteLine("  2. List all transactions");
        System.Console.WriteLine("  3. SARS deduction summary ★");
        System.Console.WriteLine("  4. Spending by category");
        System.Console.WriteLine("  5. Show balance");
        System.Console.WriteLine("  6. Extract data from receipt (OCR)");
        System.Console.WriteLine("  7. List supported banks");
        System.Console.WriteLine("  0. Exit");
        System.Console.Write("\nChoice > ");
    }

    // ── Actions ────────────────────────────────────────────────────

    private async Task ImportBankStatementAsync()
    {
        System.Console.Write("\nPath to CSV file (e.g. samples/standardbank-sample.csv): ");
        var path = System.Console.ReadLine()?.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            WriteColor(ConsoleColor.Yellow, "File not found.\n");
            return;
        }

        await using var stream = File.OpenRead(path);
        var result = await _importUseCase.ExecuteAsync(stream);

        if (result.IsFailure)
        {
            WriteColor(ConsoleColor.Red, $"Import failed: {result.Error}\n");
            return;
        }

        var summary = result.Value!;
        WriteColor(ConsoleColor.Green,
            $"\n✔ Imported from {summary.BankName}: " +
            $"{summary.NewTransactionsImported} new, " +
            $"{summary.DuplicatesSkipped} duplicates skipped " +
            $"({summary.TotalRowsParsed} rows total).\n");
    }

    private async Task ListTransactionsAsync()
    {
        var transactions = (await _repository.GetAllAsync())
            .OrderByDescending(t => t.Date)
            .ToList();

        System.Console.WriteLine("\n─── Transactions ───");
        if (transactions.Count == 0)
        {
            System.Console.WriteLine("(none — import a statement first)\n");
            return;
        }

        foreach (var t in transactions)
            System.Console.WriteLine("  " + t);
        System.Console.WriteLine();
    }

    private async Task ShowSarsSummaryAsync()
    {
        var all = (await _repository.GetAllAsync()).ToList();
        var deductible = all
            .Where(t => t.TaxCategory != TaxCategory.NotApplicable)
            .GroupBy(t => t.TaxCategory)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount.Amount), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .ToList();

        System.Console.WriteLine("\n─── SARS Deduction Summary ───");
        System.Console.WriteLine("(potential deductions — verify with a tax professional)\n");

        if (deductible.Count == 0)
        {
            System.Console.WriteLine("No deductible transactions found yet.");
            System.Console.WriteLine("Import a statement or add transactions to see this report.\n");
            return;
        }

        System.Console.WriteLine($"  {"Category",-30} {"Amount",15} {"Count",8}");
        System.Console.WriteLine($"  {new string('-', 30)} {new string('-', 15)} {new string('-', 8)}");

        var total = 0m;
        foreach (var item in deductible)
        {
            var label = FriendlyName(item.Category);
            System.Console.WriteLine($"  {label,-30} R {item.Total,12:N2} {item.Count,8}");
            total += item.Total;
        }

        System.Console.WriteLine($"  {new string('-', 30)} {new string('-', 15)} {new string('-', 8)}");
        WriteColor(ConsoleColor.Cyan, $"  {"Total potentially deductible",-30} R {total,12:N2}\n\n");
    }

    private async Task ShowSpendingByCategoryAsync()
    {
        var all = (await _repository.GetAllAsync()).ToList();
        var byCategory = all
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .OrderByDescending(x => x.Total)
            .ToList();

        System.Console.WriteLine("\n─── Spending by Category ───");
        if (byCategory.Count == 0)
        {
            System.Console.WriteLine("(no expenses yet)\n");
            return;
        }

        foreach (var item in byCategory)
            System.Console.WriteLine($"  {item.Category,-25} R {item.Total,12:N2}");
        System.Console.WriteLine();
    }

    private async Task ShowBalanceAsync()
    {
        var all = (await _repository.GetAllAsync()).ToList();
        var balance = all.Sum(t => t.Amount.SignedAmount(t.Type));
        var income = all.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount.Amount);
        var expenses = all.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount.Amount);

        System.Console.WriteLine("\n─── Balance ───");
        System.Console.WriteLine($"  Income:   R {income,12:N2}");
        System.Console.WriteLine($"  Expenses: R {expenses,12:N2}");
        var color = balance >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        WriteColor(color, $"  Balance:  R {balance,12:N2}\n\n");
    }

    private async Task ExtractReceiptAsync()
    {
        System.Console.Write("\nPath to receipt image (or press Enter for mock demo): ");
        var path = System.Console.ReadLine()?.Trim().Trim('"');

        Stream stream;
        if (string.IsNullOrWhiteSpace(path))
        {
            stream = new MemoryStream(); // mock service ignores stream
        }
        else if (!File.Exists(path))
        {
            WriteColor(ConsoleColor.Yellow, "File not found.\n");
            return;
        }
        else
        {
            stream = File.OpenRead(path);
        }

        try
        {
            var result = await _ocr.ExtractReceiptAsync(stream);
            if (result.IsFailure)
            {
                WriteColor(ConsoleColor.Red, $"OCR failed: {result.Error}\n");
                return;
            }

            var data = result.Value!;
            System.Console.WriteLine("\n─── Extracted Receipt Data ───");
            System.Console.WriteLine($"  Merchant:   {data.MerchantName ?? "(unknown)"}");
            System.Console.WriteLine($"  Date:       {data.TransactionDate?.ToString("yyyy-MM-dd") ?? "(unknown)"}");
            System.Console.WriteLine($"  Total:      {data.Total?.ToString() ?? "(unknown)"}");
            System.Console.WriteLine($"  Confidence: {data.ConfidenceScore:P0}");
            System.Console.WriteLine("  Items:");
            foreach (var item in data.LineItems)
                System.Console.WriteLine($"    · {item.Description,-30} {item.Amount}");
            System.Console.WriteLine();
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }

    private void ListSupportedBanks()
    {
        System.Console.WriteLine("\n─── Supported banks ───");
        foreach (var name in _parserFactory.SupportedBanks())
            System.Console.WriteLine($"  · {name}");
        System.Console.WriteLine();
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static void WriteColor(ConsoleColor color, string message)
    {
        var previous = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(message);
        System.Console.ForegroundColor = previous;
    }

    private static string FriendlyName(TaxCategory category) => category switch
    {
        TaxCategory.RetirementContribution => "Retirement (11F)",
        TaxCategory.MedicalSchemeFees      => "Medical scheme (6A)",
        TaxCategory.MedicalOutOfPocket     => "Medical out-of-pocket (6B)",
        TaxCategory.CharitableDonation     => "Donation to PBO (18A)",
        TaxCategory.HomeOffice             => "Home office",
        TaxCategory.BusinessTravel         => "Business travel",
        TaxCategory.SolarInstallation      => "Solar (12BA)",
        TaxCategory.PersonalExpense        => "Personal",
        _                                   => category.ToString()
    };
}

// ── Small extension so the balance calculation reads cleanly ───────
internal static class MoneyTypeExtensions
{
    public static decimal SignedAmount(this Domain.ValueObjects.Money money, TransactionType type)
        => type == TransactionType.Income ? money.Amount : -money.Amount;
}
