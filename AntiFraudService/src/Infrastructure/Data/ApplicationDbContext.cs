using System;
using System.Linq;
using System.Reflection;
using AntiFraudService.Domain.ApprovedTransactions;
using AntiFraudService.Domain.TransactionEvaluations;
using BuildingBlocks.Application.Abstractions.Data;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AntiFraudService.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly TimeZoneInfo _timeZone;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITimeZoneProvider timeZoneProvider)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(timeZoneProvider);
        _timeZone = timeZoneProvider.GetTimeZone();
    }

    public DbSet<ApprovedTransaction> ApprovedTransactions => Set<ApprovedTransaction>();

    public DbSet<TransactionEvaluation> TransactionEvaluations => Set<TransactionEvaluation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<DomainEvent>();

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ConfigureDateTimeOffsetConversions(builder);
    }

    private void ConfigureDateTimeOffsetConversions(ModelBuilder builder)
    {
        var toUtcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            value => value.ToUniversalTime(),
            value => TimeZoneInfo.ConvertTime(value, _timeZone));

        var toUtcNullableConverter = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            value => value.HasValue ? value.Value.ToUniversalTime() : value,
            value => value.HasValue ? TimeZoneInfo.ConvertTime(value.Value, _timeZone) : value);

        foreach (var property in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
        {
            if (property.ClrType == typeof(DateTimeOffset))
            {
                property.SetValueConverter(toUtcConverter);
            }
            else if (property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(toUtcNullableConverter);
            }
        }
    }
}
