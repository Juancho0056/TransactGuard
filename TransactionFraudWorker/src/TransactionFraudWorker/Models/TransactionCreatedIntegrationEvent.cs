using System;

namespace TransactionFraudWorker.Models;

public sealed record TransactionCreatedIntegrationEvent(
    Guid TransactionExternalId,
    Guid SourceAccountId,
    Guid TargetAccountId,
    string TransferType,
    decimal Value,
    DateTimeOffset CreatedAt);
