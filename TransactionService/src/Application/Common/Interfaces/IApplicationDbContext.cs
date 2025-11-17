using BuildingBlocks.Messaging.Outbox;
using TransactionService.Domain.Transactions;

namespace TransactionService.Application.Common.Interfaces;

public interface IApplicationDbContext : IOutboxDbContext
{
    DbSet<Transaction> Transactions { get; }

}
