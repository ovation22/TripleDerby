using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Immutable context containing all race state needed for modifier calculations.
/// Passed to SpeedModifierCalculator methods to compute speed modifiers.
/// </summary>
/// <param name="CurrentTick">Current tick number in the race (1-based)</param>
/// <param name="TotalTicks">Total expected ticks for this race distance</param>
/// <param name="Horse">Horse entity with stats and leg type</param>
/// <param name="RaceCondition">Track condition (weather/surface state)</param>
/// <param name="RaceSurface">Track surface type (dirt, turf, artificial)</param>
/// <param name="RaceFurlongs">Race distance in furlongs</param>
public record ModifierContext(
    short CurrentTick,
    short TotalTicks,
    Horse Horse,
    ConditionId RaceCondition,
    SurfaceId RaceSurface,
    decimal RaceFurlongs
);
