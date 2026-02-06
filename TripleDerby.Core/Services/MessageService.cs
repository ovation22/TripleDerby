using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
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

        // First, get total counts for each service (lightweight queries)
        var counts = new List<int>();

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Breeding)
            counts.Add(await GetBreedingCountAsync(statusFilter, cancellationToken));

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Feeding)
            counts.Add(await GetFeedingCountAsync(statusFilter, cancellationToken));

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Racing)
            counts.Add(await GetRaceCountAsync(statusFilter, cancellationToken));

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Training)
            counts.Add(await GetTrainingCountAsync(statusFilter, cancellationToken));

        var totalCount = counts.Sum();

        // Calculate skip based on pagination
        var skip = (pagination.Page - 1) * pagination.Size;
        var take = pagination.Size;

        // Calculate buffer size: need to fetch enough records to cover skip + take
        // Use 2x multiplier for safety since records are distributed across tables
        var bufferSize = (skip + take) * 2;

        // Fetch data from each service with appropriate buffer
        var allRequests = new List<MessageRequestSummary>();

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Breeding)
        {
            var breedingRequests = await GetBreedingRequestsPagedAsync(statusFilter, bufferSize, cancellationToken);
            allRequests.AddRange(breedingRequests);
        }

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Feeding)
        {
            var feedingRequests = await GetFeedingRequestsPagedAsync(statusFilter, bufferSize, cancellationToken);
            allRequests.AddRange(feedingRequests);
        }

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Racing)
        {
            var raceRequests = await GetRaceRequestsPagedAsync(statusFilter, bufferSize, cancellationToken);
            allRequests.AddRange(raceRequests);
        }

        if (!serviceTypeFilter.HasValue || serviceTypeFilter == RequestServiceType.Training)
        {
            var trainingRequests = await GetTrainingRequestsPagedAsync(statusFilter, bufferSize, cancellationToken);
            allRequests.AddRange(trainingRequests);
        }

        // Order by CreatedDate descending, skip to the correct page, and take only what we need
        var pagedItems = allRequests
            .OrderByDescending(r => r.CreatedDate)
            .Skip(skip)
            .Take(take)
            .ToList();

        var result = new PagedList<MessageRequestSummary>(pagedItems, totalCount, pagination.Page, pagination.Size);

        return result;
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

    // Optimized count methods for pagination
    private async Task<int> GetBreedingCountAsync(RequestStatus? statusFilter, CancellationToken cancellationToken)
    {
        if (statusFilter.HasValue)
        {
            var status = (BreedingRequestStatus)(byte)statusFilter.Value;
            return await repository.CountAsync<BreedingRequest>(r => r.Status == status, cancellationToken);
        }
        return await repository.CountAsync<BreedingRequest>(cancellationToken);
    }

    private async Task<int> GetFeedingCountAsync(RequestStatus? statusFilter, CancellationToken cancellationToken)
    {
        if (statusFilter.HasValue)
        {
            var status = (FeedingRequestStatus)(byte)statusFilter.Value;
            return await repository.CountAsync<FeedingRequest>(r => r.Status == status, cancellationToken);
        }
        return await repository.CountAsync<FeedingRequest>(cancellationToken);
    }

    private async Task<int> GetRaceCountAsync(RequestStatus? statusFilter, CancellationToken cancellationToken)
    {
        if (statusFilter.HasValue)
        {
            var status = (RaceRequestStatus)(byte)statusFilter.Value;
            return await repository.CountAsync<RaceRequest>(r => r.Status == status, cancellationToken);
        }
        return await repository.CountAsync<RaceRequest>(cancellationToken);
    }

    private async Task<int> GetTrainingCountAsync(RequestStatus? statusFilter, CancellationToken cancellationToken)
    {
        if (statusFilter.HasValue)
        {
            var status = (TrainingRequestStatus)(byte)statusFilter.Value;
            return await repository.CountAsync<TrainingRequest>(r => r.Status == status, cancellationToken);
        }
        return await repository.CountAsync<TrainingRequest>(cancellationToken);
    }

    // Optimized paged retrieval methods - fetch limited records from DB
    // Buffer size is calculated in GetAllRequestsAsync based on page depth
    private async Task<List<MessageRequestSummary>> GetBreedingRequestsPagedAsync(
        RequestStatus? statusFilter,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        var spec = new MessageRequestSpec<BreedingRequest, BreedingRequestStatus>(
            bufferSize,
            statusFilter.HasValue ? (BreedingRequestStatus)(byte)statusFilter.Value : null);

        var requests = await repository.ListAsync(spec, cancellationToken);

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

    private async Task<List<MessageRequestSummary>> GetFeedingRequestsPagedAsync(
        RequestStatus? statusFilter,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        var spec = new MessageRequestSpec<FeedingRequest, FeedingRequestStatus>(
            bufferSize,
            statusFilter.HasValue ? (FeedingRequestStatus)(byte)statusFilter.Value : null);

        var requests = await repository.ListAsync(spec, cancellationToken);

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

    private async Task<List<MessageRequestSummary>> GetRaceRequestsPagedAsync(
        RequestStatus? statusFilter,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        var spec = new MessageRequestSpec<RaceRequest, RaceRequestStatus>(
            bufferSize,
            statusFilter.HasValue ? (RaceRequestStatus)(byte)statusFilter.Value : null);

        var requests = await repository.ListAsync(spec, cancellationToken);

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

    private async Task<List<MessageRequestSummary>> GetTrainingRequestsPagedAsync(
        RequestStatus? statusFilter,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        var spec = new MessageRequestSpec<TrainingRequest, TrainingRequestStatus>(
            bufferSize,
            statusFilter.HasValue ? (TrainingRequestStatus)(byte)statusFilter.Value : null);

        var requests = await repository.ListAsync(spec, cancellationToken);

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

    // Simple specification for message requests with optional status filter and Take limit
    private class MessageRequestSpec<TEntity, TStatus> : Specification<TEntity>
        where TEntity : class
        where TStatus : struct, Enum
    {
        public MessageRequestSpec(int take, TStatus? statusFilter)
        {
            // Apply status filter if provided
            if (statusFilter.HasValue)
            {
                Query.Where(BuildStatusExpression(statusFilter.Value));
            }

            // Order by CreatedDate descending and take only what we need
            var orderParam = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "r");
            var createdProp = System.Linq.Expressions.Expression.Property(orderParam, "CreatedDate");
            var orderLambda = System.Linq.Expressions.Expression.Lambda<Func<TEntity, object>>(
                System.Linq.Expressions.Expression.Convert(createdProp, typeof(object)),
                orderParam);

            Query.OrderByDescending(orderLambda);
            Query.Take(take);
        }

        private static System.Linq.Expressions.Expression<Func<TEntity, bool>> BuildStatusExpression(TStatus status)
        {
            var param = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "r");
            var statusProp = System.Linq.Expressions.Expression.Property(param, "Status");
            var statusValue = System.Linq.Expressions.Expression.Constant(status);
            var equals = System.Linq.Expressions.Expression.Equal(statusProp, statusValue);
            return System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(equals, param);
        }
    }
}
