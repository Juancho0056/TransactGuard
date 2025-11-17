namespace AntiFraudService.Domain.ValueObjects;

public sealed record AntiFraudDecision(bool IsApproved, ReasonCode? Reason)
{
    public static AntiFraudDecision Approve() => new(true, null);

    public static AntiFraudDecision Reject(ReasonCode reason) => new(false, reason);
}
