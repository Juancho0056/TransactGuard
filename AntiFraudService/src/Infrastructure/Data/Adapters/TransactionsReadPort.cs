using AntiFraudService.Domain.Ports;
using BuildingBlocks.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AntiFraudService.Infrastructure.Data.Adapters;

public sealed class TransactionsReadPort : ITransactionsReadPort
{
    private readonly ApplicationDbContext _context;

    public TransactionsReadPort(ApplicationDbContext context)
    {
        _context = context;
    }

    public Money GetTotalAmountByAccountOnDate(AccountId accountId, DateOnly date)
    {
        var total = _context.ApprovedTransactions
            .AsNoTracking()
            .Where(entity => entity.SourceAccountId == accountId.Value
                && entity.OccurredOnDate == date)
            .Select(entity => entity.Amount)
            .Sum();

        return Money.From(total);
    }
}
