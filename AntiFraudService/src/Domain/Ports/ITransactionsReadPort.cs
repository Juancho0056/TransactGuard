using BuildingBlocks.Domain.ValueObjects;

namespace AntiFraudService.Domain.Ports;

public interface ITransactionsReadPort
{
    Money GetTotalAmountByAccountOnDate(AccountId accountId, DateOnly date);
}
