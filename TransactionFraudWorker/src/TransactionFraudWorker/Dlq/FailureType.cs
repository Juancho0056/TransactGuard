namespace TransactionFraudWorker.Dlq;

public enum FailureType
{
    Permanent = 1,
    TransientExhausted = 2,
    Unexpected = 3,
}
