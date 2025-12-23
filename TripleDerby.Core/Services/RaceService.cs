using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Services;

public class RaceService(
    ITripleDerbyRepository repository,
    IRandomGenerator randomGenerator,
    ISpeedModifierCalculator speedModifierCalculator,
    IStaminaCalculator staminaCalculator) : IRaceService
{
    // Configuration constants
    private const double BaseSpeedMph = 38.0; // Average horse speed in mph
    private const double MilesPerFurlong = 0.125; // 1 furlong = 1/8 mile
    private const double SecondsPerHour = 3600.0;

    // Derived: furlongs per second at base speed
    private const double FurlongsPerSecond = BaseSpeedMph * MilesPerFurlong / SecondsPerHour; // ≈ 0.001056

    // Simulation speed (adjust this to control race duration)
    private const double TicksPerSecond = 10.0; // 10 TPS = ~16 seconds for 10f, 2 TPS = ~2 minutes for 10f

    // Target race duration configuration
    private const double TargetTicksFor10Furlongs = 237.0; // How many ticks for a standard 10-furlong race

    // Derived base speed (furlongs per tick)
    private const double AverageBaseSpeed = 10.0 / TargetTicksFor10Furlongs; // ≈ 0.0422

    public Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<RacesResult>> GetAll(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<RaceRunResult> Race(byte raceId, Guid horseId, CancellationToken cancellationToken)
    {
        var race = await repository.FirstOrDefaultAsync(new RaceSpecification(raceId), cancellationToken);
        var myHorse = await repository.FirstOrDefaultAsync(new HorseForRaceSpecification(horseId), cancellationToken);

        if (race == null)
        {
            throw new ArgumentException($"Race not found with {raceId}");
        }

        if (myHorse == null)
        {
            throw new ArgumentException($"Horse not found with {horseId}");
        }

        // Fetch CPU horses with similar race experience (owned by Racers)
        var cpuHorseSpec = new SimilarRaceStartsSpecification(
            targetRaceStarts: myHorse.RaceStarts,
            tolerance: 8,
            limit: randomGenerator.Next(7,12));
        var cpuHorses = await repository.ListAsync(cpuHorseSpec, cancellationToken);

        // Combine player's horse with CPU horses
        var horses = new List<Horse> { myHorse };
        horses.AddRange(cpuHorses);

        var raceRun = new RaceRun
        {
            RaceId = raceId,
            Race = race,
            ConditionId = GenerateRandomConditionId(),
            Horses = new List<RaceRunHorse>(),
            RaceRunTicks = new List<RaceRunTick>()
        };
        InitializeHorses(raceRun, horses);

        // Calculate total ticks for the race
        var totalTicks = CalculateTotalTicks(race.Furlongs);
            
        var allHorsesFinished = false;
        short tick = 0;

        // Run the simulation until all horses finish
        while (!allHorsesFinished)
        {
            tick++;

            // Update each horse's position FIRST
            foreach (var horse in raceRun.Horses)
            {
                // Only update horses that haven't finished
                if (horse.Distance < race.Furlongs)
                {
                    var previousDistance = horse.Distance;
                    UpdateHorsePosition(horse, tick, totalTicks, raceRun);

                    // Check if horse just crossed the finish line
                    if (previousDistance < race.Furlongs && horse.Distance >= race.Furlongs)
                    {
                        var distanceToGo = race.Furlongs - previousDistance;
                        var distanceCovered = horse.Distance - previousDistance;

                        if (distanceCovered > 0)
                        {
                            var fractionOfTick = (double)(distanceToGo / distanceCovered);

                            horse.Time = tick - 1 + fractionOfTick;
                        }
                        else
                        {
                            // Fallback if somehow distance didn't change
                            horse.Time = tick;
                        }

                        // NOW cap the distance at finish line
                        horse.Distance = race.Furlongs;
                    }

                    // Feature 007: Handle overtaking and lane changes
                    HandleOvertaking(horse, raceRun, tick, totalTicks);
                }
                //ApplyRandomEvents(horse, tick);
            }

            // THEN create the tick record with the updated positions
            var raceRunTick = new RaceRunTick
            {
                Tick = tick,
                RaceRunTickHorses = new List<RaceRunTickHorse>(),
                Note = "TODO"
            };

            foreach (var raceRunHorse in raceRun.Horses)
            {
                var raceRunTickHorse = new RaceRunTickHorse
                {
                    Horse = raceRunHorse.Horse,
                    HorseId = raceRunHorse.Horse.Id,
                    Lane = raceRunHorse.Lane,
                    Distance = raceRunHorse.Distance,
                    RaceRunTick = raceRunTick
                };
                raceRunTick.RaceRunTickHorses.Add(raceRunTickHorse);
            }

            raceRun.RaceRunTicks.Add(raceRunTick);

            // Check if all horses have finished
            allHorsesFinished = raceRun.Horses.All(horse => horse.Distance >= race.Furlongs);

            // Stop if we've reached a high number of ticks
            if (tick > totalTicks * 2)
            {
                break;
            }
        }

        // Determine winners and rewards
        DetermineRaceResults(raceRun);

        await repository.CreateAsync(raceRun, cancellationToken);

        return new RaceRunResult
        {
            RaceRunId = raceRun.Id,
            RaceId = raceRun.RaceId,
            RaceName = race.Name,
            ConditionId = raceRun.ConditionId,
            ConditionName = raceRun.ConditionId.ToString(),
            TrackId = race.TrackId,
            TrackName = race.Track.Name,
            Furlongs = race.Furlongs,
            SurfaceId = race.SurfaceId,
            SurfaceName = race.Surface.Name,
            HorseResults = raceRun.Horses
                .OrderBy(h => h.Place)
                .Select(h => new RaceRunHorseResult
                {
                    HorseId = h.Horse.Id,
                    HorseName = h.Horse.Name,
                    Place = h.Place,
                    Payout = 0, // Payout is handled by Purse Distribution (sub-feature 2)
                    Time = h.Time
                })
                .ToList()
        };
    }

    private void InitializeHorses(RaceRun raceRun, IEnumerable<Horse> horses)
    {
        var horseList = horses.ToList();
        var fieldSize = horseList.Count;

        // Feature 007: Random lane assignment for fairness
        // Creates shuffled lane numbers: [1, 2, 3, ..., fieldSize] in random order
        var shuffledLanes = Enumerable.Range(1, fieldSize)
            .OrderBy(_ => randomGenerator.Next())
            .ToArray();

        for (int i = 0; i < horseList.Count; i++)
        {
            var raceRunHorse = new RaceRunHorse
            {
                Horse = horseList[i],
                InitialStamina = horseList[i].Stamina,
                CurrentStamina = horseList[i].Stamina,
                Lane = (byte)shuffledLanes[i],          // Random lane assignment
                TicksSinceLastLaneChange = 10           // Start with full cooldown elapsed
            };
            raceRun.Horses.Add(raceRunHorse);
        }
    }

    private void UpdateHorsePosition(RaceRunHorse raceRunHorse, short tick, short totalTicks, RaceRun raceRun)
    {
        var baseSpeed = AverageBaseSpeed;
        var raceProgress = (double)tick / totalTicks;

        var context = new ModifierContext(
            CurrentTick: tick,
            TotalTicks: totalTicks,
            Horse: raceRunHorse.Horse,
            RaceCondition: raceRun.ConditionId,
            RaceSurface: raceRun.Race.SurfaceId,
            RaceFurlongs: raceRun.Race.Furlongs
        );

        // Modifier Pipeline: Stats → Environment → Phase → Stamina → Random
        // Apply stat modifiers (Speed + Agility)
        var statModifier = speedModifierCalculator.CalculateStatModifiers(context);
        baseSpeed *= statModifier;

        // Apply environmental modifiers (Surface + Condition)
        var envModifier = speedModifierCalculator.CalculateEnvironmentalModifiers(context);
        baseSpeed *= envModifier;

        // Apply phase modifiers (LegType timing or conditional bonuses)
        var phaseModifier = speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);
        baseSpeed *= phaseModifier;

        // Feature 004: Apply stamina modifier (speed penalty when stamina low)
        var staminaModifier = speedModifierCalculator.CalculateStaminaModifier(raceRunHorse);
        baseSpeed *= staminaModifier;

        // Apply random variance (±1% per tick)
        var randomVariance = speedModifierCalculator.ApplyRandomVariance();
        baseSpeed *= randomVariance;

        // Calculate current speed after all modifiers
        var currentSpeed = baseSpeed;

        // Safety check: ensure speed is valid (not NaN, Infinity, or negative)
        if (double.IsNaN(currentSpeed) || double.IsInfinity(currentSpeed))
        {
            currentSpeed = 0.001; // Fallback for numerical errors
        }
        else if (currentSpeed < 0)
        {
            currentSpeed = 0; // Horse doesn't move backwards
        }
        // Note: No upper clamp needed - extreme fast speeds are handled by natural physics

        // Update horse position
        raceRunHorse.Distance += (decimal)currentSpeed;

        // Feature 004: Deplete stamina based on effort
        var depletionAmount = staminaCalculator.CalculateDepletionAmount(
            raceRunHorse.Horse,
            raceRun.Race.Furlongs,
            currentSpeed,
            AverageBaseSpeed,
            raceProgress);

        raceRunHorse.CurrentStamina = Math.Max(0, raceRunHorse.CurrentStamina - depletionAmount);
    }

    private static void DetermineRaceResults(RaceRun raceRun)
    {
        // Sort horses by time and assign places
        var sortedHorses = raceRun.Horses.OrderBy(h => h.Time).ToList();
        byte place = 1;
        
        foreach (var horse in sortedHorses)
        {
            horse.Place = place;
            horse.Horse.RaceStarts++;

            // Assign race result IDs and increment win/place/show counters
            switch (place)
            {
                case 1:
                    raceRun.WinHorseId = horse.Horse.Id;
                    horse.Horse.RaceWins++;
                    break;
                case 2:
                    raceRun.PlaceHorseId = horse.Horse.Id;
                    horse.Horse.RacePlace++;
                    break;
                case 3:
                    raceRun.ShowHorseId = horse.Horse.Id;
                    horse.Horse.RaceShow++;
                    break;
            }

            place++;
        }
    }

    private static short CalculateTotalTicks(decimal furlongs)
    {
        // At 0.0422 furlongs/tick (derived from ~38 mph), calculate required ticks
        // This ensures horses can actually complete the race distance
        return (short)Math.Ceiling((double)furlongs / AverageBaseSpeed);
    }

    private ConditionId GenerateRandomConditionId()
    {
        var values = Enum.GetValues(typeof(ConditionId));
        return (ConditionId)values.GetValue(randomGenerator.Next(values.Length))!;
    }

    // ============================================================================
    // Overtaking & Lane Change System (Feature 007 - Phase 1)
    // ============================================================================

    /// <summary>
    /// Calculates the distance threshold for detecting overtaking opportunities.
    /// Combines base threshold with speed factor and race phase multiplier.
    /// </summary>
    /// <param name="horse">The horse attempting to overtake</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    /// <returns>Threshold distance in furlongs</returns>
    private static decimal CalculateOvertakingThreshold(RaceRunHorse horse, short currentTick, short totalTicks)
    {
        var raceProgress = (double)currentTick / totalTicks;

        // Late race aggression: 1.5x threshold in final 25%
        var phaseMultiplier = raceProgress > 0.75
            ? Configuration.RaceModifierConfig.OvertakingLateRaceMultiplier
            : 1.0;

        // Speed influence: faster horses detect from further away
        var speedFactor = 1.0 + (horse.Horse.Speed * Configuration.RaceModifierConfig.OvertakingSpeedFactor);

        return Configuration.RaceModifierConfig.OvertakingBaseThreshold * (decimal)(speedFactor * phaseMultiplier);
    }

    /// <summary>
    /// Checks if a target lane is clear for a lane change.
    /// Uses asymmetric clearance: more space required ahead than behind.
    /// </summary>
    /// <param name="horse">The horse attempting lane change</param>
    /// <param name="targetLane">The lane to check</param>
    /// <param name="raceRun">Current race state</param>
    /// <returns>True if lane is clear, false if blocked</returns>
    private static bool IsLaneClear(RaceRunHorse horse, int targetLane, RaceRun raceRun)
    {
        return !raceRun.Horses.Any(h =>
            h != horse &&
            h.Lane == targetLane &&
            (
                // Horse behind us - prevent cutting off
                (horse.Distance - h.Distance < Configuration.RaceModifierConfig.LaneChangeMinClearanceBehind &&
                 h.Distance < horse.Distance) ||

                // Horse ahead of us - prevent collisions
                (h.Distance - horse.Distance < Configuration.RaceModifierConfig.LaneChangeMinClearanceAhead &&
                 h.Distance > horse.Distance)
            )
        );
    }

    /// <summary>
    /// Determines the desired lane for a horse based on leg type strategy.
    /// Phase 1: Only RailRunner implemented (seeks lane 1).
    /// Phase 2 will add other leg type behaviors.
    /// </summary>
    /// <param name="horse">The race run horse</param>
    /// <returns>Desired lane number (1-based)</returns>
    private static int DetermineDesiredLane(RaceRunHorse horse)
    {
        return horse.Horse.LegTypeId switch
        {
            LegTypeId.RailRunner => 1,  // Always seek the rail
            _ => horse.Lane              // Phase 1: All others stay in current lane
        };
    }

    /// <summary>
    /// Attempts to change lanes toward the desired lane.
    /// Phase 1: Only clean lane changes (adjacent lane, clear path required).
    /// Phase 2 will add risky squeeze plays.
    /// </summary>
    /// <param name="horse">The horse attempting lane change</param>
    /// <param name="raceRun">Current race state</param>
    /// <returns>True if lane changed, false otherwise</returns>
    private bool AttemptLaneChange(RaceRunHorse horse, RaceRun raceRun)
    {
        var desiredLane = DetermineDesiredLane(horse);

        // Already in desired lane
        if (horse.Lane == desiredLane)
            return false;

        // Move one lane at a time (gradual drift)
        var targetLane = horse.Lane < desiredLane
            ? horse.Lane + 1  // Move right
            : horse.Lane - 1; // Move left

        // Check if target lane is clear
        if (IsLaneClear(horse, targetLane, raceRun))
        {
            horse.Lane = (byte)targetLane;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles overtaking detection and lane change logic for a horse.
    /// Called once per tick per horse during race simulation.
    /// Phase 1: Basic overtaking detection + RailRunner positioning.
    /// </summary>
    /// <param name="horse">The horse being updated</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    private void HandleOvertaking(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
    {
        // Increment cooldown timer
        horse.TicksSinceLastLaneChange++;

        // Calculate agility-based cooldown requirement
        var requiredCooldown = Configuration.RaceModifierConfig.BaseLaneChangeCooldown -
                              (horse.Horse.Agility * Configuration.RaceModifierConfig.AgilityCooldownReduction);

        // Check if cooldown allows lane change attempt
        if (horse.TicksSinceLastLaneChange < requiredCooldown)
            return;

        // Determine if we want to change lanes
        var desiredLane = DetermineDesiredLane(horse);
        var wantsToChangeLanes = horse.Lane != desiredLane;

        // Check if we want to overtake (detect horse ahead within threshold)
        var overtakingThreshold = CalculateOvertakingThreshold(horse, currentTick, totalTicks);
        var wantsToOvertake = raceRun.Horses.Any(h =>
            h != horse &&
            h.Lane == horse.Lane &&
            h.Distance > horse.Distance &&
            h.Distance - horse.Distance < overtakingThreshold);

        // Attempt lane change if either condition met
        if (wantsToChangeLanes || wantsToOvertake)
        {
            // Consume cooldown regardless of success (commitment cost)
            horse.TicksSinceLastLaneChange = 0;

            AttemptLaneChange(horse, raceRun);
        }
    }
}
