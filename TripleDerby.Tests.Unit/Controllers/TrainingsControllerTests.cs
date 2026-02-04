using Microsoft.AspNetCore.Mvc;
using Moq;
using TripleDerby.Api.Controllers;
using TripleDerby.Core.Abstractions.Services;

namespace TripleDerby.Tests.Unit.Controllers;

/// <summary>
/// Tests for TrainingsController replay endpoints (Phase 1)
/// </summary>
public class TrainingsControllerTests
{
    private readonly Mock<ITrainingService> _mockTrainingService;
    private readonly TrainingsController _controller;

    public TrainingsControllerTests()
    {
        _mockTrainingService = new Mock<ITrainingService>();
        _controller = new TrainingsController(_mockTrainingService.Object);
    }

    [Fact]
    public async Task ReplayAll_ValidRequest_Returns202WithCount()
    {
        // Arrange
        var expectedCount = 7;

        _mockTrainingService
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

        _mockTrainingService.Verify(
            s => s.ReplayAllNonComplete(10, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
