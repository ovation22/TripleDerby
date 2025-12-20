using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Services;

public class RaceService(ITripleDerbyRepository repository, IRandomGenerator randomGenerator) : IRaceService
{
    // Phase 2: Speed Modifier Calculator
    private readonly SpeedModifierCalculator _speedModifierCalculator = new(randomGenerator);

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

        // Initialization phase
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
        byte tick = 0;

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
                        // Calculate the exact fractional tick when they crossed
                        // BEFORE we cap the distance
                        var distanceToGo = race.Furlongs - previousDistance;
                        var distanceCovered = horse.Distance - previousDistance;

                        if (distanceCovered > 0)
                        {
                            var fractionOfTick = (double)(distanceToGo / distanceCovered);

                            // Store the precise finish time
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
        //DistributePurse(raceRun);

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

    private static void InitializeHorses(RaceRun raceRun, IEnumerable<Horse> horses)
    {
        byte lane = 1;
        foreach (var horse in horses)
        {
            var raceRunHorse = new RaceRunHorse
            {
                Horse = horse,
                InitialStamina = horse.Stamina,
                CurrentStamina = horse.Stamina,
                Lane = lane++
            };
            raceRun.Horses.Add(raceRunHorse);
        }
    }

    private void UpdateHorsePosition(RaceRunHorse raceRunHorse, byte tick, int totalTicks, RaceRun raceRun)
    {
        var baseSpeed = AverageBaseSpeed;

        // Phase 2 & 3: Apply modifiers using new calculator
        var context = new ModifierContext(
            CurrentTick: tick,
            TotalTicks: totalTicks,
            Horse: raceRunHorse.Horse,
            RaceCondition: raceRun.ConditionId,
            RaceSurface: raceRun.Race.SurfaceId,
            RaceFurlongs: raceRun.Race.Furlongs
        );

        // Apply stat modifiers (Speed + Agility)
        baseSpeed *= _speedModifierCalculator.CalculateStatModifiers(context);

        // Phase 3: Apply environmental modifiers (Surface + Condition)
        baseSpeed *= _speedModifierCalculator.CalculateEnvironmentalModifiers(context);

        // Adjust speed dynamically during the race based on leg type (Phase 4 will replace this)
        baseSpeed = AdjustSpeedForLegTypeDuringRace(baseSpeed, tick, totalTicks, raceRunHorse.Horse.LegTypeId);

        // Apply random performance fluctuations (Phase 5 will replace this)
        baseSpeed = ApplyRandomPerformanceFluctuations(baseSpeed);

        //var staminaModifier = GetStaminaModifierForCondition(raceRun.ConditionId);
        //staminaModifier *= GetStaminaModifierForLaneAndLegType(raceRunHorse.Lane);
        //staminaModifier *= GetStaminaModifierForSurface(raceRun.Race.SurfaceId);
        //staminaModifier *= GetDurabilityModifier(raceRunHorse.Horse.Durability);
        //staminaModifier *= GetHappinessStaminaModifier(raceRunHorse.Horse.Happiness);

        // Factor in stamina depletion - PASS RACE DISTANCE
        /*var staminaConsumption = GetStaminaConsumption(
            staminaModifier,
            raceRun.ConditionId,
            raceRunHorse.Horse.LegTypeId,
            raceRun.Race.Furlongs,
            totalTicks);*/

        //raceRunHorse.CurrentStamina -= staminaConsumption;

        // Ensure stamina doesn't go below zero
        /*if (raceRunHorse.CurrentStamina < 0)
        {
            raceRunHorse.CurrentStamina = 0;
        }*/

        // Adjust speed based on remaining stamina
        //baseSpeed = ApplyStaminaEffect(baseSpeed, raceRunHorse);

        // Apply traffic interference
        //baseSpeed = ApplyTrafficInterference(baseSpeed, raceRunHorse, raceRun);

        // Apply random incidents
        //baseSpeed = ApplyRandomIncidents(baseSpeed, tick, raceRunHorse, totalTicks);

        // Update horse position
        raceRunHorse.Distance += (decimal)baseSpeed;

        // Handle overtaking or lane changing logic
        //HandleOvertaking(raceRunHorse, raceRun);
    }

    private double GetStaminaConsumption(
        double staminaModifier,
        ConditionId conditionId,
        LegTypeId legTypeId,
        decimal raceFurlongs,
        int totalTicks)
    {
        // Base stamina consumption is now scaled by race distance
        // Target: Use 40% of stamina for a 10-furlong race over 150 ticks (reduced from 70%)
        const decimal standardDistance = 10m;
        const int standardTicks = 150;
        const double targetStaminaUsage = 0.4; // Use 40% of total stamina (REDUCED)

        // Calculate per-tick consumption adjusted for this race's distance
        var distanceScalingFactor = (double)(raceFurlongs / standardDistance);
        var tickScalingFactor = (double)totalTicks / standardTicks;

        // Base consumption per tick (normalized for standard race)
        var baseConsumption = (targetStaminaUsage / standardTicks) * distanceScalingFactor / tickScalingFactor;

        // Modify based on race conditions
        var conditionModifier = conditionId switch
        {
            ConditionId.Fast => 0.97,        // Reduced from 0.95
            ConditionId.WetFast => 0.99,     // Reduced from 0.98
            ConditionId.Good => 1.0,
            ConditionId.Muddy => 1.02,       // Reduced from 1.05
            ConditionId.Sloppy => 1.03,      // Reduced from 1.07
            ConditionId.Frozen => 1.04,      // Reduced from 1.1
            ConditionId.Slow => 1.06,        // Reduced from 1.15
            ConditionId.Heavy => 1.08,       // Reduced from 1.2
            ConditionId.Firm => 0.99,        // Reduced from 0.98
            ConditionId.Soft => 1.01,        // Reduced from 1.03
            ConditionId.Yielding => 1.01,    // Reduced from 1.02
            _ => 1.0
        };

        // Modify based on leg type (some run harder early/late)
        var legTypeModifier = legTypeId switch
        {
            LegTypeId.StartDash => 1.02,    // Burns stamina early
            LegTypeId.LastSpurt => 1.02,    // Burns stamina late
            LegTypeId.FrontRunner => 1.01,  // Slightly more effort
            LegTypeId.StretchRunner => 1.0, // Balanced
            LegTypeId.RailRunner => 0.99,   // Efficient
            _ => 1.0
        };

        // Combine all modifiers
        var totalConsumption = baseConsumption * staminaModifier * conditionModifier * legTypeModifier;

        var randomFactor = 1 + (randomGenerator.NextDouble() * 0.06 - 0.03); // ±3% 
        totalConsumption *= randomFactor;

        return totalConsumption;
    }

    private static double ApplyStaminaEffect(double baseSpeed, RaceRunHorse horse)
    {
        var staminaPercent = horse.CurrentStamina / horse.InitialStamina;

        if (staminaPercent < 0.1)
        {
            return baseSpeed * 0.95; // Minor 5% penalty only when nearly exhausted
        }
        if (staminaPercent < 0.25)
        {
            // Mild penalty
            var penalty = 1.0 - ((0.5 - staminaPercent) * 0.32);
            return baseSpeed * penalty;
        }
        
        return baseSpeed; // No penalty otherwise
    }

    /*
    private static double ApplyStaminaEffect(double baseSpeed, RaceRunHorse horse)
    {
        // Calculate stamina percentage remaining
        var staminaPercent = horse.CurrentStamina / horse.InitialStamina;

        // Apply speed penalty as stamina depletes
        // No penalty above 50% stamina, then gradual decline
        if (staminaPercent > 0.5)
        {
            return baseSpeed; // No penalty
        }
        else if (staminaPercent > 0.25)
        {
            // Mild penalty: 50% stamina = 98% speed, 25% stamina = 90% speed
            var penalty = 1.0 - ((0.5 - staminaPercent) * 0.32); // 0.98 to 0.90
            return baseSpeed * penalty;
        }
        else if (staminaPercent > 0)
        {
            // Severe penalty: 25% stamina = 90% speed, 0% stamina = 70% speed
            var penalty = 0.9 - ((0.25 - staminaPercent) * 0.8); // 0.90 to 0.70
            return baseSpeed * penalty;
        }
        else
        {
            // Exhausted: 70% speed
            return baseSpeed * 0.7;
        }
    }*/

    [Obsolete("Replaced by SpeedModifierCalculator.CalculateStatModifiers in Phase 2. Will be removed in Phase 6.")]
    private static double ApplySpeedModifier(double baseSpeed, int speedActual)
    {
        // Use a percentage-based scaling factor for speed
        var speedModifier = 1 + ((speedActual - 50) / 1000.0); // Adjust scaling factor as needed
        return baseSpeed * speedModifier;
    }

    [Obsolete("Disabled per race-modifiers-refactor spec. Will be removed in Phase 6.")]
    private static double GetHappinessModifier(int happiness)
    {
        // Logarithmic scaling with very subtle effect
        // Uses natural log to create diminishing returns curve
        // Neutral point is at happiness = 50

        // Clamp happiness to valid range [0, 100]
        happiness = Math.Clamp(happiness, 0, 100);

        // Normalize happiness to range around 0 (shift 50 to 0)
        var normalizedHappiness = happiness - 50.0;

        // Apply logarithmic scaling with very small coefficient
        // Log provides diminishing returns: big changes at low happiness, smaller at high
        if (normalizedHappiness == 0)
        {
            return 1.0; // Neutral, no effect
        }

        var logEffect = Math.Sign(normalizedHappiness) * Math.Log(1.0 + Math.Abs(normalizedHappiness)) / 5000.0;

        return 1.0 + logEffect;
    }

    private static double GetHappinessStaminaModifier(int happiness)
    {
        // Logarithmic scaling with very subtle effect on stamina consumption
        // Uses natural log to create diminishing returns curve
        // Neutral point is at happiness = 50

        // Clamp happiness to valid range [0, 100]
        happiness = Math.Clamp(happiness, 0, 100);

        // Normalize happiness to range around 0 (shift 50 to 0)
        var normalizedHappiness = happiness - 50.0;

        // Apply logarithmic scaling with inverted effect (higher happiness = less stamina consumption)
        // Note: Sign is inverted compared to speed modifier
        if (normalizedHappiness == 0)
        {
            return 1.0; // Neutral, no effect
        }

        var logEffect = -Math.Sign(normalizedHappiness) * Math.Log(1.0 + Math.Abs(normalizedHappiness)) / 5000.0;

        return 1.0 + logEffect;
    }

    [Obsolete("Replaced by SpeedModifierCalculator.CalculateEnvironmentalModifiers in Phase 3. Will be removed in Phase 6.")]
    private static double AdjustSpeedForCondition(double baseSpeed, ConditionId conditionId)
    {
        return conditionId switch
        {
            ConditionId.Fast => baseSpeed * 1.02, 
            ConditionId.WetFast => baseSpeed * 1.01,
            ConditionId.Good => baseSpeed,
            ConditionId.Muddy => baseSpeed * 0.99,
            ConditionId.Sloppy => baseSpeed * 0.98,
            ConditionId.Frozen => baseSpeed * 0.97,
            ConditionId.Slow => baseSpeed * 0.96,
            ConditionId.Heavy => baseSpeed * 0.95,
            ConditionId.Firm => baseSpeed * 1.01,
            ConditionId.Soft => baseSpeed * 0.99,
            ConditionId.Yielding => baseSpeed * 0.99,
            _ => baseSpeed
        };
    }

    private static double GetStaminaModifierForCondition(ConditionId conditionId)
    {
        return conditionId switch
        {
            ConditionId.Fast => 1.00,
            ConditionId.WetFast => 1.01,
            ConditionId.Muddy => 1.05,
            ConditionId.Sloppy => 1.07,
            ConditionId.Frozen => 1.08,
            ConditionId.Slow => 1.10,
            ConditionId.Heavy => 1.12,
            ConditionId.Soft => 1.03,
            ConditionId.Yielding => 1.04,
            _ => 1.00
        };
    }

    [Obsolete("Replaced by SpeedModifierCalculator.CalculateEnvironmentalModifiers in Phase 3. Lane position no longer affects speed. Will be removed in Phase 6.")]
    private double AdjustSpeedForLaneAndLegType(double baseSpeed, int lane, LegTypeId legTypeId)
    {
        var r = randomGenerator.NextDouble() * 0.0002 - 0.0001; // ±0.0001 variance

        return legTypeId switch
        {
            LegTypeId.FrontRunner => lane <= 3 ? baseSpeed * (1.005 + r) : baseSpeed * (0.995 + r),
            LegTypeId.StartDash => lane <= 3 ? baseSpeed * (1.003 + r) : baseSpeed * (0.997 + r),
            LegTypeId.LastSpurt => lane > 6 ? baseSpeed * (1.004 + r) : baseSpeed * (0.996 + r),
            LegTypeId.StretchRunner => lane > 6 ? baseSpeed * (1.006 + r) : baseSpeed * (0.994 + r),
            LegTypeId.RailRunner => lane == 1 ? baseSpeed * (1.008 + r) : baseSpeed * (0.992 + r),
            _ => baseSpeed
        };
    }

    private static double GetStaminaModifierForLaneAndLegType(int lane)
    {
        return (lane <= 3) ? 1.02 : 1.00; // Inner lanes might cause faster stamina drain
    }

    private double AdjustSpeedForLegTypeDuringRace(double baseSpeed, int currentTick, int totalTicks, LegTypeId legTypeId)
    {
        var modifier = 1.0;

        switch (legTypeId)
        {
            case LegTypeId.StartDash:
                if (currentTick < totalTicks * 0.25)
                {
                    modifier = 1.02; // Boost early in the race
                }
                break;

            case LegTypeId.LastSpurt:
                if (currentTick > totalTicks * 0.75)
                {
                    modifier = 1.02; // Boost late in the race
                }
                break;

            case LegTypeId.StretchRunner:
                if (currentTick > totalTicks * 0.4 && currentTick < totalTicks * 0.6)
                {
                    modifier = 1.015; // Boost in the middle of the race
                }
                break;

            case LegTypeId.FrontRunner:
                if (currentTick < totalTicks * 0.2)
                {
                    modifier = 1.015; // Boost early in the race
                }
                break;

            case LegTypeId.RailRunner:
                if (currentTick > totalTicks * 0.7)
                {
                    modifier = 1.01; // Boost late in the race
                }
                break;
        }

        // Optionally, add a random late race boost
        if (currentTick > totalTicks * 0.8)
        {
            modifier *= (1 + (randomGenerator.NextDouble() * 0.01));
        }

        return baseSpeed * modifier;
    }

    [Obsolete("Replaced by SpeedModifierCalculator.CalculateStatModifiers in Phase 2. Will be removed in Phase 6.")]
    private static double GetAgilityModifier(int agility)
    {
        return 1 + ((agility - 50) / 5000.0); // Agility boosts speed by a very small percentage (max ±1%)
    }

    private static double GetDurabilityModifier(int durability)
    {
        return 1 - ((100 - durability) / 100.0); // Durability affects stamina drain; higher durability means less stamina loss
    }

    private double ApplyRandomPerformanceFluctuations(double baseSpeed)
    {
        var fluctuation = randomGenerator.NextDouble() * 0.02 - 0.01; // ±1% random fluctuation
        return baseSpeed * (1 + fluctuation);
    }

    private double ApplyTrafficInterference(double baseSpeed, RaceRunHorse horse, RaceRun raceRun)
    {
        const decimal proximityThreshold = 0.2m; // furlongs
    
        // Count nearby horses in adjacent lanes
        var nearbyHorses = raceRun.Horses.Count(h => 
            h != horse &&
            Math.Abs(h.Lane - horse.Lane) <= 1 && // Adjacent lanes
            Math.Abs(h.Distance - horse.Distance) < proximityThreshold);
    
        // Apply penalty: 1-2% per nearby horse
        var trafficPenalty = nearbyHorses * 0.015;
        return baseSpeed * (1 - trafficPenalty);
    }

    /*
    private byte GetBaseStaminaConsumption(ConditionId conditionId, LegTypeId legTypeId)
    {
        // Base stamina consumption factor
        var baseConsumption = 1.0;

        // Modify base consumption based on race conditions
        var conditionModifier = conditionId switch
        {
            ConditionId.Fast => 0.95,
            ConditionId.WetFast => 0.98,
            ConditionId.Good => 1.0,
            ConditionId.Muddy => 1.05,
            ConditionId.Sloppy => 1.07,
            ConditionId.Frozen => 1.1,
            ConditionId.Slow => 1.15,
            ConditionId.Heavy => 1.2,
            ConditionId.Firm => 0.98,
            ConditionId.Soft => 1.03,
            ConditionId.Yielding => 1.02,
            _ => 1.0
        };

        // Modify based on leg type
        var legTypeModifier = legTypeId switch
        {
            LegTypeId.StartDash => 0.9,    // Less stamina drain in the start dash phase
            LegTypeId.LastSpurt => 1.1,    // More stamina drain in the last spurt phase
            LegTypeId.FrontRunner => 1.0,  // Neutral
            LegTypeId.StretchRunner => 1.05, // Slightly increased consumption
            LegTypeId.RailRunner => 0.95,  // Slightly reduced consumption
            _ => 1.0
        };

        // Combine the modifiers
        baseConsumption *= conditionModifier * legTypeModifier;

        // Introduce a small random fluctuation to account for unpredictable factors
        var randomFluctuation = 1 + (randomGenerator.NextDouble() * 0.1 - 0.05); // ±5% random fluctuation
        baseConsumption *= randomFluctuation;

        // Ensure the consumption factor is not less than a certain threshold to avoid negative stamina values
        baseConsumption = Math.Max(baseConsumption, 0.5);

        // Convert to byte
        return Convert.ToByte(Math.Min(baseConsumption, 255)); // Ensure it fits within byte range
    }
    */

    /*private void ApplyInRaceEvents(RaceRun raceRun, int tick)
    {
        // Example: if two horses are too close, they might slow each other down
        // Iterate over all horses and apply effects based on proximity
    }*/

    private void HandleOvertaking(RaceRunHorse raceRunHorse, RaceRun raceRun)
    {
        // Sort horses by their current distance, in descending order
        var sortedHorses = raceRun.Horses.OrderByDescending(h => h.Distance).ToList();

        // Find the position of the current horse in the sorted list
        var horsePosition = sortedHorses.IndexOf(raceRunHorse);

        if (horsePosition > 0)
        {
            // Check if the horse is close enough to the horse in front
            var horseInFront = sortedHorses[horsePosition - 1];

            // Example threshold for overtaking, considering LegType
            var overtakingThreshold = raceRunHorse.Distance + (decimal)(raceRunHorse.Horse.Speed * 0.5);

            if (horseInFront.Distance < overtakingThreshold)
            {
                // Attempt to overtake and change lane if possible
                AttemptLaneChange(raceRunHorse, raceRun);
            }
        }
    }

    private void AttemptLaneChange(RaceRunHorse horse, RaceRun raceRun)
    {
        int newLane = horse.Lane;
        var changeProbability = horse.Horse.LegTypeId switch
        {
            LegTypeId.FrontRunner => 0.3,
            LegTypeId.StartDash => 0.6,
            LegTypeId.LastSpurt => 0.4,
            LegTypeId.StretchRunner => 0.5,
            LegTypeId.RailRunner => 0.2,
            _ => 0.5
        };

        if (randomGenerator.NextDouble() < changeProbability)
        {
            if (horse.Lane > 1 && randomGenerator.NextDouble() < 0.5)
            {
                newLane--; // Move to the left
            }
            else if (horse.Lane < 8 && randomGenerator.NextDouble() < 0.5)
            {
                newLane++; // Move to the right
            }
        }

        // Check if the lane is clear (no horse ahead in that lane)
        if (newLane != horse.Lane && IsLaneClear(horse, newLane, raceRun))
        {
            horse.Lane = (byte)newLane;
        }
    }

    private static bool IsLaneClear(RaceRunHorse horse, int newLane, RaceRun raceRun)
    {
        const decimal lateralBlockingDistance = 0.1m; // furlongs
    
        // Check for horses ahead OR alongside in the target lane
        return !raceRun.Horses.Any(h => 
            h != horse && 
            h.Lane == newLane && 
            Math.Abs(h.Distance - horse.Distance) < lateralBlockingDistance);
    }

    /*
    private static void ApplyRandomEvents(RaceRunHorse horse, int tick)
    {
        // Implement random events affecting race dynamics.
    }
    */

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

    /*
    private static void DistributePurse(RaceRun raceRun)
    {
        // Distribute prize money based on final standings.
    }
    */

    private static int CalculateTotalTicks(decimal furlongs)
    {
        // At 0.0422 furlongs/tick (derived from ~38 mph), calculate required ticks
        // This ensures horses can actually complete the race distance
        return (int)Math.Ceiling((double)furlongs / AverageBaseSpeed);
    }

    [Obsolete("Replaced by SpeedModifierCalculator.CalculateEnvironmentalModifiers in Phase 3. Will be removed in Phase 6.")]
    private static double AdjustSpeedForSurface(double baseSpeed, SurfaceId surfaceId)
    {
        return surfaceId switch
        {
            SurfaceId.Dirt => baseSpeed * 0.98, // Slightly slower on dirt
            SurfaceId.Turf => baseSpeed * 1.02, // Slightly faster on turf
            SurfaceId.Artificial => baseSpeed * 1.03, // Slightly faster on artificial surfaces
            _ => baseSpeed
        };
    }

    private static double GetStaminaModifierForSurface(SurfaceId surfaceId)
    {
        return surfaceId switch
        {
            SurfaceId.Dirt => 1.05, // More stamina consumption on dirt
            SurfaceId.Turf => 0.95, // Less stamina consumption on turf
            SurfaceId.Artificial => 0.90, // Even less stamina consumption on artificial surfaces
            _ => 1.00
        };
    }

    private ConditionId GenerateRandomConditionId()
    {
        var values = Enum.GetValues(typeof(ConditionId));
        return (ConditionId)values.GetValue(randomGenerator.Next(values.Length))!;
    }

    private double ApplyRandomIncidents(double baseSpeed, byte tick, RaceRunHorse horse, int totalTicks)
    {
        // Calculate race phase (0.0 to 1.0)
        var racePhase = (double)tick / totalTicks;
        
        // Base 5% chance, but varies by race phase
        var incidentChance = 0.05;
        
        // Higher incident rate in middle of race (traffic, jockeying for position)
        if (racePhase > 0.3 && racePhase < 0.7)
        {
            incidentChance = 0.08; // 8% chance in heavy traffic
        }
        // Lower incident rate at start (clean break) and finish (horses spread out)
        else if (racePhase < 0.1 || racePhase > 0.85)
        {
            incidentChance = 0.02; // 2% chance
        }
        
        if (randomGenerator.NextDouble() < incidentChance)
        {
            // Agility helps recover from negative incidents
            var agilityFactor = horse.Horse.Agility / 100.0; // 0.0 to 1.0
            
            // Base incident range: -5% to +15%
            var incident = randomGenerator.NextDouble() * 0.2 - 0.05;
            
            // If negative incident, agility reduces the penalty
            if (incident < 0)
            {
                incident *= (1 - (agilityFactor * 0.5)); // Up to 50% reduction in penalty
            }
            
            return baseSpeed * (1 + incident);
        }
        return baseSpeed;
    }
}
