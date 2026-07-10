# Architecture

## Guiding principle

Clean Architecture with strict dependency direction. Business logic depends on nothing; adapters
depend on business logic. This lets us swap infrastructure (Azure → AWS, JSON → SQL, CLI → Web) without touching the code that expresses *what the system does*.

## Layers

```
┌───────────────────────────────────────────────────────────────┐
│  Presentation  (Console app; later: Blazor, minimal API)      │
│  - Composition root, DI wiring                                │
│  - User interaction                                           │
└───────────────────────────────┬───────────────────────────────┘
                                │ references
                                ▼
┌───────────────────────────────────────────────────────────────┐
│  Application  (Use cases + ports)                             │
│  - ImportBankStatementUseCase                                 │
│  - IBankStatementParser, IReceiptOcrService (interfaces)      │
│  - DTOs shaped for use-case consumption                       │
└───────────────────────────────┬───────────────────────────────┘
                                │ references
                                ▼
┌───────────────────────────────────────────────────────────────┐
│  Domain  (Pure business rules — zero dependencies)            │
│  - Money (value object, currency-safe)                        │
│  - Transaction, TaxCategory (SARS deduction buckets)          │
│  - Result<T> (functional error handling)                      │
└───────────────────────────────────────────────────────────────┘
                                ▲ implements ports of
                                │
┌───────────────────────────────────────────────────────────────┐
│  Infrastructure  (Adapters — everything I/O)                  │
│  - StandardBankCsvParser, FnbCsvParser, CapitecCsvParser      │
│  - AzureFormRecognizerReceiptOcr                              │
│  - RuleBasedCategorizationEngine + SARS rules                 │
│  - JsonTransactionRepository (later: SQLite)                  │
└───────────────────────────────────────────────────────────────┘
```

**Rule of thumb:** if code touches a file, an HTTP client, a database, or the clock, it goes in Infrastructure. If it expresses a business concept ("a transaction can be tax-deductible if..."), it belongs in Domain.

## Design patterns in use

| Pattern | Location | Why |
|---|---|---|
| **Strategy** | `IBankStatementParser` + N implementations | Each bank has its own CSV quirks. Adding a bank = adding a class, not editing existing code. |
| **Factory** | `BankParserFactory` | Selects the right parser at runtime by sniffing the file header. |
| **Chain of Responsibility** | `RuleBasedCategorizationEngine` walking `ICategorizationRule[]` | Categorization rules registered in order of specificity; first match wins. New rules are new classes. |
| **Adapter** | `AzureFormRecognizerReceiptOcr` | Wraps Azure SDK types behind our clean `IReceiptOcrService`. Cloud swap is one class change. |
| **Value Object** | `Money`, `TaxCategory` | Prevents mixing currencies at compile time; `Money.Zar(x) + Money.Usd(y)` throws — you can never accidentally add rand to dollar. |
| **Result pattern** | `Result<T>` | Expected failures (bad CSV row, missing header) return a `Result`, not an exception. Fast, no stack unwinding, forces the caller to handle failure. |
| **Repository** | `ITransactionRepository` | Isolates persistence; today JSON, tomorrow SQLite/EF Core. |

## The SARS angle

South African individual taxpayers get deductions across a handful of buckets defined by the Income Tax Act:

| Section | Category | What we track |
|---|---|---|
| 6A | Medical scheme fees tax credit | Debit orders to Discovery Health, Momentum, Bonitas, etc. |
| 6B | Additional medical (out of pocket) | Doctor visits, prescribed meds not reimbursed |
| 11F | Retirement fund contributions | 10X, Sygnia, Allan Gray RA — deductible up to 27.5% of income, capped at R350k |
| 18A | Charitable donations to PBOs | Gift of the Givers, Smile Foundation, etc. |
| 12BA | Solar installation incentive | Applicable installations |

The `TaxCategory` enum reflects these, and `SarsCategoryRules` matches merchant patterns to categories. At year-end, filtering by `TaxCategory != NotApplicable` gives a taxpayer a rough "here's what you can potentially claim" report.

**Disclaimer**: this is a helper, not tax advice. Rules and thresholds change annually.

## Testing strategy

- **Domain**: pure unit tests. No mocks. `MoneyTests` is the exemplar — every arithmetic, formatting, and edge case covered.
- **Infrastructure**: integration-flavored unit tests with real inputs (sample CSV files) but no external services. Azure OCR is tested against a fake in-memory implementation.
- **Application**: use-case tests mock the ports (parsers, OCR, repo) and verify orchestration.
- **End-to-end**: CI runs on Linux and Windows to catch platform-specific issues (path separators, line endings, culture).

Coverage tracked with Coverlet, reported to Codecov, badge on README.

## Extension roadmap

- **Additional bank parsers**: FNB (`FnbCsvParser`), Capitec (`CapitecCsvParser`), Nedbank, Absa
- **PDF import** for Standard Bank personal (currently PDF-only): a new `IBankStatementParser` implementation using PdfPig — the interface stays the same
- **SQLite persistence** via EF Core: swap `JsonTransactionRepository` for `EfTransactionRepository`
- **Budget forecasting** (v2 feature): moving average / linear regression on categorised spending
- **Blazor front-end**: reuses Application layer wholesale; only Presentation changes

## Why not just use a service bus / DDD tactical patterns / MediatR?

Because this is a personal finance tracker, not a bank. Adding CQRS, event sourcing, and MediatR would be ceremony over substance. Clean Architecture without the tactical DDD extras is the right complexity for this problem — provable by the fact that use cases stay under 100 lines each.
