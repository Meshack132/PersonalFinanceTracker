using System.Globalization;
using System.Text;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Domain.ValueObjects;

namespace PersonalFinanceTracker.Infrastructure.BankParsers;

/// <summary>
/// Parses CSV exports from Standard Bank Business Online.
///
/// Expected header (case-insensitive, order-tolerant):
///   Date, Description, Amount, Balance
///
/// Standard Bank quirks handled:
/// - Dates may be dd/MM/yyyy or yyyy/MM/dd depending on the user's app locale
/// - Amount uses a leading minus for debits; may include thousands separator
/// - Header rows repeat "Balance brought forward" / "Balance carried forward" on each page —
///   these are stripped
/// - Some exports include quoted fields with embedded commas
/// </summary>
public class StandardBankCsvParser : IBankStatementParser
{
    public string BankName => "Standard Bank";

    private static readonly string[] AcceptedDateFormats =
    {
        "dd/MM/yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "dd-MM-yyyy"
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
            return RequiredHeaders.All(h => normalized.Contains(h))
                   && normalized.Contains("balance")
                   && !normalized.Contains("accrued")   // exclude FNB, which has this extra column
                   && !(normalized.Contains("debit") && normalized.Contains("credit")); // exclude Capitec's split columns
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
                    ErrorCodes.ParseFailure,
                    $"Expected columns not found in header: {headerLine}");

            var transactions = new List<Transaction>();
            var lineNumber = 1;
            string? line;

            while ((line = reader.ReadLine()) is not null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (IsPageBoundaryRow(line)) continue;

                var parsed = ParseLine(line, columnMap.Value, lineNumber);
                if (parsed.IsSuccess)
                    transactions.Add(parsed.Value!);
                // Silently skip malformed rows — the summary will show the delta.
                // For production, we'd log these to a "rejected rows" report.
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
            .Select((c, i) => (c.Trim().ToLowerInvariant(), i))
            .ToList();

        int? date = columns.FirstOrDefault(c => c.Item1.Contains("date")).i;
        int? desc = columns.FirstOrDefault(c => c.Item1.Contains("description")).i;
        int? amount = columns.FirstOrDefault(c => c.Item1 == "amount").i;
        int? balance = columns.FirstOrDefault(c => c.Item1.Contains("balance")).i;

        // Fallback for date if only one column contains "date"
        var dateCol = columns.FirstOrDefault(c => c.Item1.Contains("date"));
        if (dateCol.Item1 is null) return null;

        return new ColumnMap(
            DateIdx: dateCol.i,
            DescriptionIdx: columns.First(c => c.Item1.Contains("description")).i,
            AmountIdx: columns.First(c => c.Item1 == "amount" || c.Item1.Contains("amount")).i,
            BalanceIdx: columns.FirstOrDefault(c => c.Item1.Contains("balance")).i
        );
    }

    private static Result<Transaction> ParseLine(string line, ColumnMap map, int lineNumber)
    {
        var fields = SplitCsvRow(line);
        if (fields.Length <= Math.Max(map.DateIdx, Math.Max(map.DescriptionIdx, map.AmountIdx)))
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
            SourceBank = "Standard Bank"
        };

        return Result<Transaction>.Success(transaction);
    }

    private static bool TryParseDate(string raw, out DateTime date)
    {
        return DateTime.TryParseExact(raw.Trim(), AcceptedDateFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseAmount(string raw, out decimal amount)
    {
        var cleaned = raw.Trim().Replace(" ", "").Replace(",", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
    }

    private static bool IsPageBoundaryRow(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.Contains("balance brought forward")
               || lower.Contains("balance carried forward")
               || lower.Contains("opening balance")
               || lower.Contains("closing balance");
    }

    /// <summary>
    /// Split a CSV row honoring quoted fields (which may contain commas).
    /// Simple state machine; adequate for well-formed bank exports.
    /// </summary>
    private static string[] SplitCsvRow(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private readonly record struct ColumnMap(int DateIdx, int DescriptionIdx, int AmountIdx, int BalanceIdx);
}

