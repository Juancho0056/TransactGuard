using BuildingBlocks.Domain.Enums;
using NUnit.Framework;
using Shouldly;
using Tests.Common.Fakes;
using TransactionService.Tests.Fixtures;
using TransactionService.Domain.Transactions;
using TransactionService.Domain.Transactions.Errors;

namespace TransactionService.Tests.Unit.Transactions;

public sealed class TransactionApproveTests
{
    [Test]
    public void Approve_ShouldUpdateStatusAndRaiseEvent()
    {
        var utcNow = new FakeUtcNowProvider(new DateTimeOffset(2024, 03, 05, 8, 0, 0, TimeSpan.Zero));
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var transaction = new TransactionBuilder().Build(utcNow, timeZoneProvider);
        transaction.ClearDomainEvents();
        utcNow.Advance(TimeSpan.FromHours(1));

        var result = transaction.Approve(utcNow, timeZoneProvider, "approved");

        result.IsSuccess.ShouldBeTrue();
        transaction.Status.ShouldBe(TransactionStatus.Approved);
        transaction.UpdatedAt.ShouldBe(utcNow.UtcNow);
        transaction.DecisionReason.ShouldBe("approved");
        transaction.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TransactionApproved>();
    }

    [Test]
    public void Approve_ShouldFail_WhenTransactionAlreadyFinalized()
    {
        var utcNow = new FakeUtcNowProvider();
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var transaction = new TransactionBuilder().Build(utcNow, timeZoneProvider);
        transaction.Reject(utcNow, timeZoneProvider, "REJECT");
        transaction.ClearDomainEvents();

        var result = transaction.Approve(utcNow, timeZoneProvider);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TransactionErrors.AlreadyFinalized);
        transaction.DomainEvents.ShouldBeEmpty();
    }
}
