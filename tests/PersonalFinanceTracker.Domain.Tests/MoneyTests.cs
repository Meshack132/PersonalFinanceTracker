using PersonalFinanceTracker.Domain.ValueObjects;
using Xunit;

namespace PersonalFinanceTracker.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Zar_CreatesZarInstance()
    {
        var money = Money.Zar(100m);

        Assert.Equal(100m, money.Amount);
        Assert.Equal("ZAR", money.Currency);
    }

    [Fact]
    public void Add_SameCurrency_Succeeds()
    {
        var a = Money.Zar(100m);
        var b = Money.Zar(50m);

        var sum = a + b;

        Assert.Equal(150m, sum.Amount);
        Assert.Equal("ZAR", sum.Currency);
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        var zar = Money.Zar(100m);
        var usd = new Money(100m, "USD");

        Assert.Throws<InvalidOperationException>(() => { var _ = zar + usd; });
    }

    [Fact]
    public void Subtract_SameCurrency_Succeeds()
    {
        var a = Money.Zar(100m);
        var b = Money.Zar(30m);

        var diff = a - b;

        Assert.Equal(70m, diff.Amount);
    }

    [Fact]
    public void UnaryMinus_NegatesAmount()
    {
        var money = Money.Zar(100m);

        var negated = -money;

        Assert.Equal(-100m, negated.Amount);
        Assert.Equal("ZAR", negated.Currency);
    }

    [Theory]
    [InlineData(100, false, true, false)]
    [InlineData(-50, true, false, false)]
    [InlineData(0, false, false, true)]
    public void SignChecks_ReturnExpectedFlags(decimal amount, bool isNeg, bool isPos, bool isZero)
    {
        var money = Money.Zar(amount);

        Assert.Equal(isNeg, money.IsNegative);
        Assert.Equal(isPos, money.IsPositive);
        Assert.Equal(isZero, money.IsZero);
    }

    [Fact]
    public void Abs_ReturnsAbsoluteValue()
    {
        var negative = Money.Zar(-100m);

        var abs = negative.Abs();

        Assert.Equal(100m, abs.Amount);
    }

    [Fact]
    public void ToString_Zar_FormatsWithRandSymbol()
    {
        var money = Money.Zar(1234.56m);

        var formatted = money.ToString();

        Assert.StartsWith("R", formatted);
        Assert.Contains("1", formatted);
        Assert.Contains("234", formatted);
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_Equal()
    {
        var a = Money.Zar(100m);
        var b = Money.Zar(100m);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentCurrency_NotEqual()
    {
        var zar = Money.Zar(100m);
        var usd = new Money(100m, "USD");

        Assert.NotEqual(zar, usd);
    }
}
