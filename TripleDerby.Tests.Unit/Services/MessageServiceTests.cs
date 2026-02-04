using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Services;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Tests.Unit.Services;

public class MessageServiceTests
{
    private readonly Mock<ITripleDerbyRepository> _mockRepository;
    private readonly Mock<ILogger<MessageService>> _mockLogger;
    private readonly MessageService _service;

    public MessageServiceTests()
    {
        _mockRepository = new Mock<ITripleDerbyRepository>();
        _mockLogger = new Mock<ILogger<MessageService>>();
        _service = new MessageService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectCounts()
    {
        // Arrange - Mock repository counts for each service
        // Breeding: 5 Pending, 2 Failed, 10 Completed
        _mockRepository
            .Setup(r => r.CountAsync<BreedingRequest>(br => br.Status == BreedingRequestStatus.Pending, default))
            .ReturnsAsync(5);
        _mockRepository
            .Setup(r => r.CountAsync<BreedingRequest>(br => br.Status == BreedingRequestStatus.Failed, default))
            .ReturnsAsync(2);
        _mockRepository
            .Setup(r => r.CountAsync<BreedingRequest>(br => br.Status == BreedingRequestStatus.Completed, default))
            .ReturnsAsync(10);
        _mockRepository
            .Setup(r => r.CountAsync<BreedingRequest>(br => br.Status == BreedingRequestStatus.InProgress, default))
            .ReturnsAsync(0);

        // Feeding: 3 Pending, 1 Failed, 8 Completed
        _mockRepository
            .Setup(r => r.CountAsync<FeedingRequest>(fr => fr.Status == FeedingRequestStatus.Pending, default))
            .ReturnsAsync(3);
        _mockRepository
            .Setup(r => r.CountAsync<FeedingRequest>(fr => fr.Status == FeedingRequestStatus.Failed, default))
            .ReturnsAsync(1);
        _mockRepository
            .Setup(r => r.CountAsync<FeedingRequest>(fr => fr.Status == FeedingRequestStatus.Completed, default))
            .ReturnsAsync(8);
        _mockRepository
            .Setup(r => r.CountAsync<FeedingRequest>(fr => fr.Status == FeedingRequestStatus.InProgress, default))
            .ReturnsAsync(0);

        // Racing: 7 Pending, 0 Failed, 15 Completed
        _mockRepository
            .Setup(r => r.CountAsync<RaceRequest>(rr => rr.Status == RaceRequestStatus.Pending, default))
            .ReturnsAsync(7);
        _mockRepository
            .Setup(r => r.CountAsync<RaceRequest>(rr => rr.Status == RaceRequestStatus.Failed, default))
            .ReturnsAsync(0);
        _mockRepository
            .Setup(r => r.CountAsync<RaceRequest>(rr => rr.Status == RaceRequestStatus.Completed, default))
            .ReturnsAsync(15);
        _mockRepository
            .Setup(r => r.CountAsync<RaceRequest>(rr => rr.Status == RaceRequestStatus.InProgress, default))
            .ReturnsAsync(0);

        // Training: 2 Pending, 3 Failed, 6 Completed
        _mockRepository
            .Setup(r => r.CountAsync<TrainingRequest>(tr => tr.Status == TrainingRequestStatus.Pending, default))
            .ReturnsAsync(2);
        _mockRepository
            .Setup(r => r.CountAsync<TrainingRequest>(tr => tr.Status == TrainingRequestStatus.Failed, default))
            .ReturnsAsync(3);
        _mockRepository
            .Setup(r => r.CountAsync<TrainingRequest>(tr => tr.Status == TrainingRequestStatus.Completed, default))
            .ReturnsAsync(6);
        _mockRepository
            .Setup(r => r.CountAsync<TrainingRequest>(tr => tr.Status == TrainingRequestStatus.InProgress, default))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        Assert.Equal(5, result.Breeding.Pending);
        Assert.Equal(2, result.Breeding.Failed);
        Assert.Equal(10, result.Breeding.Completed);

        Assert.Equal(3, result.Feeding.Pending);
        Assert.Equal(1, result.Feeding.Failed);
        Assert.Equal(8, result.Feeding.Completed);

        Assert.Equal(7, result.Racing.Pending);
        Assert.Equal(0, result.Racing.Failed);
        Assert.Equal(15, result.Racing.Completed);

        Assert.Equal(2, result.Training.Pending);
        Assert.Equal(3, result.Training.Failed);
        Assert.Equal(6, result.Training.Completed);

        Assert.Equal(17, result.TotalPending); // 5+3+7+2
        Assert.Equal(6, result.TotalFailed);   // 2+1+0+3
    }

    [Fact]
    public async Task GetAllRequestsAsync_NoFilters_ReturnsAllServices()
    {
        // Arrange
        var breedingRequests = new List<BreedingRequest>
        {
            new() { Id = Guid.NewGuid(), Status = BreedingRequestStatus.Pending, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Status = BreedingRequestStatus.Failed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid() }
        };

        var feedingRequests = new List<FeedingRequest>
        {
            new() { Id = Guid.NewGuid(), Status = FeedingRequestStatus.Pending, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), FeedingId = 1, SessionId = Guid.NewGuid(), CreatedBy = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Status = FeedingRequestStatus.Completed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), FeedingId = 1, SessionId = Guid.NewGuid(), CreatedBy = Guid.NewGuid() }
        };

