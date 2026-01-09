using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class TrainingSessionSpecification : Specification<TrainingSession>
{
    public TrainingSessionSpecification(Guid horseId)
    {
        Query.Where(x => x.HorseId == horseId);

        Query.Include(x => x.Training);
    }
}
