using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;
using TripleDerby.Web.ApiClients.Extensions;

namespace TripleDerby.Web.ApiClients;

public class UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
    : BaseApiClient(httpClient, logger), IUserApiClient, IGenericApiClient
{
    /// <summary>
    /// Convenience strongly-typed filter for UserResult that delegates to the generic implementation.
    /// </summary>
    public Task<PagedList<UserResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        => FilterAsync<UserResult>(request, cancellationToken);

    /// <summary>
    /// Generic filter implementation required by IGenericApiClient.
    /// Builds the query string from the <see cref="PaginationRequest"/> and uses the BaseApiClient helper.
    /// </summary>
    public async Task<PagedList<T>?> FilterAsync<T>(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var url = request.ToQueryString("/api/users");
        var resp = await SearchAsync<PagedList<T>>(url, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to filter users for type {Type}. Status: {Status} Error: {Error}", typeof(T).Name, resp.StatusCode, resp.Error);
        return null;
    }

    /// <summary>
    /// Get a single user by id.
    /// </summary>
    public async Task<UserResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<UserResult>($"/api/users/{id}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get user {Id}. Status: {Status} Error: {Error}", id, resp.StatusCode, resp.Error);
        return null;
    }
}
