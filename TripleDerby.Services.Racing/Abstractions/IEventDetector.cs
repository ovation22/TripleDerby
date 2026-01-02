using TripleDerby.Core.Entities;

namespace TripleDerby.Services.Racing.Abstractions;

/// <summary>
/// Detects notable race events for commentary generation.
/// </summary>
public interface IEventDetector
{
    /// <summary>
    /// Detects notable events during a race tick for commentary generation.
    /// Compares current race state with previous tick to identify changes.
    /// </summary>
    /// <param name="tick">Current tick number</param>
    /// <param name="totalTicks">Total ticks in race</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="previousPositions">Horse positions from previous tick</param>
    /// <param name="previousLanes">Horse lanes from previous tick</param>
    /// <param name="previousLeader">Leader horse ID from previous tick</param>
    /// <param name="recentPositionChanges">Tracks last tick each horse had a position change</param>
    /// <param name="recentLaneChanges">Tracks last tick each horse had a lane change</param>
    /// <returns>Collection of detected events</returns>
    TickEvents DetectEvents(
        short tick,
        short totalTicks,
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        Guid? previousLeader,
        Dictionary<Guid, short> recentPositionChanges,
        Dictionary<Guid, short> recentLaneChanges);

    /// <summary>
    /// Updates the previous state tracking dictionaries for the next tick.
    /// </summary>
    /// <param name="raceRun">Current race state</param>
    /// <param name="previousPositions">Dictionary to update with current positions</param>
    /// <param name="previousLanes">Dictionary to update with current lanes</param>
    /// <param name="previousLeader">Reference to update with current leader</param>
    void UpdatePreviousState(
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        ref Guid? previousLeader);
}
