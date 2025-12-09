using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using TripleDerby.Infrastructure.Data.Repositories;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories.EFRepositoryTests;

public class ListAsyncTests
{
    [Fact]
    public async Task ListAsync_WithExpression_ReturnsMatchingItems()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new TestDbContext(options);
        ctx.TestEntities.AddRange(
            new TestEntity { Id = 1, Name = "Alpha" },
            new TestEntity { Id = 2, Name = "Beta" },
            new TestEntity { Id = 3, Name = "Gamma" });
        await ctx.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<EFRepository>>();
        var repo = new TestRepository(ctx, loggerMock.Object);

        // Act
        var results = await repo.ListAsync<TestEntity>(e => e.Name!.StartsWith("G"), CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("Gamma", results[0].Name);
    }

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsMatchingItems()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new TestDbContext(options);
        ctx.TestEntities.AddRange(
            new TestEntity { Id = 1, Name = "Alpha" },
            new TestEntity { Id = 2, Name = "Beta" },
            new TestEntity { Id = 3, Name = "AlphaX" });
        await ctx.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<EFRepository>>();
        var repo = new TestRepository(ctx, loggerMock.Object);

        var spec = new ByStartsWithSpec("Alpha");

        // Act
        var results = await repo.ListAsync(spec, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.StartsWith("Alpha", r.Name!));
    }

    [Fact]
    public async Task ListAsync_WithSpecificationProjected_ReturnsProjectedList()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new TestDbContext(options);
        ctx.TestEntities.AddRange(
            new TestEntity { Id = 1, Name = "Alpha" },
            new TestEntity { Id = 2, Name = "Beta" });
        await ctx.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<EFRepository>>();
        var repo = new TestRepository(ctx, loggerMock.Object);

        var spec = new ByNameProjectedSpec("Alpha");

        // Act
        var results = await repo.ListAsync(spec, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("Alpha", results[0]);
    }

    [Fact]
    public async Task ListAsync_GroupBy_ReturnsGroupedProjection()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new TestDbContext(options);
        ctx.TestEntities.AddRange(
            new TestEntity { Id = 1, Name = "Alpha" },
            new TestEntity { Id = 2, Name = "Apricot" },
            new TestEntity { Id = 3, Name = "Beta" });
        await ctx.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<EFRepository>>();
        var repo = new TestRepository(ctx, loggerMock.Object);

        var spec = new Specification<TestEntity>(); // no filter

        Expression<Func<TestEntity, char>> groupBy = e => e.Name![0];
        Expression<Func<IGrouping<char, TestEntity>, GroupResult>> selector = g =>
            new GroupResult { Key = g.Key, Count = g.Count() };

        // Act
        var results = await repo.ListAsync(spec, groupBy, selector, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        var aGroup = results.Single(r => r.Key == 'A');
        var bGroup = results.Single(r => r.Key == 'B');
        Assert.Equal(2, aGroup.Count);
        Assert.Equal(1, bGroup.Count);
    }
}

internal class GroupResult
{
    public char Key { get; set; }
    public int Count { get; set; }
}