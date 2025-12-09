using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Core.Abstractions.Generators;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Breeding;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Tests.Unit.Breeding;

public class BreedingRequestProcessorTests
{
    private readonly DateTimeOffset _requestedDate = new();

    [Fact]
    public async Task ProcessAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<BreedingRequestProcessor>>();
        var rnd = new Mock<IRandomGenerator>();
        var repo = new Mock<ITripleDerbyRepository>();
        var publisher = new Mock<IMessagePublisher>();
        var nameGen = new Mock<IHorseNameGenerator>();
        var timeMgr = new Mock<ITimeManager>();

        var processor = new BreedingRequestProcessor(
            logger.Object,
            rnd.Object,
            repo.Object,
            publisher.Object,
            nameGen.Object,
            timeMgr.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => processor.ProcessAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_StoredRequestNotFound_LogsWarning_AndReturns()
    {
        // Arrange
        var logger = new Mock<ILogger<BreedingRequestProcessor>>();
        var rnd = new Mock<IRandomGenerator>();
        var repo = new Mock<ITripleDerbyRepository>();
        var publisher = new Mock<IMessagePublisher>();
        var nameGen = new Mock<IHorseNameGenerator>();
        var timeMgr = new Mock<ITimeManager>();

        var requestId = Guid.NewGuid();
        var request = new BreedingRequested(requestId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _requestedDate);

        repo.Setup(r => r.FindAsync<BreedingRequest>(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BreedingRequest?)null);

        var processor = new BreedingRequestProcessor(
            logger.Object,
            rnd.Object,
            repo.Object,
            publisher.Object,
            nameGen.Object,
            timeMgr.Object);

        // Act
        await processor.ProcessAsync(request, CancellationToken.None);

        // Assert - no publish, no further repository updates
        repo.Verify(r => r.UpdateAsync(It.IsAny<BreedingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(p => p.PublishAsync(It.IsAny<BreedingCompleted>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    
    [Fact]
    public async Task ProcessAsync_StoredRequestAlreadyCompleted_LogsAndReturns()
    {
        // Arrange
        var logger = new Mock<ILogger<BreedingRequestProcessor>>();
        var rnd = new Mock<IRandomGenerator>();
        var repo = new Mock<ITripleDerbyRepository>();
        var publisher = new Mock<IMessagePublisher>();
        var nameGen = new Mock<IHorseNameGenerator>();
        var timeMgr = new Mock<ITimeManager>();

        var requestId = Guid.NewGuid();
        var request = new BreedingRequested(requestId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _requestedDate);

        var stored = new BreedingRequest
        {
            Id = requestId,
            Status = BreedingRequestStatus.Completed
        };

        repo.Setup(r => r.FindAsync<BreedingRequest>(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var processor = new BreedingRequestProcessor(
            logger.Object,
            rnd.Object,
            repo.Object,
            publisher.Object,
            nameGen.Object,
            timeMgr.Object);

        // Act
        await processor.ProcessAsync(request, CancellationToken.None);

        // Assert - we shouldn't attempt to claim or publish
        repo.Verify(r => r.UpdateAsync(It.IsAny<BreedingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(p => p.PublishAsync(It.IsAny<BreedingCompleted>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ClaimingThrows_ReloadShowsNotInProgress_LogsAndReturns()
    {
        // Arrange
        var logger = new Mock<ILogger<BreedingRequestProcessor>>();
        var rnd = new Mock<IRandomGenerator>();
        var repo = new Mock<ITripleDerbyRepository>();
        var publisher = new Mock<IMessagePublisher>();
        var nameGen = new Mock<IHorseNameGenerator>();
        var timeMgr = new Mock<ITimeManager>();

        var requestId = Guid.NewGuid();
        var request = new BreedingRequested(requestId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _requestedDate);

        var storedInitial = new BreedingRequest
        {
            Id = requestId,
            Status = BreedingRequestStatus.Pending
        };

        var storedAfterReload = new BreedingRequest
        {
            Id = requestId,
            Status = BreedingRequestStatus.Failed // anything other than InProgress should cause an informational skip
        };

        repo.SetupSequence(r => r.FindAsync<BreedingRequest>(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedInitial)
            .ReturnsAsync(storedAfterReload);

        repo.Setup(r => r.UpdateAsync(It.Is<BreedingRequest>(b => b.Status == BreedingRequestStatus.InProgress), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("concurrency"));

        var processor = new BreedingRequestProcessor(
            logger.Object,
            rnd.Object,
            repo.Object,
            publisher.Object,
            nameGen.Object,
            timeMgr.Object);

        // Act
        await processor.ProcessAsync(request, CancellationToken.None);

        // Assert - after failing to claim and reloading to a non-InProgress status, processor should return
        // Verify UpdateAsync was attempted (the claim)
        repo.Verify(r => r.UpdateAsync(It.IsAny<BreedingRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        // No publish should have occurred
        publisher.Verify(p => p.PublishAsync(It.IsAny<BreedingCompleted>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}