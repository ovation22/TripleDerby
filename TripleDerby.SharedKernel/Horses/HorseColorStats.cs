namespace TripleDerby.SharedKernel.Horses;

/// <summary>
/// DTO for color statistics.
/// </summary>
public sealed record HorseColorStats(byte ColorId, string? ColorName, int Count, double Percentage);
