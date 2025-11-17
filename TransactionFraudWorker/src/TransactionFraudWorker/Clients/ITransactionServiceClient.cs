using System;
using System.Threading;
using System.Threading.Tasks;
using TransactionFraudWorker.Models;

namespace TransactionFraudWorker.Clients;

public interface ITransactionServiceClient
{
    Task UpdateStatusAsync(
        Guid transactionExternalId,
        TransactionStatus status,
        string? reason,
        CancellationToken cancellationToken);
}
