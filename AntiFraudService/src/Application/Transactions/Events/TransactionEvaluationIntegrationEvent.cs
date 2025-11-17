using AntiFraudService.Domain.ValueObjects;

namespace AntiFraudService.Application.Transactions.Events;

public sealed record TransactionEvaluationIntegrationEvent(
    Guid TransactionExternalId,
    bool IsApproved,
    ReasonCode? Reason,
    DateTimeOffset EvaluatedAt);
