using System.Text;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.BankParsers;
using Xunit;

namespace PersonalFinanceTracker.Infrastructure.Tests.BankParsers;

public class FnbCsvParserTests
{
    private const string SamplePath = "BankParsers/testdata/fnb-sample.csv";
    private static Stream OpenSample() => File.OpenRead(SamplePath);
    private static Stream StreamFromString(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void CanParse_ValidFnbHeader_ReturnsTrue()
    {
        var parser = new FnbCsvParser();
        using var stream = OpenSample();

        Assert.True(parser.CanParse(stream));
    }

    [Fact]
    public void CanParse_StandardBankShapedHeader_ReturnsFalse()
    {
        // Same Date/Description/Amount/Balance shape as Standard Bank, but no
        // "Accrued Charges" column — FNB parser must not claim this file.
        var parser = new FnbCsvParser();
        using var stream = StreamFromString("Date,Description,Amount,Balance\n2025/01/01,TEST,100,100\n");

        Assert.False(parser.CanParse(stream));
    }

    [Fact]
    public void Parse_ValidSample_ReturnsAllRealTransactions()
    {
        var parser = new FnbCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Value!.Count); // 9 real rows; summary rows excluded
    }

    [Fact]
    public void Parse_SkipsStatementSummaryRows()
    {
        var parser = new FnbCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.DoesNotContain(result.Value!, t => t.Description.ToLower().Contains("opening balance"));
        Assert.DoesNotContain(result.Value!, t => t.Description.ToLower().Contains("closing balance"));
    }

    [Fact]
    public void Parse_DDMMMYYYYDateFormat_ParsedCorrectly()
    {
        var parser = new FnbCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var first = result.Value!.First();
        Assert.Equal(new DateTime(2025, 1, 3), first.Date);
    }

    [Fact]
    public void Parse_SetsSourceBankToFnb()
    {
        var parser = new FnbCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.All(result.Value!, t => Assert.Equal("FNB", t.SourceBank));
    }

    [Fact]
    public void Parse_NegativeAmount_MarkedAsExpense()
    {
        var parser = new FnbCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var medical = result.Value!.Single(t => t.Description.Contains("MOMENTUM"));
        Assert.Equal(TransactionType.Expense, medical.Type);
        Assert.Equal(3800.00m, medical.Amount.Amount);
    }

    [Fact]
    public void Parse_PositiveAmount_MarkedAsIncome()
    {
        var parser = new FnbCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var salary = result.Value!.Single(t => t.Description.Contains("SALARY"));
        Assert.Equal(TransactionType.Income, salary.Type);
        Assert.Equal(22000.00m, salary.Amount.Amount);
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsFailure()
    {
        var parser = new FnbCsvParser();
        using var stream = StreamFromString("");

        var result = parser.Parse(stream);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.EmptyFile, result.Error!.Value.Code);
    }
}
