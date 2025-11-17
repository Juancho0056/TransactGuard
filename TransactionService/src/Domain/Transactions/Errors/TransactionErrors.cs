using BuildingBlocks.Domain.Messages;
using BuildingBlocks.Domain.Results;

namespace TransactionService.Domain.Transactions.Errors;

public static class TransactionErrors
{
    public static readonly Error InvalidReason = new("transaction.invalid_reason", "The rejection reason is required.");
    public static readonly Error AlreadyFinalized = new("transaction.already_finalized", "The transaction has already been finalized.");
    public static readonly Error InvalidAccounts = new("transaction.invalid_accounts", "The source and target accounts must be different.");
    public static readonly Error InvalidValue = new("transaction.invalid_value", DefaultMessage.InvalidValue);
    public static readonly Error InvalidStatus = new("transaction.invalid_status", "The transaction status is not valid for this operation.");

}
