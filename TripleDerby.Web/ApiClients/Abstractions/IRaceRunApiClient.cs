using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IRaceRunApiClient
{
    Task<RaceRequestStatusResult?> SubmitRaceRunAsync(byte raceId, Guid horseId, CancellationToken cancellationToken = default);
    Task<RaceRequestStatusResult?> GetRequestStatusAsync(byte raceId, Guid requestId, CancellationToken cancellationToken = default);
    Task<RaceRunResult?> GetRaceRunResultAsync(byte raceId, Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated, sortable list of race runs for a specific race.
    /// </summary>
    /// <param name="raceId">The race identifier.</param>
    /// <param name="request">Pagination request with page, size, and sort parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of race run summaries, or null if the request fails.</returns>
    Task<PagedList<RaceRunSummary>?> FilterAsync(byte raceId, PaginationRequest request, CancellationToken cancellationToken = default);
}
