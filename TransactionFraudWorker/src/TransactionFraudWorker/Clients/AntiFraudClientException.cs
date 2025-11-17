namespace TransactionFraudWorker.Clients;

public class AntiFraudClientException : Exception
{
    public AntiFraudClientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class AntiFraudTransientException : AntiFraudClientException
{
    public AntiFraudTransientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class AntiFraudPermanentException : AntiFraudClientException
{
    public AntiFraudPermanentException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
