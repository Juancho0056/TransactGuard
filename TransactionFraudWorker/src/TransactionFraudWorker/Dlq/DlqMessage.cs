using System;
using TransactionFraudWorker.Models;

namespace TransactionFraudWorker.Dlq;

public sealed record DlqMessage(
    Guid TransactionExternalId,
    string OriginalPayload,
    FailureType FailureType,
    string ExceptionType,
    string ExceptionMessage,
    string? ExceptionStackTrace,
    TransactionStatus? LastKnownStatus,
    DateTimeOffset FailedAtUtc);
