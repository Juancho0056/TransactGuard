using System;
using BuildingBlocks.Application.Abstractions.Data;
using BuildingBlocks.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAuditableDbContext<TContext, TInterface>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureOptions)
        where TContext : DbContext, TInterface
        where TInterface : class, IApplicationDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
            configureOptions(serviceProvider, options);
        });

        services.AddScoped<TInterface>(provider => provider.GetRequiredService<TContext>());

        return services;
    }

    public static IServiceCollection AddTimeProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BuildingBlocks.Domain.Interfaces.IUtcNowProvider, BuildingBlocks.Domain.Time.SystemUtcNowProvider>();
        services.Configure<BuildingBlocks.Domain.Time.TimeZoneOptions>(configuration.GetSection("TimeZone"));
        services.AddSingleton<BuildingBlocks.Domain.Interfaces.ITimeZoneProvider, BuildingBlocks.Domain.Time.ConfiguredTimeZoneProvider>();

        return services;
    }
}
