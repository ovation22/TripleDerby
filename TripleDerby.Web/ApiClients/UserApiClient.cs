using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;
using TripleDerby.Web.ApiClients.Extensions;

namespace TripleDerby.Web.ApiClients;

public class UserApiClient : BaseApiClient, IUserApiClient, IGenericApiClient
{
    public UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
        : base(httpClient, logger) { }

    /// <summary>
    /// Convenience strongly-typed search for UserResult that delegates to the generic implementation.
    /// </summary>
    public Task<PagedList<UserResult>?> SearchAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        => SearchAsync<UserResult>(request, cancellationToken);

    /// <summary>
    /// Generic search implementation required by IGenericApiClient.
    /// Builds the query string from the <see cref="PaginationRequest"/> and uses the BaseApiClient helper.
    /// </summary>
    public async Task<PagedList<T>?> SearchAsync<T>(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var url = request.ToQueryString("/api/users");
        var resp = await SearchAsync<PagedList<T>>(url, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to search users for type {Type}. Status: {Status} Error: {Error}", typeof(T).Name, resp.StatusCode, resp.Error);
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
