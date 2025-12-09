using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Infrastructure.Data.Repositories;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories.EFRepositoryTests;

public class CreateAsyncTests
{
    [Fact]
    public async Task CreateAsync_AddsEntity_AndPersists_WhenUsingInMemoryDb()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        await using (var dbContext = new TestDbContext(options))
        {
            var loggerMock = new Mock<ILogger<EFRepository>>();
            var repo = new TestRepository(dbContext, loggerMock.Object);

            var entity = new TestEntity { Id = 1, Name = "Alpha" };

            // Act
            var created = await repo.CreateAsync(entity, CancellationToken.None);

            // Assert - returned entity is the same reference/value
            Assert.NotNull(created);
            Assert.Equal(1, created.Id);
            Assert.Equal("Alpha", created.Name);
        }

        // Verify entity persisted in a new context (confirm SaveChanges occurred)
        await using (var verificationContext = new TestDbContext(options))
        {
            var persisted = await verificationContext.TestEntities.FindAsync(1);
            Assert.NotNull(persisted);
            Assert.Equal("Alpha", persisted.Name);
        }
    }

    [Fact]
    public async Task CreateAsync_WhenSaveChangesThrows_LogsError_AndThrowsWrappedException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ThrowingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ThrowingDbContext(options);
        var loggerMock = new Mock<ILogger<EFRepository>>();

        var repo = new TestRepository(dbContext, loggerMock.Object);

        var entity = new TestEntity { Id = 1, Name = "WillFail" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => repo.CreateAsync(entity, CancellationToken.None));

        // The repository wraps DbUpdateException in a generic Exception with a message
        Assert.NotNull(ex);
        Assert.Contains("An error occurred", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify logger was used for an error and that the logged state contains the expected message.
        // Use It.Is<It.IsAnyType> to match the runtime state object and inspect its string representation.
        loggerMock.Verify(
            l => l.Log(
                It.Is<LogLevel>(level => level == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.IndexOf("An error occurred while updating the database.", StringComparison.OrdinalIgnoreCase) >= 0),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.AtLeastOnce);
    }
}