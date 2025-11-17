namespace AntiFraudService.Domain.Policies;

public sealed class CompositeAntiFraudPolicy : IAntiFraudPolicy
{
    private readonly IReadOnlyCollection<IAntiFraudPolicy> _policies;

    public CompositeAntiFraudPolicy(IEnumerable<IAntiFraudPolicy> policies)
    {
        _policies = policies.ToArray();
    }

    public AntiFraudDecision Evaluate(EvaluationContext context)
    {
        foreach (var policy in _policies)
        {
            var decision = policy.Evaluate(context);
            if (!decision.IsApproved)
            {
                return decision;
            }
        }

        return AntiFraudDecision.Approve();
    }
}
