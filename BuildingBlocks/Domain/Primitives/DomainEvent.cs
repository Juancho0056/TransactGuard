using MediatR;

namespace BuildingBlocks.Domain.Primitives;

public abstract record DomainEvent : INotification
{
    protected DomainEvent(DateTimeOffset occurredOn)
    {
        Id = Guid.NewGuid();
        OccurredOn = occurredOn;
    }

    public Guid Id { get; init; }

    public DateTimeOffset OccurredOn { get; init; }
}
