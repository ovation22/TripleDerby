using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;
using TripleDerby.Web.ApiClients.Extensions;

namespace TripleDerby.Web.ApiClients;

public class RaceRunApiClient(HttpClient httpClient, ILogger<RaceRunApiClient> logger)
    : BaseApiClient(httpClient, logger), IRaceRunApiClient
{
    /// <summary>
    /// Submits a race run request for a specific race and horse.
    /// </summary>
    public async Task<RaceRequestStatusResult?> SubmitRaceRunAsync(byte raceId, Guid horseId, CancellationToken cancellationToken = default)
    {
        var url = $"/api/races/{raceId}/runs?horseId={horseId}";
        var resp = await PostAsync<Resource<RaceRequestStatusResult>>(url, cancellationToken);

        if (resp.Success && resp.Data != null)
            return resp.Data.Data;

        Logger.LogError("Unable to submit race run. RaceId: {RaceId}, HorseId: {HorseId}, Status: {Status} Error: {Error}",
            raceId, horseId, resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Gets the status of a race request.
    /// </summary>
    public async Task<RaceRequestStatusResult?> GetRequestStatusAsync(byte raceId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var url = $"/api/races/{raceId}/runs/requests/{requestId}";
        var resp = await GetAsync<Resource<RaceRequestStatusResult>>(url, cancellationToken);

        if (resp.Success && resp.Data != null)
            return resp.Data.Data;

        Logger.LogError("Unable to get race request status. RaceId: {RaceId}, RequestId: {RequestId}, Status: {Status} Error: {Error}",
            raceId, requestId, resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Gets the full race run result including play-by-play commentary.
    /// </summary>
    public async Task<RaceRunResult?> GetRaceRunResultAsync(byte raceId, Guid runId, CancellationToken cancellationToken = default)
    {
        var url = $"/api/races/{raceId}/runs/{runId}";
        var resp = await GetAsync<RaceRunResult>(url, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get race run result. RaceId: {RaceId}, RunId: {RunId}, Status: {Status} Error: {Error}",
            raceId, runId, resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Gets a paginated, sortable list of race runs for a specific race.
    /// </summary>
    public async Task<PagedList<RaceRunSummary>?> FilterAsync(byte raceId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var url = request.ToQueryString($"/api/races/{raceId}/runs");
        var resp = await GetAsync<PagedList<RaceRunSummary>>(url, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get race runs. RaceId: {RaceId}, Page: {Page}, Size: {Size}, Status: {Status} Error: {Error}",
            raceId, request.Page, request.Size, resp.StatusCode, resp.Error);
        return null;
    }
}
