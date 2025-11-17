using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.Kafka.Consumers;

public delegate KafkaDeadLetter<TMessage> KafkaDeadLetterFactory<TMessage>(
    KafkaConsumerContext<TMessage> context,
    Exception exception,
    int attempt);

public delegate Task KafkaDeadLetterAction<TMessage>(
    KafkaConsumerContext<TMessage> context,
    Exception exception,
    int attempt,
    CancellationToken cancellationToken);
