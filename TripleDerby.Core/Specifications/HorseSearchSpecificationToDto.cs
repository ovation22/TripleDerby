using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

public sealed class HorseFilterSpecificationToDto : FilterSpecification<Horse, HorseResult>
{
    private static readonly Dictionary<string, string> Mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Horse", "Name" },
        { "Color", "Color.Name" },
        { "Sire", "Sire.Name" },
        { "Dam", "Dam.Name" },
        { "Owner", "Owner.Username" },
        { "Created", "CreatedDate" }
    };

    public HorseFilterSpecificationToDto(PaginationRequest request)
        : base(request, Mappings, defaultSortBy: "Horse", defaultSortDirection: SortDirection.Asc)
    {
        Query.Select(h => new HorseResult
        {
            Id = h.Id,
            Name = h.Name,
            IsMale = h.IsMale,
            Color = h.Color.Name,
            Earnings = h.Earnings,
            RacePlace = h.RacePlace,
            RaceShow = h.RaceShow,
            RaceStarts = h.RaceStarts,
            RaceWins = h.RaceWins,
            Sire = h.Sire != null ? h.Sire.Name : null,
            Dam = h.Dam != null ? h.Dam.Name : null,
            Created = h.CreatedDate,
            Owner = h.Owner.Username
        });
    }
}