using BuildingBlocks.Domain.Primitives;

namespace AntiFraudService.Domain.TransactionEvaluations;

public sealed class TransactionEvaluation : Entity
{
    private TransactionEvaluation()
    {
    }

    private TransactionEvaluation(
        Guid id,
        Guid transactionExternalId,
        bool isApproved,
        string? reasonCode,
        DateTimeOffset evaluatedAt,
        DateTimeOffset createdAt) : base(id)
    {
        TransactionExternalId = transactionExternalId;
        IsApproved = isApproved;
        ReasonCode = reasonCode;
        EvaluatedAt = evaluatedAt;
        CreatedAt = createdAt;
    }

    public Guid TransactionExternalId { get; private set; }

    public bool IsApproved { get; private set; }

    public string? ReasonCode { get; private set; }

    public DateTimeOffset EvaluatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static TransactionEvaluation Create(
        Guid transactionExternalId,
        bool isApproved,
        string? reasonCode,
        DateTimeOffset evaluatedAt,
        DateTimeOffset createdAt)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reasonCode)
            ? null
            : reasonCode.Trim().ToUpperInvariant();

        return new TransactionEvaluation(
            Guid.NewGuid(),
            transactionExternalId,
            isApproved,
            normalizedReason,
            evaluatedAt,
            createdAt);
    }
}
