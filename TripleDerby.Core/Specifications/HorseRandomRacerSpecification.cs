using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class HorseRandomRacerSpecification : Specification<Horse>
{
    public HorseRandomRacerSpecification()
    {
        Query
            .Where(x => !x.IsRetired)
            .OrderBy(x => Guid.NewGuid());

        Query.Include(x => x.Statistics);

        Query.Take(10);
    }
}
