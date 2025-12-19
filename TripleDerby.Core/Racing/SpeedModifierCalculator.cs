using TripleDerby.Core.Abstractions.Utilities;

namespace TripleDerby.Core.Racing;

/// <summary>
/// Calculates speed modifiers for horse racing simulation.
/// Applies modifiers in a consistent pipeline: Stats → Environment → Phase → Random
/// All modifiers are multiplicative and stack together.
/// </summary>
public class SpeedModifierCalculator
{
    private readonly IRandomGenerator _randomGenerator;

    /// <summary>
    /// Creates a new speed modifier calculator.
    /// </summary>
    /// <param name="randomGenerator">Random number generator for variance calculations</param>
    public SpeedModifierCalculator(IRandomGenerator randomGenerator)
    {
        _randomGenerator = randomGenerator;
    }

    /// <summary>
    /// Calculates stat-based speed modifiers (Speed and Agility).
    /// Uses linear scaling from neutral point (50) for both stats.
    /// </summary>
    /// <param name="context">Race context with horse stats</param>
    /// <returns>Combined stat modifier (Speed × Agility multipliers)</returns>
    public double CalculateStatModifiers(ModifierContext context)
    {
        var speedMultiplier = CalculateSpeedMultiplier(context.Horse.Speed);
        var agilityMultiplier = CalculateAgilityMultiplier(context.Horse.Agility);

        return speedMultiplier * agilityMultiplier;
    }

    /// <summary>
    /// Calculates speed stat multiplier using linear scaling.
    /// Formula: 1.0 + ((speed - 50) * SpeedModifierPerPoint)
    /// Range: Speed 0 = 0.90x, Speed 50 = 1.0x, Speed 100 = 1.10x
    /// </summary>
    private static double CalculateSpeedMultiplier(int speed)
    {
        return 1.0 + ((speed - 50) * Configuration.RaceModifierConfig.SpeedModifierPerPoint);
    }

    /// <summary>
    /// Calculates agility stat multiplier using linear scaling.
    /// Formula: 1.0 + ((agility - 50) * AgilityModifierPerPoint)
    /// Range: Agility 0 = 0.95x, Agility 50 = 1.0x, Agility 100 = 1.05x
    /// </summary>
    private static double CalculateAgilityMultiplier(int agility)
    {
        return 1.0 + ((agility - 50) * Configuration.RaceModifierConfig.AgilityModifierPerPoint);
    }

    /// <summary>
    /// Calculates environmental speed modifiers (Surface and Condition).
    /// Returns 1.0 (neutral) for now - implementation in Phase 3.
    /// </summary>
    /// <param name="context">Race context with surface and condition</param>
    /// <returns>Combined environmental modifier (neutral = 1.0)</returns>
    public double CalculateEnvironmentalModifiers(ModifierContext context)
    {
        // TODO: Phase 3 - Implement environmental modifiers
        return 1.0;
    }

    /// <summary>
    /// Calculates phase-based speed modifiers (LegType timing).
    /// Returns 1.0 (neutral) for now - implementation in Phase 4.
    /// </summary>
    /// <param name="context">Race context with tick progress and leg type</param>
    /// <returns>Phase modifier (neutral = 1.0)</returns>
    public double CalculatePhaseModifiers(ModifierContext context)
    {
        // TODO: Phase 4 - Implement phase-based modifiers
        return 1.0;
    }

    /// <summary>
    /// Applies random variance to simulate tick-to-tick performance fluctuation.
    /// Returns 1.0 (neutral) for now - implementation in Phase 5.
    /// </summary>
    /// <returns>Random variance modifier (neutral = 1.0)</returns>
    public double ApplyRandomVariance()
    {
        // TODO: Phase 5 - Implement random variance
        return 1.0;
    }
}
