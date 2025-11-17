namespace TransactionService.Application.Transactions.Models;

public sealed record AccountDailyTotalDto(
    Guid SourceAccountId,
    DateOnly Date,
    decimal Total);
