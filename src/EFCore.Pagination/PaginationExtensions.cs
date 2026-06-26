using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Swevo.EFCore.Pagination;

public static class PaginationExtensions
{
    private const int MaxPageSize = 1000;

    /// <summary>
    /// Executes two queries — a count and a paged fetch — and returns an offset-paginated result.
    /// </summary>
    /// <param name="query">An ordered <see cref="IQueryable{T}"/>.</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Number of items per page (1–1000).</param>
    public static async Task<Page<T>> ToPageAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePageArgs(pageNumber, pageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
            return Page<T>.Empty(pageSize);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new Page<T>(items.AsReadOnly(), totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Executes a single keyset-paginated query using an opaque cursor token.
    /// Pass <c>null</c> for the first page; use <see cref="CursorPage{T}.NextCursor"/> for subsequent pages.
    /// </summary>
    /// <param name="query">An ordered <see cref="IQueryable{T}"/> (must be ordered by <typeparamref name="TKey"/>).</param>
    /// <param name="keySelector">Expression that selects the cursor key from an entity.</param>
    /// <param name="afterCursor">Opaque cursor from the previous page, or <c>null</c> for the first page.</param>
    /// <param name="pageSize">Number of items per page (1–1000).</param>
    public static async Task<CursorPage<T>> ToCursorPageAsync<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        string? afterCursor = null,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
        where TKey : struct
    {
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be at least 1.");
        if (pageSize > MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"pageSize must not exceed {MaxPageSize}.");

        if (afterCursor is not null)
        {
            var afterKey = CursorTokenEncoder.Decode<TKey>(afterCursor);
            var body = Expression.GreaterThan(
                keySelector.Body,
                Expression.Constant(afterKey, typeof(TKey)));
            var predicate = Expression.Lambda<Func<T, bool>>(body, keySelector.Parameters);
            query = query.Where(predicate);
        }

        // Fetch one extra item to detect whether a next page exists
        var items = await query
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
            items.RemoveAt(items.Count - 1);

        var nextCursor = hasNextPage
            ? CursorTokenEncoder.Encode(keySelector.Compile()(items[^1]))
            : null;

        return new CursorPage<T>(items.AsReadOnly(), nextCursor, hasNextPage);
    }

    private static void ValidatePageArgs(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "pageNumber must be at least 1.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be at least 1.");
        if (pageSize > MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"pageSize must not exceed {MaxPageSize}.");
    }
}
