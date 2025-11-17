using AntiFraudService.Application.Common.Interfaces;
using AntiFraudService.Domain.TransactionEvaluations;
using Microsoft.EntityFrameworkCore;

namespace AntiFraudService.Infrastructure.Data.Adapters;

public sealed class TransactionEvaluationsRepository : ITransactionEvaluationsRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionEvaluationsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionEvaluation?> GetAsync(Guid transactionExternalId, CancellationToken cancellationToken)
    {
        return await _context.TransactionEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(evaluation => evaluation.TransactionExternalId == transactionExternalId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task AddAsync(TransactionEvaluation evaluation, CancellationToken cancellationToken)
    {
        _context.TransactionEvaluations.Add(evaluation);
        return Task.CompletedTask;
    }
}
