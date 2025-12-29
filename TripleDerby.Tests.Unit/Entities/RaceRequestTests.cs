using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for RaceRequest entity (Phase 4)
/// </summary>
public class RaceRequestTests
{
    [Fact]
    public void RaceRequest_DefaultStatus_IsPending()
    {
        // Arrange & Act
        var request = new RaceRequest();

        // Assert
        Assert.Equal(RaceRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void RaceRequest_SetProperties_Succeeds()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var raceRunId = Guid.NewGuid();

        // Act
        var request = new RaceRequest
        {
            Id = requestId,
            RaceId = 5,
            HorseId = horseId,
            RaceRunId = raceRunId,
            OwnerId = ownerId,
            Status = RaceRequestStatus.InProgress,
            CreatedDate = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        // Assert
        Assert.Equal(requestId, request.Id);
        Assert.Equal(5, request.RaceId);
        Assert.Equal(horseId, request.HorseId);
        Assert.Equal(raceRunId, request.RaceRunId);
        Assert.Equal(ownerId, request.OwnerId);
        Assert.Equal(RaceRequestStatus.InProgress, request.Status);
    }

    [Fact]
    public void RaceRequest_StatusTransition_PendingToInProgress()
    {
        // Arrange
        var request = new RaceRequest
        {
            Status = RaceRequestStatus.Pending
        };

        // Act
        request.Status = RaceRequestStatus.InProgress;

        // Assert
        Assert.Equal(RaceRequestStatus.InProgress, request.Status);
    }

    [Fact]
    public void RaceRequest_StatusTransition_InProgressToCompleted()
    {
        // Arrange
        var request = new RaceRequest
        {
            Status = RaceRequestStatus.InProgress
        };
        var raceRunId = Guid.NewGuid();

        // Act
        request.Status = RaceRequestStatus.Completed;
        request.RaceRunId = raceRunId;
        request.ProcessedDate = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(RaceRequestStatus.Completed, request.Status);
        Assert.Equal(raceRunId, request.RaceRunId);
        Assert.NotNull(request.ProcessedDate);
    }

    [Fact]
    public void RaceRequest_StatusTransition_InProgressToFailed()
    {
        // Arrange
        var request = new RaceRequest
        {
            Status = RaceRequestStatus.InProgress
        };

        // Act
        request.Status = RaceRequestStatus.Failed;
        request.FailureReason = "Database connection failed";
        request.ProcessedDate = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(RaceRequestStatus.Failed, request.Status);
        Assert.Equal("Database connection failed", request.FailureReason);
        Assert.NotNull(request.ProcessedDate);
    }

    [Fact]
    public void RaceRequest_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var request = new RaceRequest
        {
            Id = Guid.NewGuid(),
            RaceId = 10,
            HorseId = Guid.NewGuid(),
            RaceRunId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Status = RaceRequestStatus.Completed,
            FailureReason = null,
            CreatedDate = DateTimeOffset.UtcNow.AddMinutes(-5),
            CreatedBy = Guid.NewGuid(),
            UpdatedDate = DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedBy = Guid.NewGuid(),
            ProcessedDate = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(10, request.RaceId);
        Assert.NotEqual(Guid.Empty, request.HorseId);
        Assert.NotNull(request.RaceRunId);
        Assert.NotEqual(Guid.Empty, request.OwnerId);
        Assert.Equal(RaceRequestStatus.Completed, request.Status);
        Assert.Null(request.FailureReason);
        Assert.NotNull(request.UpdatedDate);
        Assert.NotNull(request.UpdatedBy);
        Assert.NotNull(request.ProcessedDate);
    }
}
