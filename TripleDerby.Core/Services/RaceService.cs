using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
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
    IStaminaCalculator staminaCalculator,
    IRaceCommentaryGenerator commentaryGenerator) : IRaceService
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
        var horses = new HashSet<Horse> { myHorse };
        horses.UnionWith(cpuHorses);

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

        // Feature 008: Track previous state for event detection
        var previousPositions = new Dictionary<Guid, int>();
        var previousLanes = new Dictionary<Guid, byte>();
        Guid? previousLeader = null;
        var recentPositionChanges = new Dictionary<Guid, short>(); // Track last tick each horse had a position change
        var recentLaneChanges = new Dictionary<Guid, short>(); // Track last tick each horse had a lane change

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

                        // Assign place based on finish order (Feature 008)
                        var finishedCount = raceRun.Horses.Count(h => h.Distance >= race.Furlongs);
                        horse.Place = (byte)finishedCount;

                        // NOW cap the distance at finish line
                        horse.Distance = race.Furlongs;
                    }

                    // Feature 007: Handle overtaking and lane changes
                    HandleOvertaking(horse, raceRun, tick, totalTicks);
                }
                //ApplyRandomEvents(horse, tick);
            }

            // Feature 008: Detect events and generate commentary
            var events = DetectEvents(tick, totalTicks, raceRun, previousPositions, previousLanes, previousLeader, recentPositionChanges, recentLaneChanges);
            var commentary = commentaryGenerator.GenerateCommentary(events, tick, raceRun);

            // THEN create the tick record with the updated positions
            var raceRunTick = new RaceRunTick
            {
                Tick = tick,
                RaceRunTickHorses = new List<RaceRunTickHorse>(),
                Note = commentary
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

            // Feature 008: Update previous state for next tick
            UpdatePreviousState(raceRun, previousPositions, previousLanes, ref previousLeader);

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
            PlayByPlay = raceRun.RaceRunTicks
                .Where(t => !string.IsNullOrEmpty(t.Note))
                .Select(t => t.Note)
                .ToList(),
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

        // Feature 007: Apply risky lane change penalty (if active)
        if (raceRunHorse.SpeedPenaltyTicksRemaining > 0)
        {
            baseSpeed *= RaceModifierConfig.RiskyLaneChangeSpeedPenalty;
            raceRunHorse.SpeedPenaltyTicksRemaining--;
        }

        // Feature 007: Apply traffic response effects (speed capping / frustration)
        ApplyTrafficEffects(raceRunHorse, raceRun, ref baseSpeed);

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
            ? RaceModifierConfig.OvertakingLateRaceMultiplier
            : 1.0;

        // Speed influence: faster horses detect from further away
        var speedFactor = 1.0 + (horse.Horse.Speed * RaceModifierConfig.OvertakingSpeedFactor);

        return RaceModifierConfig.OvertakingBaseThreshold * (decimal)(speedFactor * phaseMultiplier);
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
                (horse.Distance - h.Distance < RaceModifierConfig.LaneChangeMinClearanceBehind &&
                 h.Distance < horse.Distance) ||

                // Horse ahead of us - prevent collisions
                (h.Distance - horse.Distance < RaceModifierConfig.LaneChangeMinClearanceAhead &&
                 h.Distance > horse.Distance)
            )
        );
    }

    /// <summary>
    /// Determines the desired lane for a horse based on leg type strategy.
    /// Phase 2: All leg types implemented with distinct personalities.
    /// </summary>
    /// <param name="horse">The race run horse</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    /// <returns>Desired lane number (1-based)</returns>
    private int DetermineDesiredLane(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
    {
        var raceProgress = (double)currentTick / totalTicks;

        return horse.Horse.LegTypeId switch
        {
            // Always seek rail position for bonus
            LegTypeId.RailRunner => 1,

            // Conservative leader - stays in current lane
            LegTypeId.FrontRunner => horse.Lane,

            // Traffic avoider - seeks least congested lane
            LegTypeId.StartDash => FindLeastCongestedLane(horse, raceRun),

            // Patient early, aggressive late (>75%)
            LegTypeId.LastSpurt => raceProgress > 0.75
                ? FindBestOvertakingLane(horse, raceRun, currentTick, totalTicks)
                : horse.Lane,

            // Prefers center lanes 4-5, drifts toward center if outside
            LegTypeId.StretchRunner => horse.Lane switch
            {
                <= 3 => horse.Lane + 1,  // Drift right toward center
                >= 6 => horse.Lane - 1,  // Drift left toward center
                _ => horse.Lane           // Already in center (4-5), stay
            },

            _ => horse.Lane
        };
    }

    /// <summary>
    /// Finds the least congested lane ahead of the horse.
    /// Used by StartDash to avoid traffic.
    /// </summary>
    /// <param name="horse">The horse seeking a clear lane</param>
    /// <param name="raceRun">Current race state</param>
    /// <returns>Lane number with least traffic ahead (1-based)</returns>
    private static int FindLeastCongestedLane(RaceRunHorse horse, RaceRun raceRun)
    {
        var maxLane = raceRun.Horses.Count; // Dynamic max lane = field size
        var laneTraffic = new int[maxLane + 1]; // Index 0 unused, lanes 1 to maxLane

        // Count horses ahead in each lane within look-ahead distance
        var lookAhead = RaceModifierConfig.StartDashLookAheadDistance;
        foreach (var h in raceRun.Horses.Where(h =>
            h.Distance > horse.Distance &&
            h.Distance - horse.Distance < lookAhead))
        {
            laneTraffic[h.Lane]++;
        }

        // Find lane with minimum traffic (prefer current lane if tied)
        var minTraffic = laneTraffic[horse.Lane];
        var bestLane = (int)horse.Lane;

        for (int lane = 1; lane <= maxLane; lane++)
        {
            if (laneTraffic[lane] < minTraffic)
            {
                minTraffic = laneTraffic[lane];
                bestLane = lane;
            }
        }

        return bestLane;
    }

    /// <summary>
    /// Finds the lane with most overtaking opportunities.
    /// Used by LastSpurt in late race (>75%) to hunt for passes.
    /// </summary>
    /// <param name="horse">The horse hunting for overtaking opportunities</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    /// <returns>Lane number with most overtaking opportunities (1-based)</returns>
    private int FindBestOvertakingLane(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
    {
        var maxLane = raceRun.Horses.Count;
        var opportunities = new int[maxLane + 1];

        // Count overtaking opportunities in each lane
        var overtakingRange = CalculateOvertakingThreshold(horse, currentTick, totalTicks);
        foreach (var h in raceRun.Horses.Where(h =>
            h.Distance > horse.Distance &&
            h.Distance - horse.Distance < overtakingRange))
        {
            opportunities[h.Lane]++;
        }

        // Find lane with most opportunities (prefer current lane if tied)
        var maxOpportunities = opportunities[horse.Lane];
        var bestLane = (int)horse.Lane;

        for (int lane = 1; lane <= maxLane; lane++)
        {
            if (opportunities[lane] > maxOpportunities)
            {
                maxOpportunities = opportunities[lane];
                bestLane = lane;
            }
        }

        return bestLane;
    }

    /// <summary>
    /// Attempts to change lanes toward the desired lane.
    /// Phase 2: Includes risky squeeze plays when clean change not possible.
    /// </summary>
    /// <param name="horse">The horse attempting lane change</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    /// <returns>True if lane changed, false otherwise</returns>
    private bool AttemptLaneChange(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
    {
        var desiredLane = DetermineDesiredLane(horse, raceRun, currentTick, totalTicks);

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
            // Clean lane change - no penalty
            horse.Lane = (byte)targetLane;
            return true;
        }

        // Lane blocked - attempt risky squeeze play
        return AttemptRiskySqueezePlay(horse, targetLane);
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
        var requiredCooldown = RaceModifierConfig.BaseLaneChangeCooldown -
                              (horse.Horse.Agility * RaceModifierConfig.AgilityCooldownReduction);

        // Check if cooldown allows lane change attempt
        if (horse.TicksSinceLastLaneChange < requiredCooldown)
            return;

        // Determine if we want to change lanes
        var desiredLane = DetermineDesiredLane(horse, raceRun, currentTick, totalTicks);
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

            AttemptLaneChange(horse, raceRun, currentTick, totalTicks);
        }
    }

    /// <summary>
    /// Attempts a risky lane change when the target lane is blocked.
    /// Success probability based on agility, with durability-based penalty on success.
    /// </summary>
    /// <param name="horse">The horse attempting risky change</param>
    /// <param name="targetLane">The lane to squeeze into</param>
    /// <returns>True if successful, false if failed</returns>
    private bool AttemptRiskySqueezePlay(RaceRunHorse horse, int targetLane)
    {
        // Calculate success probability from agility (0% to 50%)
        var squeezeSuccessChance = horse.Horse.Agility / RaceModifierConfig.RiskySqueezeAgilityDivisor;

        if (randomGenerator.NextDouble() < squeezeSuccessChance)
        {
            // Success! Thread the needle
            horse.Lane = (byte)targetLane;

            // Apply durability-based penalty
            var penaltyTicks = RaceModifierConfig.RiskyLaneChangePenaltyBaseTicks -
                              (horse.Horse.Durability * RaceModifierConfig.RiskyLaneChangePenaltyReduction);
            horse.SpeedPenaltyTicksRemaining = (byte)Math.Max(1, Math.Round(penaltyTicks));

            return true;
        }

        // Failed - stay in current lane, cooldown already consumed
        return false;
    }

    // ============================================================================
    // Traffic Response System (Feature 007 - Phase 2)
    // ============================================================================

    /// <summary>
    /// Applies leg-type-specific traffic response effects when horse is blocked.
    /// Modifies speed based on traffic ahead and horse's personality.
    /// </summary>
    /// <param name="horse">The horse being affected</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="currentSpeed">Current speed to modify (passed by reference)</param>
    private void ApplyTrafficEffects(RaceRunHorse horse, RaceRun raceRun, ref double currentSpeed)
    {
        // Find horse ahead in same lane within blocking distance
        var horseAhead = FindHorseAheadInLane(horse, raceRun);

        if (horseAhead == null)
            return; // No traffic ahead, no effect

        // Apply leg-type-specific response
        switch (horse.Horse.LegTypeId)
        {
            case LegTypeId.FrontRunner:
                // Frustration penalty when blocked with no clear lanes
                if (!HasClearLaneAvailable(horse, raceRun))
                {
                    currentSpeed *= (1.0 - RaceModifierConfig.FrontRunnerFrustrationPenalty);
                }
                break;

            case LegTypeId.StartDash:
                // Speed cap: match leader minus penalty
                var startDashCap = CalculateHorseSpeed(horseAhead) *
                                  (1.0 - RaceModifierConfig.StartDashSpeedCapPenalty);
                if (currentSpeed > startDashCap)
                    currentSpeed = startDashCap;
                break;

            case LegTypeId.LastSpurt:
                // Patient: minimal speed cap, no frustration
                var lastSpurtCap = CalculateHorseSpeed(horseAhead) *
                                  (1.0 - RaceModifierConfig.LastSpurtSpeedCapPenalty);
                if (currentSpeed > lastSpurtCap)
                    currentSpeed = lastSpurtCap;
                break;

            case LegTypeId.StretchRunner:
                // Speed cap: match leader minus penalty
                var stretchCap = CalculateHorseSpeed(horseAhead) *
                                (1.0 - RaceModifierConfig.StretchRunnerSpeedCapPenalty);
                if (currentSpeed > stretchCap)
                    currentSpeed = stretchCap;
                break;

            case LegTypeId.RailRunner:
                // Extra cautious on rail: higher speed cap penalty
                var railCap = CalculateHorseSpeed(horseAhead) *
                             (1.0 - RaceModifierConfig.RailRunnerSpeedCapPenalty);
                if (currentSpeed > railCap)
                    currentSpeed = railCap;
                break;
        }
    }

    /// <summary>
    /// Finds the horse directly ahead in the same lane within blocking distance.
    /// </summary>
    /// <param name="horse">The horse to check</param>
    /// <param name="raceRun">Current race state</param>
    /// <returns>Horse ahead if found, null otherwise</returns>
    private static RaceRunHorse? FindHorseAheadInLane(RaceRunHorse horse, RaceRun raceRun)
    {
        return raceRun.Horses
            .Where(h =>
                h != horse &&
                h.Lane == horse.Lane &&
                h.Distance > horse.Distance &&
                h.Distance - horse.Distance < RaceModifierConfig.TrafficBlockingDistance)
            .OrderBy(h => h.Distance) // Closest horse ahead
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if any adjacent lane is clear for lane change.
    /// Used by FrontRunner to determine frustration (frustrated when boxed in).
    /// </summary>
    /// <param name="horse">The horse checking for clear lanes</param>
    /// <param name="raceRun">Current race state</param>
    /// <returns>True if at least one adjacent lane is clear, false if boxed in</returns>
    private static bool HasClearLaneAvailable(RaceRunHorse horse, RaceRun raceRun)
    {
        var maxLane = raceRun.Horses.Count;

        // Check lane to the left (if exists)
        if (horse.Lane > 1 && IsLaneClear(horse, horse.Lane - 1, raceRun))
            return true;

        // Check lane to the right (if exists)
        if (horse.Lane < maxLane && IsLaneClear(horse, horse.Lane + 1, raceRun))
            return true;

        return false; // Boxed in
    }

    /// <summary>
    /// Estimates the current speed of another horse based on current distance delta.
    /// Used for traffic speed capping calculations.
    /// </summary>
    /// <param name="horse">The horse to estimate speed for</param>
    /// <returns>Estimated speed in furlongs per tick</returns>
    private static double CalculateHorseSpeed(RaceRunHorse horse)
    {
        // Approximate speed based on average base speed
        // In reality this would track per-tick movement, but this is sufficient for traffic response
        return AverageBaseSpeed;
    }

    // ============================================================================
    // Play-by-Play Commentary System (Feature 008)
    // ============================================================================

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
    private static TickEvents DetectEvents(
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
                    events.PhotoFinish = new PhotoFinish(
                        top2[0].Horse.Name,
                        top2[1].Horse.Name,
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
    /// <param name="raceRun">Current race state</param>
    /// <param name="previousPositions">Dictionary to update with current positions</param>
    /// <param name="previousLanes">Dictionary to update with current lanes</param>
    /// <param name="previousLeader">Reference to update with current leader</param>
    private static void UpdatePreviousState(
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
