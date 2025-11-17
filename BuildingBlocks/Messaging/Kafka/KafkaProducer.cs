using BuildingBlocks.Application.Abstractions.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging.Kafka;

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<string, string> _producer;
    private bool _disposed;

    public KafkaProducer(IOptions<KafkaSettings> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var settings = options.Value;
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            ClientId = string.IsNullOrWhiteSpace(settings.ClientId) ? Environment.MachineName : settings.ClientId
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KafkaProducer));
        }

        try
        {
            var message = new Message<string, string> { Key = key, Value = value };
            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Delivered Kafka message to {TopicPartitionOffset}", deliveryResult.TopicPartitionOffset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to deliver Kafka message to topic {Topic}", topic);
            throw;
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
