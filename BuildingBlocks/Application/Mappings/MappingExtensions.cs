using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Application.Mappings;

public static class MappingExtensions
{
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<TDestination>(
        this IQueryable<TDestination> queryable,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => PaginatedList<TDestination>.CreateAsync(queryable.AsNoTracking(), pageNumber, pageSize, cancellationToken);
}
