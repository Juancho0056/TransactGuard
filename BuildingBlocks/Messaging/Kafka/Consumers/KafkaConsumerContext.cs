using Confluent.Kafka;

namespace BuildingBlocks.Messaging.Kafka.Consumers;

public sealed record KafkaConsumerContext<TMessage>(
    ConsumeResult<string, string> ConsumeResult,
    TMessage Message,
    string Payload,
    int Attempt);
