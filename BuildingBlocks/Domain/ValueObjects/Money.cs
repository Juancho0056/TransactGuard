using BuildingBlocks.Domain.Guards;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.ValueObjects;

public sealed class Money : ValueObject, IComparable<Money>
{
    private const int MaxDecimalPlaces = 2;

    private Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Money amount cannot be negative.");
        }

        if (decimal.Round(amount, MaxDecimalPlaces) != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, $"Money amount cannot have more than {MaxDecimalPlaces} decimal places.");
        }

        Amount = amount;
    }

    public decimal Amount { get; }

    public static Money Zero() => new(0);

    public static Money From(decimal amount) => new(amount);

    public Money Add(Money other)
    {
        Guard.AgainstNull(other, parameterName: nameof(other));
        return From(Amount + other.Amount);
    }

    public Money Subtract(Money other)
    {
        Guard.AgainstNull(other, parameterName: nameof(other));
        var result = Amount - other.Amount;
        if (result < 0)
        {
            throw new InvalidOperationException("Money result cannot be negative.");
        }

        return From(result);
    }

    public int CompareTo(Money? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Amount.CompareTo(other.Amount);
    }

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
    }
}
