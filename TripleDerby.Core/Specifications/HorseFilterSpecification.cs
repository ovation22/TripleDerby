using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

public sealed class HorseFilterSpecification : FilterSpecification<Horse>
{
    private static readonly Dictionary<string, string> mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Horse", "Name" },
        { "Color", "Color.Name" },
        { "Sire", "Sire.Name" },
        { "Dam", "Dam.Name" },
        { "Owner", "Owner.Username" },
        { "Created", "CreatedDate" }
    };

    public HorseFilterSpecification(PaginationRequest request)
        : base(request, mappings, defaultSortBy: "Horse", defaultSortDirection: SortDirection.Asc)
    {
        Query.Include(x => x.Color);
        Query.Include(x => x.Sire);
        Query.Include(x => x.Dam);
        Query.Include(x => x.Owner);
    }
}