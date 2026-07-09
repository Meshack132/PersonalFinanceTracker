using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Application.Abstractions;

/// <summary>
/// Strategy interface: one implementation per bank statement format.
/// Implementations are selected at runtime by <c>IBankParserFactory</c> based on file signature.
/// </summary>
public interface IBankStatementParser
{
    /// <summary>Human-readable name of the bank this parser supports (e.g., "Standard Bank").</summary>
    string BankName { get; }

    /// <summary>Cheap sniff — does this look like a file this parser can handle?</summary>
    bool CanParse(Stream fileStream);

    /// <summary>
    /// Parse the entire stream into transactions.
    /// Returns <c>Result</c> so parse errors don't bubble up as exceptions.
    /// </summary>
    Result<IReadOnlyList<Transaction>> Parse(Stream fileStream);
}
