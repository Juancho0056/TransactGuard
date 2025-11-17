using AntiFraudService.Domain.Policies;
using AntiFraudService.Domain.ValueObjects;
using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace AntiFraudService.Tests.Unit.Policies;

public sealed class DailyAmountLimitPolicyTests
{
    [Test]
    public void Evaluate_ShouldApprove_WhenProjectedTotalIsWithinLimit()
    {
        var accountId = AccountId.New();
        var occurredOn = DateTimeOffset.Now;
        var occurredOnDate = DateOnly.FromDateTime(occurredOn.DateTime);
        var existingTotal = Money.From(1000);
        var context = EvaluationContext.Create(accountId, Money.From(500), occurredOn, occurredOnDate);
        var stub = new Unit.Ports.TransactionsReadPortStub().WithTotal(accountId, occurredOnDate, existingTotal);
        var policy = new DailyAmountLimitPolicy(stub, Money.From(2000));

        var decision = policy.Evaluate(context);

        decision.IsApproved.ShouldBeTrue();
    }

    [Test]
    public void Evaluate_ShouldReject_WhenProjectedTotalExceedsLimit()
    {
        var accountId = AccountId.New();
        var occurredOn = DateTimeOffset.Now;
        var occurredOnDate = DateOnly.FromDateTime(occurredOn.DateTime);
        var existingTotal = Money.From(1500);
        var context = EvaluationContext.Create(accountId, Money.From(600), occurredOn, occurredOnDate);
        var stub = new Unit.Ports.TransactionsReadPortStub().WithTotal(accountId, occurredOnDate, existingTotal);
        var policy = new DailyAmountLimitPolicy(stub, Money.From(2000));

        var decision = policy.Evaluate(context);

        decision.IsApproved.ShouldBeFalse();
        decision.Reason.ShouldBe(ReasonCode.DailyLimitExceeded);
    }
}
