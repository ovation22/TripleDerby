using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using TripleDerby.Infrastructure.Data.Repositories;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories.EFRepositoryTests;

public class FirstOrDefaultAsyncTests
{
    [Fact]
    public async Task FirstOrDefaultAsync_WithExpression_ReturnsEntity_WhenMatches()
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

        // Act
        var result = await repo.FirstOrDefaultAsync<TestEntity>(e => e.Name == "Alpha", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithExpression_ReturnsNull_WhenNoMatch()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new TestDbContext(options);
        ctx.TestEntities.Add(new TestEntity { Id = 1, Name = "Alpha" });
        await ctx.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<EFRepository>>();
        var repo = new TestRepository(ctx, loggerMock.Object);

        // Act
        var result = await repo.FirstOrDefaultAsync<TestEntity>(e => e.Name == "Gamma", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithExpressionAndOrderBy_RespectsOrder_WhenMultipleMatch()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new TestDbContext(options);
        ctx.TestEntities.AddRange(
            new TestEntity { Id = 1, Name = "Alpha" },
            new TestEntity { Id = 2, Name = "Beta" },
            new TestEntity { Id = 3, Name = "Charlie" });
        await ctx.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<EFRepository>>();
        var repo = new TestRepository(ctx, loggerMock.Object);

        // Both match the predicate (Name length >= 5), order by Name descending -> expect "Charlie"
        Expression<Func<TestEntity, bool>> predicate = e => e.Name!.Length >= 5;

        // Act
        var result = await repo.FirstOrDefaultAsync(predicate, OrderBy, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Charlie", result.Name);
        return;

        static IOrderedQueryable<TestEntity> OrderBy(IQueryable<TestEntity> q) => q.OrderByDescending(e => e.Name);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpecification_ReturnsEntity_WhenMatches()
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

        var spec = new ByNameSpec("Beta");

        // Act
        var result = await repo.FirstOrDefaultAsync(spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Beta", result.Name);
    }
}