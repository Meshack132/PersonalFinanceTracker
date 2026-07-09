using PersonalFinanceTracker.Repositories;
using PersonalFinanceTracker.Services;
using PersonalFinanceTracker.UI;

namespace PersonalFinanceTracker;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Composition root — simple manual DI.
        // (For a bigger app I'd wire up Microsoft.Extensions.DependencyInjection.)
        var repository = new JsonTransactionRepository("transactions.json");
        var transactionService = new TransactionService(repository);
        var reportService = new ReportService(repository);

        var menu = new ConsoleMenu(transactionService, reportService);
        await menu.RunAsync();
    }
}
