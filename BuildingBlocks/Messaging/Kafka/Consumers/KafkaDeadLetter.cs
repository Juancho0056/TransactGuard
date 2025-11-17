using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace BuildingBlocks.Messaging.Kafka.Consumers;

public sealed record KafkaDeadLetter<TMessage>(
    string Topic,
    TopicPartitionOffset TopicPartitionOffset,
    string? Key,
    string Payload,
    TMessage Message,
    Exception Exception,
    string FailureCode,
    string? FailureReason,
    IReadOnlyDictionary<string, object?>? Metadata);
