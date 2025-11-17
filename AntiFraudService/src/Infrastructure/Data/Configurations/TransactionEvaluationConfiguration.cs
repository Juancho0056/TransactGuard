using AntiFraudService.Domain.TransactionEvaluations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiFraudService.Infrastructure.Data.Configurations;

public sealed class TransactionEvaluationConfiguration : IEntityTypeConfiguration<TransactionEvaluation>
{
    public void Configure(EntityTypeBuilder<TransactionEvaluation> builder)
    {
        builder.ToTable("transaction_evaluations");

        builder.HasKey(evaluation => evaluation.Id);

        builder.Property(evaluation => evaluation.Id)
            .HasColumnName("transaction_evaluation_id")
            .ValueGeneratedNever();

        builder.Property(evaluation => evaluation.TransactionExternalId)
            .HasColumnName("transaction_external_id")
            .IsRequired();

        builder.HasIndex(evaluation => evaluation.TransactionExternalId)
            .IsUnique();

        builder.Property(evaluation => evaluation.IsApproved)
            .HasColumnName("is_approved")
            .IsRequired();

        builder.Property(evaluation => evaluation.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(64);

        builder.Property(evaluation => evaluation.EvaluatedAt)
            .HasColumnName("evaluated_at")
            .IsRequired();

        builder.Property(evaluation => evaluation.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
