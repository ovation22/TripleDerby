using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

public sealed class TrackFilterSpecificationToDto : FilterSpecification<Track, TracksResult>
{
    public TrackFilterSpecificationToDto(PaginationRequest request)
        : base(request, defaultSortBy: "Name", defaultSortDirection: SortDirection.Asc)
    {
        Query.Select(t => new TracksResult
        {
            Id = t.Id,
            Name = t.Name
        });
    }
}
