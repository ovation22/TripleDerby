using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class HorseWithStatsSpecification : Specification<Horse>
{
    public HorseWithStatsSpecification(Guid horseId)
    {
        Query.Where(x => x.Id == horseId);

        Query.Include(x => x.Statistics);
    }
}
