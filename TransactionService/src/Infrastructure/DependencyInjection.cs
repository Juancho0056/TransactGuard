using BuildingBlocks.Domain.Guards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Infrastructure.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("TransactionServiceDb");
        Guard.AgainstNull(connectionString, message: "Connection string 'TransactionServiceDb' not found.");

        builder.Services.AddAuditableDbContext<ApplicationDbContext, IApplicationDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddTimeProviders(builder.Configuration);

        builder.Services.AddKafkaProducer(builder.Configuration);
    }
}
