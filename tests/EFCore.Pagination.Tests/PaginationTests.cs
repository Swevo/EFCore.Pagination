using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Swevo.EFCore.Pagination;
using Xunit;

namespace EFCore.Pagination.Tests;

// ── Test infrastructure ───────────────────────────────────────────────────────

public record Product(int Id, string Name, decimal Price);

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Product>().HasKey(p => p.Id);
}

// ── Offset pagination tests ───────────────────────────────────────────────────

public class ToPageAsyncTests
{
    private static TestDbContext CreateContext(int productCount)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.Products.AddRange(
            Enumerable.Range(1, productCount).Select(i => new Product(i, $"Product {i}", i * 1.5m)));
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task FirstPage_ReturnsCorrectItems()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.Items.Should().HaveCount(10);
        page.Items.First().Id.Should().Be(1);
        page.Items.Last().Id.Should().Be(10);
    }

    [Fact]
    public async Task FirstPage_HasCorrectTotalCount()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task FirstPage_HasCorrectTotalPages()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task FirstPage_HasNextPage()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task FirstPage_HasNoPreviousPage()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task MiddlePage_HasNextAndPreviousPage()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(2, 10);
        page.HasNextPage.Should().BeTrue();
        page.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task LastPage_HasNoPreviousNextPage()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(3, 10);
        page.HasNextPage.Should().BeFalse();
        page.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task LastPage_ReturnsRemainingItems()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(3, 10);
        page.Items.Should().HaveCount(5);
        page.Items.Last().Id.Should().Be(25);
    }

    [Fact]
    public async Task ExactlyOnePage_HasNoNextOrPreviousPage()
    {
        using var ctx = CreateContext(10);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.HasNextPage.Should().BeFalse();
        page.HasPreviousPage.Should().BeFalse();
        page.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task EmptyQuery_ReturnsTotalCountZero()
    {
        using var ctx = CreateContext(0);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task EmptyQuery_HasNoNextOrPreviousPage()
    {
        using var ctx = CreateContext(0);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 10);
        page.HasNextPage.Should().BeFalse();
        page.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidPageNumber_Throws()
    {
        using var ctx = CreateContext(5);
        var act = () => ctx.Products.OrderBy(p => p.Id).ToPageAsync(0, 10);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("pageNumber");
    }

    [Fact]
    public async Task InvalidPageSize_Zero_Throws()
    {
        using var ctx = CreateContext(5);
        var act = () => ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    [Fact]
    public async Task InvalidPageSize_TooLarge_Throws()
    {
        using var ctx = CreateContext(5);
        var act = () => ctx.Products.OrderBy(p => p.Id).ToPageAsync(1, 1001);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    [Fact]
    public async Task PageNumber_ReflectedInResult()
    {
        using var ctx = CreateContext(30);
        var page = await ctx.Products.OrderBy(p => p.Id).ToPageAsync(2, 10);
        page.PageNumber.Should().Be(2);
        page.PageSize.Should().Be(10);
    }
}

// ── Cursor pagination tests ───────────────────────────────────────────────────

public class ToCursorPageAsyncTests
{
    private static TestDbContext CreateContext(int productCount)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.Products.AddRange(
            Enumerable.Range(1, productCount).Select(i => new Product(i, $"Product {i}", i * 1.5m)));
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task FirstPage_NoCursor_ReturnsFirstItems()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 10);
        page.Items.Should().HaveCount(10);
        page.Items.First().Id.Should().Be(1);
        page.Items.Last().Id.Should().Be(10);
    }

    [Fact]
    public async Task FirstPage_HasNextCursorWhenMoreItemsExist()
    {
        using var ctx = CreateContext(25);
        var page = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 10);
        page.HasNextPage.Should().BeTrue();
        page.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FirstPage_NoNextCursorWhenExactlyPageSize()
    {
        using var ctx = CreateContext(10);
        var page = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 10);
        page.HasNextPage.Should().BeFalse();
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task WithCursor_ReturnsItemsAfterCursor()
    {
        using var ctx = CreateContext(25);
        var firstPage = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 10);

        var secondPage = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, afterCursor: firstPage.NextCursor, pageSize: 10);

        secondPage.Items.Should().HaveCount(10);
        secondPage.Items.First().Id.Should().Be(11);
        secondPage.Items.Last().Id.Should().Be(20);
    }

    [Fact]
    public async Task LastPage_HasNoNextCursor()
    {
        using var ctx = CreateContext(25);
        var firstPage = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 10);

        var secondPage = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, afterCursor: firstPage.NextCursor, pageSize: 10);

        var thirdPage = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, afterCursor: secondPage.NextCursor, pageSize: 10);

        thirdPage.Items.Should().HaveCount(5);
        thirdPage.HasNextPage.Should().BeFalse();
        thirdPage.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task CursorRoundTrip_PagesAreContinuous()
    {
        using var ctx = CreateContext(30);
        var allIds = new List<int>();
        string? cursor = null;

        do
        {
            var page = await ctx.Products.OrderBy(p => p.Id)
                .ToCursorPageAsync(p => p.Id, afterCursor: cursor, pageSize: 10);
            allIds.AddRange(page.Items.Select(p => p.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        allIds.Should().HaveCount(30);
        allIds.Should().BeInAscendingOrder();
        allIds.First().Should().Be(1);
        allIds.Last().Should().Be(30);
    }

    [Fact]
    public async Task EmptyQuery_ReturnsNoCursorAndNoItems()
    {
        using var ctx = CreateContext(0);
        var page = await ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 10);
        page.Items.Should().BeEmpty();
        page.HasNextPage.Should().BeFalse();
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task InvalidPageSize_Zero_Throws()
    {
        using var ctx = CreateContext(5);
        var act = () => ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    [Fact]
    public async Task InvalidPageSize_TooLarge_Throws()
    {
        using var ctx = CreateContext(5);
        var act = () => ctx.Products.OrderBy(p => p.Id)
            .ToCursorPageAsync(p => p.Id, pageSize: 1001);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }
}

// ── Page<T> unit tests ────────────────────────────────────────────────────────

public class PageTests
{
    [Fact]
    public void TotalPages_RoundsUp()
    {
        var page = new Page<int>([1, 2, 3], totalCount: 25, pageNumber: 1, pageSize: 10);
        page.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_ExactDivision()
    {
        var page = new Page<int>([1, 2, 3], totalCount: 20, pageNumber: 1, pageSize: 10);
        page.TotalPages.Should().Be(2);
    }

    [Fact]
    public void Empty_ReturnsZeroTotalCount()
    {
        var page = Page<int>.Empty();
        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
        page.HasNextPage.Should().BeFalse();
        page.HasPreviousPage.Should().BeFalse();
    }
}
