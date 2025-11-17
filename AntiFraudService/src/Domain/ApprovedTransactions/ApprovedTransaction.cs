using BuildingBlocks.Domain.Primitives;

namespace AntiFraudService.Domain.ApprovedTransactions;

public sealed class ApprovedTransaction : Entity
{
    public Guid TransactionExternalId { get; private set; }

    public Guid SourceAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly OccurredOnDate { get; private set; }

    public DateTimeOffset OccurredOn { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private ApprovedTransaction()
    {
    }

    private ApprovedTransaction(
        Guid id,
        Guid transactionExternalId,
        Guid sourceAccountId,
        decimal amount,
        DateOnly occurredOnDate,
        DateTimeOffset occurredOn,
        DateTimeOffset createdAt) : base(id)
    {
        TransactionExternalId = transactionExternalId;
        SourceAccountId = sourceAccountId;
        Amount = amount;
        OccurredOnDate = occurredOnDate;
        OccurredOn = occurredOn;
        CreatedAt = createdAt;
    }

    public static ApprovedTransaction Create(
        Guid transactionExternalId,
        Guid sourceAccountId,
        decimal amount,
        DateOnly occurredOnDate,
        DateTimeOffset occurredOn,
        DateTimeOffset createdAt)
    {
        return new ApprovedTransaction(
            Guid.NewGuid(),
            transactionExternalId,
            sourceAccountId,
            amount,
            occurredOnDate,
            occurredOn,
            createdAt);
    }
}
