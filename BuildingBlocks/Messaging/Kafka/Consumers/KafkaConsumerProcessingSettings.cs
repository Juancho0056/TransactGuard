namespace BuildingBlocks.Messaging.Kafka.Consumers;

public sealed class KafkaConsumerProcessingSettings
{
    public int MaxRetries { get; init; } = 3;

    public double InitialBackoffSeconds { get; init; } = 2;

    public double MaxBackoffSeconds { get; init; } = 30;

    public bool EnableJitter { get; init; } = true;

    public double JitterSeconds { get; init; } = 0.5;
}
