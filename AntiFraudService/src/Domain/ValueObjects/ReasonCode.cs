using BuildingBlocks.Domain.Primitives;

namespace AntiFraudService.Domain.ValueObjects;

public sealed class ReasonCode : ValueObject
{
    private ReasonCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El código de razón es obligatorio.", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    public static readonly ReasonCode SingleLimitExceeded = new("SINGLE_LIMIT_EXCEEDED");
    public static readonly ReasonCode DailyLimitExceeded = new("DAILY_LIMIT_EXCEEDED");

    public static ReasonCode From(string value) => new(value);

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
