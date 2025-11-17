using BuildingBlocks.Domain.Messages;
using BuildingBlocks.Domain.Primitives;

namespace TransactionService.Domain.Transactions.ValueObjects;

public sealed class TransferTypeId : ValueObject
{
    private TransferTypeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(DefaultMessage.IsRequired, nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    public static TransferTypeId From(string value) => new(value);

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
