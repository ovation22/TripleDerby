using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class FeedingSpecification : Specification<Feeding>
{
    public FeedingSpecification(byte id)
    {
        Query.Where(x => x.Id == id);
    }
}
