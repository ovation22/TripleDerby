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
}
