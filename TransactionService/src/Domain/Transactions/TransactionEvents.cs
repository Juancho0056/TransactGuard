using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.ValueObjects;

namespace TransactionService.Domain.Transactions;

public sealed record TransactionCreated(
    TransactionId TransactionId,
    Money Value,
    DateTimeOffset CreatedAt)
    : DomainEvent(CreatedAt);

public sealed record TransactionApproved(
    TransactionId TransactionId,
    DateTimeOffset DecidedAt,
    string? Reason)
    : DomainEvent(DecidedAt);

public sealed record TransactionRejected(
    TransactionId TransactionId,
    DateTimeOffset DecidedAt,
    string? Reason)
    : DomainEvent(DecidedAt);

public sealed record TransactionMarkedAsAntiFraudFailed(
    TransactionId TransactionId,
    DateTimeOffset FailedAt,
    string? Reason)
    : DomainEvent(FailedAt);
