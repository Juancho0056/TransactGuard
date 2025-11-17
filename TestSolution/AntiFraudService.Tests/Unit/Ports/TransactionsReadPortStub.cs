using AntiFraudService.Domain.Ports;
using BuildingBlocks.Domain.ValueObjects;

namespace AntiFraudService.Tests.Unit.Ports;

public sealed class TransactionsReadPortStub : ITransactionsReadPort
{
    private readonly Dictionary<(AccountId AccountId, DateOnly Date), Money> _totals = new();

    public TransactionsReadPortStub WithTotal(AccountId accountId, DateOnly date, Money total)
    {
        _totals[(accountId, date)] = total;
        return this;
    }

    public Money GetTotalAmountByAccountOnDate(AccountId accountId, DateOnly date)
    {
        if (_totals.TryGetValue((accountId, date), out var total))
        {
            return total;
        }

        return Money.Zero();
    }
}
