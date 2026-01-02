namespace TripleDerby.Services.Racing.Racing;

/// <summary>
/// Defines a phase-based speed modifier for leg types.
/// Specifies when during the race (start/end percent) a multiplier is applied.
/// </summary>
/// <param name="StartPercent">Race progress percentage when modifier becomes active (0.0 to 1.0)</param>
/// <param name="EndPercent">Race progress percentage when modifier ends (0.0 to 1.0)</param>
/// <param name="Multiplier">Speed multiplier during active phase (e.g., 1.04 = +4% speed)</param>
public record PhaseModifier(
    double StartPercent,
    double EndPercent,
    double Multiplier
);
