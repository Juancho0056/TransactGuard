using BuildingBlocks.Domain.Guards;
using BuildingBlocks.Domain.ValueObjects;
using AntiFraudService.Domain.ValueObjects;

namespace AntiFraudService.Domain.Policies;

public sealed class SingleTransactionLimitPolicy : IAntiFraudPolicy
{
    public static readonly decimal DefaultLimitAmount = 2000m;

    private readonly decimal _limitAmount;

    public SingleTransactionLimitPolicy(decimal limitAmount)
    {
        var limit = Money.From(limitAmount);

        _limitAmount = limit.Amount;
    }

    public SingleTransactionLimitPolicy(Money limit)
    {
        Guard.AgainstNull(limit, parameterName: nameof(limit));

        _limitAmount = limit.Amount;
    }

    public static SingleTransactionLimitPolicy CreateDefault() => new(DefaultLimitAmount);

    public AntiFraudDecision Evaluate(EvaluationContext context)
    {
        var limit = CreateLimitForContext(context);

        if (context.Amount > limit)
        {
            return AntiFraudDecision.Reject(ReasonCode.SingleLimitExceeded);
        }

        return AntiFraudDecision.Approve();
    }

    private Money CreateLimitForContext(EvaluationContext context)
    {
        Guard.AgainstNull(context, parameterName: nameof(context));

        return Money.From(_limitAmount);
    }
}
