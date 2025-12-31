using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface ITrackApiClient
{
    Task<PagedList<TracksResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default);
}
