using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

/// <summary>
/// Specification for fetching a specific race run with all related data for detail view.
/// Projects directly to RaceRunResult to minimize data transfer.
/// </summary>
public sealed class RaceRunDetailSpecification : Specification<RaceRun, RaceRunResult>
{
    public RaceRunDetailSpecification(byte raceId, Guid raceRunId)
    {
        Query.Where(rr => rr.RaceId == raceId && rr.Id == raceRunId);

        Query.Select(rr => new RaceRunResult
        {
            RaceRunId = rr.Id,
            RaceId = rr.RaceId,
            RaceName = rr.Race.Name,
            TrackId = rr.Race.TrackId,
            TrackName = rr.Race.Track.Name,
            ConditionId = rr.ConditionId,
            ConditionName = rr.ConditionId.ToString(),
            SurfaceId = rr.Race.SurfaceId,
            SurfaceName = rr.Race.Surface.Name,
            Furlongs = rr.Race.Furlongs,
            HorseResults = rr.Horses
                .OrderBy(h => h.Place)
                .Select(h => new RaceRunHorseResult
                {
                    Place = h.Place,
                    HorseId = h.HorseId,
                    HorseName = h.Horse.Name,
                    Time = h.Time,
                    Payout = h.Payout
                })
                .ToList(),
            PlayByPlay = rr.RaceRunTicks
                .Where(t => !string.IsNullOrWhiteSpace(t.Note))
                .OrderBy(t => t.Tick)
                .Select(t => t.Note!)
                .ToList()
        });
    }
}

/// <summary>
/// Specification for fetching race runs for a specific race with filtering, sorting, and pagination.
/// Projects to RaceRunSummary for grid display.
/// </summary>
public sealed class RaceRunFilterSpecification : SearchSpecification<RaceRun, RaceRunSummary>
{
    private static readonly Dictionary<string, string> PropertyMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "WinnerName", "WinHorse.Name" },
        { "WinnerTime", "Horses.FirstOrDefault(h => h.Place == 1).Time" }, // Note: This won't work for sorting in SQL, handled manually
        { "FieldSize", "Horses.Count" }, // Note: This won't work for sorting in SQL, handled manually
        { "ConditionName", "ConditionId" }
    };

    public RaceRunFilterSpecification(byte raceId, PaginationRequest request)
        : base(request, PropertyMappings, defaultSortBy: "Id", defaultSortDirection: SortDirection.Desc)
    {
        // Filter by race
        Query.Where(rr => rr.RaceId == raceId);

        // Project to RaceRunSummary
        Query.Select(rr => new RaceRunSummary
        {
            RaceRunId = rr.Id,
            ConditionId = rr.ConditionId,
            ConditionName = rr.ConditionId.ToString(),
            WinnerName = rr.WinHorse.Name,
            WinnerTime = rr.Horses.Where(h => h.Place == 1).Select(h => h.Time).FirstOrDefault(),
            FieldSize = rr.Horses.Count
        });
    }
}
