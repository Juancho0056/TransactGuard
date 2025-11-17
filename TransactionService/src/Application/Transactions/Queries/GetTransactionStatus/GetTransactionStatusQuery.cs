using System;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Application.Transactions.Models;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Application.Transactions.Queries.GetTransactionStatus;

public sealed record GetTransactionStatusQuery(Guid TransactionExternalId, DateOnly CreatedAt) : IRequest<TransactionStatusDto>;

internal sealed class GetTransactionStatusQueryHandler : IRequestHandler<GetTransactionStatusQuery, TransactionStatusDto>
{
    private readonly IApplicationDbContext _context;

    public GetTransactionStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionStatusDto> Handle(GetTransactionStatusQuery request, CancellationToken cancellationToken)
    {
        var externalId = TransactionExternalId.From(request.TransactionExternalId.ToString());

        var startOfDay = request.CreatedAt.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var startOfDayOffset = new DateTimeOffset(startOfDay);
        var endOfDayOffset = startOfDayOffset.AddDays(1);

        var transaction = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.ExternalId == externalId
                     && t.CreatedAt >= startOfDayOffset
                     && t.CreatedAt < endOfDayOffset,
                cancellationToken)
            .ConfigureAwait(false);

        if (transaction is null)
        {
            throw new BuildingBlocks.Application.Exceptions.NotFoundException($"Transaction '{request.TransactionExternalId}' was not found.");
        }

        return new TransactionStatusDto(
            Guid.Parse(transaction.ExternalId.Value),
            transaction.Status,
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.DecisionReason);
    }
}
