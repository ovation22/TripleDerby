using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Infrastructure.Data.Repositories;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories.EFRepositoryTests;

public class FindAsyncTests
{
    [Fact]
    public async Task FindAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestDbContext(options);
        var loggerMock = new Mock<ILogger<EFRepository>>();
        var repo = new TestRepository(dbContext, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.FindAsync<TestEntity>(null!, CancellationToken.None));
    }
}