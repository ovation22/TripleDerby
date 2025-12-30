using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;
using TripleDerby.Web.ApiClients.Extensions;

namespace TripleDerby.Web.ApiClients;

public class HorseApiClient(HttpClient httpClient, ILogger<HorseApiClient> logger)
    : BaseApiClient(httpClient, logger), IHorseApiClient, IGenericApiClient
{
    /// <summary>
    /// Convenience strongly-typed search for HorseResult that delegates to the generic implementation.
    /// </summary>
    public Task<PagedList<HorseResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        => FilterAsync<HorseResult>(request, cancellationToken);

    /// <summary>
    /// Generic search implementation required by IGenericApiClient.
    /// Builds the query string from the <see cref="PaginationRequest"/> and uses the BaseApiClient helper.
    /// </summary>
    public async Task<PagedList<T>?> FilterAsync<T>(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var url = request.ToQueryString("/api/horses");
        var resp = await SearchAsync<PagedList<T>>(url, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to search horses for type {Type}. Status: {Status} Error: {Error}", typeof(T).Name, resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Get a single horse by id.
    /// </summary>
    public async Task<HorseResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<HorseResult>($"/api/horses/{id}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get horse {Id}. Status: {Status} Error: {Error}", id, resp.StatusCode, resp.Error);
        return null;
    }
}
