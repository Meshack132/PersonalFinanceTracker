using PersonalFinanceTracker.Domain.ValueObjects;

namespace PersonalFinanceTracker.Application.DTOs;

/// <summary>
/// Structured data extracted from a receipt image.
/// Nullable fields because OCR is imperfect — the caller decides how to handle gaps.
/// </summary>
public record ReceiptData(
    string? MerchantName,
    DateTime? TransactionDate,
    Money? Total,
    IReadOnlyList<ReceiptLineItem> LineItems,
    double ConfidenceScore
);

public record ReceiptLineItem(string Description, Money Amount, int? Quantity);
