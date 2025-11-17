using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BuildingBlocks.Messaging.Kafka.Consumers;

public abstract class KafkaConsumerWorker<TMessage> : BackgroundService
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IKafkaDeadLetterPublisher<TMessage>? _deadLetterPublisher;
    private readonly IConsumer<string, string> _consumer;

    protected KafkaConsumerWorker(
        ILogger logger,
        IOptions<KafkaConsumerSettings> consumerSettings,
        IOptions<KafkaConsumerProcessingSettings> processingSettings,
        JsonSerializerOptions serializerOptions,
        IKafkaDeadLetterPublisher<TMessage>? deadLetterPublisher = null,
        Action<ConsumerBuilder<string, string>>? configureBuilder = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _deadLetterPublisher = deadLetterPublisher;

        ConsumerSettings = consumerSettings?.Value ?? throw new ArgumentNullException(nameof(consumerSettings));
        ProcessingSettings = processingSettings?.Value ?? throw new ArgumentNullException(nameof(processingSettings));

        ValidateSettings();

        var config = CreateConsumerConfig();
        var builder = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                {
                    _logger.LogError("Fatal Kafka error: {Error}", error.Reason);
                }
                else
                {
                    _logger.LogWarning("Kafka error: {Error}", error.Reason);
                }
            });

        configureBuilder?.Invoke(builder);

        _consumer = builder.Build();
    }

    protected KafkaConsumerSettings ConsumerSettings { get; }

    protected KafkaConsumerProcessingSettings ProcessingSettings { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await EnsureTopicExistsAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Kafka topic {Topic} exists.", ConsumerSettings.Topic);
            throw;
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        _logger.LogInformation("Starting to consume Kafka topic {Topic} with group {GroupId}", ConsumerSettings.Topic, ConsumerSettings.GroupId);
        _consumer.Subscribe(ConsumerSettings.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result is null)
                {
                    continue;
                }

                await HandleConsumeResultAsync(result, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming Kafka message");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Kafka message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing Kafka message");
            }
        }

        _logger.LogInformation("Stopping Kafka consumer for topic {Topic}", ConsumerSettings.Topic);
    }

    protected abstract Task<KafkaConsumerResult<TMessage>> ProcessMessageAsync(
        KafkaConsumerContext<TMessage> context,
        CancellationToken cancellationToken);

    private ConsumerConfig CreateConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = ConsumerSettings.BootstrapServers,
            GroupId = ConsumerSettings.GroupId,
            EnableAutoCommit = ConsumerSettings.EnableAutoCommit,
            EnableAutoOffsetStore = ConsumerSettings.EnableAutoOffsetStore,
            AutoOffsetReset = ConsumerSettings.AutoOffsetReset,
        };

        if (ConsumerSettings.SessionTimeoutMs.HasValue)
        {
            config.SessionTimeoutMs = ConsumerSettings.SessionTimeoutMs;
        }

        if (ConsumerSettings.AdditionalConfig is not null)
        {
            foreach (var kvp in ConsumerSettings.AdditionalConfig)
            {
                config.Set(kvp.Key, kvp.Value);
            }
        }

        return config;
    }

    private async Task HandleConsumeResultAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        var payload = result.Message?.Value;
        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogWarning("Received empty Kafka message at {TopicPartitionOffset}", result.TopicPartitionOffset);
            return;
        }

        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(payload, _serializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Kafka message at {TopicPartitionOffset}", result.TopicPartitionOffset);
            await PublishDeadLetterForDeserializationAsync(result, payload, ex, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Unable to deserialize message at {TopicPartitionOffset}", result.TopicPartitionOffset);
            return;
        }

        var attempt = 0;
        var lastResult = default(KafkaConsumerResult<TMessage>);

        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            var context = new KafkaConsumerContext<TMessage>(result, message, payload, attempt);

            try
            {
                lastResult = await ProcessMessageAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing Kafka message at {TopicPartitionOffset}", result.TopicPartitionOffset);
                lastResult = KafkaConsumerResult<TMessage>.Retry(ex);
            }

            switch (lastResult.Status)
            {
                case KafkaConsumerResultStatus.Success:
                    await CommitAsync(result).ConfigureAwait(false);
                    return;
                case KafkaConsumerResultStatus.Ignore:
                    await CommitAsync(result).ConfigureAwait(false);
                    return;
                case KafkaConsumerResultStatus.DeadLetter:
                    await HandleDeadLetterAsync(context, lastResult, cancellationToken).ConfigureAwait(false);
                    await CommitAsync(result).ConfigureAwait(false);
                    return;
                case KafkaConsumerResultStatus.Retry:
                    if (attempt >= Math.Max(1, ProcessingSettings.MaxRetries))
                    {
                        await HandleDeadLetterAsync(context, lastResult, cancellationToken).ConfigureAwait(false);
                        await CommitAsync(result).ConfigureAwait(false);
                        return;
                    }

                    var delay = lastResult.RetryDelay ?? CalculateBackoff(attempt);

                    _logger.LogWarning(
                        lastResult.Exception,
                        "Attempt {Attempt} failed for message at {TopicPartitionOffset}. Retrying in {DelaySeconds} second(s).",
                        attempt,
                        result.TopicPartitionOffset,
                        delay.TotalSeconds);

                    try
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    continue;
            }
        }
    }

    private Task CommitAsync(ConsumeResult<string, string> result)
    {
        if (!ConsumerSettings.EnableAutoOffsetStore)
        {
            _consumer.StoreOffset(result);
        }

        if (!ConsumerSettings.EnableAutoCommit)
        {
            _consumer.Commit(result);
        }
        return Task.CompletedTask;
    }

    private async Task HandleDeadLetterAsync(
        KafkaConsumerContext<TMessage> context,
        KafkaConsumerResult<TMessage> result,
        CancellationToken cancellationToken)
    {
        if (result.DeadLetterAction is not null)
        {
            try
            {
                await result.DeadLetterAction(context, result.Exception ?? new InvalidOperationException("Dead-letter result requires an exception."), context.Attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing dead-letter action for message at {TopicPartitionOffset}", context.ConsumeResult.TopicPartitionOffset);
            }
        }

        if (_deadLetterPublisher is null || result.DeadLetterFactory is null || result.Exception is null)
        {
            _logger.LogWarning(
                "Dead-letter publisher or factory not configured for message at {TopicPartitionOffset}.",
                context.ConsumeResult.TopicPartitionOffset);
            return;
        }

        try
        {
            var deadLetter = result.DeadLetterFactory(context, result.Exception, context.Attempt);
            await _deadLetterPublisher.PublishAsync(deadLetter, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish dead-letter message for topic {Topic}", context.ConsumeResult.TopicPartitionOffset.Topic);
        }
    }

    private async Task PublishDeadLetterForDeserializationAsync(
        ConsumeResult<string, string> result,
        string payload,
        JsonException exception,
        CancellationToken cancellationToken)
    {
        if (_deadLetterPublisher is null)
        {
            return;
        }

        var deadLetter = new KafkaDeadLetter<TMessage>(
            result.Topic,
            result.TopicPartitionOffset,
            result.Message?.Key,
            payload,
            default!,
            exception,
            "deserialization_error",
            exception.Message,
            null);

        try
        {
            await _deadLetterPublisher.PublishAsync(deadLetter, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish deserialization dead-letter message for topic {Topic}", result.Topic);
        }
    }

    private async Task EnsureTopicExistsAsync(CancellationToken cancellationToken)
    {
        if (!ConsumerSettings.EnsureTopicExists)
        {
            return;
        }

        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = ConsumerSettings.BootstrapServers,
        };

        var attempts = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            attempts++;

            try
            {
                using var adminClient = new AdminClientBuilder(adminConfig).Build();

                var metadata = adminClient.GetMetadata(ConsumerSettings.Topic, TimeSpan.FromSeconds(5));
                var topicMetadata = metadata.Topics.FirstOrDefault(t => string.Equals(t.Topic, ConsumerSettings.Topic, StringComparison.Ordinal));
                if (topicMetadata is { Error.Code: ErrorCode.NoError or ErrorCode.TopicAlreadyExists or ErrorCode.UnknownTopicOrPart })
                {
                    if (topicMetadata.Error.Code != ErrorCode.UnknownTopicOrPart)
                    {
                        return;
                    }
                }

                var topicSpecification = new TopicSpecification
                {
                    Name = ConsumerSettings.Topic,
                    NumPartitions = Math.Max(1, ConsumerSettings.NumPartitions),
                    ReplicationFactor = (short)Math.Max(1, (int)ConsumerSettings.ReplicationFactor),
                };

                await adminClient.CreateTopicsAsync(new[] { topicSpecification }).ConfigureAwait(false);
                _logger.LogInformation(
                    "Created Kafka topic {Topic} with {Partitions} partition(s) and replication factor {ReplicationFactor}",
                    topicSpecification.Name,
                    topicSpecification.NumPartitions,
                    topicSpecification.ReplicationFactor);

                return;
            }
            catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                _logger.LogInformation("Kafka topic {Topic} already exists", ConsumerSettings.Topic);
                return;
            }
            catch (CreateTopicsException ex)
            {
                var error = ex.Results.FirstOrDefault()?.Error ?? ex.Error;
                _logger.LogWarning(
                    ex,
                    "Kafka rejected topic creation for {Topic} with error {Error}",
                    ConsumerSettings.Topic,
                    error.Reason);
            }
            catch (KafkaException ex)
            {
                _logger.LogWarning(ex, "Failed to ensure Kafka topic {Topic} exists on attempt {Attempt}", ConsumerSettings.Topic, attempts);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error ensuring Kafka topic {Topic} exists on attempt {Attempt}", ConsumerSettings.Topic, attempts);
            }

            if (ConsumerSettings.TopicCreationMaxAttempts > 0 && attempts >= ConsumerSettings.TopicCreationMaxAttempts)
            {
                throw new InvalidOperationException($"Unable to ensure Kafka topic '{ConsumerSettings.Topic}' exists after {attempts} attempt(s).");
            }

            var delayMilliseconds = Math.Max(100, ConsumerSettings.TopicCreationBackoffMs);

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private TimeSpan CalculateBackoff(int attempt)
    {
        var baseSeconds = Math.Max(0.5, ProcessingSettings.InitialBackoffSeconds);
        var exponent = Math.Max(0, attempt - 1);
        var delaySeconds = Math.Pow(2, exponent) * baseSeconds;
        var maxSeconds = Math.Max(baseSeconds, ProcessingSettings.MaxBackoffSeconds);

        if (delaySeconds > maxSeconds)
        {
            delaySeconds = maxSeconds;
        }

        if (ProcessingSettings.EnableJitter && ProcessingSettings.JitterSeconds > 0)
        {
            delaySeconds += Random.Shared.NextDouble() * ProcessingSettings.JitterSeconds;
        }

        return TimeSpan.FromSeconds(delaySeconds);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(ConsumerSettings.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers must be configured.");
        }

        if (string.IsNullOrWhiteSpace(ConsumerSettings.Topic))
        {
            throw new InvalidOperationException("Kafka topic must be configured.");
        }

        if (string.IsNullOrWhiteSpace(ConsumerSettings.GroupId))
        {
            throw new InvalidOperationException("Kafka group id must be configured.");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _consumer.Close();
        _consumer.Dispose();
    }
}
