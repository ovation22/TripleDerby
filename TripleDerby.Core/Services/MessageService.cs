using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

/// <summary>
/// Service for querying and managing async request messages across all services.
/// Provides unified view of Breeding, Feeding, Racing, and Training requests.
/// </summary>
public class MessageService(
    ITripleDerbyRepository repository,
    ILogger<MessageService> logger) : IMessageService
{
    public async Task<MessageRequestsSummaryResult> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching message request summary across all services");

        // Query counts for each service and status
        var breedingSummary = await GetBreedingSummaryAsync(cancellationToken);
        var feedingSummary = await GetFeedingSummaryAsync(cancellationToken);
        var racingSummary = await GetRacingSummaryAsync(cancellationToken);
        var trainingSummary = await GetTrainingSummaryAsync(cancellationToken);

        var result = new MessageRequestsSummaryResult
        {
            Breeding = breedingSummary,
            Feeding = feedingSummary,
            Racing = racingSummary,
            Training = trainingSummary,
            TotalPending = breedingSummary.Pending + feedingSummary.Pending + racingSummary.Pending + trainingSummary.Pending,
            TotalFailed = breedingSummary.Failed + feedingSummary.Failed + racingSummary.Failed + trainingSummary.Failed
        };

        logger.LogInformation("Message summary: TotalPending={Pending}, TotalFailed={Failed}",
            result.TotalPending, result.TotalFailed);

        return result;
    }

    public async Task<PagedList<MessageRequestSummary>> GetAllRequestsAsync(
        PaginationRequest pagination,
        RequestStatus? statusFilter = null,
        RequestServiceType? serviceTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Fetching all message requests: Page={Page}, Size={Size}, Status={Status}, ServiceType={ServiceType}",
            pagination.Page, pagination.Size, statusFilter, serviceTypeFilter);

        var allRequests = new List<MessageRequestSummary>();

        // Fetch from each service unless filtered by service type
        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Breeding)
        {
            var breedingRequests = await GetBreedingRequestsAsync(statusFilter, cancellationToken);
            allRequests.AddRange(breedingRequests);
        }

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Feeding)
        {
            var feedingRequests = await GetFeedingRequestsAsync(statusFilter, cancellationToken);
            allRequests.AddRange(feedingRequests);
        }

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Racing)
        {
            var raceRequests = await GetRaceRequestsAsync(statusFilter, cancellationToken);
            allRequests.AddRange(raceRequests);
        }

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Training)
        {
            var trainingRequests = await GetTrainingRequestsAsync(statusFilter, cancellationToken);
            allRequests.AddRange(trainingRequests);
        }

        // Order by CreatedDate descending
        var orderedRequests = allRequests.OrderByDescending(r => r.CreatedDate).ToList();

        // Apply pagination
        var totalCount = orderedRequests.Count;
        var skip = (pagination.Page - 1) * pagination.Size;
        var pagedItems = orderedRequests.Skip(skip).Take(pagination.Size).ToList();

        logger.LogInformation("Returning {Count} of {Total} message requests", pagedItems.Count, totalCount);

        return new PagedList<MessageRequestSummary>(pagedItems, totalCount, pagination.Page, pagination.Size);
    }

    private async Task<ServiceSummary> GetBreedingSummaryAsync(CancellationToken cancellationToken)
    {
        var pending = await repository.CountAsync<BreedingRequest>(r => r.Status == BreedingRequestStatus.Pending, cancellationToken);
        var inProgress = await repository.CountAsync<BreedingRequest>(r => r.Status == BreedingRequestStatus.InProgress, cancellationToken);
        var completed = await repository.CountAsync<BreedingRequest>(r => r.Status == BreedingRequestStatus.Completed, cancellationToken);
        var failed = await repository.CountAsync<BreedingRequest>(r => r.Status == BreedingRequestStatus.Failed, cancellationToken);

        return new ServiceSummary
        {
            Pending = pending,
            InProgress = inProgress,
            Completed = completed,
            Failed = failed
        };
    }

    private async Task<ServiceSummary> GetFeedingSummaryAsync(CancellationToken cancellationToken)
    {
        var pending = await repository.CountAsync<FeedingRequest>(r => r.Status == FeedingRequestStatus.Pending, cancellationToken);
        var inProgress = await repository.CountAsync<FeedingRequest>(r => r.Status == FeedingRequestStatus.InProgress, cancellationToken);
        var completed = await repository.CountAsync<FeedingRequest>(r => r.Status == FeedingRequestStatus.Completed, cancellationToken);
        var failed = await repository.CountAsync<FeedingRequest>(r => r.Status == FeedingRequestStatus.Failed, cancellationToken);

        return new ServiceSummary
        {
            Pending = pending,
            InProgress = inProgress,
            Completed = completed,
            Failed = failed
        };
    }

    private async Task<ServiceSummary> GetRacingSummaryAsync(CancellationToken cancellationToken)
    {
        var pending = await repository.CountAsync<RaceRequest>(r => r.Status == RaceRequestStatus.Pending, cancellationToken);
        var inProgress = await repository.CountAsync<RaceRequest>(r => r.Status == RaceRequestStatus.InProgress, cancellationToken);
        var completed = await repository.CountAsync<RaceRequest>(r => r.Status == RaceRequestStatus.Completed, cancellationToken);
        var failed = await repository.CountAsync<RaceRequest>(r => r.Status == RaceRequestStatus.Failed, cancellationToken);

        return new ServiceSummary
        {
            Pending = pending,
            InProgress = inProgress,
            Completed = completed,
            Failed = failed
        };
    }

    private async Task<ServiceSummary> GetTrainingSummaryAsync(CancellationToken cancellationToken)
    {
        var pending = await repository.CountAsync<TrainingRequest>(r => r.Status == TrainingRequestStatus.Pending, cancellationToken);
        var inProgress = await repository.CountAsync<TrainingRequest>(r => r.Status == TrainingRequestStatus.InProgress, cancellationToken);
        var completed = await repository.CountAsync<TrainingRequest>(r => r.Status == TrainingRequestStatus.Completed, cancellationToken);
        var failed = await repository.CountAsync<TrainingRequest>(r => r.Status == TrainingRequestStatus.Failed, cancellationToken);

        return new ServiceSummary
        {
            Pending = pending,
            InProgress = inProgress,
            Completed = completed,
            Failed = failed
        };
    }

    private async Task<List<MessageRequestSummary>> GetBreedingRequestsAsync(
        RequestStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        List<BreedingRequest> requests;

        if (statusFilter.HasValue)
        {
            var status = (BreedingRequestStatus)(byte)statusFilter.Value;
            requests = await repository.ListAsync<BreedingRequest>(r => r.Status == status, cancellationToken);
        }
        else
        {
            requests = await repository.ListAsync<BreedingRequest>(r => true, cancellationToken);
        }

        return requests.Select(r => new MessageRequestSummary
        {
            Id = r.Id,
            ServiceType = RequestServiceType.Breeding,
            Status = (RequestStatus)(byte)r.Status,
            CreatedDate = r.CreatedDate,
            ProcessedDate = r.ProcessedDate,
            FailureReason = r.FailureReason,
            TargetDescription = $"Breeding Request",
            OwnerId = r.OwnerId,
            ResultId = r.FoalId
        }).ToList();
    }

    private async Task<List<MessageRequestSummary>> GetFeedingRequestsAsync(
        RequestStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        List<FeedingRequest> requests;

        if (statusFilter.HasValue)
        {
            var status = (FeedingRequestStatus)(byte)statusFilter.Value;
            requests = await repository.ListAsync<FeedingRequest>(r => r.Status == status, cancellationToken);
        }
        else
        {
            requests = await repository.ListAsync<FeedingRequest>(r => true, cancellationToken);
        }

        return requests.Select(r => new MessageRequestSummary
        {
            Id = r.Id,
            ServiceType = RequestServiceType.Feeding,
            Status = (RequestStatus)(byte)r.Status,
            CreatedDate = r.CreatedDate,
            ProcessedDate = r.ProcessedDate,
            FailureReason = r.FailureReason,
            TargetDescription = $"Feeding Request",
            OwnerId = r.OwnerId,
            ResultId = r.FeedingSessionId
        }).ToList();
    }

    private async Task<List<MessageRequestSummary>> GetRaceRequestsAsync(
        RequestStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        List<RaceRequest> requests;

        if (statusFilter.HasValue)
        {
            var status = (RaceRequestStatus)(byte)statusFilter.Value;
            requests = await repository.ListAsync<RaceRequest>(r => r.Status == status, cancellationToken);
        }
        else
        {
            requests = await repository.ListAsync<RaceRequest>(r => true, cancellationToken);
        }

        return requests.Select(r => new MessageRequestSummary
        {
            Id = r.Id,
            ServiceType = RequestServiceType.Racing,
            Status = (RequestStatus)(byte)r.Status,
            CreatedDate = r.CreatedDate,
            ProcessedDate = r.ProcessedDate,
            FailureReason = r.FailureReason,
            TargetDescription = $"Race Request",
            OwnerId = r.OwnerId,
            ResultId = r.RaceRunId
        }).ToList();
    }

    private async Task<List<MessageRequestSummary>> GetTrainingRequestsAsync(
        RequestStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        List<TrainingRequest> requests;

        if (statusFilter.HasValue)
        {
            var status = (TrainingRequestStatus)(byte)statusFilter.Value;
            requests = await repository.ListAsync<TrainingRequest>(r => r.Status == status, cancellationToken);
        }
        else
        {
            requests = await repository.ListAsync<TrainingRequest>(r => true, cancellationToken);
        }

        return requests.Select(r => new MessageRequestSummary
        {
            Id = r.Id,
            ServiceType = RequestServiceType.Training,
            Status = (RequestStatus)(byte)r.Status,
            CreatedDate = r.CreatedDate,
            ProcessedDate = r.ProcessedDate,
            FailureReason = r.FailureReason,
            TargetDescription = $"Training Request",
            OwnerId = r.OwnerId
        }).ToList();
    }
}
