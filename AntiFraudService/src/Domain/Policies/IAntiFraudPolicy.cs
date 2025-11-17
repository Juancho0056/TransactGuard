using AntiFraudService.Domain.ValueObjects;

namespace AntiFraudService.Domain.Policies;

public interface IAntiFraudPolicy
{
    AntiFraudDecision Evaluate(EvaluationContext context);
}
