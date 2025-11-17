using BuildingBlocks.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionService.Domain.Transactions;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("transaction_id")
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.ExternalId)
            .HasConversion(
                externalId => externalId.Value,
                value => TransactionExternalId.From(value))
            .HasColumnName("transaction_external_id")
            .HasMaxLength(36)
            .IsRequired();

        builder.HasIndex(transaction => transaction.ExternalId).IsUnique();

        builder.Property(transaction => transaction.SourceAccountId)
            .HasConversion(
                accountId => accountId.Value,
                value => AccountId.Create(value))
            .HasColumnName("source_account_id")
            .IsRequired();

        builder.Property(transaction => transaction.TargetAccountId)
            .HasConversion(
                accountId => accountId.Value,
                value => AccountId.Create(value))
            .HasColumnName("target_account_id")
            .IsRequired();

        builder.Property(transaction => transaction.TransferType)
            .HasConversion(
                transferType => transferType.Value,
                value => TransferTypeId.From(value))
            .HasColumnName("transfer_type_id")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(transaction => transaction.Status)
            .HasConversion<int>()
            .HasColumnName("status")
            .IsRequired();

        builder.Property(transaction => transaction.Description)
            .HasColumnName("description")
            .HasMaxLength(512);

        builder.Property(transaction => transaction.DecisionReason)
            .HasColumnName("decision_reason")
            .HasMaxLength(256);

        builder.Property(transaction => transaction.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(transaction => transaction.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.OwnsOne(transaction => transaction.Value, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.Navigation(transaction => transaction.Value)
            .IsRequired();
    }
}
