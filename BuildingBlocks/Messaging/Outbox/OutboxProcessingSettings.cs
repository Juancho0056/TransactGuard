namespace BuildingBlocks.Messaging.Outbox;

public sealed class OutboxProcessingSettings
{
    public int BatchSize { get; set; } = 50;

    public int PollingIntervalSeconds { get; set; } = 5;
}
