using PersonalFinanceTracker.Application.DTOs;
using PersonalFinanceTracker.Domain.Common;

namespace PersonalFinanceTracker.Application.Abstractions;

/// <summary>
/// Extracts structured data (merchant, amount, date, line items) from a receipt image.
/// Implementations may back onto Azure Form Recognizer, AWS Textract, or a local model.
/// The application layer doesn't care which — it just gets a <see cref="ReceiptData"/>.
/// </summary>
public interface IReceiptOcrService
{
    Task<Result<ReceiptData>> ExtractReceiptAsync(Stream imageStream, CancellationToken ct = default);
}
