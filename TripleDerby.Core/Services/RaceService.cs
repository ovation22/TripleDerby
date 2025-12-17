using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Services;

public class RaceService(ITripleDerbyRepository repository, IRandomGenerator randomGenerator) : IRaceService
{
    private const double AverageBaseSpeed = 0.0422; // Average speed in furlongs per tick, derived from 38 mph

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

        // Initialization phase
        var raceRun = new RaceRun
        {
            RaceId = raceId,
            Race = race,
            ConditionId = GenerateRandomConditionId(),
            Horses = new List<RaceRunHorse>(),
            RaceRunTicks = new List<RaceRunTick>()
        };
        IEnumerable<Horse> horses = new List<Horse>
        {
            myHorse!
        };
        InitializeHorses(raceRun, horses);

        // Calculate total ticks for the race
        var totalTicks = CalculateTotalTicks(race.Furlongs);

        var allHorsesFinished = false;
        byte tick = 0;

        // Run the simulation until all horses finish
        while (!allHorsesFinished)
        {
            // Update each horse's position FIRST
            foreach (var horse in raceRun.Horses)
            {
                UpdateHorsePosition(horse, tick, raceRun);
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
                    Distance = raceRunHorse.Distance, // Use the calculated distance
                    RaceRunTick = raceRunTick
                };
                raceRunTick.RaceRunTickHorses.Add(raceRunTickHorse);
            }

            raceRun.RaceRunTicks.Add(raceRunTick);

            // Check if all horses have finished
            allHorsesFinished = raceRun.Horses.All(horse => horse.Distance >= race.Furlongs);

            tick++;

