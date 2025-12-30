using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IUserApiClient
{
    Task<UserResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedList<UserResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default);
}