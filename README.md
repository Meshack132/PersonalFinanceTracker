# 🇿🇦 Personal Finance Tracker

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/Meshack132/PersonalFinanceTracker/ci.yml?branch=main)](https://github.com/Meshack132/PersonalFinanceTracker/actions)
[![codecov](https://img.shields.io/codecov/c/github/Meshack132/PersonalFinanceTracker?logo=codecov)](https://codecov.io/gh/Meshack132/PersonalFinanceTracker)
[![Azure](https://img.shields.io/badge/Azure-Form%20Recognizer-0089D6?logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/services/ai-services/ai-document-intelligence/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Architecture](https://img.shields.io/badge/architecture-clean-brightgreen)](docs/ARCHITECTURE.md)

**A South African personal finance analyser with tax-aware categorization and receipt OCR.**

Import your Standard Bank / FNB / Capitec CSV statements, snap till slips with your phone, and see what you can potentially claim from SARS at year-end.

---

## Why this exists

Every other personal finance app assumes USD and lumps everything into "Food" or "Shopping". South African taxpayers need something different:

- **Local bank formats**: Standard Bank Business Online, FNB, Capitec — each with their own CSV quirks
- **SARS awareness**: recognises deductible categories (medical scheme, retirement contributions, Section 18A donations)
- **ZAR-first**: `Money` value object enforces currency safety at the type level

## Features

- ✅ Multi-bank CSV import with automatic format detection
- ✅ Receipt OCR via Azure Form Recognizer (`prebuilt-receipt` model)
- ✅ Rule-based categorization mapped to SARS deduction sections
- ✅ Deduplication on re-import (deterministic hash of date + amount + description)
- ✅ Console UI (Blazor front-end on the roadmap)
- ✅ 40+ unit tests across Domain and Infrastructure layers, coverage reported to Codecov

## Architecture at a glance

Clean Architecture with 4 layers:

```
Presentation ──▶ Application ──▶ Domain ◀── Infrastructure
                (use cases)    (business)   (I/O adapters)
```

Full breakdown in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), including design patterns used (Strategy, Chain of Responsibility, Adapter, Value Object, Result), why each was chosen, and how to extend.

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- (Optional) Azure Form Recognizer resource — free tier is fine

### Build and test

```bash
git clone https://github.com/Meshack132/PersonalFinanceTracker.git
cd PersonalFinanceTracker
dotnet restore
dotnet build
dotnet test
```

### Configure Azure OCR (optional)

Copy `appsettings.example.json` to `appsettings.json` and fill in your Azure endpoint + key:

```json
{
  "Azure": {
    "FormRecognizer": {
      "Endpoint": "https://<your-resource>.cognitiveservices.azure.com/",
      "ApiKey": "<your-key>"
    }
  }
}
```

Or set environment variables:

```bash
export AZURE__FORMRECOGNIZER__ENDPOINT="https://..."
export AZURE__FORMRECOGNIZER__APIKEY="..."
```

Without config, the app falls back to a mock OCR service — CSV import still works fully.

### Run

```bash
dotnet run --project src/PersonalFinanceTracker.Console
```

## Project structure

```
PersonalFinanceTracker/
├── src/
│   ├── PersonalFinanceTracker.Domain/          # Pure business logic — no dependencies
│   │   ├── ValueObjects/Money.cs
│   │   ├── Entities/Transaction.cs
│   │   ├── Enums/TaxCategory.cs                # SARS sections
│   │   └── Common/Result.cs                    # Functional error handling
│   ├── PersonalFinanceTracker.Application/     # Use cases + port interfaces
│   │   ├── Abstractions/IBankStatementParser.cs
│   │   ├── Abstractions/IReceiptOcrService.cs
│   │   └── UseCases/ImportBankStatementUseCase.cs
│   ├── PersonalFinanceTracker.Infrastructure/  # Adapters (I/O, cloud, DB)
│   │   ├── BankParsers/StandardBankCsvParser.cs
│   │   ├── Categorization/SarsCategoryRules.cs
│   │   ├── Ocr/AzureFormRecognizerReceiptOcr.cs
│   │   └── Persistence/JsonTransactionRepository.cs
│   └── PersonalFinanceTracker.Console/         # UI & composition root
├── tests/
│   ├── PersonalFinanceTracker.Domain.Tests/
│   └── PersonalFinanceTracker.Infrastructure.Tests/
├── docs/ARCHITECTURE.md
└── .github/workflows/ci.yml                    # Multi-stage: build → test (matrix) → security scan
```

## What's showcased

- **Clean Architecture** with strict dependency direction (Domain has zero references)
- **Value Objects** — `Money` prevents mixing currencies at the type system level
- **Design patterns**: Strategy (parsers), Chain of Responsibility (categorization), Adapter (Azure OCR)
- **Result pattern** for expected failures — no exception-driven control flow
- **Testing**: unit + integration, matrix CI (Linux + Windows), coverage reporting
- **CI/CD sophistication**: cached restore, format check, warnings-as-errors, security audit, multi-OS

## Roadmap

- [ ] FNB and Capitec CSV parsers (interfaces already defined, need implementations)
- [ ] Standard Bank personal PDF parser (using PdfPig)
- [ ] SQLite persistence via EF Core
- [ ] Budget forecasting (linear regression on category spend)
- [ ] Blazor front-end
- [ ] Docker Compose for local dev with SQL Server

## Acknowledgments

Built as a portfolio project demonstrating Clean Architecture in C# .NET 8.

<!-- Continuous improvement in progress. -->

## License

[MIT](LICENSE)