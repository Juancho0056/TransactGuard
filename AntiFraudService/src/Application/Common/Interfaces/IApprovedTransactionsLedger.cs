using System;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Domain.ValueObjects;

namespace AntiFraudService.Application.Common.Interfaces;

public interface IApprovedTransactionsLedger
{
    Task AddAsync(
        Guid transactionExternalId,
        AccountId sourceAccountId,
        Money amount,
        DateOnly occurredOnDate,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken);
}
