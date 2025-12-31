using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Abstractions.Services;

public interface ITrackService
{
    Task<PagedList<TracksResult>> Filter(PaginationRequest request, CancellationToken cancellationToken = default);
}
