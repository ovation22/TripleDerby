using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Services;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Tests.Unit.Services;

/// <summary>
/// Tests for FeedingService replay functionality (Phase 1)
/// </summary>
public class FeedingServiceTests
{
    private readonly Mock<ICacheManager> _mockCache;
    private readonly Mock<ITripleDerbyRepository> _mockRepository;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly Mock<ITimeManager> _mockTimeManager;
    private readonly Mock<IRandomGenerator> _mockRandomGenerator;
    private readonly FeedingService _service;

    public FeedingServiceTests()
    {
        _mockCache = new Mock<ICacheManager>();
        _mockRepository = new Mock<ITripleDerbyRepository>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _mockTimeManager = new Mock<ITimeManager>();
        _mockRandomGenerator = new Mock<IRandomGenerator>();

        _service = new FeedingService(
            _mockCache.Object,
            _mockRepository.Object,
            _mockPublisher.Object,
            _mockTimeManager.Object,
            _mockRandomGenerator.Object,
            NullLogger<FeedingService>.Instance);
    }

    #region ReplayFeedingRequest Tests

    [Fact]
    public async Task ReplayFeedingRequest_ValidPendingRequest_RepublishesMessage()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var feedingId = (byte)1;
        var ownerId = Guid.NewGuid();
        var createdDate = DateTimeOffset.UtcNow.AddHours(-1);

        var feedingRequest = new FeedingRequest
        {
            Id = requestId,
            HorseId = horseId,
            FeedingId = feedingId,
            SessionId = Guid.NewGuid(),
            OwnerId = ownerId,
            Status = FeedingRequestStatus.Pending,
            CreatedDate = createdDate
        };

        _mockRepository
            .Setup(r => r.FindAsync<FeedingRequest>(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedingRequest);

        // Act
        var result = await _service.ReplayFeedingRequest(requestId);

        // Assert
        Assert.True(result);
        _mockPublisher.Verify(
            p => p.PublishAsync(
                It.Is<FeedingRequested>(msg =>
                    msg.SessionId == feedingRequest.SessionId &&
                    msg.HorseId == horseId &&
                    msg.FeedingId == feedingId),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplayFeedingRequest_FailedRequest_ResetsStatusAndRepublishes()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var feedingRequest = new FeedingRequest
        {
            Id = requestId,
            HorseId = horseId,
            FeedingId = 2,
            SessionId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Status = FeedingRequestStatus.Failed,
            FailureReason = "Previous failure",
            ProcessedDate = now.AddMinutes(-10),
            CreatedDate = now.AddHours(-2)
        };

        _mockRepository
            .Setup(r => r.FindAsync<FeedingRequest>(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedingRequest);

        _mockTimeManager
            .Setup(t => t.OffsetUtcNow())
            .Returns(now);

        // Act
        var result = await _service.ReplayFeedingRequest(requestId);

        // Assert
        Assert.True(result);

        // Verify status was reset
        Assert.Equal(FeedingRequestStatus.Pending, feedingRequest.Status);
        Assert.Null(feedingRequest.FailureReason);
        Assert.Null(feedingRequest.ProcessedDate);
        Assert.Equal(now, feedingRequest.UpdatedDate);

        _mockRepository.Verify(
            r => r.UpdateAsync(feedingRequest, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPublisher.Verify(
            p => p.PublishAsync(
                It.IsAny<FeedingRequested>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplayFeedingRequest_CompletedRequest_ReturnsFalse()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        var feedingRequest = new FeedingRequest
        {
            Id = requestId,
            HorseId = Guid.NewGuid(),
            FeedingId = 3,
            SessionId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Status = FeedingRequestStatus.Completed,
            FeedingSessionId = Guid.NewGuid(),
            CreatedDate = DateTimeOffset.UtcNow.AddHours(-1),
            ProcessedDate = DateTimeOffset.UtcNow.AddMinutes(-30)
        };

        _mockRepository
            .Setup(r => r.FindAsync<FeedingRequest>(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedingRequest);

        // Act
        var result = await _service.ReplayFeedingRequest(requestId);

        // Assert
        Assert.False(result);
        _mockPublisher.Verify(
            p => p.PublishAsync(It.IsAny<FeedingRequested>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplayFeedingRequest_NotFound_ReturnsFalse()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.FindAsync<FeedingRequest>(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingRequest?)null);

        // Act
        var result = await _service.ReplayFeedingRequest(requestId);

        // Assert
        Assert.False(result);
        _mockPublisher.Verify(
            p => p.PublishAsync(It.IsAny<FeedingRequested>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ReplayAllNonComplete Tests

    [Fact]
    public async Task ReplayAllNonComplete_MultipleRequests_PublishesInParallel()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var requests = new List<FeedingRequest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                HorseId = Guid.NewGuid(),
                FeedingId = 1,
                SessionId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Status = FeedingRequestStatus.Pending,
                CreatedDate = now.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                HorseId = Guid.NewGuid(),
                FeedingId = 2,
                SessionId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Status = FeedingRequestStatus.Failed,
                FailureReason = "Test failure",
                CreatedDate = now.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                HorseId = Guid.NewGuid(),
                FeedingId = 3,
                SessionId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Status = FeedingRequestStatus.InProgress,
                CreatedDate = now.AddMinutes(-30)
            }
        };

        _mockRepository
            .Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<FeedingRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(requests);

        _mockTimeManager
            .Setup(t => t.OffsetUtcNow())
            .Returns(now);

        // Act
        var result = await _service.ReplayAllNonComplete(maxDegreeOfParallelism: 2);

        // Assert
        Assert.Equal(3, result);
        _mockPublisher.Verify(
            p => p.PublishAsync(It.IsAny<FeedingRequested>(), null, It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ReplayAllNonComplete_NoRequests_ReturnsZero()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<FeedingRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedingRequest>());

        // Act
        var result = await _service.ReplayAllNonComplete();

        // Assert
        Assert.Equal(0, result);
        _mockPublisher.Verify(
            p => p.PublishAsync(It.IsAny<FeedingRequested>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
