using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Common;

namespace PersonalFinanceTracker.Application.Abstractions;

/// <summary>
/// Selects the correct parser for an incoming file by asking each registered
/// parser <c>CanParse</c>. First match wins.
/// </summary>
public interface IBankParserFactory
{
    Result<IBankStatementParser> GetParserFor(Stream fileStream);
    IEnumerable<string> SupportedBanks();
}
