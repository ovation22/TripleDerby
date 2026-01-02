using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Racing.Abstractions;
using TripleDerby.Services.Racing.Config;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Racing.Calculators;

/// <summary>
/// Calculates speed modifiers for horse racing simulation.
/// Applies modifiers in a consistent pipeline: Stats → Environment → Phase → Random
/// All modifiers are multiplicative and stack together.
/// </summary>
public class SpeedModifierCalculator : ISpeedModifierCalculator
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
    /// Calculates stat-based speed modifiers (Speed, Agility, and Happiness).
    /// Speed and Agility use linear scaling from neutral point (50).
    /// Happiness uses two-phase logarithmic scaling for diminishing returns.
    /// </summary>
    /// <param name="context">Race context with horse stats</param>
    /// <returns>Combined stat modifier (Speed × Agility × Happiness multipliers)</returns>
    public double CalculateStatModifiers(ModifierContext context)
    {
        var speedMultiplier = CalculateSpeedMultiplier(context.Horse.Speed);
        var agilityMultiplier = CalculateAgilityMultiplier(context.Horse.Agility);
        var happinessMultiplier = CalculateHappinessSpeedModifier(context.Horse.Happiness);

        return speedMultiplier * agilityMultiplier * happinessMultiplier;
    }

    /// <summary>
    /// Calculates speed stat multiplier using linear scaling.
    /// Formula: 1.0 + ((speed - 50) * SpeedModifierPerPoint)
    /// Range: Speed 0 = 0.90x, Speed 50 = 1.0x, Speed 100 = 1.10x
    /// </summary>
    private static double CalculateSpeedMultiplier(int speed)
    {
        return 1.0 + ((speed - 50) * RaceModifierConfig.SpeedModifierPerPoint);
    }

    /// <summary>
    /// Calculates agility stat multiplier using linear scaling.
    /// Formula: 1.0 + ((agility - 50) * AgilityModifierPerPoint)
    /// Range: Agility 0 = 0.95x, Agility 50 = 1.0x, Agility 100 = 1.05x
    /// </summary>
    private static double CalculateAgilityMultiplier(int agility)
    {
        return 1.0 + ((agility - 50) * RaceModifierConfig.AgilityModifierPerPoint);
    }

    /// <summary>
    /// Calculates happiness stat multiplier using two-phase logarithmic scaling.
    /// Uses logarithmic curves to create diminishing returns effect.
    /// Asymmetric design: unhappiness penalty (3.39%) > happiness bonus (2.55%).
    ///
    /// Formula:
    ///   If happiness >= 50: 1.0 + log10(1 + excess) / HappinessSpeedBonusDivisor
    ///   If happiness < 50:  1.0 - log10(1 + deficit) / HappinessSpeedPenaltyDivisor
    ///
    /// Range: Happiness 0 = 0.9661x (-3.39%), Happiness 50 = 1.0x, Happiness 100 = 1.0255x (+2.55%)
    /// Total effect: ±3% (tertiary stat, weaker than Agility ±5%)
    ///
    /// Rationale: Logarithmic curve reflects psychological reality - mood changes have bigger
    /// impact at extremes (0→25) than when already content (75→100). Happy horses run more
    /// enthusiastically, unhappy horses are reluctant.
    /// </summary>
    private static double CalculateHappinessSpeedModifier(int happiness)
    {
        // Clamp happiness to valid range [0, 100]
        happiness = Math.Clamp(happiness, 0, 100);

        if (happiness >= 50)
        {
            // Above neutral: logarithmic growth with diminishing returns
            // Happy horses run modestly faster, but gains diminish at high happiness
            double excess = happiness - 50.0;
            if (excess == 0)
                return 1.0; // Exactly neutral, no effect

            // log10(1 + x) provides smooth diminishing returns curve
            // Divided by 20 to scale to ~2.5% bonus at happiness=100
            double modifier = Math.Log10(1.0 + excess) / RaceModifierConfig.HappinessSpeedBonusDivisor;
            return 1.0 + modifier;
        }
        else
        {
            // Below neutral: logarithmic penalty with steeper curve
            // Unhappiness has bigger impact than happiness (negative emotions stronger)
            // Psychological realism: depression/frustration affects performance more than joy improves it
            double deficit = 50.0 - happiness;

            // Divided by 15 instead of 20 for steeper penalty curve
            // Creates asymmetry: happiness=0 penalty > happiness=100 bonus
            double modifier = Math.Log10(1.0 + deficit) / RaceModifierConfig.HappinessSpeedPenaltyDivisor;
            return 1.0 - modifier;
        }
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
            RaceModifierConfig.SurfaceModifiers,
            context.RaceSurface);

        var conditionModifier = GetModifierOrDefault(
            RaceModifierConfig.ConditionModifiers,
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
    /// RailRunner uses conditional lane/traffic bonus instead of phase timing (Feature 005).
    /// </summary>
    /// <param name="context">Race context with tick progress and leg type</param>
    /// <param name="raceRun">Current race run with all horses (for traffic detection)</param>
    /// <returns>Phase modifier based on race progress and leg type (1.0 = neutral)</returns>
    public double CalculatePhaseModifiers(ModifierContext context, RaceRun raceRun)
    {
        // Special case: RailRunner uses conditional lane/traffic bonus
        if (context.Horse.LegTypeId == LegTypeId.RailRunner)
        {
            return CalculateRailRunnerBonus(context, raceRun);
        }

        // All other leg types use phase-based timing
        var raceProgress = (double)context.CurrentTick / context.TotalTicks;

        if (!RaceModifierConfig.LegTypePhaseModifiers.TryGetValue(context.Horse.LegTypeId, out var phaseModifier))
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
    /// Calculates rail runner conditional bonus based on lane position and traffic.
    /// Bonus applies when horse is in lane 1 with clear path ahead (Feature 005).
    /// </summary>
    /// <param name="context">Race context with horse information</param>
    /// <param name="raceRun">Current race run with all horses</param>
    /// <returns>1.03x bonus if conditions met, 1.0x otherwise</returns>
    private static double CalculateRailRunnerBonus(ModifierContext context, RaceRun raceRun)
    {
        // Find the RaceRunHorse entity for this horse
        var raceRunHorse = raceRun.Horses.FirstOrDefault(h => h.Horse.Id == context.Horse.Id);

        if (raceRunHorse == null)
        {
            return 1.0; // Safety fallback
        }

        // Check lane position: must be in lane 1 (the rail)
        if (raceRunHorse.Lane != 1)
        {
            return 1.0; // Not on rail, no bonus
        }

        // Check for clear path ahead
        if (!HasClearPathAhead(
            raceRunHorse,
            raceRun.Horses,
            RaceModifierConfig.RailRunnerClearPathDistance))
        {
            return 1.0; // Traffic ahead, no bonus
        }

        // All conditions met: apply rail position bonus
        return RaceModifierConfig.RailRunnerBonusMultiplier;
    }

    /// <summary>
    /// Checks if a horse has a clear path ahead in its current lane.
    /// Used for rail runner traffic detection (Feature 005).
    /// </summary>
    /// <param name="horse">The horse to check</param>
    /// <param name="allHorses">All horses in the race</param>
    /// <param name="clearDistance">Required clear distance (furlongs)</param>
    /// <returns>True if path is clear, false if blocked by traffic</returns>
    private static bool HasClearPathAhead(
        RaceRunHorse horse,
        IEnumerable<RaceRunHorse> allHorses,
        decimal clearDistance)
    {
        // Check for horses in same lane ahead within clearDistance
        return !allHorses.Any(h =>
            h != horse &&                              // Not the same horse
            h.Lane == horse.Lane &&                    // Same lane
            h.Distance > horse.Distance &&             // Horse is ahead
            (h.Distance - horse.Distance) < clearDistance  // Within blocking range
        );
    }

    /// <summary>
    /// Calculates stamina-based speed modifier.
    /// Low stamina progressively reduces horse speed using a quadratic curve.
    /// </summary>
    /// <param name="raceRunHorse">Race run horse with current stamina state</param>
    /// <returns>Stamina modifier (1.0 = no penalty, lower = speed penalty)</returns>
    public double CalculateStaminaModifier(RaceRunHorse raceRunHorse)
    {
        // Edge case: if initial stamina is zero, treat as no stamina system (neutral modifier)
        if (raceRunHorse.InitialStamina == 0)
        {
            return 1.0; // No penalty for horses with no stamina pool
        }

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
        var variance = _randomGenerator.NextDouble() * 2 * RaceModifierConfig.RandomVarianceRange
                       - RaceModifierConfig.RandomVarianceRange;

        return 1.0 + variance;
    }
}
