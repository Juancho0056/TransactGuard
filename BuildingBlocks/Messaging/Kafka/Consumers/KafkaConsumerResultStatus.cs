namespace BuildingBlocks.Messaging.Kafka.Consumers;

public enum KafkaConsumerResultStatus
{
    Success = 1,
    Retry = 2,
    DeadLetter = 3,
    Ignore = 4,
}
