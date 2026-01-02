using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Services;

namespace TripleDerby.Tests.Unit.Services;

public class ColorCacheTests
{
    private readonly Mock<ILogger<ColorCache>> _loggerMock;
    private readonly Mock<ITripleDerbyRepository> _repositoryMock;

    public ColorCacheTests()
    {
        _loggerMock = new Mock<ILogger<ColorCache>>();
        _repositoryMock = new Mock<ITripleDerbyRepository>();
    }

    [Fact]
    public async Task ColorCache_FirstCall_LoadsFromRepository()
    {
        // Arrange
        var expectedColors = new List<Color>
        {
            new() { Id = 1, Name = "Bay", Weight = 10, IsSpecial = false },
            new() { Id = 2, Name = "Chestnut", Weight = 10, IsSpecial = false },
            new() { Id = 3, Name = "Black", Weight = 15, IsSpecial = false },
            new() { Id = 4, Name = "White", Weight = 20, IsSpecial = false },
            new() { Id = 5, Name = "Gray", Weight = 12, IsSpecial = false },
            new() { Id = 6, Name = "Palomino", Weight = 18, IsSpecial = false },
            new() { Id = 7, Name = "Buckskin", Weight = 16, IsSpecial = false },
            new() { Id = 8, Name = "Dapple Gray", Weight = 25, IsSpecial = false },
            new() { Id = 9, Name = "Roan", Weight = 22, IsSpecial = false },
            new() { Id = 10, Name = "Golden", Weight = 50, IsSpecial = true }
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedColors);

        var cache = new ColorCache(_loggerMock.Object);

        // Act
        var result = await cache.GetColorsAsync(_repositoryMock.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
        Assert.Equal(expectedColors, result);
        _repositoryMock.Verify(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ColorCache_SubsequentCalls_ReturnsFromCache()
    {
        // Arrange
        var colors = new List<Color>
        {
            new() { Id = 1, Name = "Bay", Weight = 10, IsSpecial = false },
            new() { Id = 2, Name = "Chestnut", Weight = 10, IsSpecial = false }
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        var cache = new ColorCache(_loggerMock.Object);

        // Act
        var result1 = await cache.GetColorsAsync(_repositoryMock.Object, CancellationToken.None);
        var result2 = await cache.GetColorsAsync(_repositoryMock.Object, CancellationToken.None);
        var result3 = await cache.GetColorsAsync(_repositoryMock.Object, CancellationToken.None);

        // Assert
        Assert.Same(result1, result2);
        Assert.Same(result2, result3);
        _repositoryMock.Verify(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ColorCache_ConcurrentCalls_LoadsOnlyOnce()
    {
        // Arrange
        var colors = new List<Color>
        {
            new() { Id = 1, Name = "Bay", Weight = 10, IsSpecial = false },
            new() { Id = 2, Name = "Chestnut", Weight = 10, IsSpecial = false }
        };

        var callCount = 0;
        _repositoryMock
            .Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(100); // Simulate slow DB query
                return (IEnumerable<Color>)colors;
            });

        var cache = new ColorCache(_loggerMock.Object);

        // Act - Call GetColorsAsync concurrently from 10 tasks
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => cache.GetColorsAsync(_repositoryMock.Object, CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, callCount); // Repository called exactly once despite concurrent requests
        Assert.All(results, result => Assert.Same(results[0], result)); // All results are same instance
    }

    [Fact]
    public async Task ColorCache_Invalidate_ClearsCache()
    {
        // Arrange
        var initialColors = new List<Color>
        {
            new() { Id = 1, Name = "Bay", Weight = 10, IsSpecial = false }
        };

        var updatedColors = new List<Color>
        {
            new() { Id = 1, Name = "Bay", Weight = 10, IsSpecial = false },
            new() { Id = 2, Name = "NewColor", Weight = 15, IsSpecial = true }
        };

        var callCount = 0;
        _repositoryMock
            .Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? initialColors : updatedColors;
            });

        var cache = new ColorCache(_loggerMock.Object);

        // Act
        var result1 = await cache.GetColorsAsync(_repositoryMock.Object, CancellationToken.None);
        cache.Invalidate();
        var result2 = await cache.GetColorsAsync(_repositoryMock.Object, CancellationToken.None);

        // Assert
        Assert.Single(result1);
        Assert.Equal(2, result2.Count);
        Assert.NotSame(result1, result2);
        _repositoryMock.Verify(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
