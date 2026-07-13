using System.Globalization;
using System.Text;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Domain.ValueObjects;

namespace PersonalFinanceTracker.Infrastructure.BankParsers;

/// <summary>
/// Parses CSV exports of Capitec transaction data.
///
/// Capitec's own app and internet banking only offer PDF statements — there is no native
/// CSV export. In practice, users get a CSV via a third-party statement converter, and
/// those tools consistently standardise Capitec data into separate Debit/Credit columns
/// rather than one signed Amount column (unlike Standard Bank and FNB).
///
/// Expected header (case-insensitive, order-tolerant):
///   Date, Description, Debit, Credit, Balance
///
/// This is genuinely a different shape from the other two parsers — proof that the
/// Strategy pattern earns its keep rather than being three near-identical classes.
///
/// Capitec-specific quirks handled:
/// - Separate Debit and Credit columns; exactly one is populated per row, the other blank
/// - Dates typically DD/MM/YYYY
/// - Merchant descriptions may be abbreviated to ~16 characters (e.g. "PnP Sandton City" →
///   "PNP SANDTON C") — we don't attempt to un-abbreviate these, just pass them through;
///   the categorization engine's keyword matching is tolerant of partial merchant names
/// </summary>
public class CapitecCsvParser : IBankStatementParser
{
    public string BankName => "Capitec";

    private static readonly string[] AcceptedDateFormats =
    {
        "dd/MM/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "dd-MM-yyyy", "dd MMM yyyy"
    };

    public bool CanParse(Stream fileStream)
    {
        var start = fileStream.Position;
        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
            var header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header)) return false;

            var normalized = header.ToLowerInvariant();

            // The separate debit+credit columns are the signature that distinguishes
            // Capitec exports from the single-signed-Amount shape of Standard Bank / FNB.
            return normalized.Contains("date")
                   && normalized.Contains("description")
                   && normalized.Contains("debit")
                   && normalized.Contains("credit");
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

                var parsed = ParseLine(line, columnMap.Value, lineNumber);
                if (parsed.IsSuccess)
                    transactions.Add(parsed.Value!);
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
        var debitCol = columns.FirstOrDefault(c => c.Name.Contains("debit"));
        var creditCol = columns.FirstOrDefault(c => c.Name.Contains("credit"));

        if (dateCol.Name is null || descCol.Name is null ||
            debitCol.Name is null || creditCol.Name is null)
            return null;

        return new ColumnMap(dateCol.Index, descCol.Index, debitCol.Index, creditCol.Index);
    }

    private static Result<Transaction> ParseLine(string line, ColumnMap map, int lineNumber)
    {
        var fields = SplitCsvRow(line);
        var maxIdx = new[] { map.DateIdx, map.DescriptionIdx, map.DebitIdx, map.CreditIdx }.Max();
        if (fields.Length <= maxIdx)
            return Result<Transaction>.Failure(ErrorCodes.ParseFailure,
                $"Line {lineNumber}: not enough columns.");

        if (!TryParseDate(fields[map.DateIdx], out var date))
            return Result<Transaction>.Failure(ErrorCodes.InvalidDate,
                $"Line {lineNumber}: invalid date '{fields[map.DateIdx]}'.");

        var debitRaw = fields[map.DebitIdx].Trim();
        var creditRaw = fields[map.CreditIdx].Trim();

        var hasDebit = TryParseAmount(debitRaw, out var debit) && debit != 0;
        var hasCredit = TryParseAmount(creditRaw, out var credit) && credit != 0;

        if (!hasDebit && !hasCredit)
            return Result<Transaction>.Failure(ErrorCodes.InvalidAmount,
                $"Line {lineNumber}: neither debit nor credit column has a value.");

        var (amount, type) = hasDebit
            ? (Math.Abs(debit), TransactionType.Expense)
            : (Math.Abs(credit), TransactionType.Income);

        var description = fields[map.DescriptionIdx].Trim().Trim('"');

        var transaction = new Transaction
        {
            Date = date,
            Amount = Money.Zar(amount),
            Description = description,
            Type = type,
            SourceBank = "Capitec"
        };

        return Result<Transaction>.Success(transaction);
    }

    private static bool TryParseDate(string raw, out DateTime date)
        => DateTime.TryParseExact(raw.Trim(), AcceptedDateFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool TryParseAmount(string raw, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(raw)) { amount = 0; return true; } // blank = "no value", not an error
        var cleaned = raw.Trim().Replace(" ", "").Replace(",", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
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

    private readonly record struct ColumnMap(int DateIdx, int DescriptionIdx, int DebitIdx, int CreditIdx);
}
