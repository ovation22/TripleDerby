using TripleDerby.Core.Racing;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Configuration;

/// <summary>
/// Configuration constants for race speed modifiers.
/// All modifier values are multiplicative and stack together to calculate final speed.
///
/// <para>
/// <strong>Modifier Pipeline:</strong>
/// Final Speed = Base Speed × Stat Modifiers × Environmental Modifiers × Phase Modifiers × Random Variance
/// </para>
///
/// <para>
/// <strong>Design Goals:</strong>
/// - No magic numbers: All values defined as constants or in dictionaries
/// - Easy to balance: Adjust values in config, no code changes needed
/// - Independent testing: Each modifier category testable in isolation
/// - Clear impact ranges: Each modifier documents its min/max effect
/// </para>
///
/// <para>
/// <strong>Reference:</strong> See race-modifiers-refactor.md feature spec for design rationale and balancing decisions.
/// </para>
/// </summary>
public static class RaceModifierConfig
{
    // ============================================================================
    // Base Speed Configuration
    // ============================================================================

    /// <summary>
    /// Target number of ticks for a standard 10-furlong race.
    /// Used to calculate base speed: baseSpeed = raceFurlongs / targetTicks
    /// </summary>
    public const double TargetTicksFor10Furlongs = 237.0;

    // ============================================================================
    // Stat Modifier Configuration
    // ============================================================================

    /// <summary>
    /// Speed stat modifier per point from neutral (50).
    /// Range: Speed 0 = 0.90x, Speed 50 = 1.0x, Speed 100 = 1.10x
    /// Total effect: ±10%
    /// </summary>
    public const double SpeedModifierPerPoint = 0.002;

    /// <summary>
    /// Agility stat modifier per point from neutral (50).
    /// Range: Agility 0 = 0.95x, Agility 50 = 1.0x, Agility 100 = 1.05x
    /// Total effect: ±5%
    /// </summary>
    public const double AgilityModifierPerPoint = 0.001;

    /// <summary>
    /// Divisor for happiness bonus calculation (above neutral).
    /// Smaller value = stronger bonus effect.
    /// Formula: modifier = log10(1 + excess) / HappinessSpeedBonusDivisor
    /// Current value (20) yields ~2.5% bonus at happiness=100.
    /// </summary>
    public const double HappinessSpeedBonusDivisor = 20.0;

    /// <summary>
    /// Divisor for happiness penalty calculation (below neutral).
    /// Smaller value = stronger penalty effect.
    /// Formula: modifier = log10(1 + deficit) / HappinessSpeedPenaltyDivisor
    /// Current value (15) yields ~3.4% penalty at happiness=0.
    /// Asymmetric design: penalty divisor < bonus divisor (unhappiness hurts more).
    /// Range: Happiness 0 = 0.9661x (-3.39%), Happiness 50 = 1.0x, Happiness 100 = 1.0255x (+2.55%)
    /// Total effect: ±3% (tertiary stat, weaker than Agility ±5%, stronger than Stamina at 10f)
    /// </summary>
    public const double HappinessSpeedPenaltyDivisor = 15.0;

    /// <summary>
    /// Divisor for happiness stamina efficiency bonus (above neutral).
    /// Affects stamina DEPLETION RATE, not stamina pool size.
    /// Formula: efficiency = log10(1 + excess) / HappinessStaminaBonusDivisor
    /// Applied as: depletionRate = 1.0 - efficiency (INVERTED: lower = less depletion)
    /// Current value (25) yields ~6.82% less depletion at happiness=100.
    /// Example: Happiness 100 → 1.0 - log10(51)/25 = 0.9318 (~6.82% more efficient)
    /// </summary>
    public const double HappinessStaminaBonusDivisor = 25.0;

    /// <summary>
    /// Divisor for happiness stamina efficiency penalty (below neutral).
    /// Formula: efficiency = log10(1 + deficit) / HappinessStaminaPenaltyDivisor
    /// Applied as: depletionRate = 1.0 + efficiency (MORE depletion)
    /// Current value (20) yields ~8.54% more depletion at happiness=0.
    /// Asymmetric design: penalty divisor < bonus divisor (unhappiness hurts more).
    /// Example: Happiness 0 → 1.0 + log10(51)/20 = 1.0854 (~8.54% less efficient)
    /// </summary>
    public const double HappinessStaminaPenaltyDivisor = 20.0;

