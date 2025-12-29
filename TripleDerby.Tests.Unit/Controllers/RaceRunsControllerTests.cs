using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Api.Controllers;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

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
    public async Task CreateRun_PublishesRaceRequestedMessage()
    {
        // Arrange
        var mockPublisher = new Mock<IMessagePublisher>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        var controller = new RaceRunsController(
            mockPublisher.Object,
            mockRepository.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        SetupControllerContext(controller);

        byte raceId = 5;
        var horseId = Guid.NewGuid();

        // Act
        await controller.CreateRun(raceId, horseId);

        // Assert
        mockPublisher.Verify(
            x => x.PublishAsync(
                It.Is<RaceRequested>(r =>
                    r.RaceId == raceId &&
                    r.HorseId == horseId &&
                    r.CorrelationId != Guid.Empty),
                It.Is<MessagePublishOptions>(o => o.Destination == "race-requests"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRun_CreatesRaceRequestEntity()
    {
        // Arrange
        var mockPublisher = new Mock<IMessagePublisher>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        var controller = new RaceRunsController(
            mockPublisher.Object,
            mockRepository.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        SetupControllerContext(controller);

        byte raceId = 5;
        var horseId = Guid.NewGuid();

        // Act
        await controller.CreateRun(raceId, horseId);

        // Assert
        mockRepository.Verify(
            x => x.CreateAsync(
                It.Is<RaceRequest>(r =>
                    r.RaceId == raceId &&
                    r.HorseId == horseId &&
                    r.Status == RaceRequestStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRun_ReturnsAcceptedWithRequestId()
    {
        // Arrange
        var mockPublisher = new Mock<IMessagePublisher>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        var controller = new RaceRunsController(
            mockPublisher.Object,
            mockRepository.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        SetupControllerContext(controller);

        byte raceId = 5;
        var horseId = Guid.NewGuid();

        // Act
        var result = await controller.CreateRun(raceId, horseId);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result.Result);
        Assert.Equal(StatusCodes.Status202Accepted, acceptedResult.StatusCode);

        var value = Assert.IsType<RaceRequestResponse>(acceptedResult.Value);
        Assert.NotEqual(Guid.Empty, value.RequestId);
        Assert.Equal(RaceRequestStatus.Pending, value.Status);
    }

    [Fact]
    public async Task CreateRun_CorrelationIdMatchesRequestId()
    {
        // Arrange
        var mockPublisher = new Mock<IMessagePublisher>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockRaceRunService = new Mock<IRaceRunService>();

        RaceRequested? publishedMessage = null;
        mockPublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<RaceRequested>(),
                It.IsAny<MessagePublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<RaceRequested, MessagePublishOptions, CancellationToken>((msg, _, _) =>
            {
                publishedMessage = msg;
            });

        RaceRequest? savedRequest = null;
        mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<RaceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRequest, CancellationToken>((req, _) =>
            {
                savedRequest = req;
            });

        var controller = new RaceRunsController(
            mockPublisher.Object,
            mockRepository.Object,
            mockRaceRunService.Object,
            NullLogger<RaceRunsController>.Instance);

        SetupControllerContext(controller);

        // Act
        var result = await controller.CreateRun(5, Guid.NewGuid());

        // Assert
        Assert.NotNull(publishedMessage);
        Assert.NotNull(savedRequest);
        Assert.Equal(publishedMessage.CorrelationId, savedRequest.Id);
    }
}
