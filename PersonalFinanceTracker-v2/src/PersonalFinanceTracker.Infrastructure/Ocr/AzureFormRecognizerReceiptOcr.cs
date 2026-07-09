using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.ValueObjects;

namespace PersonalFinanceTracker.Infrastructure.Ocr;

/// <summary>
/// Adapter over Azure AI Document Intelligence (formerly Form Recognizer).
/// Uses the prebuilt "prebuilt-receipt" model which is optimised for till slips.
///
/// The Azure SDK types stay inside this file — the application layer only sees
/// <see cref="ReceiptData"/>. This is the Adapter pattern: swap the cloud provider
/// tomorrow, no ripple effects.
/// </summary>
public class AzureFormRecognizerReceiptOcr : IReceiptOcrService
{
    private readonly DocumentAnalysisClient _client;
    private const string ReceiptModelId = "prebuilt-receipt";

    public AzureFormRecognizerReceiptOcr(string endpoint, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Azure endpoint is required.", nameof(endpoint));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Azure API key is required.", nameof(apiKey));

        _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<Result<ReceiptData>> ExtractReceiptAsync(Stream imageStream, CancellationToken ct = default)
    {
        try
        {
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed, ReceiptModelId, imageStream, cancellationToken: ct);

            var result = operation.Value;
            if (result.Documents.Count == 0)
                return Result<ReceiptData>.Failure(
                    ErrorCodes.OcrFailure, "No receipt detected in the image.");

            var doc = result.Documents[0];
            return Result<ReceiptData>.Success(MapToReceiptData(doc));
        }
        catch (RequestFailedException ex)
        {
            return Result<ReceiptData>.Failure(
                ErrorCodes.OcrFailure, $"Azure OCR failed: {ex.Message}");
        }
    }

    private static ReceiptData MapToReceiptData(AnalyzedDocument doc)
    {
        var merchant = TryGetString(doc, "MerchantName");
        var date = TryGetDate(doc, "TransactionDate");
        var total = TryGetMoney(doc, "Total");
        var lineItems = ExtractLineItems(doc);

        return new ReceiptData(
            MerchantName: merchant,
            TransactionDate: date,
            Total: total,
            LineItems: lineItems,
            ConfidenceScore: doc.Confidence
        );
    }

    private static IReadOnlyList<ReceiptLineItem> ExtractLineItems(AnalyzedDocument doc)
    {
        if (!doc.Fields.TryGetValue("Items", out var itemsField) ||
            itemsField.FieldType != DocumentFieldType.List)
            return Array.Empty<ReceiptLineItem>();

        var items = new List<ReceiptLineItem>();
        foreach (var item in itemsField.Value.AsList())
        {
            if (item.FieldType != DocumentFieldType.Dictionary) continue;
            var fields = item.Value.AsDictionary();

            var desc = fields.TryGetValue("Description", out var d) ? d.Value.AsString() : "Unknown";
            var price = fields.TryGetValue("TotalPrice", out var p) ? (decimal)p.Value.AsDouble() : 0m;
            var qty = fields.TryGetValue("Quantity", out var q) ? (int?)q.Value.AsDouble() : null;

            items.Add(new ReceiptLineItem(desc, Money.Zar(price), qty));
        }

        return items;
    }

    private static string? TryGetString(AnalyzedDocument doc, string fieldName)
        => doc.Fields.TryGetValue(fieldName, out var f) ? f.Value.AsString() : null;

    private static DateTime? TryGetDate(AnalyzedDocument doc, string fieldName)
        => doc.Fields.TryGetValue(fieldName, out var f) ? f.Value.AsDate().DateTime : null;

    private static Money? TryGetMoney(AnalyzedDocument doc, string fieldName)
    {
        if (!doc.Fields.TryGetValue(fieldName, out var f)) return null;
        var amount = (decimal)f.Value.AsDouble();
        return Money.Zar(amount);
    }
}
