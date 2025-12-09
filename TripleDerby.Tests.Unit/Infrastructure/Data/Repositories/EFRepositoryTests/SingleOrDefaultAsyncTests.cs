using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Infrastructure.Data.Repositories;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories.EFRepositoryTests;

public class SingleOrDefaultAsync
{
    [Fact]
    public async Task SingleOrDefaultAsync_WithExpression_ReturnsEntity_WhenMatches()
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
        var result = await repo.SingleOrDefaultAsync<TestEntity>(e => e.Name == "Alpha", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_WithExpression_ReturnsNull_WhenNoMatch()
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
        var result = await repo.SingleOrDefaultAsync<TestEntity>(e => e.Name == "Gamma", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_WithSpecification_ReturnsEntity_WhenMatches()
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
        var result = await repo.SingleOrDefaultAsync(spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Beta", result.Name);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_WithSpecificationProjected_ReturnsProjection_WhenMatches()
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
        var result = await repo.SingleOrDefaultAsync(spec, CancellationToken.None);

        // Assert
        Assert.Equal("Alpha", result);
    }
}

internal class ByNameSpec : Specification<TestEntity>
{
    public ByNameSpec(string name)
    {
        Query.Where(e => e.Name == name);
    }
}

internal class ByNameProjectedSpec : Specification<TestEntity, string>
{
    public ByNameProjectedSpec(string name)
    {
        Query.Where(e => e.Name == name).Select(e => e.Name!);
    }
}