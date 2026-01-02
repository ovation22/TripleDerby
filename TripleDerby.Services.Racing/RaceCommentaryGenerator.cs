using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Utilities;
using TripleDerby.Services.Racing.Abstractions;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Generates natural language commentary for race events.
/// Phase 2 implementation uses synonym pools for language variation.
/// </summary>
public class RaceCommentaryGenerator(IRandomGenerator random) : IRaceCommentaryGenerator
{
    public string GenerateCommentary(TickEvents events, short tick, RaceRun raceRun)
    {
        var notes = new List<string>();

        // Priority order for multiple events in one tick
        if (events.IsRaceStart)
            notes.Add(GenerateRaceStart(raceRun));

        if (events.LeadChange != null)
            notes.Add(GenerateLeadChange(events.LeadChange));

        // Interleave lane changes and position changes by horse for more natural flow
        // Each horse's events are returned as separate strings for better narrative separation
        notes.AddRange(GenerateInterleavedHorseEvents(events));

        if (events.IsFinalStretch)
            notes.Add(GenerateFinalStretch(raceRun));

        // Photo finish must come BEFORE individual finish announcements
        if (events.PhotoFinish != null)
            notes.Add(GeneratePhotoFinish(events.PhotoFinish));

        notes.AddRange(events.Finishes.Select(GenerateFinish));

        // Join with semicolon only for special events (start, lead change, stretch, finishes)
        // Horse events are kept separate to avoid narrative clustering
        return notes.Count > 0 ? string.Join("; ", notes) : string.Empty;
    }

    /// <summary>
    /// Generates commentary for lane changes and position changes, interleaved by horse.
    /// Groups each horse's events together for more natural narrative flow.
    /// Maintains insertion order (chronological) rather than sorting, for natural event flow.
    /// </summary>
    private IEnumerable<string> GenerateInterleavedHorseEvents(TickEvents events)
    {
        // Track which horses we've already narrated (to avoid duplicates)
        var narratedHorses = new HashSet<string>();
        var allNotes = new List<string>();

        // Process position changes first (they're usually more important than lane changes)
        foreach (var pc in events.PositionChanges)
        {
            var horseNotes = new List<string>();

            // Add position change
            horseNotes.Add(GeneratePositionChange(pc));

            // Check if this horse also has a lane change to include
            var laneChange = events.LaneChanges.FirstOrDefault(lc => lc.HorseName == pc.HorseName);
            if (laneChange != null)
                horseNotes.Add(GenerateLaneChange(laneChange));

            allNotes.Add(string.Join("; ", horseNotes));
            narratedHorses.Add(pc.HorseName);
        }

        // Process remaining lane changes (for horses that didn't have position changes)
        foreach (var lc in events.LaneChanges)
        {
            if (!narratedHorses.Contains(lc.HorseName))
            {
                allNotes.Add(GenerateLaneChange(lc));
                narratedHorses.Add(lc.HorseName);
            }
        }

        return allNotes;
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
    /// Generates lead change commentary with varied language.
    /// </summary>
    private string GenerateLeadChange(LeadChange leadChange)
    {
        var leadPhrase = random.PickRandom(CommentaryConfig.LeadPhrases);
        var template = random.PickRandom(CommentaryConfig.LeadChangeTemplates);

        return template
            .Replace("{newLeader}", leadChange.NewLeader)
            .Replace("{oldLeader}", leadChange.OldLeader)
            .Replace("{leadPhrase}", leadPhrase);
    }

    /// <summary>
    /// Generates lane change commentary based on type with varied language.
    /// </summary>
    private string GenerateLaneChange(LaneChange lc)
    {
        return lc.Type switch
        {
            LaneChangeType.Clean => GenerateCleanLaneChange(lc),
            LaneChangeType.RiskySuccess => GenerateRiskySqueezeSuccess(lc),
            LaneChangeType.RiskyFailure => $"{lc.HorseName} blocked, unable to change lanes",
            _ => ""
        };
    }

    private string GenerateCleanLaneChange(LaneChange lc)
    {
        var laneVerb = random.PickRandom(CommentaryConfig.LaneChangeVerbs);
        var template = random.PickRandom(CommentaryConfig.LaneChangeTemplates);

        return template
            .Replace("{horse}", lc.HorseName)
            .Replace("{laneVerb}", laneVerb)
            .Replace("{lane}", lc.NewLane.ToString())
            .Replace("{oldLane}", lc.OldLane.ToString())
            .Replace("{newLane}", lc.NewLane.ToString());
    }

    private string GenerateRiskySqueezeSuccess(LaneChange lc)
    {
        var squeezeVerb = random.PickRandom(CommentaryConfig.RiskySqueezeVerbs);
        var template = random.PickRandom(CommentaryConfig.RiskySqueezeTemplates);

        return template
            .Replace("{horse}", lc.HorseName)
            .Replace("{squeezeVerb}", squeezeVerb)
            .Replace("{lane}", lc.NewLane.ToString());
    }

    /// <summary>
    /// Generates position change commentary with varied language.
    /// </summary>
    private string GeneratePositionChange(PositionChange pc)
    {
        var ordinal = GetOrdinal(pc.NewPosition);
        var passVerb = random.PickRandom(CommentaryConfig.PassVerbs);
        var surgeVerb = random.PickRandom(CommentaryConfig.SurgeVerbs);

        // If we know who was passed, include them in the commentary
        if (!string.IsNullOrEmpty(pc.OpponentPassed))
        {
            var templateChoice = random.Next(2);
            return templateChoice switch
            {
                0 => $"{pc.HorseName} {passVerb} {pc.OpponentPassed} into {ordinal} place",
                _ => $"{pc.HorseName} {surgeVerb} past {pc.OpponentPassed}"
            };
        }

        // Otherwise, just mention the new position
        var simpleTemplateChoice = random.Next(2);
        return simpleTemplateChoice switch
        {
            0 => $"{pc.HorseName} {passVerb} into {ordinal} place",
            _ => $"{pc.HorseName} {surgeVerb} to {ordinal} place"
        };
    }

    /// <summary>
    /// Generates final stretch entry commentary with varied language.
    /// </summary>
    private string GenerateFinalStretch(RaceRun raceRun)
    {
        var leader = raceRun.Horses
            .OrderByDescending(h => h.Distance)
            .FirstOrDefault();

        var intro = random.PickRandom(CommentaryConfig.FinalStretchIntros);
        return leader != null ? $"{intro} {leader.Horse.Name} leads" : intro;
    }

    /// <summary>
    /// Generates finish line commentary with varied language.
    /// </summary>
    private string GenerateFinish(HorseFinish finish)
    {
        var ordinal = GetOrdinal(finish.Place);
        var finishVerb = random.PickRandom(CommentaryConfig.FinishVerbs);
        var template = random.PickRandom(CommentaryConfig.FinishTemplates);

        return template
            .Replace("{horse}", finish.HorseName)
            .Replace("{finishVerb}", finishVerb)
            .Replace("{place}", ordinal);
    }

    /// <summary>
    /// Generates photo finish commentary.
    /// </summary>
    private static string GeneratePhotoFinish(PhotoFinish pf)
    {
        return $"Photo finish! {pf.Horse1} edges {pf.Horse2} by a nose!";
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
