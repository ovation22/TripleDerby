using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;

namespace TripleDerby.Web.ApiClients;

public class MessagesApiClient(HttpClient httpClient, ILogger<MessagesApiClient> logger)
    : BaseApiClient(httpClient, logger), IMessagesApiClient
{
    /// <summary>
    /// Gets aggregated status counts for all message request services.
    /// </summary>
    public async Task<MessageRequestsSummaryResult?> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var resp = await GetAsync<MessageRequestsSummaryResult>("/api/messages/summary", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get message summary. Status: {Status} Error: {Error}", resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Gets a paginated list of message requests with optional filtering.
    /// </summary>
    public async Task<PagedList<MessageRequestSummary>?> GetAllRequestsAsync(
        PaginationRequest pagination,
        RequestStatus? status = null,
        RequestServiceType? serviceType = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"page={pagination.Page}",
            $"size={pagination.Size}"
        };

        if (status.HasValue)
            queryParams.Add($"status={status.Value}");

        if (serviceType.HasValue)
            queryParams.Add($"serviceType={serviceType.Value}");

        var query = string.Join("&", queryParams);
        var resp = await GetAsync<PagedList<MessageRequestSummary>>($"/api/messages?{query}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get message requests. Status: {Status} Error: {Error}", resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Replays an individual message request.
    /// </summary>
    public async Task<bool> ReplayRequestAsync(
        RequestServiceType serviceType,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var resp = await PostAsync<object>($"/api/messages/{serviceType}/{id}/replay", cancellationToken);

        if (resp.Success)
            return true;

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Logger.LogWarning("Request {Id} not found for replay", id);
            return false;
        }

        Logger.LogError("Unable to replay request {Id}. Status: {Status} Error: {Error}", id, resp.StatusCode, resp.Error);
        return false;
    }

    /// <summary>
    /// Replays all non-complete requests for a service type.
    /// </summary>
    public async Task<int> ReplayAllAsync(
        RequestServiceType serviceType,
        int maxDegreeOfParallelism = 10,
        CancellationToken cancellationToken = default)
    {
        var resp = await PostAsync<object>($"/api/messages/{serviceType}/replay-all?maxDegreeOfParallelism={maxDegreeOfParallelism}", cancellationToken);

        if (resp.Success && resp.Data != null)
        {
            var publishedProperty = resp.Data.GetType().GetProperty("published");
            if (publishedProperty != null)
            {
                var publishedValue = publishedProperty.GetValue(resp.Data);
                if (publishedValue != null && int.TryParse(publishedValue.ToString(), out var published))
                {
                    return published;
                }
            }
        }

        Logger.LogError("Unable to replay all requests for {ServiceType}. Status: {Status} Error: {Error}", serviceType, resp.StatusCode, resp.Error);
        return 0;
    }
}
