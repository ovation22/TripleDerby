using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Utilities;

namespace TripleDerby.Core.Services;

/// <summary>
/// Generates natural language commentary for race events.
/// Phase 2 implementation uses synonym pools for language variation.
/// </summary>
public class RaceCommentaryGenerator : IRaceCommentaryGenerator
{
    private readonly IRandomGenerator _random;

    public RaceCommentaryGenerator(IRandomGenerator random)
    {
        _random = random;
    }

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
    /// Generates lead change commentary with varied language.
    /// </summary>
    private string GenerateLeadChange(LeadChange leadChange)
    {
        var leadPhrase = _random.PickRandom(CommentaryConfig.LeadPhrases);
        var template = _random.PickRandom(CommentaryConfig.LeadChangeTemplates);

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
        var laneVerb = _random.PickRandom(CommentaryConfig.LaneChangeVerbs);
        var template = _random.PickRandom(CommentaryConfig.LaneChangeTemplates);

        return template
            .Replace("{horse}", lc.HorseName)
            .Replace("{laneVerb}", laneVerb)
            .Replace("{lane}", lc.NewLane.ToString())
            .Replace("{oldLane}", lc.OldLane.ToString())
            .Replace("{newLane}", lc.NewLane.ToString());
    }

    private string GenerateRiskySqueezeSuccess(LaneChange lc)
    {
        var squeezeVerb = _random.PickRandom(CommentaryConfig.RiskySqueezeVerbs);
        var template = _random.PickRandom(CommentaryConfig.RiskySqueezeTemplates);

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
        var passVerb = _random.PickRandom(CommentaryConfig.PassVerbs);
        var surgeVerb = _random.PickRandom(CommentaryConfig.SurgeVerbs);

        // If we know who was passed, include them in the commentary
        if (!string.IsNullOrEmpty(pc.OpponentPassed))
        {
            var templateChoice = _random.Next(2);
            return templateChoice switch
            {
                0 => $"{pc.HorseName} {passVerb} {pc.OpponentPassed} into {ordinal} place",
                _ => $"{pc.HorseName} {surgeVerb} past {pc.OpponentPassed}"
            };
        }

        // Otherwise, just mention the new position
        var simpleTemplateChoice = _random.Next(2);
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

        var intro = _random.PickRandom(CommentaryConfig.FinalStretchIntros);
        return leader != null ? $"{intro} {leader.Horse.Name} leads" : intro;
    }

    /// <summary>
    /// Generates finish line commentary with varied language.
    /// </summary>
    private string GenerateFinish(HorseFinish finish)
    {
        var ordinal = GetOrdinal(finish.Place);
        var finishVerb = _random.PickRandom(CommentaryConfig.FinishVerbs);
        var template = _random.PickRandom(CommentaryConfig.FinishTemplates);

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
