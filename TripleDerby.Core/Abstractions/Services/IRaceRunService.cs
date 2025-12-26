using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Abstractions.Services;

/// <summary>
/// Service for querying race run results and history.
/// </summary>
public interface IRaceRunService
{
    /// <summary>
    /// Gets detailed results for a specific race run including full play-by-play.
    /// </summary>
    /// <param name="raceId">The race identifier</param>
    /// <param name="raceRunId">The race run identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed race run results, or null if not found</returns>
    Task<RaceRunResult?> GetRaceRunDetails(byte raceId, Guid raceRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of race runs for a specific race.
    /// </summary>
    /// <param name="raceId">The race identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated race run summaries, or null if race not found</returns>
    Task<PagedResult<RaceRunSummary>?> GetRaceRuns(byte raceId, int page, int pageSize, CancellationToken cancellationToken = default);
}
