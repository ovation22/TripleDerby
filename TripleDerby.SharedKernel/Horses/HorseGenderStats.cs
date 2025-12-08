namespace TripleDerby.SharedKernel.Horses;

/// <summary>
/// DTO for gender statistics.
/// </summary>
public sealed record HorseGenderStats(bool IsMale, string? GenderName, int Count, double Percentage);