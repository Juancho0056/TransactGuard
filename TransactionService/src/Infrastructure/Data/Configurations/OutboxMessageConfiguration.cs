using BuildingBlocks.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TransactionService.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.AggregateId)
            .HasColumnName("aggregate_id");

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.OccurredOnUtc)
            .HasColumnName("occurred_on_utc")
            .IsRequired();

        builder.Property(message => message.Status)
            .HasConversion<short>()
            .HasColumnName("status")
            .IsRequired();

        builder.Property(message => message.ProcessedOnUtc)
            .HasColumnName("processed_on_utc");

        builder.Property(message => message.Error)
            .HasColumnName("error")
            .HasMaxLength(1024);

        builder.Property(message => message.Retries)
            .HasColumnName("retries")
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasIndex(message => new { message.Status, message.OccurredOnUtc });
    }
}
