using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Common;

namespace PersonalFinanceTracker.Infrastructure.BankParsers;

/// <summary>
/// Asks each registered parser whether it recognises the file.
/// First affirmative wins. Order of registration matters when signatures overlap.
/// </summary>
public class BankParserFactory : IBankParserFactory
{
    private readonly IEnumerable<IBankStatementParser> _parsers;

    public BankParserFactory(IEnumerable<IBankStatementParser> parsers)
    {
        _parsers = parsers.ToList();
    }

    public Result<IBankStatementParser> GetParserFor(Stream fileStream)
    {
        foreach (var parser in _parsers)
        {
            fileStream.Position = 0;
            if (parser.CanParse(fileStream))
            {
                fileStream.Position = 0;
                return Result<IBankStatementParser>.Success(parser);
            }
        }

        fileStream.Position = 0;
        return Result<IBankStatementParser>.Failure(
            ErrorCodes.UnknownBank,
            $"No registered parser recognized the file format. " +
            $"Supported: {string.Join(", ", _parsers.Select(p => p.BankName))}");
    }

    public IEnumerable<string> SupportedBanks() => _parsers.Select(p => p.BankName);
}
