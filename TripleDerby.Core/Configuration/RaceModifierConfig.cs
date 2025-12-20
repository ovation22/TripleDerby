using TripleDerby.Core.Racing;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Configuration;

/// <summary>
/// Configuration constants for race speed modifiers.
/// All modifier values are multiplicative and stack together to calculate final speed.
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
    /// TODO: Populate in Phase 4 - Phase Modifiers
    /// </summary>
    public static readonly IReadOnlyDictionary<LegTypeId, PhaseModifier> LegTypePhaseModifiers =
        new Dictionary<LegTypeId, PhaseModifier>
        {
            // Will be populated in Phase 4
        };
}
