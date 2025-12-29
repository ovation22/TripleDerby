using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Detects notable race events for commentary generation.
/// Compares current race state with previous tick to identify changes.
/// </summary>
public class EventDetector : IEventDetector
{
    /// <summary>
    /// Detects notable events during a race tick for commentary generation.
    /// Compares current race state with previous tick to identify changes.
    /// </summary>
    public TickEvents DetectEvents(
        short tick,
        short totalTicks,
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        Guid? previousLeader,
        Dictionary<Guid, short> recentPositionChanges,
        Dictionary<Guid, short> recentLaneChanges)
    {
        var events = new TickEvents();

        // Race start
        if (tick == 1)
            events.IsRaceStart = true;

        // Final stretch (last 25%)
        var raceProgress = (double)tick / totalTicks;
        if (raceProgress >= 0.75 && (tick - 1) / (double)totalTicks < 0.75)
            events.IsFinalStretch = true;

        // Current positions (sorted by distance)
        var currentPositions = raceRun.Horses
            .OrderByDescending(h => h.Distance)
            .Select((h, index) => new { h.Horse.Id, h.Horse.Name, Position = index + 1 })
            .ToList();

        var currentLeader = currentPositions.FirstOrDefault()?.Id;

        // Lead change (only report if both horses are still racing)
        if (currentLeader != null && previousLeader != null && currentLeader != previousLeader)
        {
            var newLeaderHorse = raceRun.Horses.First(h => h.Horse.Id == currentLeader);
            var oldLeaderHorse = raceRun.Horses.First(h => h.Horse.Id == previousLeader);

            // Skip lead change if either horse has finished
            if (newLeaderHorse.Distance < raceRun.Race.Furlongs && oldLeaderHorse.Distance < raceRun.Race.Furlongs)
            {
                var newLeaderName = currentPositions.First(p => p.Id == currentLeader).Name;
                var oldLeaderName = oldLeaderHorse.Horse.Name;
                events.LeadChange = new LeadChange(newLeaderName, oldLeaderName);
            }
        }

        // Position changes (only report improvements for horses still racing)
        foreach (var current in currentPositions)
        {
            var horse = raceRun.Horses.First(h => h.Horse.Id == current.Id);

            // Skip horses that finished this tick (they'll get finish commentary instead)
            if (horse.Distance >= raceRun.Race.Furlongs)
                continue;

            if (previousPositions.TryGetValue(current.Id, out var oldPos))
            {
                if (current.Position < oldPos) // Improved position (lower number = better)
                {
                    // Check if this horse had a recent position change (within cooldown window)
                    if (recentPositionChanges.TryGetValue(current.Id, out var lastChangeTick))
                    {
                        if (tick - lastChangeTick < CommentaryConfig.PositionChangeCooldown)
                            continue; // Skip this position change, too soon after last one
                    }

                    // Find who they passed (the horse now in the position they left)
                    string? opponentPassed = null;
                    var horseInOldPosition = currentPositions.FirstOrDefault(p => p.Position == oldPos);
                    if (horseInOldPosition != null && horseInOldPosition.Id != current.Id)
                    {
                        opponentPassed = horseInOldPosition.Name;
                    }

                    events.PositionChanges.Add(new PositionChange(
                        current.Name,
                        oldPos,
                        current.Position,
                        opponentPassed));

                    // Record this position change
                    recentPositionChanges[current.Id] = tick;
                }
            }
        }

        // Lane changes (compare current vs previous lanes)
        foreach (var horse in raceRun.Horses)
        {
            if (previousLanes.TryGetValue(horse.Horse.Id, out var oldLane))
            {
                if (horse.Lane != oldLane)
                {
                    // Determine type: risky success if speed penalty active
                    var type = horse.SpeedPenaltyTicksRemaining > 0
                        ? LaneChangeType.RiskySuccess
                        : LaneChangeType.Clean;

                    // Check cooldown for clean lane changes (risky squeezes always reported)
                    var shouldReport = type == LaneChangeType.RiskySuccess;

                    if (!shouldReport)
                    {
                        // Check if this horse had a recent lane change
                        if (recentLaneChanges.TryGetValue(horse.Horse.Id, out var lastChangeTick))
                        {
                            if (tick - lastChangeTick >= CommentaryConfig.LaneChangeCooldown)
                                shouldReport = true;
                        }
                        else
                        {
                            // First lane change for this horse
                            shouldReport = true;
                        }
                    }

                    if (shouldReport)
                    {
                        events.LaneChanges.Add(new LaneChange(
                            horse.Horse.Name,
                            oldLane,
                            horse.Lane,
                            type));

                        // Record this lane change
                        recentLaneChanges[horse.Horse.Id] = tick;
                    }
                }
            }
        }

        // Photo finish detection (check if top 2 have both finished and were close)
        var finishedHorses = raceRun.Horses
            .Where(h => h.Distance >= raceRun.Race.Furlongs)
            .OrderBy(h => h.Time)
            .ToList();

        // Check for photo finish when top 2 have finished
        if (finishedHorses.Count >= 2)
        {
            var top2 = finishedHorses.Take(2).ToList();
            var margin = top2[1].Time - top2[0].Time;

            // Only report photo finish once (check if we haven't already detected it)
            if (margin <= CommentaryConfig.PhotoFinishMargin && events.PhotoFinish == null)
            {
                // Check if the 2nd place horse just finished this tick
                if (top2[1].Time >= tick - 1 && top2[1].Time < tick)
                {
                    // Order by Place (not Time) for photo finish announcement
                    // Horse1 = 1st place, Horse2 = 2nd place
                    var byPlace = top2.OrderBy(h => h.Place).ToList();
                    events.PhotoFinish = new PhotoFinish(
                        byPlace[0].Horse.Name,  // 1st place
                        byPlace[1].Horse.Name,  // 2nd place
                        margin);
                }
            }
        }

        // Horses crossing finish line (check Time field set this tick)
        var finishedThisTick = raceRun.Horses
            .Where(h => h.Distance >= raceRun.Race.Furlongs &&
                       h.Time >= tick - 1 &&
                       h.Time < tick)
            .OrderBy(h => h.Place)  // Report in place order, not time order
            .ToList();

        foreach (var horse in finishedThisTick)
        {
            events.Finishes.Add(new HorseFinish(horse.Horse.Name, horse.Place));
        }

        return events;
    }

    /// <summary>
    /// Updates the previous state tracking dictionaries for the next tick.
    /// </summary>
    public void UpdatePreviousState(
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        ref Guid? previousLeader)
    {
        previousPositions.Clear();
        previousLanes.Clear();

        // Calculate current positions
        var positions = raceRun.Horses
            .OrderByDescending(h => h.Distance)
            .Select((h, index) => new { h.Horse.Id, Position = index + 1 })
            .ToList();

        foreach (var pos in positions)
        {
            previousPositions[pos.Id] = pos.Position;
        }

        // Store current lanes
        foreach (var horse in raceRun.Horses)
        {
            previousLanes[horse.Horse.Id] = horse.Lane;
        }

        // Store current leader
        previousLeader = positions.FirstOrDefault()?.Id;
    }
}
