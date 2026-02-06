using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TripleDerby.Api.Controllers;
using TripleDerby.Core.Abstractions.Services;

namespace TripleDerby.Tests.Unit.Controllers;

/// <summary>
/// Tests for FeedingsController replay endpoints (Phase 1)
/// </summary>
public class FeedingsControllerTests
{
    private readonly Mock<IFeedingService> _mockFeedingService;
    private readonly FeedingsController _controller;

    public FeedingsControllerTests()
    {
        _mockFeedingService = new Mock<IFeedingService>();
        _controller = new FeedingsController(_mockFeedingService.Object);
    }

    [Fact]
    public async Task ReplayRequest_ValidId_Returns202Accepted()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _mockFeedingService
            .Setup(s => s.ReplayFeedingRequest(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReplayRequest(requestId);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        _mockFeedingService.Verify(
            s => s.ReplayFeedingRequest(requestId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplayRequest_NotFound_Returns404()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _mockFeedingService
            .Setup(s => s.ReplayFeedingRequest(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReplayRequest(requestId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ReplayAll_ValidRequest_Returns202WithCount()
    {
        // Arrange
        var expectedCount = 5;

        _mockFeedingService
            .Setup(s => s.ReplayAllNonComplete(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.ReplayAll(maxDegreeOfParallelism: 10);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(acceptedResult.Value);

        // Use reflection to check the anonymous object
        var publishedProperty = acceptedResult.Value.GetType().GetProperty("published");
        Assert.NotNull(publishedProperty);
        var publishedValue = publishedProperty.GetValue(acceptedResult.Value);
        Assert.Equal(expectedCount, publishedValue);

        _mockFeedingService.Verify(
            s => s.ReplayAllNonComplete(10, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
