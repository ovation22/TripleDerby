using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Api.Controllers;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Tests.Unit.Controllers;

/// <summary>
/// Tests for RaceRunsController integration with Race microservice (Phase 7)
/// </summary>
public class RaceRunsControllerTests
{
    private static void SetupControllerContext(ControllerBase controller)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new ControllerActionDescriptor());

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("/api/races/5/runs/requests/test-request-id");

        controller.ControllerContext = new ControllerContext(actionContext);
        controller.Url = mockUrlHelper.Object;
    }

    [Fact]
    public async Task CreateRun_CallsRaceServiceQueueRaceAsync()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        byte raceId = 5;
        var horseId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        mockRaceService
            .Setup(x => x.QueueRaceAsync(raceId, horseId, Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaceRequestStatusResult(
                Id: requestId,
                RaceId: raceId,
                HorseId: horseId,
                Status: RaceRequestStatus.Pending,
                RaceRunId: null,
                OwnerId: Guid.Empty,
                CreatedDate: DateTimeOffset.UtcNow,
                ProcessedDate: null,
                UpdatedDate: null,
                FailureReason: null
            ));

        var controller = new RaceRunsController(
            mockRaceService.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        SetupControllerContext(controller);

        // Act
        await controller.CreateRun(raceId, horseId);

        // Assert
        mockRaceService.Verify(
            x => x.QueueRaceAsync(raceId, horseId, Guid.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRun_ReturnsAcceptedWithRequestId()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        byte raceId = 5;
        var horseId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        mockRaceService
            .Setup(x => x.QueueRaceAsync(raceId, horseId, Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaceRequestStatusResult(
                Id: requestId,
                RaceId: raceId,
                HorseId: horseId,
                Status: RaceRequestStatus.Pending,
                RaceRunId: null,
                OwnerId: Guid.Empty,
                CreatedDate: DateTimeOffset.UtcNow,
                ProcessedDate: null,
                UpdatedDate: null,
                FailureReason: null
            ));

        var controller = new RaceRunsController(
            mockRaceService.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        SetupControllerContext(controller);

        // Act
        var result = await controller.CreateRun(raceId, horseId);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status202Accepted, acceptedResult.StatusCode);

        var resource = Assert.IsType<Resource<RaceRequestStatusResult>>(acceptedResult.Value);
        Assert.NotNull(resource.Data);
        Assert.Equal(requestId, resource.Data.Id);
        Assert.Equal(RaceRequestStatus.Pending, resource.Data.Status);
        Assert.NotNull(resource.Links);
        Assert.Contains(resource.Links, l => l.Rel == "self");
        Assert.Contains(resource.Links, l => l.Rel == "replay");
    }

    [Fact]
    public async Task GetRuns_WithSortByWinnerName_ReturnsSortedAscending()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        byte raceId = 1;
        var summaries = new List<RaceRunSummary>
        {
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Alpha Horse", WinnerTime = 120.5, FieldSize = 8, ConditionId = ConditionId.Fast, ConditionName = "Fast" },
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Beta Horse", WinnerTime = 121.0, FieldSize = 10, ConditionId = ConditionId.Fast, ConditionName = "Fast" },
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Gamma Horse", WinnerTime = 119.8, FieldSize = 9, ConditionId = ConditionId.Muddy, ConditionName = "Muddy" }
        };
        var expectedRuns = new PagedList<RaceRunSummary>(summaries, 3, 1, 10);

        mockRaceRunService
            .Setup(x => x.GetRaceRuns(raceId, It.Is<PaginationRequest>(r =>
                r.Page == 1 && r.Size == 10 && r.SortBy == "WinnerName" && r.Direction == SortDirection.Asc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRuns);

        var controller = new RaceRunsController(
            mockRaceService.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        // Act
        var request = new PaginationRequest { Page = 1, Size = 10, SortBy = "WinnerName", Direction = SortDirection.Asc };
        var result = await controller.GetRuns(raceId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedList = Assert.IsType<PagedList<RaceRunSummary>>(okResult.Value);

        Assert.Equal(3, pagedList.Total);
        var runs = pagedList.Data.ToList();
        Assert.Equal("Alpha Horse", runs[0].WinnerName);
        Assert.Equal("Beta Horse", runs[1].WinnerName);
        Assert.Equal("Gamma Horse", runs[2].WinnerName);
    }

    [Fact]
    public async Task GetRuns_WithSortByWinnerTime_ReturnsSortedDescending()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        byte raceId = 1;
        var summaries = new List<RaceRunSummary>
        {
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Slow Horse", WinnerTime = 125.0, FieldSize = 8, ConditionId = ConditionId.Muddy, ConditionName = "Muddy" },
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Medium Horse", WinnerTime = 121.0, FieldSize = 10, ConditionId = ConditionId.Fast, ConditionName = "Fast" },
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Fast Horse", WinnerTime = 119.8, FieldSize = 9, ConditionId = ConditionId.Fast, ConditionName = "Fast" }
        };
        var expectedRuns = new PagedList<RaceRunSummary>(summaries, 3, 1, 10);

        mockRaceRunService
            .Setup(x => x.GetRaceRuns(raceId, It.Is<PaginationRequest>(r =>
                r.Page == 1 && r.Size == 10 && r.SortBy == "WinnerTime" && r.Direction == SortDirection.Desc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRuns);

        var controller = new RaceRunsController(
            mockRaceService.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        // Act
        var request = new PaginationRequest { Page = 1, Size = 10, SortBy = "WinnerTime", Direction = SortDirection.Desc };
        var result = await controller.GetRuns(raceId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedList = Assert.IsType<PagedList<RaceRunSummary>>(okResult.Value);

        Assert.Equal(3, pagedList.Total);
        var runs = pagedList.Data.ToList();
        Assert.Equal(125.0, runs[0].WinnerTime);
        Assert.Equal(121.0, runs[1].WinnerTime);
        Assert.Equal(119.8, runs[2].WinnerTime);
    }

    [Fact]
    public async Task GetRuns_WithSortByFieldSize_ReturnsSortedAscending()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        byte raceId = 1;
        var summaries = new List<RaceRunSummary>
        {
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Horse A", WinnerTime = 120.0, FieldSize = 6, ConditionId = ConditionId.Fast, ConditionName = "Fast" },
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Horse B", WinnerTime = 121.0, FieldSize = 8, ConditionId = ConditionId.Fast, ConditionName = "Fast" },
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Horse C", WinnerTime = 119.8, FieldSize = 12, ConditionId = ConditionId.Muddy, ConditionName = "Muddy" }
        };
        var expectedRuns = new PagedList<RaceRunSummary>(summaries, 3, 1, 10);

        mockRaceRunService
            .Setup(x => x.GetRaceRuns(raceId, It.Is<PaginationRequest>(r =>
                r.Page == 1 && r.Size == 10 && r.SortBy == "FieldSize" && r.Direction == SortDirection.Asc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRuns);

        var controller = new RaceRunsController(
            mockRaceService.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        // Act
        var request = new PaginationRequest { Page = 1, Size = 10, SortBy = "FieldSize", Direction = SortDirection.Asc };
        var result = await controller.GetRuns(raceId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedList = Assert.IsType<PagedList<RaceRunSummary>>(okResult.Value);

        Assert.Equal(3, pagedList.Total);
        var runs = pagedList.Data.ToList();
        Assert.Equal(6, runs[0].FieldSize);
        Assert.Equal(8, runs[1].FieldSize);
        Assert.Equal(12, runs[2].FieldSize);
    }

    [Fact]
    public async Task GetRuns_WithoutSortParameters_ReturnsDefaultSort()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        byte raceId = 1;
        var summaries = new List<RaceRunSummary>
        {
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Most Recent", WinnerTime = 120.0, FieldSize = 8, ConditionId = ConditionId.Fast, ConditionName = "Fast" },
            new() { RaceRunId = Guid.NewGuid(), WinnerName = "Previous", WinnerTime = 121.0, FieldSize = 10, ConditionId = ConditionId.Fast, ConditionName = "Fast" }
        };
        var expectedRuns = new PagedList<RaceRunSummary>(summaries, 2, 1, 10);

        mockRaceRunService
            .Setup(x => x.GetRaceRuns(raceId, It.Is<PaginationRequest>(r =>
                r.Page == 1 && r.Size == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRuns);

        var controller = new RaceRunsController(
            mockRaceService.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        // Act
        var request = new PaginationRequest { Page = 1, Size = 10 };
        var result = await controller.GetRuns(raceId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedList = Assert.IsType<PagedList<RaceRunSummary>>(okResult.Value);

        Assert.Equal(2, pagedList.Total);
        Assert.Equal(2, pagedList.Data.Count());
    }

    [Fact]
    public async Task GetRuns_WithInvalidRaceId_ReturnsNotFound()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        byte raceId = 99;

        mockRaceRunService
            .Setup(x => x.GetRaceRuns(raceId, It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedList<RaceRunSummary>?)null);

        var controller = new RaceRunsController(
            mockRaceService.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        // Act
        var request = new PaginationRequest { Page = 1, Size = 10 };
        var result = await controller.GetRuns(raceId, request);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
