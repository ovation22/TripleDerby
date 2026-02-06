using TripleDerby.SharedKernel;
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
}
