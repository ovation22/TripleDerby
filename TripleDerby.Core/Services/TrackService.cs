using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

public class TrackService(ITripleDerbyRepository repository) : ITrackService
{
    public async Task<PagedList<TracksResult>> Filter(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var spec = new TrackFilterSpecificationToDto(request);

        return await repository.ListAsync(spec, cancellationToken);
    }
}
