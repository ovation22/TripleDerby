using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Racing.Abstractions;
using TripleDerby.Services.Racing.Config;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Manages overtaking detection, lane changes, and traffic response during race simulation.
/// Handles leg-type-specific strategies and risky squeeze play mechanics.
/// </summary>
public class OvertakingManager(
    IRandomGenerator randomGenerator,
    ISpeedModifierCalculator speedModifierCalculator)
    : IOvertakingManager
{
    /// <summary>
    /// Handles overtaking detection and lane change logic for a horse.
    /// Called once per tick per horse during race simulation.
    /// </summary>
    public void HandleOvertaking(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
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
    /// Applies leg-type-specific traffic response effects when horse is blocked.
    /// Modifies speed based on traffic ahead and horse's personality.
    /// Uses actual horse speed calculation for realistic traffic dynamics.
    /// </summary>
    public void ApplyTrafficEffects(
        RaceRunHorse horse,
        RaceRun raceRun,
        short currentTick,
        short totalTicks,
        ref double currentSpeed)
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
                var startDashCap = CalculateHorseSpeed(horseAhead, currentTick, totalTicks, raceRun) *
                                  (1.0 - RaceModifierConfig.StartDashSpeedCapPenalty);
                if (currentSpeed > startDashCap)
                    currentSpeed = startDashCap;
                break;

            case LegTypeId.LastSpurt:
                // Patient: minimal speed cap, no frustration
                var lastSpurtCap = CalculateHorseSpeed(horseAhead, currentTick, totalTicks, raceRun) *
                                  (1.0 - RaceModifierConfig.LastSpurtSpeedCapPenalty);
                if (currentSpeed > lastSpurtCap)
                    currentSpeed = lastSpurtCap;
                break;

            case LegTypeId.StretchRunner:
                // Speed cap: match leader minus penalty
                var stretchCap = CalculateHorseSpeed(horseAhead, currentTick, totalTicks, raceRun) *
                                (1.0 - RaceModifierConfig.StretchRunnerSpeedCapPenalty);
                if (currentSpeed > stretchCap)
                    currentSpeed = stretchCap;
                break;

            case LegTypeId.RailRunner:
                // Extra cautious on rail: higher speed cap penalty
                var railCap = CalculateHorseSpeed(horseAhead, currentTick, totalTicks, raceRun) *
                             (1.0 - RaceModifierConfig.RailRunnerSpeedCapPenalty);
                if (currentSpeed > railCap)
                    currentSpeed = railCap;
                break;
        }
    }

    // Private Helper Methods

    /// <summary>
    /// Calculates the distance threshold for detecting overtaking opportunities.
    /// Combines base threshold with speed factor and race phase multiplier.
    /// </summary>
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
    /// All leg types implemented with distinct personalities.
    /// </summary>
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
    /// Includes risky squeeze plays when clean change not possible.
    /// </summary>
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
    /// Attempts a risky lane change when the target lane is blocked.
    /// Success probability based on agility, with durability-based penalty on success.
    /// </summary>
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

    /// <summary>
    /// Finds the horse directly ahead in the same lane within blocking distance.
    /// </summary>
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
    /// Calculates the current speed of a horse using the full modifier pipeline.
    /// Uses same calculation as UpdateHorsePosition for consistent traffic response.
    /// Applies Stats → Environment → Phase → Stamina modifiers.
    /// </summary>
    /// <param name="horse">The horse to calculate speed for</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    /// <param name="raceRun">Current race state</param>
    /// <returns>Calculated speed in furlongs per tick</returns>
    private double CalculateHorseSpeed(
        RaceRunHorse horse,
        short currentTick,
        short totalTicks,
        RaceRun raceRun)
    {
        var baseSpeed = RaceModifierConfig.AverageBaseSpeed;

        // Build context for modifier calculations
        var context = new ModifierContext(
            CurrentTick: currentTick,
            TotalTicks: totalTicks,
            Horse: horse.Horse,
            RaceCondition: raceRun.ConditionId,
            RaceSurface: raceRun.Race.SurfaceId,
            RaceFurlongs: raceRun.Race.Furlongs
        );

        // Apply modifier pipeline (same order as UpdateHorsePosition)
        // Stats → Environment → Phase → Stamina
        baseSpeed *= speedModifierCalculator.CalculateStatModifiers(context);
        baseSpeed *= speedModifierCalculator.CalculateEnvironmentalModifiers(context);
        baseSpeed *= speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);
        baseSpeed *= speedModifierCalculator.CalculateStaminaModifier(horse);

        // Note: We intentionally skip:
        // - Risky lane change penalty (temporary state, not inherent speed)
        // - Random variance (too volatile for traffic comparison)
        // - Traffic effects (avoid circular dependency)

        return baseSpeed;
    }
}
