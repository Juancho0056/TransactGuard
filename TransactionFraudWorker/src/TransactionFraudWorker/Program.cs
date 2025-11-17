using BuildingBlocks.Messaging.Kafka.Consumers;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TransactionFraudWorker;
using TransactionFraudWorker.Clients;
using TransactionFraudWorker.Dlq;
using TransactionFraudWorker.Models;
using TransactionFraudWorker.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<KafkaConsumerSettings>(context.Configuration.GetSection("Kafka"));
        services.Configure<AntiFraudApiSettings>(context.Configuration.GetSection("AntiFraudApi"));
        services.Configure<TransactionServiceApiSettings>(context.Configuration.GetSection("TransactionServiceApi"));
        services.Configure<KafkaDlqSettings>(context.Configuration.GetSection("KafkaDlq"));
        services.Configure<KafkaConsumerProcessingSettings>(context.Configuration.GetSection("Processing"));

        services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        services.AddHttpClient<IAntiFraudClient, AntiFraudClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<AntiFraudApiSettings>>().Value;
            client.BaseAddress = settings.BaseAddress ?? throw new InvalidOperationException("AntiFraudApi:BaseAddress is required.");
        });

        services.AddHttpClient<ITransactionServiceClient, TransactionServiceClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<TransactionServiceApiSettings>>().Value;
            client.BaseAddress = settings.BaseAddress ?? throw new InvalidOperationException("TransactionServiceApi:BaseAddress is required.");
        });

        services.AddSingleton<IKafkaDeadLetterPublisher<TransactionCreatedIntegrationEvent>, KafkaDeadLetterQueuePublisher>();

        services.AddHostedService<TransactionCreatedWorker>();
    })
    .Build();

await host.RunAsync();
