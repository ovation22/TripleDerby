using TripleDerby.Core.Entities;

namespace TripleDerby.Services.Racing.Abstractions;

/// <summary>
/// Calculates stamina depletion for horses during races.
/// Stamina depletes based on distance, pace, horse stats (Stamina/Durability), and running style (LegType).
/// </summary>
public interface IStaminaCalculator
{
    /// <summary>
    /// Calculates the base stamina depletion rate based on race distance.
    /// Longer races have higher per-tick depletion rates (progressive scaling).
    /// </summary>
    /// <param name="furlongs">Race distance in furlongs</param>
    /// <returns>Base depletion rate (percentage of stamina pool per 100 ticks)</returns>
    double CalculateBaseDepletionRate(decimal furlongs);

    /// <summary>
    /// Calculates stamina efficiency multiplier based on Stamina and Durability stats.
    /// Higher Stamina = bigger fuel tank (slower depletion)
    /// Higher Durability = fuel efficiency (slower depletion)
    /// </summary>
    /// <param name="horse">Horse entity with stats</param>
    /// <returns>Efficiency multiplier (lower = better endurance)</returns>
    double CalculateStaminaEfficiency(Horse horse);

    /// <summary>
    /// Calculates pace multiplier based on current speed relative to base speed.
    /// Faster running = more effort = faster stamina depletion.
    /// </summary>
    /// <param name="currentSpeed">Current speed in furlongs/tick</param>
    /// <param name="baseSpeed">Base/neutral speed for comparison</param>
    /// <returns>Pace multiplier (linear scaling with speed)</returns>
    double CalculatePaceMultiplier(double currentSpeed, double baseSpeed);

    /// <summary>
    /// Calculates LegType-based stamina usage multiplier.
    /// Different running styles burn stamina at different rates during race phases.
    /// </summary>
    /// <param name="horse">Horse entity with LegType</param>
    /// <param name="raceProgress">Current race progress (0.0 to 1.0)</param>
    /// <returns>LegType stamina multiplier</returns>
    double CalculateLegTypeStaminaMultiplier(Horse horse, double raceProgress);

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
    double CalculateDepletionAmount(
        Horse horse,
        decimal furlongs,
        double currentSpeed,
        double baseSpeed,
        double raceProgress);
}
