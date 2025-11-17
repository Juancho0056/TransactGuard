using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.ValueObjects;

public abstract class StronglyTypedId<TSelf> : ValueObject
    where TSelf : StronglyTypedId<TSelf>
{
    protected StronglyTypedId(Guid value)
    {
        BuildingBlocks.Domain.Guards.Guard.AgainstEmptyGuid(value, nameof(value));
        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString();

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static TSelf Create(Guid value)
        => (TSelf)Activator.CreateInstance(typeof(TSelf), value)!;

    public static TSelf New() => Create(Guid.NewGuid());

    public static implicit operator Guid(StronglyTypedId<TSelf> id) => id.Value;
}
