using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Services;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Tests.Unit.Services;

/// <summary>
/// Tests for TrainingService replay functionality (Phase 1)
/// </summary>
public class TrainingServiceReplayTests
{
    private readonly Mock<ITripleDerbyRepository> _mockRepository;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly Mock<ITimeManager> _mockTimeManager;
    private readonly TrainingService _service;

    public TrainingServiceReplayTests()
    {
        _mockRepository = new Mock<ITripleDerbyRepository>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _mockTimeManager = new Mock<ITimeManager>();

        _service = new TrainingService(
            _mockRepository.Object,
            _mockPublisher.Object,
            _mockTimeManager.Object,
            NullLogger<TrainingService>.Instance);
    }

    #region ReplayAllNonComplete Tests

    [Fact]
    public async Task ReplayAllNonComplete_MultipleRequests_PublishesInParallel()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var requests = new List<TrainingRequest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                HorseId = Guid.NewGuid(),
                TrainingId = 1,
                SessionId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Status = TrainingRequestStatus.Pending,
                CreatedDate = now.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                HorseId = Guid.NewGuid(),
                TrainingId = 2,
                SessionId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Status = TrainingRequestStatus.Failed,
                FailureReason = "Test failure",
                CreatedDate = now.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                HorseId = Guid.NewGuid(),
                TrainingId = 3,
                SessionId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Status = TrainingRequestStatus.InProgress,
                CreatedDate = now.AddMinutes(-30)
            },
            new()
            {
                Id = Guid.NewGuid(),
                HorseId = Guid.NewGuid(),
                TrainingId = 1,
                SessionId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Status = TrainingRequestStatus.Cancelled,
                CreatedDate = now.AddHours(-3)
            }
        };

        _mockRepository
            .Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TrainingRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(requests);

        _mockTimeManager
            .Setup(t => t.OffsetUtcNow())
            .Returns(now);

        // Act
        var result = await _service.ReplayAllNonComplete(maxDegreeOfParallelism: 2);

        // Assert
        Assert.Equal(4, result);
        _mockPublisher.Verify(
            p => p.PublishAsync(It.IsAny<TrainingRequested>(), null, It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task ReplayAllNonComplete_NoRequests_ReturnsZero()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TrainingRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainingRequest>());

        // Act
        var result = await _service.ReplayAllNonComplete();

        // Assert
        Assert.Equal(0, result);
        _mockPublisher.Verify(
            p => p.PublishAsync(It.IsAny<TrainingRequested>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
