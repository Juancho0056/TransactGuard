using System;

namespace TransactionFraudWorker.Models;

public sealed record EvaluationResultDto(
    Guid TransactionExternalId,
    bool IsApproved,
    ReasonCodeDto? Reason,
    DateTimeOffset EvaluatedAt);

public sealed record ReasonCodeDto(string Value);
