using System;
using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class HorseWithHappinessAndPriorFeedingsSpecification : Specification<Horse>
{
    public HorseWithHappinessAndPriorFeedingsSpecification(Guid horseId, byte feedingId)
    {
        Query.Where(x => x.Id == horseId);

        Query.Include(x => x.Statistics);

        Query.Include(x => x.FeedingSessions);
    }
}
