using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Services;

/// <summary>
/// Generates natural language commentary for race events.
/// Phase 1 implementation uses simple templates.
/// </summary>
public class RaceCommentaryGenerator : IRaceCommentaryGenerator
{
    public string GenerateCommentary(TickEvents events, short tick, RaceRun raceRun)
    {
        var notes = new List<string>();

        // Priority order for multiple events in one tick
        if (events.IsRaceStart)
            notes.Add(GenerateRaceStart(raceRun));

        if (events.LeadChange != null)
            notes.Add(GenerateLeadChange(events.LeadChange));

        notes.AddRange(events.LaneChanges.Select(GenerateLaneChange));

        notes.AddRange(events.PositionChanges.Select(GeneratePositionChange));

        if (events.IsFinalStretch)
            notes.Add(GenerateFinalStretch(raceRun));

        notes.AddRange(events.Finishes.Select(GenerateFinish));

        if (events.PhotoFinish != null)
            notes.Add(GeneratePhotoFinish(events.PhotoFinish));

        // Combine multiple events with semicolon separator
        return string.Join("; ", notes);
    }

    /// <summary>
    /// Generates race start commentary.
    /// </summary>
    private static string GenerateRaceStart(RaceRun raceRun)
    {
        var fieldSize = raceRun.Horses.Count;
        var condition = raceRun.ConditionId.ToString();
        var distance = raceRun.Race.Furlongs;

        return $"And they're off! {fieldSize} horses break from the gate for {distance} furlongs on a {condition} track.";
    }

    /// <summary>
    /// Generates lead change commentary.
    /// </summary>
    private static string GenerateLeadChange(LeadChange leadChange)
    {
        return $"{leadChange.NewLeader} takes the lead from {leadChange.OldLeader}!";
    }

    /// <summary>
    /// Generates lane change commentary based on type.
    /// </summary>
    private static string GenerateLaneChange(LaneChange lc)
    {
        return lc.Type switch
        {
            LaneChangeType.Clean =>
                $"{lc.HorseName} moves to lane {lc.NewLane}",

            LaneChangeType.RiskySuccess =>
                $"{lc.HorseName} threads through traffic into lane {lc.NewLane}!",

            LaneChangeType.RiskyFailure =>
                $"{lc.HorseName} blocked, unable to change lanes",

            _ => ""
        };
    }

    /// <summary>
    /// Generates position change commentary.
    /// </summary>
    private static string GeneratePositionChange(PositionChange pc)
    {
        var ordinal = GetOrdinal(pc.NewPosition);
        return $"{pc.HorseName} advances to {ordinal} place";
    }

    /// <summary>
    /// Generates final stretch entry commentary.
    /// </summary>
    private static string GenerateFinalStretch(RaceRun raceRun)
    {
        var leader = raceRun.Horses
            .OrderByDescending(h => h.Distance)
            .FirstOrDefault();

        return leader != null ? $"Into the final stretch! {leader.Horse.Name} leads" : "Into the final stretch!";
    }

    /// <summary>
    /// Generates finish line commentary.
    /// </summary>
    private static string GenerateFinish(HorseFinish finish)
    {
        var ordinal = GetOrdinal(finish.Place);
        return $"{finish.HorseName} crosses the line in {ordinal} place";
    }

    /// <summary>
    /// Generates photo finish commentary.
    /// </summary>
    private static string GeneratePhotoFinish(PhotoFinish pf)
    {
        return $"Photo finish! {pf.Horse1} edges {pf.Horse2} by {pf.Margin:F2} ticks!";
    }

    /// <summary>
    /// Converts position number to ordinal string (1 -> "1st", 2 -> "2nd", etc.).
    /// </summary>
    private static string GetOrdinal(int number)
    {
        if (number <= 0) return number.ToString();

        return (number % 100) switch
        {
            11 or 12 or 13 => $"{number}th",
            _ => (number % 10) switch
            {
                1 => $"{number}st",
                2 => $"{number}nd",
                3 => $"{number}rd",
                _ => $"{number}th"
            }
        };
    }
}
