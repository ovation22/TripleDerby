using TripleDerby.Services.Feeding.DTOs;

namespace TripleDerby.Services.Feeding.Abstractions;

/// <summary>
/// Interface for feeding execution logic.
/// </summary>
public interface IFeedingExecutor
{
    /// <summary>
    /// Executes a feeding session for a horse.
    /// </summary>
    Task<FeedingSessionResult> ExecuteFeedingAsync(
        Guid horseId,
        byte feedingId,
        CancellationToken cancellationToken = default);
}
