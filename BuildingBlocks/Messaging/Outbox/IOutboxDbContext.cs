using BuildingBlocks.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Messaging.Outbox;

public interface IOutboxDbContext : IApplicationDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
