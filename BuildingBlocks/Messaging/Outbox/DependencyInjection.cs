using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxBackgroundService<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "OutboxProcessing")
        where TContext : class, BuildingBlocks.Messaging.Outbox.IOutboxDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<BuildingBlocks.Messaging.Outbox.OutboxProcessingSettings>(configuration.GetSection(sectionName));
        services.AddHostedService<BuildingBlocks.Messaging.Outbox.OutboxBackgroundService<TContext>>();

        return services;
    }
}
