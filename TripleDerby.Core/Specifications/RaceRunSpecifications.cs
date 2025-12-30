using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;

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