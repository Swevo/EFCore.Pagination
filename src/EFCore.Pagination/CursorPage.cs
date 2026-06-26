namespace Swevo.EFCore.Pagination;

/// <summary>The result of a cursor-paginated (keyset) query.</summary>
public sealed class CursorPage<T>(IReadOnlyList<T> items, string? nextCursor, bool hasNextPage)
{
    public IReadOnlyList<T> Items { get; } = items;

    /// <summary>
    /// Opaque Base64 token to pass as <c>afterCursor</c> on the next request.
    /// <c>null</c> when this is the last page.
    /// </summary>
    public string? NextCursor { get; } = nextCursor;

    public bool HasNextPage { get; } = hasNextPage;

    public static CursorPage<T> Empty() =>
        new(Array.Empty<T>(), null, false);
}
