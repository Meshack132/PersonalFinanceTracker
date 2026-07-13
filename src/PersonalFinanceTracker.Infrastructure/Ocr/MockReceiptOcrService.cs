using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.ValueObjects;

namespace PersonalFinanceTracker.Infrastructure.Ocr;

/// <summary>
/// Fake OCR service used when no Azure credentials are configured.
/// Returns deterministic, plausible-looking data so the rest of the app
/// still functions end-to-end for local development and demos.
/// </summary>
public class MockReceiptOcrService : IReceiptOcrService
{
    public Task<Result<ReceiptData>> ExtractReceiptAsync(Stream imageStream, CancellationToken ct = default)
    {
        var mockData = new ReceiptData(
            MerchantName: "Checkers Sandton City (mock)",
            TransactionDate: DateTime.Today,
            Total: Money.Zar(347.85m),
            LineItems: new List<ReceiptLineItem>
            {
                new("Bread — Albany", Money.Zar(24.99m), 2),
                new("Milk — Clover 2L", Money.Zar(39.99m), 1),
                new("Chicken breasts 1kg", Money.Zar(129.99m), 1),
                new("Bananas 1kg", Money.Zar(19.99m), 1),
                new("Coffee — Frisco 250g", Money.Zar(109.99m), 1),
            },
            ConfidenceScore: 0.99
        );

        return Task.FromResult(Result<ReceiptData>.Success(mockData));
    }
}
