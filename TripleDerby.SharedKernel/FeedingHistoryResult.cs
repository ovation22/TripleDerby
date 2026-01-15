using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

/// <summary>
/// DTO for a past feeding session in a horse's feeding history.
/// </summary>
public record FeedingHistoryResult
{
    public Guid Id { get; init; }
    public string FeedingName { get; init; } = null!;
    public DateTime SessionDate { get; init; }
    public FeedResponse Response { get; init; }
    public double HappinessGain { get; init; }
    public double SpeedGain { get; init; }
    public double StaminaGain { get; init; }
    public double AgilityGain { get; init; }
    public double DurabilityGain { get; init; }
    public bool UpsetStomachOccurred { get; init; }
    public string Result { get; init; } = null!;
}
