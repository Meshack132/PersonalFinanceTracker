using System.Text;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.BankParsers;
using Xunit;

namespace PersonalFinanceTracker.Infrastructure.Tests.BankParsers;

public class CapitecCsvParserTests
{
    private const string SamplePath = "BankParsers/testdata/capitec-sample.csv";
    private static Stream OpenSample() => File.OpenRead(SamplePath);
    private static Stream StreamFromString(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void CanParse_ValidCapitecHeader_ReturnsTrue()
    {
        var parser = new CapitecCsvParser();
        using var stream = OpenSample();

        Assert.True(parser.CanParse(stream));
    }

    [Fact]
    public void CanParse_SingleAmountColumnHeader_ReturnsFalse()
    {
        // Standard Bank / FNB shape — single Amount column, no separate Debit/Credit.
        var parser = new CapitecCsvParser();
        using var stream = StreamFromString("Date,Description,Amount,Balance\n2025/01/01,TEST,100,100\n");

        Assert.False(parser.CanParse(stream));
    }

    [Fact]
    public void Parse_ValidSample_ReturnsAllTransactions()
    {
        var parser = new CapitecCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Value!.Count);
    }

    [Fact]
    public void Parse_DebitColumnPopulated_MarkedAsExpense()
    {
        var parser = new CapitecCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var medical = result.Value!.Single(t => t.Description.Contains("DISCOVERY"));
        Assert.Equal(TransactionType.Expense, medical.Type);
        Assert.Equal(4200.00m, medical.Amount.Amount);
    }

    [Fact]
    public void Parse_CreditColumnPopulated_MarkedAsIncome()
    {
        var parser = new CapitecCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var salary = result.Value!.Single(t => t.Description.Contains("SALARY"));
        Assert.Equal(TransactionType.Income, salary.Type);
        Assert.Equal(20000.00m, salary.Amount.Amount);
    }

    [Fact]
    public void Parse_SlashDateFormat_ParsedCorrectly()
    {
        var parser = new CapitecCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var first = result.Value!.First();
        Assert.Equal(new DateTime(2025, 1, 2), first.Date);
    }

    [Fact]
    public void Parse_SetsSourceBankToCapitec()
    {
        var parser = new CapitecCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.All(result.Value!, t => Assert.Equal("Capitec", t.SourceBank));
    }

    [Fact]
    public void Parse_BothDebitAndCreditBlank_RowSkipped()
    {
        var csv = "Date,Description,Debit,Credit,Balance\n" +
                  "01/01/2025,GOOD ROW,100.00,,100.00\n" +
                  "02/01/2025,BAD ROW,,,100.00\n" +
                  "03/01/2025,ANOTHER GOOD,,50.00,150.00\n";
        var parser = new CapitecCsvParser();
        using var stream = StreamFromString(csv);

        var result = parser.Parse(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count); // the blank-blank row is dropped
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsFailure()
    {
        var parser = new CapitecCsvParser();
        using var stream = StreamFromString("");

        var result = parser.Parse(stream);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.EmptyFile, result.Error!.Value.Code);
    }
}
