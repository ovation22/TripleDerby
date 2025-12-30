using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

public sealed class RaceFilterSpecificationToDto : FilterSpecification<Race, RacesResult>
{
    private static readonly Dictionary<string, string> mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Surface", "Surface.Name" },
        { "Track", "Track.Name" }
    };

    public RaceFilterSpecificationToDto(PaginationRequest request)
        : base(request, mappings, defaultSortBy: "Name", defaultSortDirection: SortDirection.Asc)
    {
        Query.Select(r => new RacesResult
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Furlongs = r.Furlongs,
            SurfaceId = r.SurfaceId,
            Surface = r.Surface.Name,
            TrackId = r.TrackId,
            Track = r.Track.Name,
            MinFieldSize = r.MinFieldSize,
            MaxFieldSize = r.MaxFieldSize
        });
    }
}
