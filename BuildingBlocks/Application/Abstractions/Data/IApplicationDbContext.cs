using Microsoft.EntityFrameworkCore.Storage;

namespace BuildingBlocks.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException($"Transactions are not supported by {GetType().Name}.");
}
