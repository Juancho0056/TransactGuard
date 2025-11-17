using System;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.ValueObjects;
using BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Domain.Transactions;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Tests.Common;

internal sealed class TestTransactionDbContext : DbContext, IApplicationDbContext
{
    public TestTransactionDbContext(DbContextOptions<TestTransactionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<DomainEvent>();

        ConfigureTransaction(builder.Entity<Transaction>());
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => Task.FromResult<IDbContextTransaction>(new TestDbContextTransaction());

    private static void ConfigureTransaction(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.ExternalId)
            .HasConversion(
                externalId => externalId.Value,
                value => TransactionExternalId.From(value));

        builder.Property(transaction => transaction.SourceAccountId)
            .HasConversion(
                accountId => accountId.Value,
                value => AccountId.Create(value));

        builder.Property(transaction => transaction.TargetAccountId)
            .HasConversion(
                accountId => accountId.Value,
                value => AccountId.Create(value));

        builder.Property(transaction => transaction.TransferType)
            .HasConversion(
                transferType => transferType.Value,
                value => TransferTypeId.From(value));

        builder.OwnsOne(transaction => transaction.Value, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasPrecision(18, 2);
        });

        builder.Navigation(transaction => transaction.Value)
            .IsRequired();
    }
}

internal sealed class TestDbContextTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.NewGuid();

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public void Commit()
    {
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Rollback()
    {
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
