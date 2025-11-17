using AntiFraudService.Domain.TransactionEvaluations;

namespace AntiFraudService.Application.Common.Interfaces;

public interface ITransactionEvaluationsRepository
{
    Task<TransactionEvaluation?> GetAsync(Guid transactionExternalId, CancellationToken cancellationToken);

    Task AddAsync(TransactionEvaluation evaluation, CancellationToken cancellationToken);
}
