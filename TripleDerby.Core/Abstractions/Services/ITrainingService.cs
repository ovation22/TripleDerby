using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Abstractions.Services;

/// <summary>
/// Core service for horse training orchestration.
/// Follows async request pattern like BreedingService.
/// </summary>
public interface ITrainingService
{
    /// <summary>
    /// Gets a specific training type by ID.
    /// </summary>
    Task<TrainingResult> Get(byte id);

    /// <summary>
    /// Gets all available training types.
    /// </summary>
    Task<IEnumerable<TrainingsResult>> GetAll();

    /// <summary>
    /// Queues a training session for async processing.
    /// Creates TrainingRequest entity and publishes TrainingRequested message.
    /// </summary>
    /// <param name="horseId">Horse to train</param>
    /// <param name="trainingId">Training type to perform</param>
    /// <param name="sessionId">Session ID for idempotency</param>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Published TrainingRequested message</returns>
    Task<TrainingRequested> QueueTrainingAsync(
        Guid horseId,
        byte trainingId,
        Guid sessionId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a training request.
    /// </summary>
    Task<TrainingRequestStatusResult> GetRequestStatus(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-publishes a failed training request.
    /// </summary>
    Task ReplayTrainingRequest(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays all non-complete training requests.
    /// </summary>
    Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available training options for a horse (3 random options, cached by sessionId).
    /// </summary>
    Task<List<TrainingOptionResult>> GetTrainingOptions(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated training history for a horse with optional filtering and sorting.
    /// </summary>
    Task<PagedList<TrainingHistoryResult>> GetTrainingHistory(Guid horseId, PaginationRequest request, CancellationToken cancellationToken = default);
}
