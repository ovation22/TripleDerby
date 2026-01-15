using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class FeedingSessionSpecification : Specification<FeedingSession>
{
    public FeedingSessionSpecification(Guid horseId)
    {
        Query.Where(x => x.HorseId == horseId);

        Query.Include(x => x.Feeding);
    }
}
