using System.Text;
using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.BankParsers;
using Xunit;

namespace PersonalFinanceTracker.Infrastructure.Tests.BankParsers;

public class StandardBankCsvParserTests
{
    private const string SampleCsvPath = "BankParsers/testdata/standardbank-sample.csv";

    private static Stream OpenSample() => File.OpenRead(SampleCsvPath);
    private static Stream StreamFromString(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void CanParse_ValidStandardBankHeader_ReturnsTrue()
    {
        var parser = new StandardBankCsvParser();
        using var stream = OpenSample();

        Assert.True(parser.CanParse(stream));
    }

    [Fact]
    public void CanParse_UnknownFormat_ReturnsFalse()
    {
        var parser = new StandardBankCsvParser();
        using var stream = StreamFromString("random,unrelated,columns\n1,2,3\n");

        Assert.False(parser.CanParse(stream));
    }

    [Fact]
    public void CanParse_EmptyStream_ReturnsFalse()
    {
        var parser = new StandardBankCsvParser();
        using var stream = StreamFromString("");

        Assert.False(parser.CanParse(stream));
    }

    [Fact]
    public void Parse_ValidSample_ReturnsAllRealTransactions()
    {
        var parser = new StandardBankCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.True(result.IsSuccess);
        // 12 lines in file, minus 2 "balance forward" rows = 10 real transactions
        Assert.Equal(10, result.Value!.Count);
    }

    [Fact]
    public void Parse_SkipsBalanceBroughtForwardRows()
    {
        var parser = new StandardBankCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.DoesNotContain(result.Value!,
            t => t.Description.ToLower().Contains("balance brought forward"));
        Assert.DoesNotContain(result.Value!,
            t => t.Description.ToLower().Contains("balance carried forward"));
    }

    [Fact]
    public void Parse_NegativeAmounts_MarkedAsExpense()
    {
        var parser = new StandardBankCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var checkers = result.Value!.Single(t => t.Description.Contains("CHECKERS"));
        Assert.Equal(TransactionType.Expense, checkers.Type);
        Assert.Equal(1250.75m, checkers.Amount.Amount);  // stored as absolute value
        Assert.Equal("ZAR", checkers.Amount.Currency);
    }

    [Fact]
    public void Parse_PositiveAmounts_MarkedAsIncome()
    {
        var parser = new StandardBankCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        var salary = result.Value!.Single(t => t.Description.Contains("SALARY"));
        Assert.Equal(TransactionType.Income, salary.Type);
        Assert.Equal(25000m, salary.Amount.Amount);
    }

    [Fact]
    public void Parse_SetsSourceBank()
    {
        var parser = new StandardBankCsvParser();
        using var stream = OpenSample();

        var result = parser.Parse(stream);

        Assert.All(result.Value!, t => Assert.Equal("Standard Bank", t.SourceBank));
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsFailureWithEmptyFileError()
    {
        var parser = new StandardBankCsvParser();
        using var stream = StreamFromString("");

        var result = parser.Parse(stream);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.EmptyFile, result.Error!.Value.Code);
    }

    [Theory]
    [InlineData("2025/01/03")]
    [InlineData("03/01/2025")]
    [InlineData("2025-01-03")]
    public void Parse_AcceptsMultipleDateFormats(string dateFormat)
    {
        var csv = $"Date,Description,Amount,Balance\n{dateFormat},TEST,100.00,100.00\n";
        var parser = new StandardBankCsvParser();
        using var stream = StreamFromString(csv);

        var result = parser.Parse(stream);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(new DateTime(2025, 1, 3), result.Value![0].Date);
    }

    [Fact]
    public void Parse_MalformedRow_SkippedRatherThanCrashing()
    {
        var csv = "Date,Description,Amount,Balance\n" +
                  "2025/01/03,GOOD ROW,100.00,100.00\n" +
                  "not-a-date,BAD ROW,not-a-number,also-bad\n" +
                  "2025/01/04,ANOTHER GOOD,50.00,150.00\n";
        var parser = new StandardBankCsvParser();
        using var stream = StreamFromString(csv);

        var result = parser.Parse(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);  // bad row silently dropped
    }

    [Fact]
    public void Parse_QuotedFieldsWithCommas_HandledCorrectly()
    {
        var csv = "Date,Description,Amount,Balance\n" +
                  "2025/01/03,\"Payment to Smith, John\",100.00,100.00\n";
        var parser = new StandardBankCsvParser();
        using var stream = StreamFromString(csv);

        var result = parser.Parse(stream);

        Assert.True(result.IsSuccess);
        Assert.Equal("Payment to Smith, John", result.Value![0].Description);
    }

    [Fact]
    public void Parse_ProducesDeterministicDedupeKeys()
    {
        // Import the same file twice — dedupe keys should be identical
        var parser1 = new StandardBankCsvParser();
        using var s1 = OpenSample();
        var run1 = parser1.Parse(s1).Value!;

        var parser2 = new StandardBankCsvParser();
        using var s2 = OpenSample();
        var run2 = parser2.Parse(s2).Value!;

        var keys1 = run1.Select(t => t.DedupeKey).ToHashSet();
        var keys2 = run2.Select(t => t.DedupeKey).ToHashSet();

        Assert.Equal(keys1, keys2);
    }
}
