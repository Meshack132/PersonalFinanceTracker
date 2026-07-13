using System.Globalization;
using System.Text;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Domain.ValueObjects;

namespace PersonalFinanceTracker.Infrastructure.BankParsers;

/// <summary>
/// Parses CSV exports from FNB Online Banking's manual "Transaction History" download
/// (not the emailed statement format, which uses an entirely different multi-section
/// layout with Afrikaans headers and yearless dates — that would need its own parser).
///
/// Expected header (case-insensitive, order-tolerant):
///   Date, Description, Amount, Balance, Accrued Charges
///
/// FNB-specific quirks handled:
/// - Dates commonly appear as "03 Jan 2025" (DD MMM YYYY) in addition to numeric formats
/// - The "Accrued Charges" column distinguishes this format from Standard Bank's export,
///   which has no such column — used as the format signature in <see cref="CanParse"/>
/// - Single signed Amount column (negative = debit), same convention as Standard Bank
/// </summary>
public class FnbCsvParser : IBankStatementParser
{
    public string BankName => "FNB";

    private static readonly string[] AcceptedDateFormats =
    {
        "dd MMM yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "dd-MM-yyyy"
    };

    private static readonly string[] RequiredHeaders = { "date", "description", "amount" };

    public bool CanParse(Stream fileStream)
    {
        var start = fileStream.Position;
        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
            var header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header)) return false;

            var normalized = header.ToLowerInvariant();

            // "accrued" is the FNB-specific signature that distinguishes this from
            // Standard Bank's otherwise near-identical Date/Description/Amount/Balance shape.
            return RequiredHeaders.All(h => normalized.Contains(h))
                   && normalized.Contains("accrued");
        }
        finally
        {
            fileStream.Position = start;
        }
    }

    public Result<IReadOnlyList<Transaction>> Parse(Stream fileStream)
    {
        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
            var headerLine = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(headerLine))
                return Result<IReadOnlyList<Transaction>>.Failure(
                    ErrorCodes.EmptyFile, "The statement file is empty.");

            var columnMap = MapColumns(headerLine);
            if (columnMap is null)
                return Result<IReadOnlyList<Transaction>>.Failure(
                    ErrorCodes.ParseFailure, $"Expected columns not found in header: {headerLine}");

            var transactions = new List<Transaction>();
            var lineNumber = 1;
            string? line;

            while ((line = reader.ReadLine()) is not null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (IsSummaryRow(line)) continue;

                var parsed = ParseLine(line, columnMap.Value, lineNumber);
                if (parsed.IsSuccess)
                    transactions.Add(parsed.Value!);
                // Malformed rows are silently skipped; caller sees the delta via row counts.
            }

            return Result<IReadOnlyList<Transaction>>.Success(transactions);
        }
        catch (IOException ex)
        {
            return Result<IReadOnlyList<Transaction>>.Failure(
                ErrorCodes.ParseFailure, $"IO error while reading: {ex.Message}");
        }
    }

    private static ColumnMap? MapColumns(string headerLine)
    {
        var columns = SplitCsvRow(headerLine)
            .Select((c, i) => (Name: c.Trim().ToLowerInvariant(), Index: i))
            .ToList();

        var dateCol = columns.FirstOrDefault(c => c.Name.Contains("date"));
        var descCol = columns.FirstOrDefault(c => c.Name.Contains("description"));
        var amountCol = columns.FirstOrDefault(c => c.Name == "amount" || c.Name.Contains("amount"));

        if (dateCol.Name is null || descCol.Name is null || amountCol.Name is null)
            return null;

        return new ColumnMap(dateCol.Index, descCol.Index, amountCol.Index);
    }

    private static Result<Transaction> ParseLine(string line, ColumnMap map, int lineNumber)
    {
        var fields = SplitCsvRow(line);
        var maxIdx = Math.Max(map.DateIdx, Math.Max(map.DescriptionIdx, map.AmountIdx));
        if (fields.Length <= maxIdx)
            return Result<Transaction>.Failure(ErrorCodes.ParseFailure,
                $"Line {lineNumber}: not enough columns.");

        if (!TryParseDate(fields[map.DateIdx], out var date))
            return Result<Transaction>.Failure(ErrorCodes.InvalidDate,
                $"Line {lineNumber}: invalid date '{fields[map.DateIdx]}'.");

        if (!TryParseAmount(fields[map.AmountIdx], out var amount))
            return Result<Transaction>.Failure(ErrorCodes.InvalidAmount,
                $"Line {lineNumber}: invalid amount '{fields[map.AmountIdx]}'.");

        var description = fields[map.DescriptionIdx].Trim().Trim('"');

        var transaction = new Transaction
        {
            Date = date,
            Amount = Money.Zar(Math.Abs(amount)),
            Description = description,
            Type = amount < 0 ? TransactionType.Expense : TransactionType.Income,
            SourceBank = "FNB"
        };

        return Result<Transaction>.Success(transaction);
    }

    private static bool TryParseDate(string raw, out DateTime date)
        => DateTime.TryParseExact(raw.Trim(), AcceptedDateFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool TryParseAmount(string raw, out decimal amount)
    {
        var cleaned = raw.Trim().Replace(" ", "").Replace(",", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
    }

    private static bool IsSummaryRow(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.Contains("opening balance")
               || lower.Contains("closing balance")
               || lower.Contains("statement summary");
    }

    private static string[] SplitCsvRow(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == ',' && !inQuotes) { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(ch);
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private readonly record struct ColumnMap(int DateIdx, int DescriptionIdx, int AmountIdx);
}
