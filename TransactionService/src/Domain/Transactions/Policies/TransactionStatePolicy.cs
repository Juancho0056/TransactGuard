using BuildingBlocks.Domain.Enums;
using BuildingBlocks.Domain.ValueObjects;
using TransactionService.Domain.Transactions.Errors;

namespace TransactionService.Domain.Transactions.Policies;

public static class TransactionStatePolicy
{
    public static bool CanTransition(TransactionStatus current, TransactionStatus next)
    {
        return current switch
        {
            TransactionStatus.Pending => next is TransactionStatus.Pending
                or TransactionStatus.Approved
                or TransactionStatus.Rejected
                or TransactionStatus.AntiFraudFailed,
            TransactionStatus.AntiFraudFailed => next is TransactionStatus.AntiFraudFailed
                or TransactionStatus.Approved
                or TransactionStatus.Rejected,
            //TransactionStatus.Approved => next is TransactionStatus.Approved,
            //TransactionStatus.Rejected => next is TransactionStatus.Rejected,
            _ => false,
        };
    }

    public static void EnsureAccountsAreDifferent(AccountId source, AccountId target)
    {
        if (source == target)
        {
            throw new InvalidOperationException(TransactionErrors.InvalidAccounts.Message);
        }
    }
}
