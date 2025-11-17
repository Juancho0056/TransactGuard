using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;
using TransactionService.Domain.Transactions;

namespace TransactionService.Tests.Unit.Events;

public sealed class TransactionEventsTests
{
    [Test]
    public void TransactionCreated_ShouldStoreTimestamp()
    {
        var transactionId = TransactionId.New();
        var occurredOn = new DateTimeOffset(2024, 03, 10, 10, 0, 0, TimeSpan.FromHours(-3));
        var money = Money.From(10);

        var @event = new TransactionCreated(transactionId, money, occurredOn);

        @event.TransactionId.ShouldBe(transactionId);
        @event.Value.ShouldBe(money);
        @event.OccurredOn.ShouldBe(occurredOn);
    }

    [Test]
    public void TransactionApproved_ShouldStoreReason()
    {
        var transactionId = TransactionId.New();
        var occurredOn = new DateTimeOffset(2024, 03, 10, 10, 0, 0, TimeSpan.Zero);

        var @event = new TransactionApproved(transactionId, occurredOn, "approved");

        @event.TransactionId.ShouldBe(transactionId);
        @event.Reason.ShouldBe("approved");
        @event.OccurredOn.ShouldBe(occurredOn);
    }

    [Test]
    public void TransactionRejected_ShouldStoreReason()
    {
        var transactionId = TransactionId.New();
        var occurredOn = new DateTimeOffset(2024, 03, 10, 10, 0, 0, TimeSpan.Zero);

        var @event = new TransactionRejected(transactionId, occurredOn, "reason");

        @event.TransactionId.ShouldBe(transactionId);
        @event.Reason.ShouldBe("reason");
        @event.OccurredOn.ShouldBe(occurredOn);
    }
}
