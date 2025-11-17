using AntiFraudService.Domain.Policies;
using AntiFraudService.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AntiFraudService.Tests.Unit.Policies;

public sealed class CompositeAntiFraudPolicyTests
{
    [Test]
    public void Evaluate_ShouldReturnFirstRejection()
    {
        var context = CreateContext();
        var approvedPolicy = CreatePolicyMock(AntiFraudDecision.Approve());
        var rejectedDecision = AntiFraudDecision.Reject(ReasonCode.SingleLimitExceeded);
        var rejectingPolicy = CreatePolicyMock(rejectedDecision);
        var anotherPolicy = CreatePolicyMock(AntiFraudDecision.Approve());
        var composite = new CompositeAntiFraudPolicy(new[]
        {
            approvedPolicy.Object,
            rejectingPolicy.Object,
            anotherPolicy.Object,
        });

        var decision = composite.Evaluate(context);

        decision.ShouldBe(rejectedDecision);
        approvedPolicy.Verify(p => p.Evaluate(context), Times.Once);
        rejectingPolicy.Verify(p => p.Evaluate(context), Times.Once);
        anotherPolicy.Verify(p => p.Evaluate(It.IsAny<EvaluationContext>()), Times.Never);
    }

    [Test]
    public void Evaluate_ShouldApprove_WhenAllPoliciesApprove()
    {
        var context = CreateContext();
        var policies = Enumerable.Range(0, 3)
            .Select(_ => CreatePolicyMock(AntiFraudDecision.Approve()).Object)
            .ToArray();
        var composite = new CompositeAntiFraudPolicy(policies);

        var decision = composite.Evaluate(context);

        decision.IsApproved.ShouldBeTrue();
    }

    private static Mock<IAntiFraudPolicy> CreatePolicyMock(AntiFraudDecision decision)
    {
        var mock = new Mock<IAntiFraudPolicy>();
        mock.Setup(p => p.Evaluate(It.IsAny<EvaluationContext>())).Returns(decision);
        return mock;
    }

    private static EvaluationContext CreateContext()
    {
        var accountId = BuildingBlocks.Domain.ValueObjects.AccountId.New();
        var money = BuildingBlocks.Domain.ValueObjects.Money.From(100);
        var occurredOn = DateTimeOffset.Now;
        var occurredOnDate = DateOnly.FromDateTime(occurredOn.DateTime);
        return EvaluationContext.Create(accountId, money, occurredOn, occurredOnDate);
    }
}
