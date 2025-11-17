using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Application.Transactions.Models;
using BuildingBlocks.Domain.Enums;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.ValueObjects;

namespace TransactionService.Application.Transactions.Queries.GetAccountDailyTotal;

public sealed record GetAccountDailyTotalQuery(Guid SourceAccountId, DateOnly Date) : IRequest<AccountDailyTotalDto>;

internal sealed class GetAccountDailyTotalQueryHandler : IRequestHandler<GetAccountDailyTotalQuery, AccountDailyTotalDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimeZoneProvider _timeZoneProvider;

    public GetAccountDailyTotalQueryHandler(IApplicationDbContext context, ITimeZoneProvider timeZoneProvider)
    {
        _context = context;
        _timeZoneProvider = timeZoneProvider;
    }

    public async Task<AccountDailyTotalDto> Handle(GetAccountDailyTotalQuery request, CancellationToken cancellationToken)
    {
        var timeZone = _timeZoneProvider.GetTimeZone();
        var localStart = request.Date.ToDateTime(TimeOnly.MinValue);
        var startOfDayOffset = new DateTimeOffset(localStart, timeZone.GetUtcOffset(localStart));
        var startOfNextDayOffset = startOfDayOffset.AddDays(1);

        var sourceAccountId = AccountId.Create(request.SourceAccountId);

        var total = await _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.SourceAccountId == sourceAccountId
                && transaction.CreatedAt >= startOfDayOffset
                && transaction.CreatedAt < startOfNextDayOffset
                && transaction.Status == TransactionStatus.Approved)
            .Select(transaction => transaction.Value.Amount)
            .SumAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AccountDailyTotalDto(
            request.SourceAccountId,
            request.Date,
            total);
    }
}
