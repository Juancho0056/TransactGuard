using AntiFraudService.Application.Common.Interfaces;
using AntiFraudService.Domain.Ports;
using AntiFraudService.Infrastructure.Data;
using AntiFraudService.Infrastructure.Data.Adapters;
using BuildingBlocks.Application.Abstractions.Data;
using BuildingBlocks.Domain.Guards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("AntiFraudServiceDb");
        Guard.AgainstNull(connectionString, message: "Connection string 'AntiFraudServiceDb' not found.");

        builder.Services.AddAuditableDbContext<ApplicationDbContext, IApplicationDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddScoped<ITransactionsReadPort, TransactionsReadPort>();
        builder.Services.AddScoped<IApprovedTransactionsLedger, ApprovedTransactionsLedger>();
        builder.Services.AddScoped<ITransactionEvaluationsRepository, TransactionEvaluationsRepository>();

        builder.Services.AddTimeProviders(builder.Configuration);

        builder.Services.AddKafkaProducer(builder.Configuration);
    }
}
