using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Messaging.Kafka.Consumers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionFraudWorker.Clients;
using TransactionFraudWorker.Dlq;
using TransactionFraudWorker.Models;

namespace TransactionFraudWorker;

public sealed class TransactionCreatedWorker : KafkaConsumerWorker<TransactionCreatedIntegrationEvent>
{
    private readonly ILogger<TransactionCreatedWorker> _logger;
    private readonly IAntiFraudClient _antiFraudClient;
    private readonly ITransactionServiceClient _transactionServiceClient;

    public TransactionCreatedWorker(
        ILogger<TransactionCreatedWorker> logger,
        IAntiFraudClient antiFraudClient,
        ITransactionServiceClient transactionServiceClient,
        IKafkaDeadLetterPublisher<TransactionCreatedIntegrationEvent> deadLetterPublisher,
        IOptions<KafkaConsumerSettings> consumerSettings,
        IOptions<KafkaConsumerProcessingSettings> processingSettings,
        JsonSerializerOptions serializerOptions)
        : base(logger, consumerSettings, processingSettings, serializerOptions, deadLetterPublisher)
    {
        _logger = logger;
        _antiFraudClient = antiFraudClient;
        _transactionServiceClient = transactionServiceClient;
    }

    protected override async Task<KafkaConsumerResult<TransactionCreatedIntegrationEvent>> ProcessMessageAsync(
        KafkaConsumerContext<TransactionCreatedIntegrationEvent> context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var integrationEvent = context.Message;
        TransactionStatus? lastKnownStatus = null;

        _logger.LogInformation(
            "Processing transaction {TransactionId} from topic {Topic}",
            integrationEvent.TransactionExternalId,
            context.ConsumeResult.Topic);

        try
        {
            var evaluation = await _antiFraudClient
                .EvaluateTransactionAsync(integrationEvent, cancellationToken)
                .ConfigureAwait(false);

            var targetStatus = evaluation.IsApproved ? TransactionStatus.Approved : TransactionStatus.Rejected;
            lastKnownStatus = targetStatus;

            await _transactionServiceClient
                .UpdateStatusAsync(
                    integrationEvent.TransactionExternalId,
                    targetStatus,
                    evaluation.Reason?.Value,
                    cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Transaction {TransactionId} evaluated as {Status}",
                integrationEvent.TransactionExternalId,
                targetStatus);

            return KafkaConsumerResult<TransactionCreatedIntegrationEvent>.Success();
        }
        catch (AntiFraudPermanentException ex)
        {
            _logger.LogError(
                ex,
                "Permanent failure in antifraud for transaction {TransactionId}",
                integrationEvent.TransactionExternalId);

            return KafkaConsumerResult<TransactionCreatedIntegrationEvent>.DeadLetter(
                ex,
                (ctx, exception, _) => CreateDeadLetter(
                    ctx,
                    exception,
                    FailureType.Permanent,
                    lastKnownStatus,
                    "antifraud_permanent_error",
                    BuildFailureReason("Error permanente en antifraude", exception)),
                (ctx, exception, _, token) => MarkTransactionAsFailedAsync(
                    ctx.Message,
                    BuildFailureReason("Error permanente en antifraude", exception),
                    token));
        }
        catch (TransactionServicePermanentException ex)
        {
            _logger.LogError(
                ex,
                "Permanent failure while updating transaction {TransactionId}",
                integrationEvent.TransactionExternalId);

            return KafkaConsumerResult<TransactionCreatedIntegrationEvent>.DeadLetter(
                ex,
                (ctx, exception, _) => CreateDeadLetter(
                    ctx,
                    exception,
                    FailureType.Permanent,
                    lastKnownStatus,
                    "transaction_permanent_error",
                    BuildFailureReason("Error permanente al actualizar la transacción", exception)),
                (ctx, exception, _, token) => MarkTransactionAsFailedAsync(
                    ctx.Message,
                    BuildFailureReason("Error permanente al actualizar la transacción", exception),
                    token));
        }
        catch (AntiFraudTransientException ex)
        {
            return CreateRetryResult(
                context,
                ex,
                lastKnownStatus,
                "antifraud_transient_error",
                "Error transitorio en antifraude",
                FailureType.TransientExhausted,
                integrationEvent.TransactionExternalId);
        }
        catch (TransactionServiceTransientException ex)
        {
            return CreateRetryResult(
                context,
                ex,
                lastKnownStatus,
                "transaction_transient_error",
                "Error transitorio al actualizar la transacción",
                FailureType.TransientExhausted,
                integrationEvent.TransactionExternalId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CreateRetryResult(
                context,
                ex,
                lastKnownStatus,
                "unexpected_processing_error",
                "Error inesperado durante el procesamiento",
                FailureType.Unexpected,
                integrationEvent.TransactionExternalId);
        }
    }

    private KafkaConsumerResult<TransactionCreatedIntegrationEvent> CreateRetryResult(
        KafkaConsumerContext<TransactionCreatedIntegrationEvent> context,
        Exception exception,
        TransactionStatus? lastKnownStatus,
        string failureCode,
        string failureReasonPrefix,
        FailureType failureType,
        Guid transactionExternalId)
    {
        var maxRetries = Math.Max(1, ProcessingSettings.MaxRetries);

        _logger.LogWarning(
            exception,
            "Attempt {Attempt} failed for transaction {TransactionId}. Max retries: {MaxRetries}",
            context.Attempt,
            transactionExternalId,
            maxRetries);

        return KafkaConsumerResult<TransactionCreatedIntegrationEvent>.Retry(
            exception,
            deadLetterFactory: (ctx, ex, _) => CreateDeadLetter(
                ctx,
                ex,
                failureType,
                lastKnownStatus,
                failureCode,
                BuildFailureReason($"{failureReasonPrefix}. Se agotaron los reintentos", ex)),
            deadLetterAction: async (ctx, ex, _, token) =>
            {
                var reason = BuildFailureReason($"{failureReasonPrefix}. Se agotaron los reintentos", ex);
                await MarkTransactionAsFailedAsync(ctx.Message, reason, token).ConfigureAwait(false);
            });
    }

    private async Task MarkTransactionAsFailedAsync(
        TransactionCreatedIntegrationEvent integrationEvent,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await _transactionServiceClient
                .UpdateStatusAsync(
                    integrationEvent.TransactionExternalId,
                    TransactionStatus.AntiFraudFailed,
                    reason,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TransactionServiceClientException ex)
        {
            _logger.LogError(
                ex,
                "Failed to mark transaction {TransactionId} as AntiFraudFailed",
                integrationEvent.TransactionExternalId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error when marking transaction {TransactionId} as AntiFraudFailed",
                integrationEvent.TransactionExternalId);
        }
    }

    private static KafkaDeadLetter<TransactionCreatedIntegrationEvent> CreateDeadLetter(
        KafkaConsumerContext<TransactionCreatedIntegrationEvent> context,
        Exception exception,
        FailureType failureType,
        TransactionStatus? lastKnownStatus,
        string failureCode,
        string failureReason)
    {
        var metadata = new Dictionary<string, object?>
        {
            [DeadLetterMetadataKeys.FailureType] = failureType,
            [DeadLetterMetadataKeys.LastKnownStatus] = lastKnownStatus,
        };

        return new KafkaDeadLetter<TransactionCreatedIntegrationEvent>(
            context.ConsumeResult.Topic,
            context.ConsumeResult.TopicPartitionOffset,
            context.ConsumeResult.Message?.Key,
            context.Payload,
            context.Message,
            exception,
            failureCode,
            failureReason,
            metadata);
    }

    private static string BuildFailureReason(string prefix, Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? prefix
            : $"{prefix}: {exception.Message}";

        return message.Length <= 256
            ? message
            : message[..256];
    }
}
