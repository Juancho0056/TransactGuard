namespace TransactionFraudWorker.Dlq;

public static class DeadLetterMetadataKeys
{
    public const string FailureType = "FailureType";
    public const string LastKnownStatus = "LastKnownStatus";
}
