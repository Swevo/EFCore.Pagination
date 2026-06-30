# Swevo.EFCore.Pagination

[![NuGet](https://img.shields.io/nuget/v/Swevo.EFCore.Pagination
[![NuGet Downloads](https://img.shields.io/nuget/dt/Swevo.EFCore.Pagination.svg)](https://www.nuget.org/packages/Swevo.EFCore.Pagination).svg)](https://www.nuget.org/packages/Swevo.EFCore.Pagination)
[![CI](https://github.com/Swevo/EFCore.Pagination/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/EFCore.Pagination/actions)

Offset and cursor-based pagination for EF Core. Two extension methods on `IQueryable<T>`, two result types — nothing else.

```csharp
// Offset pagination
var page = await db.Orders
    .Where(o => o.CustomerId == customerId)
    .OrderByDescending(o => o.CreatedAt)
    .ToPageAsync(pageNumber: 1, pageSize: 20);

// page.Items          IReadOnlyList<Order>
// page.TotalCount     int
// page.TotalPages     int
// page.HasNextPage    bool
// page.HasPreviousPage bool

// Cursor (keyset) pagination
var page = await db.Orders
    .Where(o => o.CustomerId == customerId)
    .OrderBy(o => o.Id)
    .ToCursorPageAsync(o => o.Id, afterCursor: cursor, pageSize: 20);

// page.Items       IReadOnlyList<Order>
// page.NextCursor  string?  — pass to the next request
// page.HasNextPage bool
```

## Install

```bash
dotnet add package Swevo.EFCore.Pagination
```

## Offset vs cursor

| | Offset (`ToPageAsync`) | Cursor (`ToCursorPageAsync`) |
|---|---|---|
| Jump to page | ✅ | ❌ |
| Total count | ✅ | ❌ |
| Consistent under inserts | ❌ | ✅ |
| Scalable to millions of rows | ❌ | ✅ |
| Single query | ❌ (count + fetch) | ✅ |

Use **offset** for admin grids with page numbers. Use **cursor** for infinite-scroll feeds and APIs.

## Offset pagination

```csharp
// GET /orders?page=2&pageSize=20
var page = await db.Orders
    .OrderByDescending(o => o.CreatedAt)
    .ToPageAsync(pageNumber: 2, pageSize: 20, cancellationToken);

return new
{
    items      = page.Items,
    totalCount = page.TotalCount,
    totalPages = page.TotalPages,
    page       = page.PageNumber,
    pageSize   = page.PageSize,
    hasPrev    = page.HasPreviousPage,
    hasNext    = page.HasNextPage,
};
```

- `pageNumber` is 1-based.
- Issues two SQL queries: `COUNT(*)` then `SELECT ... SKIP ... TAKE`.
- `pageSize` is capped at 1000.

## Cursor (keyset) pagination

```csharp
// GET /orders?cursor=eyJJZCI6MTAwfQ==&pageSize=20
var page = await db.Orders
    .OrderBy(o => o.Id)          // MUST be ordered by the cursor key
    .ToCursorPageAsync(
        keySelector:  o => o.Id,
        afterCursor:  Request.Query["cursor"],   // null on first page
        pageSize:     20,
        cancellationToken);

return new
{
    items      = page.Items,
    nextCursor = page.NextCursor,   // null on last page
    hasNext    = page.HasNextPage,
};
```

- Issues a **single** `SELECT ... WHERE id > ? LIMIT pageSize + 1`.
- Cursor is an opaque Base64-encoded JSON token — do not parse it.
- Supported key types: `int`, `long`, `Guid`, `DateTime`, `DateTimeOffset`, and any value type with a `>` SQL operator.

## Full cursor loop example

```csharp
string? cursor = null;
do
{
    var page = await db.Products
        .OrderBy(p => p.Id)
        .ToCursorPageAsync(p => p.Id, afterCursor: cursor, pageSize: 100);

    await ProcessBatchAsync(page.Items);
    cursor = page.NextCursor;
} while (cursor is not null);
```

## Part of the Swevo EF Core toolkit

Stack with the other Swevo EF Core packages:

```csharp
[Auditable]  // → CreatedAt, UpdatedAt
[SoftDelete] // → IsDeleted, global query filter
public partial class Order
{
    public OrderId Id { get; set; } // → Swevo.EFCore.StronglyTyped
}

// Paginate with cursor
var page = await db.Orders
    .OrderBy(o => o.Id)
    .ToCursorPageAsync(o => o.Id, afterCursor: cursor, pageSize: 20);
```

| Package | Purpose |
|---|---|
| [Swevo.EFCore.Pagination](https://github.com/Swevo/EFCore.Pagination) | This package |
| [Swevo.EFCore.StronglyTyped](https://github.com/Swevo/EFCore.StronglyTyped) | Strongly-typed IDs |
| [Swevo.AutoAudit](https://github.com/Swevo/AutoAudit) | Audit fields |
| [Swevo.EFCore.SoftDelete](https://github.com/Swevo/EFCore.SoftDelete) | Soft delete |
| [Swevo.EFCore.Outbox](https://github.com/Swevo/EFCore.Outbox) | Transactional outbox |


## Also by the same author

> 🌐 Full suite overview: **[swevo.github.io](https://swevo.github.io/)**

| Package | Description |
|---|---|
| [**AutoLog.Generator**](https://github.com/Swevo/AutoLog.Generator) | Compile-time high-performance logging — `[Log(Level, Message)]` generates `LoggerMessage.Define`. AOT-safe. |
| [**AutoHttpClient.Generator**](https://github.com/Swevo/AutoHttpClient.Generator) | Compile-time typed HTTP client — `[HttpClient]` on an interface generates a strongly-typed client. AOT-safe Refit alternative. |
| [**AutoDispatch.Generator**](https://github.com/Swevo/AutoDispatch.Generator) | Compile-time CQRS dispatcher — `[Handler]` generates a strongly-typed `IDispatcher`. No MediatR, no reflection. |
| [**AutoWire**](https://github.com/Swevo/AutoWire) | Compile-time DI auto-registration — `[Scoped]`/`[Singleton]`/`[Transient]` generates `IServiceCollection` registration code. |
| [**AutoMap.Generator**](https://github.com/Swevo/AutoMap.Generator) | Compile-time object mapping with generated extension methods. AOT-safe AutoMapper alternative. |
## License

MIT © 2026 Justin Bannister