            // Stop if we've reached a high number of ticks (optional safeguard)
            if (tick > totalTicks * 2) // Example safeguard
            {
                break;
            }
        }

        //Determine winners and rewards
        //DetermineRaceResults(raceRun);
        //DistributePurse(raceRun);

        await repository.CreateAsync(raceRun, cancellationToken);

        return new RaceRunResult
        {
            RaceRunId = raceRun.Id,
            RaceId = raceRun.RaceId,
            RaceName = "TODO",
            ConditionId = raceRun.ConditionId,
            ConditionName = "TODO",
            TrackId = race.TrackId,
            TrackName = "TODO",
            Furlongs = race.Furlongs,
            SurfaceId = race.SurfaceId,
            SurfaceName = "TODO",
            HorseResults =
            [
                new RaceRunHorseResult { HorseId = horseId, HorseName = "TODO", Payout = 1, Place = 1 }
            ]
        };
    }

    private static void InitializeHorses(RaceRun raceRun, IEnumerable<Horse> horses)
    {
        foreach (var horse in horses)
        {
            var raceRunHorse = new RaceRunHorse
            {
                Horse = horse,
                InitialStamina = horse.Stamina
            };
            raceRun.Horses.Add(raceRunHorse);
        }
    }

    private void UpdateHorsePosition(RaceRunHorse raceRunHorse, byte tick, RaceRun raceRun)
    {
        var baseSpeed = AverageBaseSpeed;

        // Apply the horse's speed modifier
        baseSpeed = ApplySpeedModifier(baseSpeed, raceRunHorse.Horse.Speed);

        // Adjust speed and stamina consumption based on race conditions
        baseSpeed = AdjustSpeedForCondition(baseSpeed, raceRun.ConditionId);
        var staminaModifier = GetStaminaModifierForCondition(raceRun.ConditionId);

        // Adjust speed and stamina consumption based on lane and leg type
        baseSpeed = AdjustSpeedForLaneAndLegType(baseSpeed, raceRunHorse.Lane, raceRunHorse.Horse.LegTypeId);
        staminaModifier *= GetStaminaModifierForLaneAndLegType(raceRunHorse.Lane);

        // Adjust speed and stamina consumption based on track surface
        baseSpeed = AdjustSpeedForSurface(baseSpeed, raceRun.Race.SurfaceId);
        staminaModifier *= GetStaminaModifierForSurface(raceRun.Race.SurfaceId);

        // Adjust speed dynamically during the race based on leg type
        baseSpeed = AdjustSpeedForLegTypeDuringRace(baseSpeed, tick, CalculateTotalTicks(raceRun.Race.Furlongs), raceRunHorse.Horse.LegTypeId);

        // Factor in horse's agility, durability, and happiness
        baseSpeed *= GetAgilityModifier(raceRunHorse.Horse.Agility);
        staminaModifier *= GetDurabilityModifier(raceRunHorse.Horse.Durability);
        baseSpeed *= GetHappinessModifier(raceRunHorse.Horse.Happiness);
        staminaModifier *= GetHappinessStaminaModifier(raceRunHorse.Horse.Happiness);

        // Apply random performance fluctuations
        baseSpeed = ApplyRandomPerformanceFluctuations(baseSpeed);

        // Factor in stamina depletion
        //var staminaConsumption = staminaModifier * GetBaseStaminaConsumption(raceRun.ConditionId, raceRunHorse.Horse.LegTypeId);
        //raceRunHorse.Horse.Stamina -= staminaConsumption;

        // Adjust speed based on remaining stamina
        //baseSpeed *= (raceRunHorse.Horse.Stamina / raceRunHorse.InitialStamina);

        // Update horse position - FIXED: just add baseSpeed per tick
        raceRunHorse.Distance += (decimal)baseSpeed;

        // Handle overtaking or lane changing logic
        HandleOvertaking(raceRunHorse, raceRun);
    }

    private static double ApplySpeedModifier(double baseSpeed, int speedActual)
    {
        // Use a percentage-based scaling factor for speed
        var speedModifier = 1 + ((speedActual - 50) / 1000.0); // Adjust scaling factor as needed
        return baseSpeed * speedModifier;
    }

    private static double GetHappinessModifier(int happiness)
    {
        return happiness switch
        {
            > 80 => 1.05,   // High happiness gives a small speed boost
            < 40 => 0.95,   // Low happiness slightly reduces speed
            _ => 1.00       // Neutral happiness has no effect
        };
    }

    private static double GetHappinessStaminaModifier(int happiness)
    {
        return happiness switch
        {
            > 80 => 0.95,   // High happiness reduces stamina consumption
            < 40 => 1.05,   // Low happiness increases stamina consumption
            _ => 1.00       // Neutral happiness has no effect
        };
    }

    private static double AdjustSpeedForCondition(double baseSpeed, ConditionId conditionId)
    {
        return conditionId switch
        {
            ConditionId.Fast => baseSpeed * 1.05,
            ConditionId.WetFast => baseSpeed * 1.03,
            ConditionId.Good => baseSpeed,
            ConditionId.Muddy => baseSpeed * 0.95,
            ConditionId.Sloppy => baseSpeed * 0.93,
            ConditionId.Frozen => baseSpeed * 0.92,
            ConditionId.Slow => baseSpeed * 0.90,
            ConditionId.Heavy => baseSpeed * 0.88,
            ConditionId.Firm => baseSpeed * 1.02,
            ConditionId.Soft => baseSpeed * 0.97,
            ConditionId.Yielding => baseSpeed * 0.95,
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

    private static double AdjustSpeedForLaneAndLegType(double baseSpeed, int lane, LegTypeId legTypeId)
    {
        return legTypeId switch
        {
            LegTypeId.FrontRunner => (lane <= 3) ? baseSpeed * 1.05 : baseSpeed * 0.95,
            LegTypeId.StartDash => (lane <= 3) ? baseSpeed * 1.03 : baseSpeed * 0.97,
            LegTypeId.LastSpurt => (lane > 6) ? baseSpeed * 1.04 : baseSpeed * 0.98,
            LegTypeId.StretchRunner => (lane > 6) ? baseSpeed * 1.03 : baseSpeed * 0.97,
            LegTypeId.RailRunner => (lane == 1) ? baseSpeed * 1.07 : baseSpeed * 0.93,
            _ => baseSpeed
        };
    }

    private static double GetStaminaModifierForLaneAndLegType(int lane)
    {
        return (lane <= 3) ? 1.02 : 1.00; // Inner lanes might cause faster stamina drain
    }

    private double AdjustSpeedForLegTypeDuringRace(double baseSpeed, int currentTick, int totalTicks, LegTypeId legTypeId)
    {
        switch (legTypeId)
        {
            case LegTypeId.StartDash:
                if (currentTick < totalTicks * 0.25)
                {
                    return baseSpeed * 1.10; // Boost early in the race
                }

                break;

            case LegTypeId.LastSpurt:
                if (currentTick > totalTicks * 0.75)
                {
                    return baseSpeed * 1.10; // Boost late in the race
                }

                break;

            case LegTypeId.StretchRunner:
                if (currentTick > totalTicks * 0.4 && currentTick < totalTicks * 0.6)
                {
                    return baseSpeed * 1.05; // Boost in the middle of the race
                }

                break;

            case LegTypeId.FrontRunner:
                if (currentTick < totalTicks * 0.2)
                {
                    return baseSpeed * 1.05; // Boost early in the race
                }

                break;

            case LegTypeId.RailRunner:
                if (currentTick > totalTicks * 0.7)
                {
                    return baseSpeed * 1.03; // Boost late in the race
                }

                break;
        }

        // Optionally, add a random late race boost
        if (currentTick > totalTicks * 0.8)
        {
            return baseSpeed * (1 + (randomGenerator.NextDouble() * 0.05)); // Up to 5% boost
        }

        return baseSpeed;
    }

    private static double GetAgilityModifier(int agility)
    {
        return 1 + ((agility - 50) / 100.0); // Agility boosts speed by a percentage based on deviation from 50
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

        // Ensure the new lane is not occupied or blocked
        if (IsLaneAvailable(newLane, raceRun) && IsLaneClear(horse, newLane, raceRun))
        {
            horse.Lane = (byte)newLane;
        }
    }

    private static bool IsLaneClear(RaceRunHorse horse, int newLane, RaceRun raceRun)
    {
        return !raceRun.Horses.Any(h => h.Lane == newLane && h.Distance > horse.Distance);
    }

    private static bool IsLaneAvailable(int lane, RaceRun raceRun)
    {
        // Check if the lane is occupied or blocked
        return !raceRun.Horses.Any(h => h.Lane == lane && h.Distance > 0); // Simplified check
    }

    /*
    private static void ApplyRandomEvents(RaceRunHorse horse, int tick)
    {
        // Implement random events affecting race dynamics.
    }
    */

    /*
    private static void DetermineRaceResults(RaceRun raceRun)
    {
        // Sort horses based on final position and assign rankings.
    }
    */

    /*
    private static void DistributePurse(RaceRun raceRun)
    {
        // Distribute prize money based on final standings.
    }
    */

    private static int CalculateTotalTicks(decimal furlongs)
    {
        // Desired total number of ticks for a standard race distance (10 furlongs)
        const int standardDistance = 10;
        const int standardTicks = 120;

        // Calculate total ticks based on race distance
        return (int)(furlongs * standardTicks / standardDistance);
    }

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
}
