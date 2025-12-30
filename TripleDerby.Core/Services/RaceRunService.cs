using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

/// <summary>
/// Service for querying race run results and history.
/// </summary>
public class RaceRunService(ITripleDerbyRepository repository) : IRaceRunService
{
    public async Task<RaceRunResult?> GetRaceRunDetails(
        byte raceId,
        Guid raceRunId,
        CancellationToken cancellationToken = default)
    {
        return await repository.SingleOrDefaultAsync(
            new RaceRunDetailSpecification(raceId, raceRunId),
            cancellationToken);
    }

    public async Task<PagedList<RaceRunSummary>?> GetRaceRuns(
        byte raceId,
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify race exists
        var race = await repository.FindAsync<Race>(raceId, cancellationToken);
        if (race == null)
            return null;

        // Use specification to handle filtering, sorting, and projection
        var spec = new RaceRunFilterSpecification(raceId, request);
        var pagedList = await repository.ListAsync(spec, cancellationToken);

        return pagedList;
    }
}
