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
    /// TODO: Populate in Phase 3 - Environmental Modifiers
    /// </summary>
    public static readonly IReadOnlyDictionary<SurfaceId, double> SurfaceModifiers =
        new Dictionary<SurfaceId, double>
        {
            // Will be populated in Phase 3
        };

    /// <summary>
    /// Speed modifiers by track condition.
    /// TODO: Populate in Phase 3 - Environmental Modifiers
    /// </summary>
    public static readonly IReadOnlyDictionary<ConditionId, double> ConditionModifiers =
        new Dictionary<ConditionId, double>
        {
            // Will be populated in Phase 3
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
