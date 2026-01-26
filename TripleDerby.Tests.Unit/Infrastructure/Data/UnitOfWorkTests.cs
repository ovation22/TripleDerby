using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Core.Abstractions.Data;
using TripleDerby.Infrastructure.Data;
using TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

namespace TripleDerby.Tests.Unit.Infrastructure.Data;

public class UnitOfWorkTests
{
    private static (TestDbContext, UnitOfWork) CreateUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var dbContext = new TestDbContext(options);
        // Must open connection for in-memory SQLite
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();

        var mockLogger = new Mock<ILogger<UnitOfWork>>();
        var unitOfWork = new UnitOfWork(dbContext, mockLogger.Object);

        return (dbContext, unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_CommitsTransaction()
    {
        // Arrange
        var (dbContext, unitOfWork) = CreateUnitOfWork();
        var operationExecuted = false;

        // Act
        await unitOfWork.ExecuteAsync(async () =>
        {
            // Add an entity to verify transaction commits
            var entity = new TestEntity { Id = 1, Name = "Test" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            operationExecuted = true;
        }, CancellationToken.None);

        // Assert
        Assert.True(operationExecuted);
        var savedEntity = await dbContext.TestEntities.FindAsync(1);
        Assert.NotNull(savedEntity);
        Assert.Equal("Test", savedEntity.Name);

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteAsync_OperationThrowsException_RollsBackTransaction()
    {
        // Arrange
        var (dbContext, unitOfWork) = CreateUnitOfWork();
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await unitOfWork.ExecuteAsync(async () =>
            {
                // Add an entity that should be rolled back
                var entity = new TestEntity { Id = 1, Name = "ShouldBeRolledBack" };
                dbContext.TestEntities.Add(entity);
                await dbContext.SaveChangesAsync();

                throw expectedException;
            }, CancellationToken.None);
        });

        // Assert exception was propagated
        Assert.Same(expectedException, actualException);

        // Verify entity was NOT persisted (rolled back)
        var savedEntity = await dbContext.TestEntities.FindAsync(1);
        Assert.Null(savedEntity);

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithResult_ReturnsResultAndCommits()
    {
        // Arrange
        var (dbContext, unitOfWork) = CreateUnitOfWork();
        var expectedResult = 42;

        // Act
        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            // Add an entity to verify transaction commits
            var entity = new TestEntity { Id = 1, Name = "Test" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();

            return expectedResult;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
        var savedEntity = await dbContext.TestEntities.FindAsync(1);
        Assert.NotNull(savedEntity);

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteAsync_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var dbContext = new TestDbContext(options);
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();

        var mockLogger = new Mock<ILogger<UnitOfWork>>();
        var unitOfWork = new UnitOfWork(dbContext, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await unitOfWork.ExecuteAsync((Func<Task>)null!, CancellationToken.None);
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenNoTransaction_StartsTransaction()
    {
        // Arrange
        var (dbContext, unitOfWork) = CreateUnitOfWork();

        // Act
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        // Assert - verify we can add and commit
        var entity = new TestEntity { Id = 1, Name = "Test" };
        dbContext.TestEntities.Add(entity);
        await dbContext.SaveChangesAsync();
        await unitOfWork.CommitAsync(CancellationToken.None);

        var savedEntity = await dbContext.TestEntities.FindAsync(1);
        Assert.NotNull(savedEntity);

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenTransactionActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var (dbContext, unitOfWork) = CreateUnitOfWork();

        // Act - start first transaction
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        // Assert - second call should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        });

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CommitAsync_WhenNoTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var dbContext = new TestDbContext(options);
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();

        var mockLogger = new Mock<ILogger<UnitOfWork>>();
        var unitOfWork = new UnitOfWork(dbContext, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await unitOfWork.CommitAsync(CancellationToken.None);
        });
    }

    [Fact]
    public async Task RollbackAsync_WhenNoTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var dbContext = new TestDbContext(options);
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();

        var mockLogger = new Mock<ILogger<UnitOfWork>>();
        var unitOfWork = new UnitOfWork(dbContext, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
        });
    }
}
