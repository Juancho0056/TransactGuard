using AntiFraudService.Application.Common.Interfaces;
using AntiFraudService.Domain.ApprovedTransactions;
using BuildingBlocks.Domain.ValueObjects;

namespace AntiFraudService.Infrastructure.Data.Adapters;

public sealed class ApprovedTransactionsLedger : IApprovedTransactionsLedger
{
    private readonly ApplicationDbContext _context;

    public ApprovedTransactionsLedger(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(
        Guid transactionExternalId,
        AccountId sourceAccountId,
        Money amount,
        DateOnly occurredOnDate,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken)
    {
        var entity = ApprovedTransaction.Create(
            transactionExternalId,
            sourceAccountId.Value,
            amount.Amount,
            occurredOnDate,
            occurredOn,
            occurredOn);

        _context.ApprovedTransactions.Add(entity);

        return Task.CompletedTask;
    }
}
