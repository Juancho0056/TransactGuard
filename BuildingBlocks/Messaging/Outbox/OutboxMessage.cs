using System;
using BuildingBlocks.Domain.Guards;

namespace BuildingBlocks.Messaging.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid id,
        Guid? aggregateId,
        string type,
        string payload,
        DateTimeOffset occurredOnUtc)
    {
        Guard.AgainstEmptyGuid(id, parameterName: nameof(id));

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type is required", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload is required", nameof(payload));
        }

        Id = id;
        AggregateId = aggregateId;
        Type = type;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
        Status = OutboxMessageStatus.Pending;
        Retries = 0;
    }

    public Guid Id { get; private set; }

    public Guid? AggregateId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredOnUtc { get; private set; }

    public OutboxMessageStatus Status { get; private set; }

    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    public string? Error { get; private set; }

    public int Retries { get; private set; }

    public static OutboxMessage Create(
        Guid id,
        Guid? aggregateId,
        string type,
        string payload,
        DateTimeOffset occurredOnUtc)
        => new(id, aggregateId, type, payload, occurredOnUtc);

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    public void MarkFailed(string error, DateTimeOffset processedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error is required", nameof(error));
        }

        Status = OutboxMessageStatus.Failed;
        Error = error;
        Retries += 1;
        ProcessedOnUtc = processedOnUtc;
    }

    public void ResetToPending()
    {
        Status = OutboxMessageStatus.Pending;
        Error = null;
        ProcessedOnUtc = null;
    }
}
