using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.Abstractions.Messaging;
using BuildingBlocks.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging.Outbox;

public sealed class OutboxBackgroundService<TContext> : BackgroundService
    where TContext : notnull, IOutboxDbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService<TContext>> _logger;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IUtcNowProvider _utcNowProvider;
    private readonly IOutboxTopicResolver _topicResolver;
    private readonly OutboxProcessingSettings _settings;
    private readonly TimeSpan _pollingInterval;

    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxBackgroundService<TContext>> logger,
        IKafkaProducer kafkaProducer,
        IUtcNowProvider utcNowProvider,
        IOutboxTopicResolver topicResolver,
        IOptions<OutboxProcessingSettings> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _utcNowProvider = utcNowProvider;
        _topicResolver = topicResolver;
        _settings = options.Value;
        _pollingInterval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollingIntervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processedAny = await ProcessPendingMessagesAsync(stoppingToken).ConfigureAwait(false);

            if (!processedAny)
            {
                await Task.Delay(_pollingInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<bool> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        var pendingMessages = await context.OutboxMessages
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(Math.Max(1, _settings.BatchSize))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pendingMessages.Count == 0)
        {
            return false;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                var topic = _topicResolver.ResolveTopic(message.Type);
                var key = message.AggregateId?.ToString() ?? message.Id.ToString();

                await _kafkaProducer
                    .ProduceAsync(topic, key, message.Payload, cancellationToken)
                    .ConfigureAwait(false);

                message.MarkProcessed(_utcNowProvider.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                message.MarkFailed(ex.Message, _utcNowProvider.UtcNow);
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
