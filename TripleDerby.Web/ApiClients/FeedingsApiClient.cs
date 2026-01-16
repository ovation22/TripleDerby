using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;

namespace TripleDerby.Web.ApiClients;

/// <summary>
/// API client for feeding catalog and operations.
/// </summary>
public class FeedingsApiClient(HttpClient httpClient, ILogger<FeedingsApiClient> logger)
    : BaseApiClient(httpClient, logger), IFeedingsApiClient
{
    public async Task<List<FeedingsResult>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<List<FeedingsResult>>("/api/feedings", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get feedings. Status: {Status} Error: {Error}", resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<FeedingResult?> GetByIdAsync(byte feedingId, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<FeedingResult>($"/api/feedings/{feedingId}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get feeding {Id}. Status: {Status} Error: {Error}", feedingId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<FeedHorseResponse?> CreateRequestAsync(Guid horseId, byte feedingId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var request = new { HorseId = horseId, FeedingId = feedingId, SessionId = sessionId };
        var resp = await PostAsync<object, FeedHorseResponse>("/api/feedings/queue", request, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to create feeding request for horse {HorseId}. Status: {Status} Error: {Error}", horseId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<FeedingRequestStatusResult?> GetRequestStatusAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<FeedingRequestStatusResult>($"/api/feedings/request/{sessionId}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get feeding request status {SessionId}. Status: {Status} Error: {Error}", sessionId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<bool> ReplayRequestAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var resp = await PostAsync<object, object>($"/api/feedings/request/{sessionId}/replay", null!, cancellationToken);

        if (resp.Success)
            return true;

        Logger.LogError("Unable to replay feeding request {SessionId}. Status: {Status} Error: {Error}", sessionId, resp.StatusCode, resp.Error);
        return false;
    }

    public async Task<List<FeedingOptionResult>?> GetFeedingOptionsAsync(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<List<FeedingOptionResult>>($"/api/feedings/options?horseId={horseId}&sessionId={sessionId}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get feeding options for horse {HorseId}. Status: {Status} Error: {Error}", horseId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<PagedList<FeedingHistoryResult>?> GetFeedingHistoryAsync(Guid horseId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var queryString = $"horseId={horseId}&Page={request.Page}&Size={request.Size}";

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            queryString += $"&SortBy={request.SortBy}&Direction={request.Direction}";
        }

        var resp = await SearchAsync<PagedList<FeedingHistoryResult>>($"/api/feedings/history?{queryString}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get feeding history for horse {HorseId}. Status: {Status} Error: {Error}", horseId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<FeedingSessionResult?> GetFeedingSessionResultAsync(Guid feedingSessionId, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<FeedingSessionResult>($"/api/feedings/session/{feedingSessionId}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get feeding session result {FeedingSessionId}. Status: {Status} Error: {Error}", feedingSessionId, resp.StatusCode, resp.Error);
        return null;
    }
}
