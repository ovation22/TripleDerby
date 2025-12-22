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

    // Feature 004: Stamina Calculator
    private readonly StaminaCalculator _staminaCalculator = new();

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
        var statModifier = _speedModifierCalculator.CalculateStatModifiers(context);
        baseSpeed *= statModifier;

        // Apply environmental modifiers (Surface + Condition)
        var envModifier = _speedModifierCalculator.CalculateEnvironmentalModifiers(context);
        baseSpeed *= envModifier;

        // Apply phase modifiers (LegType timing or conditional bonuses)
        var phaseModifier = _speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);
        baseSpeed *= phaseModifier;

        // Feature 004: Apply stamina modifier (speed penalty when stamina low)
        var staminaModifier = _speedModifierCalculator.CalculateStaminaModifier(raceRunHorse);
        baseSpeed *= staminaModifier;

        // Apply random variance (±1% per tick)
        var randomVariance = _speedModifierCalculator.ApplyRandomVariance();
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
        var depletionAmount = _staminaCalculator.CalculateDepletionAmount(
            raceRunHorse.Horse,
            raceRun.Race.Furlongs,
            currentSpeed,
            AverageBaseSpeed,
            raceProgress);

        raceRunHorse.CurrentStamina = Math.Max(0, raceRunHorse.CurrentStamina - depletionAmount);

        // Handle overtaking or lane changing logic
        //HandleOvertaking(raceRunHorse, raceRun);
    }

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
}
