namespace TripleDerby.Core.Services;

/// <summary>
/// Collection of events detected during a single race tick.
/// Used by commentary generator to determine what to narrate.
/// </summary>
public class TickEvents
{
    public List<PositionChange> PositionChanges { get; set; } = [];
    public List<LaneChange> LaneChanges { get; set; } = [];
    public List<HorseFinish> Finishes { get; set; } = [];
    public bool IsRaceStart { get; set; }
    public bool IsFinalStretch { get; set; }
    public LeadChange? LeadChange { get; set; }
    public PhotoFinish? PhotoFinish { get; set; }
}

/// <summary>
/// Represents a horse improving its position in the race.
/// </summary>
/// <param name="HorseName">Name of the horse that advanced</param>
/// <param name="OldPosition">Previous position (higher number)</param>
/// <param name="NewPosition">New position (lower number = better)</param>
/// <param name="OpponentPassed">Name of the horse that was passed (optional)</param>
public record PositionChange(string HorseName, int OldPosition, int NewPosition, string? OpponentPassed = null);

/// <summary>
/// Represents a horse changing lanes during the race.
/// </summary>
/// <param name="HorseName">Name of the horse that changed lanes</param>
/// <param name="OldLane">Previous lane number</param>
/// <param name="NewLane">New lane number</param>
/// <param name="Type">Type of lane change (clean, risky success, risky failure)</param>
public record LaneChange(string HorseName, int OldLane, int NewLane, LaneChangeType Type);

/// <summary>
/// Represents a horse crossing the finish line.
/// </summary>
/// <param name="HorseName">Name of the horse that finished</param>
/// <param name="Place">Final placement (1st, 2nd, 3rd, etc.)</param>
public record HorseFinish(string HorseName, int Place);

/// <summary>
/// Represents a change in race leadership.
/// </summary>
/// <param name="NewLeader">Name of the horse taking the lead</param>
/// <param name="OldLeader">Name of the horse losing the lead</param>
public record LeadChange(string NewLeader, string OldLeader);

/// <summary>
/// Represents a very close finish between top two horses.
/// </summary>
/// <param name="Horse1">Name of the winning horse</param>
/// <param name="Horse2">Name of the second-place horse</param>
/// <param name="Margin">Time difference in ticks</param>
public record PhotoFinish(string Horse1, string Horse2, double Margin);

/// <summary>
/// Type of lane change executed by a horse.
/// </summary>
public enum LaneChangeType
{
    /// <summary>
    /// Lane change into clear space with no penalty.
    /// </summary>
    Clean,

    /// <summary>
    /// Risky lane change through traffic that succeeded.
    /// Results in speed penalty for several ticks.
    /// </summary>
    RiskySuccess,

    /// <summary>
    /// Attempted risky lane change that failed.
    /// Horse remains in original lane, cooldown consumed.
    /// </summary>
    RiskyFailure
}
