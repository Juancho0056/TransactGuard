using BuildingBlocks.Domain.Guards;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.ValueObjects;

namespace AntiFraudService.Domain.ValueObjects;
public sealed class EvaluationContext : ValueObject
{
    private EvaluationContext(AccountId sourceAccountId, Money amount, DateTimeOffset occurredOn, DateOnly occurredOnDate)
    {
        Guard.AgainstNull(sourceAccountId, parameterName: nameof(sourceAccountId));
        Guard.AgainstNull(amount, parameterName: nameof(amount));

        SourceAccountId = sourceAccountId;
        Amount = amount;
        OccurredOn = occurredOn;
        OccurredOnDate = occurredOnDate;
    }

    public AccountId SourceAccountId { get; }
    public Money Amount { get; }
    public DateTimeOffset OccurredOn { get; }
    public DateOnly OccurredOnDate { get; }

    public static EvaluationContext Create(AccountId sourceAccountId, Money amount, DateTimeOffset occurredOn, DateOnly occurredOnDate)
        => new(sourceAccountId, amount, occurredOn, occurredOnDate);

    public static EvaluationContext Create(AccountId sourceAccountId, Money amount, IUtcNowProvider utcNowProvider, Func<DateTimeOffset, DateOnly> localDateSelector)
    {
        Guard.AgainstNull(utcNowProvider, parameterName: nameof(utcNowProvider));
        Guard.AgainstNull(localDateSelector, parameterName: nameof(localDateSelector));

        var occurredOn = utcNowProvider.UtcNow;
        return new EvaluationContext(sourceAccountId, amount, occurredOn, localDateSelector(occurredOn));
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return SourceAccountId;
        yield return Amount;
        yield return OccurredOn;
        yield return OccurredOnDate;
    }
}