        var raceRequests = new List<RaceRequest>
        {
            new() { Id = Guid.NewGuid(), Status = RaceRequestStatus.Pending, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), RaceId = 1, CreatedBy = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Status = RaceRequestStatus.Failed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), RaceId = 1, CreatedBy = Guid.NewGuid() }
        };

        var trainingRequests = new List<TrainingRequest>
        {
            new() { Id = Guid.NewGuid(), Status = TrainingRequestStatus.Pending, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), TrainingId = 1, SessionId = Guid.NewGuid(), CreatedBy = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Status = TrainingRequestStatus.Completed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), TrainingId = 1, SessionId = Guid.NewGuid(), CreatedBy = Guid.NewGuid() }
        };

        _mockRepository
            .Setup(r => r.ListAsync<BreedingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<BreedingRequest, bool>>>(), default))
            .ReturnsAsync(breedingRequests);
        _mockRepository
            .Setup(r => r.ListAsync<FeedingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<FeedingRequest, bool>>>(), default))
            .ReturnsAsync(feedingRequests);
        _mockRepository
            .Setup(r => r.ListAsync<RaceRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<RaceRequest, bool>>>(), default))
            .ReturnsAsync(raceRequests);
        _mockRepository
            .Setup(r => r.ListAsync<TrainingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<TrainingRequest, bool>>>(), default))
            .ReturnsAsync(trainingRequests);

        var pagination = new PaginationRequest { Page = 1, Size = 50 };

        // Act
        var result = await _service.GetAllRequestsAsync(pagination);

        // Assert
        Assert.Equal(8, result.Total); // 2+2+2+2
        Assert.Equal(8, result.Data.Count());
    }

    [Fact]
    public async Task GetAllRequestsAsync_StatusFilter_ReturnsFiltered()
    {
        // Arrange
        var breedingRequests = new List<BreedingRequest>
        {
            new() { Id = Guid.NewGuid(), Status = BreedingRequestStatus.Failed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid() }
        };

        var feedingRequests = new List<FeedingRequest>
        {
            new() { Id = Guid.NewGuid(), Status = FeedingRequestStatus.Failed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), FeedingId = 1, SessionId = Guid.NewGuid(), CreatedBy = Guid.NewGuid() }
        };

        var raceRequests = new List<RaceRequest>();
        var trainingRequests = new List<TrainingRequest>
        {
            new() { Id = Guid.NewGuid(), Status = TrainingRequestStatus.Failed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid(), HorseId = Guid.NewGuid(), TrainingId = 1, SessionId = Guid.NewGuid(), CreatedBy = Guid.NewGuid() }
        };

        _mockRepository
            .Setup(r => r.ListAsync<BreedingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<BreedingRequest, bool>>>(), default))
            .ReturnsAsync(breedingRequests);
        _mockRepository
            .Setup(r => r.ListAsync<FeedingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<FeedingRequest, bool>>>(), default))
            .ReturnsAsync(feedingRequests);
        _mockRepository
            .Setup(r => r.ListAsync<RaceRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<RaceRequest, bool>>>(), default))
            .ReturnsAsync(raceRequests);
        _mockRepository
            .Setup(r => r.ListAsync<TrainingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<TrainingRequest, bool>>>(), default))
            .ReturnsAsync(trainingRequests);

        var pagination = new PaginationRequest { Page = 1, Size = 50 };

        // Act
        var result = await _service.GetAllRequestsAsync(pagination, RequestStatus.Failed);

        // Assert
        Assert.Equal(3, result.Total); // Only Failed requests
        Assert.All(result.Data, item => Assert.Equal(RequestStatus.Failed, item.Status));
    }

    [Fact]
    public async Task GetAllRequestsAsync_ServiceTypeFilter_ReturnsFiltered()
    {
        // Arrange
        var breedingRequests = new List<BreedingRequest>
        {
            new() { Id = Guid.NewGuid(), Status = BreedingRequestStatus.Pending, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Status = BreedingRequestStatus.Failed, CreatedDate = DateTimeOffset.UtcNow, OwnerId = Guid.NewGuid() }
        };

        _mockRepository
            .Setup(r => r.ListAsync<BreedingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<BreedingRequest, bool>>>(), default))
            .ReturnsAsync(breedingRequests);

        // Other services should NOT be called when filtering by Breeding
        _mockRepository
            .Setup(r => r.ListAsync<FeedingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<FeedingRequest, bool>>>(), default))
            .ReturnsAsync(new List<FeedingRequest>());
        _mockRepository
            .Setup(r => r.ListAsync<RaceRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<RaceRequest, bool>>>(), default))
            .ReturnsAsync(new List<RaceRequest>());
        _mockRepository
            .Setup(r => r.ListAsync<TrainingRequest>(It.IsAny<System.Linq.Expressions.Expression<Func<TrainingRequest, bool>>>(), default))
            .ReturnsAsync(new List<TrainingRequest>());

        var pagination = new PaginationRequest { Page = 1, Size = 50 };

        // Act
        var result = await _service.GetAllRequestsAsync(pagination, serviceTypeFilter: RequestServiceType.Breeding);

        // Assert
        Assert.Equal(2, result.Total);
        Assert.All(result.Data, item => Assert.Equal(RequestServiceType.Breeding, item.ServiceType));
    }
}
