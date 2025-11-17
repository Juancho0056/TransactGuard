using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Messaging.Kafka.Consumers;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionFraudWorker.Models;
using TransactionFraudWorker.Options;

namespace TransactionFraudWorker.Dlq;

public sealed class KafkaDeadLetterQueuePublisher : IKafkaDeadLetterPublisher<TransactionCreatedIntegrationEvent>, IDisposable
{
    private readonly ILogger<KafkaDeadLetterQueuePublisher> _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly KafkaDlqSettings _settings;
    private readonly IProducer<string, string> _producer;

    public KafkaDeadLetterQueuePublisher(
        IOptions<KafkaDlqSettings> settings,
        ILogger<KafkaDeadLetterQueuePublisher> logger,
        JsonSerializerOptions serializerOptions)
    {
        _settings = settings.Value;
        _logger = logger;
        _serializerOptions = serializerOptions;

        if (string.IsNullOrWhiteSpace(_settings.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka DLQ bootstrap servers must be configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Topic))
        {
            throw new InvalidOperationException("Kafka DLQ topic must be configured.");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = MapAcks(_settings.Acks),
            EnableIdempotence = _settings.Acks == DlqAcks.All,
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                {
                    _logger.LogError("Fatal Kafka producer error: {Error}", error.Reason);
                }
                else
                {
                    _logger.LogWarning("Kafka producer warning: {Error}", error.Reason);
                }
            })
            .Build();
    }

    public async Task PublishAsync(
        KafkaDeadLetter<TransactionCreatedIntegrationEvent> deadLetter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deadLetter);

        var failureType = ResolveFailureType(deadLetter.Metadata);
        var lastKnownStatus = ResolveLastKnownStatus(deadLetter.Metadata);

        var transactionId = ResolveTransactionId(deadLetter);
        if (transactionId == Guid.Empty)
        {
            _logger.LogWarning(
                "Dead-letter message for topic {Topic} at {Offset} does not include a transaction identifier.",
                deadLetter.Topic,
                deadLetter.TopicPartitionOffset);
        }
        var message = new DlqMessage(
            transactionId,
            deadLetter.Payload,
            failureType,
            deadLetter.Exception.GetType().FullName ?? deadLetter.Exception.GetType().Name,
            deadLetter.Exception.Message,
            deadLetter.Exception.StackTrace,
            lastKnownStatus,
            DateTimeOffset.UtcNow);

        var payload = JsonSerializer.Serialize(message, _serializerOptions);

        _logger.LogWarning(
            deadLetter.Exception,
            "Sending transaction {TransactionId} to DLQ topic {Topic} with failure type {FailureType}",
            transactionId,
            _settings.Topic,
            failureType);

        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = ResolveMessageKey(deadLetter, transactionId),
                Value = payload,
            };

            await _producer.ProduceAsync(_settings.Topic, kafkaMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (ProduceException<string, string> produceException)
        {
            _logger.LogError(
                produceException,
                "Failed to publish transaction {TransactionId} to DLQ topic {Topic}",
                transactionId,
                _settings.Topic);

            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
    }

    private static Confluent.Kafka.Acks MapAcks(DlqAcks acks)
    {
        return acks switch
        {
            DlqAcks.All => Confluent.Kafka.Acks.All,
            _ => Confluent.Kafka.Acks.Leader,
        };
    }

    private static FailureType ResolveFailureType(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is not null && metadata.TryGetValue(DeadLetterMetadataKeys.FailureType, out var value))
        {
            if (value is FailureType typed)
            {
                return typed;
            }

            if (value is string stringValue && Enum.TryParse<FailureType>(stringValue, out var parsed))
            {
                return parsed;
            }
        }

        return FailureType.Unexpected;
    }

    private static TransactionStatus? ResolveLastKnownStatus(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is not null && metadata.TryGetValue(DeadLetterMetadataKeys.LastKnownStatus, out var value))
        {
            if (value is TransactionStatus status)
            {
                return status;
            }

            if (value is string stringValue && Enum.TryParse<TransactionStatus>(stringValue, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static Guid ResolveTransactionId(KafkaDeadLetter<TransactionCreatedIntegrationEvent> deadLetter)
    {
        if (deadLetter.Message is TransactionCreatedIntegrationEvent message)
        {
            return message.TransactionExternalId;
        }

        if (!string.IsNullOrWhiteSpace(deadLetter.Key) && Guid.TryParse(deadLetter.Key, out var parsedFromKey))
        {
            return parsedFromKey;
        }

        return Guid.Empty;
    }

    private static string ResolveMessageKey(KafkaDeadLetter<TransactionCreatedIntegrationEvent> deadLetter, Guid transactionId)
    {
        if (!string.IsNullOrWhiteSpace(deadLetter.Key))
        {
            return deadLetter.Key;
        }

        return transactionId != Guid.Empty ? transactionId.ToString() : Guid.NewGuid().ToString();
    }
}
