using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AntiFraudService.Domain.ApprovedTransactions;

namespace AntiFraudService.Infrastructure.Data.Configurations;

public sealed class ApprovedTransactionConfiguration : IEntityTypeConfiguration<ApprovedTransaction>
{
    public void Configure(EntityTypeBuilder<ApprovedTransaction> builder)
    {
        builder.ToTable("approved_transactions");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entity => entity.TransactionExternalId)
            .HasColumnName("transaction_external_id")
            .IsRequired();

        builder.Property(entity => entity.SourceAccountId)
            .HasColumnName("source_account_id")
            .IsRequired();

        builder.Property(entity => entity.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(entity => entity.OccurredOnDate)
            .HasColumnName("occurred_on_date")
            .IsRequired();

        builder.Property(entity => entity.OccurredOn)
            .HasColumnName("occurred_on")
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(entity => new { entity.SourceAccountId, entity.OccurredOnDate });
        builder.HasIndex(entity => entity.TransactionExternalId).IsUnique();
    }
}
