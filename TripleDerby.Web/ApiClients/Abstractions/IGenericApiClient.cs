using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IGenericApiClient
{
    Task<PagedList<T>?> SearchAsync<T>(PaginationRequest request, CancellationToken cancellationToken = default);
}