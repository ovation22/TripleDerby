using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class HorseFeedingPreferenceSpecification : Specification<HorseFeedingPreference>
{
    public HorseFeedingPreferenceSpecification(Guid horseId, byte feedingId)
    {
        Query.Where(x => x.HorseId == horseId && x.FeedingId == feedingId);
    }
}
