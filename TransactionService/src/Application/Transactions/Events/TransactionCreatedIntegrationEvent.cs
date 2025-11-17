namespace TransactionService.Application.Transactions.Events;

public sealed record TransactionCreatedIntegrationEvent(
    Guid TransactionExternalId,
    Guid SourceAccountId,
    Guid TargetAccountId,
    string TransferType,
    decimal Value,
    DateTimeOffset CreatedAt);
