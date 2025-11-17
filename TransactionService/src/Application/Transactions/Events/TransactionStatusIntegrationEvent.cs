using BuildingBlocks.Domain.Enums;

namespace TransactionService.Application.Transactions.Events;

public sealed record TransactionStatusIntegrationEvent(
    Guid TransactionExternalId,
    TransactionStatus Status,
    string? DecisionReason,
    DateTimeOffset DecidedAt);
