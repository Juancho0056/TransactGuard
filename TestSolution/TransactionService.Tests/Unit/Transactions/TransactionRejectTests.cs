using System;
using BuildingBlocks.Domain.Enums;
using NUnit.Framework;
using Shouldly;
using Tests.Common.Fakes;
using TransactionService.Domain.Transactions;
using TransactionService.Domain.Transactions.Errors;
using TransactionService.Tests.Fixtures;

namespace TransactionService.Tests.Unit.Transactions;

public sealed class TransactionRejectTests
{
    [Test]
    public void Reject_ShouldUpdateStatusAndReason()
    {
        var utcNow = new FakeUtcNowProvider(new DateTimeOffset(2024, 03, 05, 8, 0, 0, TimeSpan.Zero));
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var transaction = new TransactionBuilder().Build(utcNow, timeZoneProvider);
        transaction.ClearDomainEvents();
        utcNow.Advance(TimeSpan.FromMinutes(30));

        var result = transaction.Reject(utcNow, timeZoneProvider, "  suspicious pattern  ");

        result.IsSuccess.ShouldBeTrue();
        transaction.Status.ShouldBe(TransactionStatus.Rejected);
        transaction.DecisionReason.ShouldBe("suspicious pattern");
        transaction.UpdatedAt.ShouldBe(utcNow.UtcNow);
        transaction.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TransactionRejected>();
    }

    [Test]
    public void Reject_ShouldFail_WhenReasonIsEmpty()
    {
        var utcNow = new FakeUtcNowProvider();
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var transaction = new TransactionBuilder().Build(utcNow, timeZoneProvider);

        var result = transaction.Reject(utcNow, timeZoneProvider, " ");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TransactionErrors.InvalidReason);
    }

    [Test]
    public void Reject_ShouldFail_WhenTransactionAlreadyFinalized()
    {
        var utcNow = new FakeUtcNowProvider();
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var transaction = new TransactionBuilder().Build(utcNow, timeZoneProvider);
        transaction.Reject(utcNow, timeZoneProvider, "reason");
        transaction.ClearDomainEvents();

        var result = transaction.Reject(utcNow, timeZoneProvider, "another reason");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TransactionErrors.AlreadyFinalized);
        transaction.DomainEvents.ShouldBeEmpty();
    }
}
