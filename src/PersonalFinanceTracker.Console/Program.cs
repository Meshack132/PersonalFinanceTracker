using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.UseCases;
using PersonalFinanceTracker.Console.UI;
using PersonalFinanceTracker.Infrastructure.BankParsers;
using PersonalFinanceTracker.Infrastructure.Categorization;
using PersonalFinanceTracker.Infrastructure.Ocr;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Console;

/// <summary>
/// Composition root. Wires the Domain, Application, and Infrastructure layers
/// into a running console app via Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        RegisterServices(builder.Services, builder.Configuration);

        var host = builder.Build();

        var menu = host.Services.GetRequiredService<ConsoleMenu>();
        await menu.RunAsync();
    }

    private static void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        // ── Persistence ────────────────────────────────────────────────
        services.AddSingleton<ITransactionRepository>(_ =>
            new JsonTransactionRepository("transactions.json"));

        // ── Bank parsers (Strategy pattern) ────────────────────────────
        services.AddSingleton<IBankStatementParser, StandardBankCsvParser>();
        services.AddSingleton<IBankParserFactory, BankParserFactory>();

        // ── Categorization rules (Chain of Responsibility, order matters) ──
        // More specific / tax-relevant rules first
        services.AddSingleton<ICategorizationRule, MedicalAidRule>();
        services.AddSingleton<ICategorizationRule, RetirementContributionRule>();
        services.AddSingleton<ICategorizationRule, CharitableDonationRule>();
        services.AddSingleton<ICategorizationRule, SalaryRule>();
        services.AddSingleton<ICategorizationRule, GroceriesRule>();
        services.AddSingleton<ICategorizationRule, FuelRule>();
        services.AddSingleton<ICategorizationRule, TransportRule>();
        services.AddSingleton<ICategorizationRule, UtilitiesRule>();
        services.AddSingleton<ICategorizationRule, InvestmentRule>();
        services.AddSingleton<ICategorizationRule, BankingFeesRule>();
        services.AddSingleton<ICategorizationEngine, RuleBasedCategorizationEngine>();

        // ── OCR: Azure if configured, mock otherwise ───────────────────
        var azureEndpoint = config["Azure:FormRecognizer:Endpoint"];
        var azureKey = config["Azure:FormRecognizer:ApiKey"];

        if (!string.IsNullOrWhiteSpace(azureEndpoint) && !string.IsNullOrWhiteSpace(azureKey))
        {
            services.AddSingleton<IReceiptOcrService>(_ =>
                new AzureFormRecognizerReceiptOcr(azureEndpoint, azureKey));
        }
        else
        {
            services.AddSingleton<IReceiptOcrService, MockReceiptOcrService>();
        }

        // ── Use cases ──────────────────────────────────────────────────
        services.AddSingleton<ImportBankStatementUseCase>();

        // ── UI ─────────────────────────────────────────────────────────
        services.AddSingleton<ConsoleMenu>();
    }
}
