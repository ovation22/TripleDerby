using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;
using TripleDerby.Web.ApiClients.Extensions;

namespace TripleDerby.Web.ApiClients;

public class TrackApiClient(HttpClient httpClient, ILogger<TrackApiClient> logger)
    : BaseApiClient(httpClient, logger), ITrackApiClient, IGenericApiClient
{
    /// <summary>
    /// Convenience strongly-typed search for TracksResult that delegates to the generic implementation.
    /// </summary>
    public Task<PagedList<TracksResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        => FilterAsync<TracksResult>(request, cancellationToken);

    /// <summary>
    /// Generic filter implementation required by IGenericApiClient.
    /// Builds the query string from the <see cref="PaginationRequest"/> and uses the BaseApiClient helper.
    /// </summary>
    public async Task<PagedList<T>?> FilterAsync<T>(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var url = request.ToQueryString("/api/tracks");
        var resp = await SearchAsync<PagedList<T>>(url, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to filter tracks for type {Type}. Status: {Status} Error: {Error}", typeof(T).Name, resp.StatusCode, resp.Error);
        return null;
    }
}
