# Changelog

## [1.0.0] - 2026-06-26

### Added
- `ToPageAsync(pageNumber, pageSize, ct)` — offset pagination returning `Page<T>` with `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage`
- `ToCursorPageAsync<T, TKey>(keySelector, afterCursor, pageSize, ct)` — keyset (cursor) pagination returning `CursorPage<T>` with opaque Base64 `NextCursor`
- `Page<T>` and `CursorPage<T>` result types with `static Empty()` factories
- Guard throws `ArgumentOutOfRangeException` for invalid `pageNumber` / `pageSize`
- `pageSize` capped at 1000
- Zero extra dependencies beyond `Microsoft.EntityFrameworkCore`
