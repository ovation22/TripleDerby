using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Abstractions.Racing;

/// <summary>
/// Calculates speed modifiers for horse racing simulation.
/// Applies modifiers in a consistent pipeline: Stats → Environment → Phase → Stamina → Random
/// All modifiers are multiplicative and stack together.
/// </summary>
public interface ISpeedModifierCalculator
{
    /// <summary>
    /// Calculates stat-based speed modifiers (Speed and Agility).
    /// Uses linear scaling from neutral point (50) for both stats.
    /// </summary>
    /// <param name="context">Race context with horse stats</param>
    /// <returns>Combined stat modifier (Speed × Agility multipliers)</returns>
    double CalculateStatModifiers(Core.Racing.ModifierContext context);

    /// <summary>
    /// Calculates environmental speed modifiers (Surface and Condition).
    /// Combines surface preferences and track condition impacts.
    /// </summary>
    /// <param name="context">Race context with surface and condition</param>
    /// <returns>Combined environmental modifier</returns>
    double CalculateEnvironmentalModifiers(Core.Racing.ModifierContext context);

    /// <summary>
    /// Calculates phase-based speed modifiers (LegType timing).
    /// Each leg type gets a speed boost during specific phases of the race.
    /// RailRunner uses conditional lane/traffic bonus instead of phase timing (Feature 005).
    /// </summary>
    /// <param name="context">Race context with tick progress and leg type</param>
    /// <param name="raceRun">Current race run with all horses (for traffic detection)</param>
    /// <returns>Phase modifier based on race progress and leg type (1.0 = neutral)</returns>
    double CalculatePhaseModifiers(Core.Racing.ModifierContext context, RaceRun raceRun);

    /// <summary>
    /// Calculates stamina-based speed modifier.
    /// Low stamina progressively reduces horse speed using a quadratic curve.
    /// </summary>
    /// <param name="raceRunHorse">Race run horse with current stamina state</param>
    /// <returns>Stamina modifier (1.0 = no penalty, lower = speed penalty)</returns>
    double CalculateStaminaModifier(RaceRunHorse raceRunHorse);

    /// <summary>
    /// Applies random variance to create realistic race variability.
    /// Adds small random fluctuation per tick (±1% by default).
    /// </summary>
    /// <returns>Random variance multiplier (typically 0.99 - 1.01)</returns>
    double ApplyRandomVariance();
}
