using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.ValueObjects;
using Tests.Common.Builders;
using Tests.Common.Fakes;
using TransactionService.Domain.Transactions;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Tests.Fixtures;

public sealed class TransactionBuilder
{
    private TransactionExternalId _externalId = TransactionExternalId.From("external-1");
    private AccountId _sourceAccountId = AccountId.New();
    private AccountId _targetAccountId = AccountId.New();
    private TransferTypeId _transferTypeId = TransferTypeId.From("WIRE");
    private Money _value = new MoneyBuilder().WithAmount(100).Build();
    private string? _description;

    public TransactionBuilder WithExternalId(string externalId)
    {
        _externalId = TransactionExternalId.From(externalId);
        return this;
    }

    public TransactionBuilder WithAccounts(AccountId source, AccountId target)
    {
        _sourceAccountId = source;
        _targetAccountId = target;
        return this;
    }

    public TransactionBuilder WithTransferType(string transferType)
    {
        _transferTypeId = TransferTypeId.From(transferType);
        return this;
    }

    public TransactionBuilder WithValue(decimal amount)
    {
        _value = Money.From(amount);
        return this;
    }

    public TransactionBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public Transaction Build(FakeUtcNowProvider? utcNowProvider = null, ITimeZoneProvider? timeZoneProvider = null)
    {
        utcNowProvider ??= new FakeUtcNowProvider();
        timeZoneProvider ??= new FakeTimeZoneProvider();
        return Transaction.CreatePending(
            _externalId,
            _sourceAccountId,
            _targetAccountId,
            _transferTypeId,
            _value,
            utcNowProvider,
            timeZoneProvider,
            _description);
    }
}
