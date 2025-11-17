using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Domain.Transactions;

namespace TransactionService.Infrastructure.Data;

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

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }
}
