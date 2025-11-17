using BuildingBlocks.Application.Abstractions.Messaging;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaProducer(this IServiceCollection services, IConfiguration configuration, string sectionName = "Kafka")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<BuildingBlocks.Messaging.Kafka.KafkaSettings>(configuration.GetSection(sectionName));
        services.AddSingleton<IKafkaProducer, BuildingBlocks.Messaging.Kafka.KafkaProducer>();

        return services;
    }
}
