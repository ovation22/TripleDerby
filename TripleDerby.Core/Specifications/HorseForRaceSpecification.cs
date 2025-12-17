using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class HorseForRaceSpecification : Specification<Horse>
{
    public HorseForRaceSpecification(Guid horseId)
    {
        Query.Include(x => x.Statistics);

        Query.Where(x => x.Id == horseId);
    }
}
