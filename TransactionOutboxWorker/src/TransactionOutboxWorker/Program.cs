using BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionOutboxWorker.Publishing;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Infrastructure.Data;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddTimeProviders(configuration);

        var connectionString = configuration.GetConnectionString("TransactionServiceDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'TransactionServiceDb' is required.");
        }

        services.AddAuditableDbContext<ApplicationDbContext, IApplicationDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddKafkaProducer(configuration);

        services.AddSingleton<IOutboxTopicResolver, OutboxTopicResolver>();

        services.AddOutboxBackgroundService<IApplicationDbContext>(configuration, "Processing");
    })
    .Build();

await host.RunAsync();
