using System;
using BuildingBlocks.Domain.Enums;
using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Fakes;
using TransactionService.Domain.Transactions;
using TransactionService.Domain.Transactions.Errors;
using TransactionService.Domain.Transactions.ValueObjects;
using TransactionService.Tests.Fixtures;

namespace TransactionService.Tests.Unit.Transactions;

public sealed class TransactionCreateTests
{
    [Test]
    public void CreatePending_ShouldInitializeTransactionCorrectly()
    {
        var utcNow = new DateTimeOffset(2024, 03, 04, 12, 0, 0, TimeSpan.Zero);
        var utcNowProvider = new FakeUtcNowProvider(utcNow);
        var builder = new TransactionBuilder()
            .WithExternalId("ext-123")
            .WithAccounts(AccountId.New(), AccountId.New())
            .WithTransferType("wire")
            .WithValue(150);

        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var transaction = builder.Build(utcNowProvider, timeZoneProvider);

        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.CreatedAt.ShouldBe(utcNow);
        transaction.UpdatedAt.ShouldBe(utcNow);
        transaction.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TransactionCreated>();
    }

    [Test]
    public void CreatePending_ShouldThrow_WhenValueIsZeroOrNegative()
    {
        var utcNowProvider = new FakeUtcNowProvider();
        var externalId = TransactionExternalId.From("ext");
        var accountId = AccountId.New();
        var transferType = TransferTypeId.From("wire");
        var money = new MoneyBuilder().WithAmount(0).Build();

        Should.Throw<InvalidOperationException>(() => Transaction.CreatePending(
            externalId,
            accountId,
            AccountId.New(),
            transferType,
            money,
            utcNowProvider,
            new FakeTimeZoneProvider()));
    }

    [Test]
    public void CreatePending_ShouldThrow_WhenAccountsAreEqual()
    {
        var utcNowProvider = new FakeUtcNowProvider();
        var accountId = AccountId.New();

        Should.Throw<InvalidOperationException>(() => Transaction.CreatePending(
            TransactionExternalId.From("ext"),
            accountId,
            accountId,
            TransferTypeId.From("wire"),
            Money.From(100),
            utcNowProvider,
            new FakeTimeZoneProvider())).Message.ShouldBe(TransactionErrors.InvalidAccounts.Message);
    }
}
