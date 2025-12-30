using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IRaceApiClient
{
    Task<RaceResult?> GetByIdAsync(byte id, CancellationToken cancellationToken = default);
    Task<PagedList<RacesResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default);
}
