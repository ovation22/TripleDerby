using Ardalis.Specification;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public sealed class SimilarRaceStartsSpecification : Specification<Horse>
{
    // CPU horses are owned by "Racers" system user
    private static readonly Guid RacersOwnerId = new("72115894-88CD-433E-9892-CAC22E335F1D");

    public SimilarRaceStartsSpecification(int targetRaceStarts, int tolerance = 2, int limit = 11)
    {
        var minStarts = Math.Max(0, targetRaceStarts - tolerance);
        var maxStarts = targetRaceStarts + tolerance;

        Query
            .Where(h => h.OwnerId == RacersOwnerId)
            .Where(h => !h.IsRetired)
            .Where(h => h.RaceStarts >= minStarts && h.RaceStarts <= maxStarts)
            .OrderBy(h => Guid.NewGuid());

        Query.Include(h => h.Statistics);

        Query.Take(limit);
    }
}
