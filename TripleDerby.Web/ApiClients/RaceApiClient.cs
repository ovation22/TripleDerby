using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;
using TripleDerby.Web.ApiClients.Extensions;

namespace TripleDerby.Web.ApiClients;

public class RaceApiClient(HttpClient httpClient, ILogger<RaceApiClient> logger)
    : BaseApiClient(httpClient, logger), IRaceApiClient, IGenericApiClient
{
    /// <summary>
    /// Convenience strongly-typed search for RacesResult that delegates to the generic implementation.
    /// </summary>
    public Task<PagedList<RacesResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        => FilterAsync<RacesResult>(request, cancellationToken);

    /// <summary>
    /// Generic filter implementation required by IGenericApiClient.
    /// Builds the query string from the <see cref="PaginationRequest"/> and uses the BaseApiClient helper.
    /// </summary>
    public async Task<PagedList<T>?> FilterAsync<T>(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var url = request.ToQueryString("/api/races");
        var resp = await SearchAsync<PagedList<T>>(url, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to filter races for type {Type}. Status: {Status} Error: {Error}", typeof(T).Name, resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Get a single race by id.
    /// </summary>
    public async Task<RaceResult?> GetByIdAsync(byte id, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<RaceResult>($"/api/races/{id}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get race {Id}. Status: {Status} Error: {Error}", id, resp.StatusCode, resp.Error);
        return null;
    }
}
