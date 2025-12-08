using System;
using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class ParentHorseSpecification : Specification<Horse>
{
    public ParentHorseSpecification(Guid id)
    {
        Query.Where(x => x.Id == id)
            .Include(x => x.Color);

        Query.Include(x => x.Statistics);
    }
}
