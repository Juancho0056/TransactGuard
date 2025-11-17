using System.Collections.Generic;
using Confluent.Kafka;

namespace BuildingBlocks.Messaging.Kafka.Consumers;

public sealed class KafkaConsumerSettings
{
    public string BootstrapServers { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;

    public bool EnableAutoCommit { get; init; }

    public bool EnableAutoOffsetStore { get; init; }

    public int? SessionTimeoutMs { get; init; }

    public AutoOffsetReset AutoOffsetReset { get; init; } = AutoOffsetReset.Earliest;

    public bool EnsureTopicExists { get; init; } = true;

    public int TopicCreationMaxAttempts { get; init; } = 10;

    public int TopicCreationBackoffMs { get; init; } = 2_000;

    public int NumPartitions { get; init; } = 1;

    public short ReplicationFactor { get; init; } = 1;

    public IDictionary<string, string>? AdditionalConfig { get; init; }
        = new Dictionary<string, string>();
}
