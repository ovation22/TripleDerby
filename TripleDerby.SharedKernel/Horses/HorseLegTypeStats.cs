using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel.Horses;

/// <summary>
/// DTO for leg type statistics.
/// </summary>
public sealed record HorseLegTypeStats(LegTypeId LegTypeId, string? LegTypeName, int Count, double Percentage);