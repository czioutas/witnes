using Microsoft.EntityFrameworkCore;

namespace Libs.Pagination;

public static class QueryableExtensions
{
    /// <summary>
    /// Apply pagination to an IQueryable and return a PagedResult
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to paginate</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with data and metadata</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(data, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <summary>
    /// Apply pagination to an IQueryable with a projection
    /// </summary>
    /// <typeparam name="TSource">The source entity type</typeparam>
    /// <typeparam name="TResult">The projected result type</typeparam>
    /// <param name="query">The query to paginate</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="selector">Projection selector</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result with projected data and metadata</returns>
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        PaginationRequest pagination,
        Func<TSource, TResult> selector,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        var projectedData = data.Select(selector).ToList();

        return new PagedResult<TResult>(projectedData, totalCount, pagination.PageNumber, pagination.PageSize);
    }
}
