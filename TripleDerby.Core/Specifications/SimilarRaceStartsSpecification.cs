using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class SimilarRaceStartsSpecification : Specification<Horse>
{
    public SimilarRaceStartsSpecification(int targetRaceStarts, Guid excludeHorseId, int tolerance = 2, int limit = 11)
    {
        var minStarts = Math.Max(0, targetRaceStarts - tolerance);
        var maxStarts = targetRaceStarts + tolerance;

        Query
            .Where(h => h.Id != excludeHorseId)
            .Where(h => !h.IsRetired)
            .Where(h => h.RaceStarts >= minStarts && h.RaceStarts <= maxStarts)
            .OrderBy(h => Guid.NewGuid());

        Query.Include(h => h.Statistics);

        Query.Take(limit);
    }
}
