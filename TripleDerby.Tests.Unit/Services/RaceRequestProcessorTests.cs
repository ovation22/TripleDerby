using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Racing;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Tests.Unit.Services;

/// <summary>
/// Tests for RaceRequestProcessor (Phase 6)
/// </summary>
public class RaceRequestProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ValidRequest_CallsRaceExecutor()
    {
        // Arrange
        var mockRaceExecutor = new Mock<IRaceExecutor>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockPublisher = new Mock<IMessagePublisher>();
        var expectedResult = CreateTestRaceRunResult();

        mockRaceExecutor
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var processor = new RaceRequestProcessor(
            mockRaceExecutor.Object,
            mockRepository.Object,
            mockPublisher.Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        var context = new MessageContext
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = request.CorrelationId.ToString(),
            CancellationToken = CancellationToken.None
        };

        // Act
        var result = await processor.ProcessAsync(request, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        mockRaceExecutor.Verify(
            x => x.Race(request.RaceId, request.HorseId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ValidRequest_PublishesRaceCompleted()
    {
        // Arrange
        var mockRaceExecutor = new Mock<IRaceExecutor>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockPublisher = new Mock<IMessagePublisher>();
        var expectedResult = CreateTestRaceRunResult();

        mockRaceExecutor
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var processor = new RaceRequestProcessor(
            mockRaceExecutor.Object,
            mockRepository.Object,
            mockPublisher.Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        var context = new MessageContext
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = request.CorrelationId.ToString(),
            CancellationToken = CancellationToken.None
        };

        // Act
        var result = await processor.ProcessAsync(request, context);

        // Assert
        Assert.True(result.Success);
        mockPublisher.Verify(
            x => x.PublishAsync(
                It.Is<RaceCompleted>(rc => rc.CorrelationId == request.CorrelationId),
                It.Is<MessagePublishOptions>(opts => opts.Destination == "race-completions"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_RaceExecutorThrows_ReturnsFailedResult()
    {
        // Arrange
        var mockRaceExecutor = new Mock<IRaceExecutor>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockPublisher = new Mock<IMessagePublisher>();

        mockRaceExecutor
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Horse not found"));

        var processor = new RaceRequestProcessor(
            mockRaceExecutor.Object,
            mockRepository.Object,
            mockPublisher.Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        var context = new MessageContext
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = request.CorrelationId.ToString(),
            CancellationToken = CancellationToken.None
        };

        // Act
        var result = await processor.ProcessAsync(request, context);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Requeue);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task ProcessAsync_CancellationRequested_ReturnsFailedResult()
    {
        // Arrange
        var mockRaceExecutor = new Mock<IRaceExecutor>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockPublisher = new Mock<IMessagePublisher>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockRaceExecutor
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var processor = new RaceRequestProcessor(
            mockRaceExecutor.Object,
            mockRepository.Object,
            mockPublisher.Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        var context = new MessageContext
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = request.CorrelationId.ToString(),
            CancellationToken = cts.Token
        };

        // Act
        var result = await processor.ProcessAsync(request, context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
        Assert.IsType<OperationCanceledException>(result.Exception);
    }

    [Fact]
    public async Task ProcessAsync_SuccessfulRace_UpdatesRaceRequestWithResult()
    {
        // Arrange
        var mockRaceExecutor = new Mock<IRaceExecutor>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var mockPublisher = new Mock<IMessagePublisher>();
        var expectedResult = CreateTestRaceRunResult();

        var raceRequest = new RaceRequest
        {
            Id = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            Status = RaceRequestStatus.Pending
        };

        mockRaceExecutor
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        mockRepository
            .Setup(x => x.FindAsync<RaceRequest>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(raceRequest);

        var processor = new RaceRequestProcessor(
            mockRaceExecutor.Object,
            mockRepository.Object,
            mockPublisher.Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = raceRequest.Id,
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        var context = new MessageContext
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = request.CorrelationId.ToString(),
            CancellationToken = CancellationToken.None
        };

        // Act
        var result = await processor.ProcessAsync(request, context);

        // Assert
        Assert.True(result.Success);

        // Verify UpdateAsync was called exactly twice (InProgress, then Completed)
        mockRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<RaceRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // Verify the final state of the RaceRequest entity
        Assert.Equal(RaceRequestStatus.Completed, raceRequest.Status);
        Assert.Equal(expectedResult.RaceRunId, raceRequest.RaceRunId);
        Assert.NotNull(raceRequest.ProcessedDate);
        Assert.NotNull(raceRequest.UpdatedDate);
    }

    private static RaceRunResult CreateTestRaceRunResult()
    {
        return new RaceRunResult
        {
            RaceRunId = Guid.NewGuid(),
            RaceId = 5,
            RaceName = "Kentucky Derby",
            TrackId = (TrackId)1,
            TrackName = "Belmont Park",
            ConditionId = (ConditionId)1,
            ConditionName = "Firm",
            SurfaceId = (SurfaceId)1,
            SurfaceName = "Dirt",
            Furlongs = 10,
            HorseResults = new List<RaceRunHorseResult>
            {
                new RaceRunHorseResult
                {
                    HorseId = Guid.NewGuid(),
                    HorseName = "Secretariat",
                    Place = 1,
                    Time = 119.4,
                    Payout = 50000
                }
            },
            PlayByPlay = new List<string> { "Race finished!" }
        };
    }
}
