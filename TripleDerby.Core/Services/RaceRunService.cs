using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;

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

    public async Task<PagedResult<RaceRunSummary>?> GetRaceRuns(
        byte raceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var race = await repository.FindAsync<Race>(raceId, cancellationToken);
        if (race == null)
            return null;

        var spec = new RaceRunsByRaceSpecification(raceId);
        var allRuns = await repository.ListAsync(spec, cancellationToken);
        var totalCount = allRuns.Count;

        var runs = allRuns
            .OrderByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RaceRunSummary
            {
                RaceRunId = r.Id,
                ConditionId = r.ConditionId,
                ConditionName = r.ConditionId.ToString(),
                WinnerName = r.WinHorse?.Name ?? "Unknown",
                WinnerTime = r.Horses.FirstOrDefault(h => h.Place == 1)?.Time ?? 0.0,
                FieldSize = r.Horses.Count
            })
            .ToList();

        return new PagedResult<RaceRunSummary>
        {
            Items = runs,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
