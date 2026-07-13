using System.Text;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Infrastructure.BankParsers;
using Xunit;

namespace PersonalFinanceTracker.Infrastructure.Tests.BankParsers;

/// <summary>
/// Confirms the Strategy pattern actually works: given files from three different
/// banks, the factory picks the correct parser for each — no cross-contamination.
/// </summary>
public class BankParserFactoryDisambiguationTests
{
    private static Stream StreamFromString(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    private static BankParserFactory BuildFactory() => new(new IBankStatementParser[]
    {
        new StandardBankCsvParser(),
        new FnbCsvParser(),
        new CapitecCsvParser()
    });

    [Fact]
    public void GetParserFor_StandardBankFile_SelectsStandardBankParser()
    {
        var factory = BuildFactory();
        using var stream = StreamFromString("Date,Description,Amount,Balance\n2025/01/01,TEST,100.00,100.00\n");

        var result = factory.GetParserFor(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal("Standard Bank", result.Value!.BankName);
    }

    [Fact]
    public void GetParserFor_FnbFile_SelectsFnbParser()
    {
        var factory = BuildFactory();
        using var stream = StreamFromString(
            "Date,Description,Amount,Balance,Accrued Charges\n03 Jan 2025,TEST,100.00,100.00,0.00\n");

        var result = factory.GetParserFor(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal("FNB", result.Value!.BankName);
    }

    [Fact]
    public void GetParserFor_CapitecFile_SelectsCapitecParser()
    {
        var factory = BuildFactory();
        using var stream = StreamFromString(
            "Date,Description,Debit,Credit,Balance\n01/01/2025,TEST,100.00,,100.00\n");

        var result = factory.GetParserFor(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal("Capitec", result.Value!.BankName);
    }

    [Fact]
    public void GetParserFor_UnrecognizedFile_ReturnsFailureListingSupportedBanks()
    {
        var factory = BuildFactory();
        using var stream = StreamFromString("Foo,Bar,Baz\n1,2,3\n");

        var result = factory.GetParserFor(stream);

        Assert.True(result.IsFailure);
        Assert.Contains("Standard Bank", result.Error!.Value.Message);
        Assert.Contains("FNB", result.Error!.Value.Message);
        Assert.Contains("Capitec", result.Error!.Value.Message);
    }

    [Fact]
    public void SupportedBanks_ListsAllThree()
    {
        var factory = BuildFactory();

        var banks = factory.SupportedBanks().ToList();

        Assert.Equal(3, banks.Count);
        Assert.Contains("Standard Bank", banks);
        Assert.Contains("FNB", banks);
        Assert.Contains("Capitec", banks);
    }
}

