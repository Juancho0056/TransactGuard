namespace BuildingBlocks.Messaging.Kafka;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string? ClientId { get; set; }
}
