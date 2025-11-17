namespace TransactionFraudWorker.Clients;

public class TransactionServiceClientException : Exception
{
    public TransactionServiceClientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class TransactionServiceTransientException : TransactionServiceClientException
{
    public TransactionServiceTransientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class TransactionServicePermanentException : TransactionServiceClientException
{
    public TransactionServicePermanentException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
