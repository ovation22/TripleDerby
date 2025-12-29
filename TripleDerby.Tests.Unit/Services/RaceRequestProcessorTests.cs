using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
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
    public async Task ProcessAsync_ValidRequest_CallsRaceService()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var expectedResult = CreateTestRaceRunResult();

        mockRaceService
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var processor = new RaceRequestProcessor(
            mockRaceService.Object,
            new Mock<ITripleDerbyRepository>().Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        // Act
        var result = await processor.ProcessAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockRaceService.Verify(
            x => x.Race(request.RaceId, request.HorseId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ValidRequest_ReturnsRaceRunResult()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var expectedResult = CreateTestRaceRunResult();

        mockRaceService
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var processor = new RaceRequestProcessor(
            mockRaceService.Object,
            new Mock<ITripleDerbyRepository>().Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        // Act
        var result = await processor.ProcessAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedResult.RaceRunId, result.RaceRunId);
        Assert.Equal(expectedResult.RaceName, result.RaceName);
    }

    [Fact]
    public async Task ProcessAsync_RaceServiceThrows_PropagatesException()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        mockRaceService
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Horse not found"));

        var processor = new RaceRequestProcessor(
            mockRaceService.Object,
            new Mock<ITripleDerbyRepository>().Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ProcessAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_CancellationRequested_RespectsCancellation()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockRaceService
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var processor = new RaceRequestProcessor(
            mockRaceService.Object,
            new Mock<ITripleDerbyRepository>().Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            processor.ProcessAsync(request, cts.Token));
    }

    [Fact]
    public async Task ProcessAsync_SuccessfulRace_UpdatesRaceRequestWithResult()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockRepository = new Mock<ITripleDerbyRepository>();
        var expectedResult = CreateTestRaceRunResult();

        var raceRequest = new RaceRequest
        {
            Id = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            Status = RaceRequestStatus.Pending
        };

        mockRaceService
            .Setup(x => x.Race(It.IsAny<byte>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        mockRepository
            .Setup(x => x.FindAsync<RaceRequest>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(raceRequest);

        var processor = new RaceRequestProcessor(
            mockRaceService.Object,
            mockRepository.Object,
            NullLogger<RaceRequestProcessor>.Instance);

        var request = new RaceRequested
        {
            CorrelationId = raceRequest.Id,
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        // Act
        await processor.ProcessAsync(request, CancellationToken.None);

        // Assert - verify UpdateAsync was called exactly twice
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
