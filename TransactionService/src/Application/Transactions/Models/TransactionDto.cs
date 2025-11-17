using BuildingBlocks.Domain.Enums;

namespace TransactionService.Application.Transactions.Models;

public sealed record TransactionDto(
    Guid TransactionExternalId,
    Guid SourceAccountId,
    Guid TargetAccountId,
    string TransferType,
    decimal Value,
    TransactionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Description,
    string? DecisionReason);
