using Microsoft.AspNetCore.Mvc;
using Moq;
using TripleDerby.Api.Controllers;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Tests.Unit.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<IBreedingService> _mockBreedingService;
    private readonly Mock<IFeedingService> _mockFeedingService;
    private readonly Mock<IRaceService> _mockRaceService;
    private readonly Mock<ITrainingService> _mockTrainingService;
    private readonly MessagesController _controller;

    public MessagesControllerTests()
    {
        _mockMessageService = new Mock<IMessageService>();
        _mockBreedingService = new Mock<IBreedingService>();
        _mockFeedingService = new Mock<IFeedingService>();
        _mockRaceService = new Mock<IRaceService>();
        _mockTrainingService = new Mock<ITrainingService>();

        _controller = new MessagesController(
            _mockMessageService.Object,
            _mockBreedingService.Object,
            _mockFeedingService.Object,
            _mockRaceService.Object,
            _mockTrainingService.Object);
    }

    [Fact]
    public async Task GetSummary_ReturnsOk200WithSummary()
    {
        // Arrange
        var expectedSummary = new MessageRequestsSummaryResult
        {
            Breeding = new ServiceSummary { Pending = 5, Failed = 2, Completed = 10 },
            Feeding = new ServiceSummary { Pending = 3, Failed = 1, Completed = 8 },
            Racing = new ServiceSummary { Pending = 7, Failed = 0, Completed = 15 },
            Training = new ServiceSummary { Pending = 2, Failed = 3, Completed = 6 },
            TotalPending = 17,
            TotalFailed = 6
        };

        _mockMessageService
            .Setup(s => s.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = Assert.IsType<ActionResult<MessageRequestsSummaryResult>>(result);
        var okObjectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var actualSummary = Assert.IsType<MessageRequestsSummaryResult>(okObjectResult.Value);

        Assert.Equal(17, actualSummary.TotalPending);
        Assert.Equal(6, actualSummary.TotalFailed);
    }

    [Fact]
    public async Task GetAll_ReturnsOk200WithPagedList()
    {
        // Arrange
        var pagination = new PaginationRequest { Page = 1, Size = 50 };
        var items = new List<MessageRequestSummary>
        {
            new() { Id = Guid.NewGuid(), ServiceType = RequestServiceType.Breeding, Status = RequestStatus.Pending, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), ServiceType = RequestServiceType.Feeding, Status = RequestStatus.Failed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid() }
        };
        var pagedList = new PagedList<MessageRequestSummary>(items, 2, 1, 50);

        _mockMessageService
            .Setup(s => s.GetAllRequestsAsync(pagination, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _controller.GetAll(pagination);

        // Assert
        var okResult = Assert.IsType<ActionResult<PagedList<MessageRequestSummary>>>(result);
        var okObjectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var actualPagedList = Assert.IsType<PagedList<MessageRequestSummary>>(okObjectResult.Value);

        Assert.Equal(2, actualPagedList.Total);
        Assert.Equal(2, actualPagedList.Data.Count());
    }

    [Fact]
    public async Task ReplayRequest_ValidBreedingRequest_Returns202()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _mockBreedingService
            .Setup(s => s.ReplayBreedingRequest(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReplayRequest(RequestServiceType.Breeding, requestId);

        // Assert
        Assert.IsType<AcceptedResult>(result);
        _mockBreedingService.Verify(s => s.ReplayBreedingRequest(requestId, It.IsAny<CancellationToken>()), Times.Once);
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
        var result = await _controller.ReplayRequest(RequestServiceType.Feeding, requestId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ReplayRequest_TrainingService_Returns202()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _mockTrainingService
            .Setup(s => s.ReplayTrainingRequest(requestId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ReplayRequest(RequestServiceType.Training, requestId);

        // Assert
        Assert.IsType<AcceptedResult>(result);
        _mockTrainingService.Verify(s => s.ReplayTrainingRequest(requestId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplayRequest_TrainingNotFound_Returns404()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _mockTrainingService
            .Setup(s => s.ReplayTrainingRequest(requestId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.ReplayRequest(RequestServiceType.Training, requestId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ReplayAll_ValidServiceType_Returns202WithCount()
    {
        // Arrange
        var expectedCount = 10;

        _mockFeedingService
            .Setup(s => s.ReplayAllNonComplete(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.ReplayAll(RequestServiceType.Feeding, maxDegreeOfParallelism: 10);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(acceptedResult.Value);

        // Use reflection to check the anonymous object
        var publishedProperty = acceptedResult.Value.GetType().GetProperty("published");
        Assert.NotNull(publishedProperty);
        var publishedValue = publishedProperty.GetValue(acceptedResult.Value);
        Assert.Equal(expectedCount, publishedValue);

        _mockFeedingService.Verify(s => s.ReplayAllNonComplete(10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
