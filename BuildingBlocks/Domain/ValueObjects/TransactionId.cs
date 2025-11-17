namespace BuildingBlocks.Domain.ValueObjects;


public sealed class TransactionId : StronglyTypedId<TransactionId>
{
    public TransactionId(Guid value) : base(value) { }

    public new static TransactionId New() => new(Guid.NewGuid());
}