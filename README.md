# 💰 Personal Finance Tracker

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-11.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/Meshack132/PersonalFinanceTracker/ci.yml?branch=main)](https://github.com/Meshack132/PersonalFinanceTracker/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![Made with love](https://img.shields.io/badge/made%20with-%E2%9D%A4-red)](#)

A clean, testable console app for tracking your personal income and expenses. Built with C# .NET 8 using a layered architecture (models, repository, services, UI) so it's easy to swap out the JSON persistence for a real database later.

---

## ✨ Features

- Add income and expense transactions
- Categorize your spending
- View your current balance in real time
- Generate summary reports (total income, expenses, net balance)
- Break spending down by category
- Delete transactions
- Data persists to a local `transactions.json` file
- **13 unit tests** using xUnit with an in-memory repository

## 📁 Project Structure

```
PersonalFinanceTracker/
├── src/
│   └── PersonalFinanceTracker/
│       ├── Models/                # Domain entities: Transaction, TransactionType, FinancialSummary
│       ├── Repositories/          # ITransactionRepository + JSON implementation
│       ├── Services/              # Business logic: TransactionService, ReportService
│       ├── UI/                    # ConsoleMenu — decoupled from business logic
│       └── Program.cs             # Composition root
├── tests/
│   └── PersonalFinanceTracker.Tests/
│       ├── TransactionServiceTests.cs
│       ├── ReportServiceTests.cs
│       └── InMemoryTransactionRepository.cs
├── .github/workflows/ci.yml       # Build + test on every push
├── PersonalFinanceTracker.slnx
├── LICENSE
└── README.md
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later

### Run the app

```bash
git clone https://github.com/Meshack132/PersonalFinanceTracker.git
cd PersonalFinanceTracker
dotnet run --project src/PersonalFinanceTracker
```

### Run the tests

```bash
dotnet test
```

## 🧠 Design Notes

- **Repository pattern**: `ITransactionRepository` isolates persistence. Today it's JSON, tomorrow it could be EF Core + SQL Server without touching the service layer.
- **Manual DI in `Program.cs`**: kept lightweight for a small app; ready to be replaced with `Microsoft.Extensions.DependencyInjection` when it grows.
- **`decimal` everywhere for money**: never use `float`/`double` for currency — rounding will bite you.
- **UI is a boundary, not a layer**: `ConsoleMenu` only knows about service interfaces, so a WPF or Web frontend could replace it.

## 🗺️ Roadmap

- [ ] SQLite persistence via EF Core
- [ ] Budget limits per category with alerts
- [ ] CSV import from bank statements
- [ ] Monthly/quarterly recurring transactions
- [ ] Web frontend (Blazor)

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you'd like to change.

## 📄 License

[MIT](LICENSE)