    // ============================================================================
    // Random Variance Configuration
    // ============================================================================

    /// <summary>
    /// Random variance range applied per tick.
    /// Value of 0.01 means ±1% random fluctuation per tick.
    /// </summary>
    public const double RandomVarianceRange = 0.01;

    // ============================================================================
    // Environmental Modifiers (populated in Phase 3)
    // ============================================================================

    /// <summary>
    /// Speed modifiers by track surface type.
    /// Dirt is neutral (1.00), Turf is faster (1.02), Artificial is consistent and slightly faster (1.01).
    /// Range: 1.00x to 1.02x (+0% to +2%)
    /// </summary>
    public static readonly IReadOnlyDictionary<SurfaceId, double> SurfaceModifiers =
        new Dictionary<SurfaceId, double>
        {
            { SurfaceId.Dirt, 1.00 },       // Neutral, most common surface
            { SurfaceId.Turf, 1.02 },       // Faster, grass surface
            { SurfaceId.Artificial, 1.01 }  // Consistent, synthetic surface
        };

    /// <summary>
    /// Speed modifiers by track condition.
    /// Fast/dry conditions provide speed bonuses, wet conditions provide penalties, extreme conditions have larger penalties.
    /// Range: 0.90x to 1.03x (-10% to +3%)
    /// </summary>
    public static readonly IReadOnlyDictionary<ConditionId, double> ConditionModifiers =
        new Dictionary<ConditionId, double>
        {
            // Dry/Fast conditions (positive modifiers)
            { ConditionId.Fast, 1.03 },      // Fastest condition
            { ConditionId.Firm, 1.02 },      // Fast and firm
            { ConditionId.Good, 1.00 },      // Neutral baseline

            // Wet conditions (slight penalties)
            { ConditionId.WetFast, 0.99 },   // Slightly wet but still fast
            { ConditionId.Soft, 0.98 },      // Softer surface
            { ConditionId.Yielding, 0.97 },  // Yielding surface
            { ConditionId.Muddy, 0.96 },     // Muddy conditions
            { ConditionId.Sloppy, 0.95 },    // Very wet and sloppy

            // Extreme conditions (larger penalties)
            { ConditionId.Heavy, 0.93 },     // Heavy, difficult conditions
            { ConditionId.Frozen, 0.92 },    // Frozen track
            { ConditionId.Slow, 0.90 }       // Slowest condition
        };

    // ============================================================================
    // Phase Modifiers (populated in Phase 4)
    // ============================================================================

    /// <summary>
    /// Phase-based modifiers for each leg type (running style).
    /// Defines when during the race each leg type gets a speed boost.
    /// StartDash and LastSpurt get highest bonus (1.04x), balanced strategies get moderate (1.03x).
    /// Note: RailRunner uses conditional lane/traffic bonus instead of phase timing (Feature 005).
    /// </summary>
    public static readonly IReadOnlyDictionary<LegTypeId, PhaseModifier> LegTypePhaseModifiers =
        new Dictionary<LegTypeId, PhaseModifier>
        {
            { LegTypeId.StartDash, new PhaseModifier(0.00, 0.25, 1.04) },      // First 25%, high risk/reward
            { LegTypeId.FrontRunner, new PhaseModifier(0.00, 0.20, 1.03) },    // First 20%, early speed
            { LegTypeId.StretchRunner, new PhaseModifier(0.60, 0.80, 1.03) },  // 60-80%, stretch run (adjusted for realism)
            { LegTypeId.LastSpurt, new PhaseModifier(0.75, 1.00, 1.04) }       // Final 25%, closing kick
            // RailRunner: Uses conditional lane/traffic bonus (see RailRunner configuration below)
        };

    // ============================================================================
    // Rail Runner Configuration (Feature 005)
    // ============================================================================

