using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Application.UseCases;

/// <summary>
/// End-to-end pipeline for importing a bank statement:
/// detect format → parse → categorize → deduplicate against existing data → persist.
/// This is the "use case" layer of Clean Architecture — no infrastructure knowledge,
/// only orchestration of ports.
/// </summary>
public class ImportBankStatementUseCase
{
    private readonly IBankParserFactory _parserFactory;
    private readonly ICategorizationEngine _categorizer;
    private readonly ITransactionRepository _repository;

    public ImportBankStatementUseCase(
        IBankParserFactory parserFactory,
        ICategorizationEngine categorizer,
        ITransactionRepository repository)
    {
        _parserFactory = parserFactory;
        _categorizer = categorizer;
        _repository = repository;
    }

    public async Task<Result<ImportSummary>> ExecuteAsync(Stream statementStream, CancellationToken ct = default)
    {
        // 1. Select the right parser (Strategy pattern)
        var parserResult = _parserFactory.GetParserFor(statementStream);
        if (parserResult.IsFailure)
            return Result<ImportSummary>.Failure(parserResult.Error!.Value);

        var parser = parserResult.Value!;
        statementStream.Position = 0;

        // 2. Parse
        var parseResult = parser.Parse(statementStream);
        if (parseResult.IsFailure)
            return Result<ImportSummary>.Failure(parseResult.Error!.Value);

        var parsed = parseResult.Value!;

        // 3. Deduplicate against existing data
        var existingKeys = await _repository.GetExistingDedupeKeysAsync(ct);
        var newTransactions = parsed
            .Where(t => !existingKeys.Contains(t.DedupeKey))
            .ToList();

        // 4. Categorize each new transaction
        var categorized = newTransactions
            .Select(ApplyCategorization)
            .ToList();

        // 5. Persist
        await _repository.AddRangeAsync(categorized, ct);
        await _repository.SaveChangesAsync(ct);

        return Result<ImportSummary>.Success(new ImportSummary(
            BankName: parser.BankName,
            TotalRowsParsed: parsed.Count,
            NewTransactionsImported: categorized.Count,
            DuplicatesSkipped: parsed.Count - categorized.Count
        ));
    }

    private Transaction ApplyCategorization(Transaction t)
    {
        var (category, taxCategory, _) = _categorizer.Categorize(t);
        return new Transaction
        {
            Id = t.Id,
            Date = t.Date,
            Amount = t.Amount,
            Description = t.Description,
            Category = category,
            Type = t.Type,
            TaxCategory = taxCategory,
            SourceBank = t.SourceBank
        };
    }
}

public record ImportSummary(
    string BankName,
    int TotalRowsParsed,
    int NewTransactionsImported,
    int DuplicatesSkipped
);
