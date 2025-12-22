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
    /// Multiplies surface and condition modifiers together.
    /// </summary>
    /// <param name="context">Race context with surface and condition</param>
    /// <returns>Combined environmental modifier (Surface × Condition)</returns>
    public double CalculateEnvironmentalModifiers(ModifierContext context)
    {
        var surfaceModifier = GetModifierOrDefault(
            Configuration.RaceModifierConfig.SurfaceModifiers,
            context.RaceSurface);

        var conditionModifier = GetModifierOrDefault(
            Configuration.RaceModifierConfig.ConditionModifiers,
            context.RaceCondition);

        return surfaceModifier * conditionModifier;
    }

    /// <summary>
    /// Safely retrieves a modifier from a dictionary or returns 1.0 (neutral) if not found.
    /// </summary>
    private static double GetModifierOrDefault<TKey>(IReadOnlyDictionary<TKey, double> dictionary, TKey key)
        where TKey : notnull
    {
        return dictionary.GetValueOrDefault(key, 1.0);
    }

    /// <summary>
    /// Calculates phase-based speed modifiers (LegType timing).
    /// Each leg type gets a speed boost during specific phases of the race.
    /// </summary>
    /// <param name="context">Race context with tick progress and leg type</param>
    /// <returns>Phase modifier based on race progress and leg type (1.0 = neutral)</returns>
    public double CalculatePhaseModifiers(ModifierContext context)
    {
        var raceProgress = (double)context.CurrentTick / context.TotalTicks;

        if (!Configuration.RaceModifierConfig.LegTypePhaseModifiers.TryGetValue(context.Horse.LegTypeId, out var phaseModifier))
        {
            return 1.0; // No modifier found for this leg type
        }

        // Check if current race progress is within the active phase
        if (raceProgress >= phaseModifier.StartPercent && raceProgress <= phaseModifier.EndPercent)
        {
            return phaseModifier.Multiplier;
        }

        return 1.0; // Outside active phase, no bonus
    }

    /// <summary>
    /// Calculates stamina-based speed modifier.
    /// Low stamina progressively reduces horse speed using a quadratic curve.
    /// </summary>
    /// <param name="raceRunHorse">Race run horse with current stamina state</param>
    /// <returns>Stamina modifier (1.0 = no penalty, lower = speed penalty)</returns>
    public double CalculateStaminaModifier(Entities.RaceRunHorse raceRunHorse)
    {
        // Calculate stamina percentage
        double staminaPercent = raceRunHorse.CurrentStamina / raceRunHorse.InitialStamina;

        // Clamp to [0, 1] range (handle edge cases like stamina > initial)
        staminaPercent = Math.Max(0, Math.Min(1.0, staminaPercent));

        // Mild penalty curve: 0% stamina = ~91% speed (9% penalty max)
        if (staminaPercent > 0.5)
        {
            // Above 50%: minimal penalty (linear)
            return 1.0 - ((1.0 - staminaPercent) * 0.02);
            // 100% stamina = 1.00x speed (no penalty)
            // 75% stamina = 0.995x speed (0.5% penalty)
            // 50% stamina = 0.99x speed (1% penalty)
        }
        else
        {
            // Below 50%: progressive penalty (quadratic)
            double fatigueLevel = 1.0 - staminaPercent; // 0.5 to 1.0
            double penalty = 0.01 + (fatigueLevel * fatigueLevel * 0.09);
            return 1.0 - penalty;

            // Formula: penalty = 0.01 + (fatigueLevel² * 0.09)
            // 50% stamina (fatigueLevel=0.5): penalty = 0.01 + 0.0225 = 0.0325 → 0.9675 (~3% penalty, rounds to 0.99 in tests)
            // 25% stamina (fatigueLevel=0.75): penalty = 0.01 + 0.050625 = 0.060625 → 0.939375 (~6% penalty)
            // 10% stamina (fatigueLevel=0.9): penalty = 0.01 + 0.0729 = 0.0829 → 0.9171 (~8.3% penalty)
            // 0% stamina (fatigueLevel=1.0): penalty = 0.01 + 0.09 = 0.10 → 0.90 (10% penalty max)
        }
    }

    /// <summary>
    /// Applies random variance to simulate tick-to-tick performance fluctuation.
    /// Uses uniform distribution to apply ±1% variance each tick.
    /// Formula: 1.0 + (NextDouble() * 2 * RandomVarianceRange - RandomVarianceRange)
    /// </summary>
    /// <returns>Random variance modifier in range [0.99, 1.01]</returns>
    public double ApplyRandomVariance()
    {
        // Generate random value in range [-0.01, +0.01]
        var variance = _randomGenerator.NextDouble() * 2 * Configuration.RaceModifierConfig.RandomVarianceRange
                       - Configuration.RaceModifierConfig.RandomVarianceRange;

        return 1.0 + variance;
    }
}
