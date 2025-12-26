using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

/// <summary>
/// Summary of a race run (for list views, no play-by-play).
/// Used for GET /api/races/{raceId}/runs
/// </summary>
public record RaceRunSummary
{
    public Guid RaceRunId { get; init; }
    public ConditionId ConditionId { get; init; }
    public string ConditionName { get; init; } = null!;
    public string WinnerName { get; init; } = null!;
    public double WinnerTime { get; init; }
    public int FieldSize { get; init; }
}
