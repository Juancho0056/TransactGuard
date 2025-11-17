using BuildingBlocks.Domain.Enums;

namespace TransactionService.Application.Transactions.Models;

public sealed record TransactionStatusDto(
    Guid TransactionExternalId,
    TransactionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? DecisionReason);
