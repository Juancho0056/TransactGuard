namespace BuildingBlocks.Domain.ValueObjects;

public sealed class AccountId : StronglyTypedId<AccountId>
{
    public AccountId(Guid value) : base(value) { }
    public new static AccountId New() => new(Guid.NewGuid());
}