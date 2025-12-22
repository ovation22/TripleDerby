using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Racing;

/// <summary>
/// Calculates stamina depletion for horses during races.
/// Stamina depletes based on distance, pace, horse stats (Stamina/Durability), and running style (LegType).
/// </summary>
public class StaminaCalculator : IStaminaCalculator
{
    /// <summary>
    /// Calculates the base stamina depletion rate based on race distance.
    /// Longer races have higher per-tick depletion rates (progressive scaling).
    /// </summary>
    /// <param name="furlongs">Race distance in furlongs</param>
    /// <returns>Base depletion rate (percentage of stamina pool per 100 ticks)</returns>
    public double CalculateBaseDepletionRate(decimal furlongs)
    {
        // Progressive scaling: minimal impact on sprints, severe on marathons
        if (furlongs <= 6)
            return RaceModifierConfig.StaminaDepletionRates.Sprint;      // 0.08
        else if (furlongs <= 10)
            return RaceModifierConfig.StaminaDepletionRates.Classic;     // 0.15
        else if (furlongs <= 12)
            return RaceModifierConfig.StaminaDepletionRates.Long;        // 0.22
        else
            return RaceModifierConfig.StaminaDepletionRates.Marathon;    // 0.30
    }

    /// <summary>
    /// Calculates stamina efficiency multiplier based on Stamina and Durability stats.
    /// Higher Stamina = bigger fuel tank (slower depletion)
    /// Higher Durability = fuel efficiency (slower depletion)
    /// </summary>
    /// <param name="horse">Horse entity with stats</param>
    /// <returns>Efficiency multiplier (lower = better endurance)</returns>
    public double CalculateStaminaEfficiency(Horse horse)
    {
        // High Stamina = bigger tank = slower depletion
        double staminaFactor = 1.0 + ((horse.Stamina - 50) * RaceModifierConfig.StaminaDepletionModifierPerPoint);
        // Stamina 0 = 1.20x depletion (burns fast)
        // Stamina 50 = 1.00x depletion (neutral)
        // Stamina 100 = 0.80x depletion (lasts longer)

        // High Durability = fuel efficient = slower depletion
        double durabilityFactor = 1.0 + ((horse.Durability - 50) * RaceModifierConfig.DurabilityDepletionModifierPerPoint);
        // Durability 0 = 1.15x depletion (inefficient)
        // Durability 50 = 1.00x depletion (neutral)
        // Durability 100 = 0.85x depletion (very efficient)

        return staminaFactor * durabilityFactor;
    }

    /// <summary>
    /// Calculates pace multiplier based on current speed relative to base speed.
    /// Faster running = more effort = faster stamina depletion.
    /// </summary>
    /// <param name="currentSpeed">Current speed in furlongs/tick</param>
    /// <param name="baseSpeed">Base/neutral speed for comparison</param>
    /// <returns>Pace multiplier (linear scaling with speed)</returns>
    public double CalculatePaceMultiplier(double currentSpeed, double baseSpeed)
    {
        // Linear scaling: ±20% speed = ±20% depletion
        return currentSpeed / baseSpeed;
    }

    /// <summary>
    /// Calculates LegType-based stamina usage multiplier.
    /// Different running styles burn stamina at different rates during race phases.
    /// </summary>
    /// <param name="horse">Horse entity with LegType</param>
    /// <param name="raceProgress">Current race progress (0.0 to 1.0)</param>
    /// <returns>LegType stamina multiplier</returns>
    public double CalculateLegTypeStaminaMultiplier(Horse horse, double raceProgress)
    {
        return horse.LegTypeId switch
        {
            LegTypeId.StartDash => raceProgress < 0.25 ? 1.30 : 0.90,
            // Explosive start (130%), cruise finish (90%)

            LegTypeId.FrontRunner => 1.10,
            // Aggressive throughout (110%)

            LegTypeId.StretchRunner => raceProgress < 0.60 ? 0.85 : 1.15,
            // Conserve early (85%), push stretch (115%)

            LegTypeId.LastSpurt => raceProgress < 0.75 ? 0.80 : 1.40,
            // Maximum conservation (80%), explosive finish (140%)

            LegTypeId.RailRunner => raceProgress < 0.70 ? 0.90 : 1.05,
            // Steady early (90%), slight late push (105%)

            _ => 1.00
        };
    }

    /// <summary>
    /// Calculates total stamina depletion amount for a single tick.
    /// Combines all depletion factors: distance, stats, pace, and running style.
    /// </summary>
    /// <param name="horse">Horse entity</param>
    /// <param name="furlongs">Race distance</param>
    /// <param name="currentSpeed">Current speed</param>
    /// <param name="baseSpeed">Base speed for pace calculation</param>
    /// <param name="raceProgress">Current race progress (0.0 to 1.0)</param>
    /// <returns>Amount of stamina to deplete this tick</returns>
    public double CalculateDepletionAmount(
        Horse horse,
        decimal furlongs,
        double currentSpeed,
        double baseSpeed,
        double raceProgress)
    {
        // 1. Calculate base depletion rate
        double baseDepletionRate = CalculateBaseDepletionRate(furlongs);

        // 2. Adjust for horse stats (Stamina + Durability)
        double staminaEfficiency = CalculateStaminaEfficiency(horse);

        // 3. Adjust for current pace/effort
        double paceMultiplier = CalculatePaceMultiplier(currentSpeed, baseSpeed);

        // 4. Adjust for LegType strategy
        double legTypeMultiplier = CalculateLegTypeStaminaMultiplier(horse, raceProgress);

        // 5. Calculate final depletion (per tick, not per 100 ticks)
        double depletionAmount = (baseDepletionRate / 100.0)
            * staminaEfficiency
            * paceMultiplier
            * legTypeMultiplier;

        return depletionAmount;
    }
}
