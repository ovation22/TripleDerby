using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class TrainingSpecification : Specification<Training>
{
    public TrainingSpecification(byte id)
    {
        Query.Where(x => x.Id == id);
    }
}
