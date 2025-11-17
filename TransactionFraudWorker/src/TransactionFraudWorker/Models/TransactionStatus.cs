namespace TransactionFraudWorker.Models;

public enum TransactionStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    AntiFraudFailed = 3,
    AntiFraudError = AntiFraudFailed,
}
