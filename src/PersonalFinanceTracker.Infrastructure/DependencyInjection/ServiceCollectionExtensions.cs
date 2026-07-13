using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.UseCases;
using PersonalFinanceTracker.Infrastructure.BankParsers;
using PersonalFinanceTracker.Infrastructure.Categorization;
using PersonalFinanceTracker.Infrastructure.Ocr;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.DependencyInjection;

/// <summary>
/// Wires Domain + Application + Infrastructure together in one place so the
/// Console host and the Blazor Web host don't duplicate registration logic.
/// Each host still owns its own UI-specific services (ConsoleMenu, Razor
/// components, etc.) — this only covers the shared backend wiring.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersonalFinanceInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // ── Persistence: SQLite via EF Core ────────────────────────────
        var connectionString = config.GetConnectionString("Default")
                                ?? "Data Source=financetracker.db";

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ITransactionRepository, EfTransactionRepository>();

        // ── Bank parsers (Strategy pattern) ────────────────────────────
        services.AddSingleton<IBankStatementParser, StandardBankCsvParser>();
        services.AddSingleton<IBankStatementParser, FnbCsvParser>();
        services.AddSingleton<IBankStatementParser, CapitecCsvParser>();
        services.AddSingleton<IBankParserFactory, BankParserFactory>();

        // ── Categorization rules (Chain of Responsibility, order matters) ──
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
        services.AddScoped<ImportBankStatementUseCase>();

        return services;
    }

    /// <summary>
    /// Creates the SQLite database file and schema if they don't already exist.
    /// Fine for this project's current stage; swap for real EF migrations
    /// (`dotnet ef migrations add`) once the schema needs versioned evolution.
    /// </summary>
    public static void EnsurePersonalFinanceDatabaseCreated(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
}
