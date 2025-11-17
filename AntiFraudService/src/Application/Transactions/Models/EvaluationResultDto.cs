using AntiFraudService.Domain.ValueObjects;

namespace AntiFraudService.Application.Transactions.Models;

public sealed record EvaluationResultDto(
    Guid TransactionExternalId,
    bool IsApproved,
    ReasonCode? Reason,
    DateTimeOffset EvaluatedAt);
