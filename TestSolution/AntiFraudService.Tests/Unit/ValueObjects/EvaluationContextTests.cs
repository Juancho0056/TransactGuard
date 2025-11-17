using AntiFraudService.Domain.ValueObjects;
using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;
using Tests.Common.Fakes;

namespace AntiFraudService.Tests.Unit.ValueObjects;

public sealed class EvaluationContextTests
{
    [Test]
    public void Create_WithExplicitDate_ShouldPersistValues()
    {
        var accountId = AccountId.New();
        var amount = Money.From(100);
        var occurredOn = new DateTimeOffset(2024, 01, 01, 10, 0, 0, TimeSpan.FromHours(-5));
        var occurredOnDate = DateOnly.FromDateTime(occurredOn.DateTime);

        var context = EvaluationContext.Create(accountId, amount, occurredOn, occurredOnDate);

        context.OccurredOn.ShouldBe(occurredOn);
        context.OccurredOnDate.ShouldBe(occurredOnDate);
    }

    [Test]
    public void Create_WithProvider_ShouldUseProviderTime()
    {
        var accountId = AccountId.New();
        var amount = Money.From(100);
        var provider = new FakeUtcNowProvider(new DateTimeOffset(2024, 02, 02, 12, 30, 0, TimeSpan.Zero));
        var selectorInvoked = false;

        DateOnly Selector(DateTimeOffset instant)
        {
            selectorInvoked = true;
            instant.ShouldBe(provider.UtcNow);
            return DateOnly.FromDateTime(instant.UtcDateTime);
        }

        var context = EvaluationContext.Create(accountId, amount, provider, Selector);

        selectorInvoked.ShouldBeTrue();
        context.OccurredOn.ShouldBe(provider.UtcNow);
        context.OccurredOnDate.ShouldBe(DateOnly.FromDateTime(provider.UtcNow.UtcDateTime));
    }

    [Test]
    public void Equality_ShouldDependOnAtomicValues()
    {
        var accountId = AccountId.New();
        var amount = Money.From(50);
        var occurredOn = DateTimeOffset.Now;
        var occurredOnDate = DateOnly.FromDateTime(occurredOn.DateTime);

        var left = EvaluationContext.Create(accountId, amount, occurredOn, occurredOnDate);
        var right = EvaluationContext.Create(accountId, amount, occurredOn, occurredOnDate);

        left.ShouldBe(right);
    }
}
