using System.Threading;
using System.Threading.Tasks;
using TransactionFraudWorker.Models;

namespace TransactionFraudWorker.Clients;

public interface IAntiFraudClient
{
    Task<EvaluationResultDto> EvaluateTransactionAsync(TransactionCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
