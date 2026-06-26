namespace Swevo.EFCore.Pagination;

/// <summary>The result of an offset-paginated query.</summary>
public sealed class Page<T>(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;
    public int TotalPages => pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    public bool HasNextPage => pageNumber < TotalPages;
    public bool HasPreviousPage => pageNumber > 1;

    public static Page<T> Empty(int pageSize = 20) =>
        new(Array.Empty<T>(), 0, 1, pageSize);
}
