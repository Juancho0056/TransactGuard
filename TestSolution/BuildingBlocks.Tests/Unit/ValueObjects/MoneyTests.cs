using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace BuildingBlocks.Tests.Unit.ValueObjects;

public sealed class MoneyTests
{
    [Test]
    public void From_ShouldCreateMoneyWithAmount()
    {
        var money = Money.From(10);

        money.Amount.ShouldBe(10);
    }

    [Test]
    public void Zero_ShouldCreateZeroMoney()
    {
        var money = Money.Zero();

        money.Amount.ShouldBe(0);
    }

    [Test]
    public void Add_ShouldReturnSum()
    {
        var left = Money.From(10);
        var right = Money.From(5);

        var result = left.Add(right);

        result.Amount.ShouldBe(15);
    }

    [Test]
    public void Subtract_ShouldReturnDifference_WhenResultIsNonNegative()
    {
        var left = Money.From(10);
        var right = Money.From(3);

        var result = left.Subtract(right);

        result.Amount.ShouldBe(7);
    }

    [Test]
    public void Subtract_ShouldThrow_WhenResultWouldBeNegative()
    {
        var left = Money.From(3);
        var right = Money.From(4);

        Should.Throw<InvalidOperationException>(() => left.Subtract(right));
    }

    [Test]
    public void CompareTo_ShouldRespectAmount()
    {
        var smaller = Money.From(1);
        var larger = Money.From(2);

        smaller.CompareTo(larger).ShouldBeLessThan(0);
        larger.CompareTo(smaller).ShouldBeGreaterThan(0);
        smaller.CompareTo(smaller).ShouldBe(0);
    }

    [Test]
    public void From_ShouldThrow_WhenAmountHasMoreThanTwoDecimals()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Money.From(1.123m));
    }

    [Test]
    public void From_ShouldThrow_WhenAmountIsNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Money.From(-1));
    }
}