    /// <summary>
    /// Rail runner speed bonus multiplier when positioned in lane 1 with clear path ahead.
    /// Bonus applied conditionally based on lane position and traffic, not race phase.
    /// Range: 1.03x (+3% speed) when conditions met, 1.0x (neutral) otherwise.
    /// </summary>
    public const double RailRunnerBonusMultiplier = 1.03;

    /// <summary>
    /// Minimum clear distance required ahead of rail runner to activate bonus (in furlongs).
    /// Checks for horses in same lane within this distance.
    /// Value of 0.5 furlongs balances realism (clear racing line) with playability.
    /// </summary>
    public const decimal RailRunnerClearPathDistance = 0.5m;

    // ============================================================================
    // Stamina Configuration (Feature 004)
    // ============================================================================

    /// <summary>
    /// Base stamina depletion rates by race distance category.
    /// Values represent percentage of stamina pool depleted per 100 ticks.
    /// </summary>
    public static class StaminaDepletionRates
    {
        public const double Sprint = 0.08;      // ≤6f: Minimal stamina impact
        public const double Classic = 0.15;     // 7-10f: Moderate stamina impact
        public const double Long = 0.22;        // 11-12f: Significant stamina impact
        public const double Marathon = 0.30;    // 13f+: Severe stamina impact
    }

    /// <summary>
    /// Stamina stat modifier per point from neutral (50).
    /// Higher Stamina = bigger fuel tank = slower depletion.
    /// Range: Stamina 0 = 1.20x depletion, Stamina 100 = 0.80x depletion
    /// </summary>
    public const double StaminaDepletionModifierPerPoint = -0.004;

    /// <summary>
    /// Durability stat modifier per point from neutral (50).
    /// Higher Durability = fuel efficiency = slower depletion.
    /// Range: Durability 0 = 1.15x depletion, Durability 100 = 0.85x depletion
    /// </summary>
    public const double DurabilityDepletionModifierPerPoint = -0.003;

    /// <summary>
    /// Maximum speed penalty when stamina is fully depleted (0%).
    /// Value of 0.10 means exhausted horse runs at 90% speed.
    /// Uses quadratic curve for progressive penalty below 50% stamina.
    /// </summary>
    public const double MaxStaminaSpeedPenalty = 0.10;  // 10% max penalty (mild)

    // ============================================================================
    // Overtaking & Lane Change Configuration (Feature 007 - Phase 1)
    // ============================================================================

    /// <summary>
    /// Base threshold distance for detecting overtaking opportunities (in furlongs).
    /// Modified by speed factor and race phase multiplier.
    /// </summary>
    public const decimal OvertakingBaseThreshold = 0.25m;

    /// <summary>
    /// Speed stat influence on overtaking threshold per point.
    /// Higher speed = larger detection range.
    /// Range: Speed 0 = 1.0x threshold, Speed 100 = 1.2x threshold
    /// </summary>
    public const double OvertakingSpeedFactor = 0.002;

    /// <summary>
    /// Multiplier applied to overtaking threshold in final 25% of race.
    /// Creates more aggressive overtaking behavior late in races.
    /// </summary>
    public const double OvertakingLateRaceMultiplier = 1.5;

    /// <summary>
    /// Base cooldown between lane change attempts at 0 agility (in ticks).
    /// Reduced by agility: cooldown = BaseLaneChangeCooldown - (Agility × AgilityCooldownReduction)
    /// </summary>
    public const int BaseLaneChangeCooldown = 10;

    /// <summary>
    /// Reduction in cooldown per point of agility.
    /// Range: Agility 0 = 10 tick cooldown, Agility 100 = 0 tick cooldown
    /// </summary>
    public const double AgilityCooldownReduction = 0.1;

    /// <summary>
    /// Minimum clearance required behind horse when changing lanes (in furlongs).
    /// Prevents cutting off horses that are close behind.
    /// </summary>
    public const decimal LaneChangeMinClearanceBehind = 0.1m;

    /// <summary>
    /// Minimum clearance required ahead of horse when changing lanes (in furlongs).
    /// Prevents collisions with horses ahead in target lane.
    /// Asymmetric: requires more clearance ahead than behind for safety.
    /// </summary>
    public const decimal LaneChangeMinClearanceAhead = 0.2m;
}
