using AntiFraudService.Domain.Ports;
using AntiFraudService.Domain.ValueObjects;
using BuildingBlocks.Domain.Guards;
using BuildingBlocks.Domain.ValueObjects;

namespace AntiFraudService.Domain.Policies;

public sealed class DailyAmountLimitPolicy : IAntiFraudPolicy
{
    public static readonly decimal DefaultLimitAmount = 20000m;
    private readonly ITransactionsReadPort _transactionsReadPort;
    private readonly decimal _limitAmount;

    public DailyAmountLimitPolicy(ITransactionsReadPort transactionsReadPort, decimal limitAmount)
    {
        var limit = Money.From(limitAmount);

        _transactionsReadPort = transactionsReadPort;
        _limitAmount = limit.Amount;
    }

    public DailyAmountLimitPolicy(ITransactionsReadPort transactionsReadPort, Money dailyLimit)
    {
        Guard.AgainstNull(dailyLimit, parameterName: nameof(dailyLimit));

        _transactionsReadPort = transactionsReadPort;
        _limitAmount = dailyLimit.Amount;
    }

    public static DailyAmountLimitPolicy CreateDefault(ITransactionsReadPort transactionsReadPort) =>
        new(transactionsReadPort, DefaultLimitAmount);

    public AntiFraudDecision Evaluate(EvaluationContext context)
    {
        var totalForDay = _transactionsReadPort.GetTotalAmountByAccountOnDate(
            context.SourceAccountId,
            context.OccurredOnDate);
        var projectedTotal = totalForDay.Add(context.Amount);

        var dailyLimit = CreateLimitForContext(context);

        if (projectedTotal > dailyLimit)
        {
            return AntiFraudDecision.Reject(ReasonCode.DailyLimitExceeded);
        }

        return AntiFraudDecision.Approve();
    }

    private Money CreateLimitForContext(EvaluationContext context)
    {
        Guard.AgainstNull(context, parameterName: nameof(context));

        return Money.From(_limitAmount);
    }
}
