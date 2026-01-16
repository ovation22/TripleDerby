using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Abstractions.Services;

public interface IFeedingService
{
    Task<FeedingResult> Get(byte id);
    Task<IEnumerable<FeedingsResult>> GetAll();

    /// <summary>
    /// Queues a feeding session for async processing.
    /// </summary>
    Task<FeedingRequested> QueueFeedingAsync(
        Guid horseId,
        byte feedingId,
        Guid sessionId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a feeding request.
    /// </summary>
    Task<FeedingRequestStatusResult> GetRequestStatus(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-publishes a failed feeding request.
    /// </summary>
    Task ReplayFeedingRequest(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available feeding options for a horse.
    /// </summary>
    Task<List<FeedingOptionResult>> GetFeedingOptions(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated feeding history for a horse with optional filtering and sorting.
    /// </summary>
    Task<PagedList<FeedingHistoryResult>> GetFeedingHistory(Guid horseId, PaginationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the details of a completed feeding session by ID.
    /// </summary>
    Task<FeedingSessionResult> GetFeedingSessionResult(Guid feedingSessionId, CancellationToken cancellationToken = default);
}
