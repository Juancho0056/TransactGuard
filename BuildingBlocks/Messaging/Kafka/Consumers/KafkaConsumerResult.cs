using System;

namespace BuildingBlocks.Messaging.Kafka.Consumers;

public sealed class KafkaConsumerResult<TMessage>
{
    private KafkaConsumerResult(
        KafkaConsumerResultStatus status,
        Exception? exception,
        TimeSpan? retryDelay,
        KafkaDeadLetterFactory<TMessage>? deadLetterFactory,
        KafkaDeadLetterAction<TMessage>? deadLetterAction)
    {
        Status = status;
        Exception = exception;
        RetryDelay = retryDelay;
        DeadLetterFactory = deadLetterFactory;
        DeadLetterAction = deadLetterAction;
    }

    public KafkaConsumerResultStatus Status { get; }

    public Exception? Exception { get; }

    public TimeSpan? RetryDelay { get; }

    public KafkaDeadLetterFactory<TMessage>? DeadLetterFactory { get; }

    public KafkaDeadLetterAction<TMessage>? DeadLetterAction { get; }

    public static KafkaConsumerResult<TMessage> Success() =>
        new(KafkaConsumerResultStatus.Success, null, null, null, null);

    public static KafkaConsumerResult<TMessage> Ignore() =>
        new(KafkaConsumerResultStatus.Ignore, null, null, null, null);

    public static KafkaConsumerResult<TMessage> Retry(
        Exception exception,
        TimeSpan? retryDelay = null,
        KafkaDeadLetterFactory<TMessage>? deadLetterFactory = null,
        KafkaDeadLetterAction<TMessage>? deadLetterAction = null) =>
        new(KafkaConsumerResultStatus.Retry, exception ?? throw new ArgumentNullException(nameof(exception)), retryDelay, deadLetterFactory, deadLetterAction);

    public static KafkaConsumerResult<TMessage> DeadLetter(
        Exception exception,
        KafkaDeadLetterFactory<TMessage> deadLetterFactory,
        KafkaDeadLetterAction<TMessage>? deadLetterAction = null) =>
        new(KafkaConsumerResultStatus.DeadLetter, exception ?? throw new ArgumentNullException(nameof(exception)), null, deadLetterFactory ?? throw new ArgumentNullException(nameof(deadLetterFactory)), deadLetterAction);
}
