using AntiFraudService.Domain.Policies;
using AntiFraudService.Domain.ValueObjects;
using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace AntiFraudService.Tests.Unit.Policies;

public sealed class SingleTransactionLimitPolicyTests
{

    [Test]
    public void Evaluate_ShouldApprove_WhenAmountIsBelowLimit()
    {
        var limit = Money.From(1000);
        var policy = new SingleTransactionLimitPolicy(limit);
        var context = CreateContext(amount: 999);

        var decision = policy.Evaluate(context);

        decision.IsApproved.ShouldBeTrue();
        decision.Reason.ShouldBeNull();
    }

    [Test]
    public void Evaluate_ShouldReject_WhenAmountExceedsLimit()
    {
        var limit = Money.From(1000);
        var policy = new SingleTransactionLimitPolicy(limit);
        var context = CreateContext(amount: 1001);

        var decision = policy.Evaluate(context);

        decision.IsApproved.ShouldBeFalse();
        decision.Reason.ShouldBe(ReasonCode.SingleLimitExceeded);
    }

    private static EvaluationContext CreateContext(decimal amount)
    {
        var money = Money.From(amount);
        var accountId = BuildingBlocks.Domain.ValueObjects.AccountId.New();
        var occurredOn = DateTimeOffset.Now;
        var occurredOnDate = DateOnly.FromDateTime(occurredOn.DateTime);
        return EvaluationContext.Create(accountId, money, occurredOn, occurredOnDate);
    }
}
