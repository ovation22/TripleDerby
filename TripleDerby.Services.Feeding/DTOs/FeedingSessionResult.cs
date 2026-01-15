using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Feeding.DTOs;

/// <summary>
/// Result DTO for a completed feeding session.
/// </summary>
public record FeedingSessionResult
{
    public Guid SessionId { get; init; }
    public string FeedingName { get; init; } = null!;
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public FeedResponse Response { get; init; }
    public double HappinessGain { get; init; }
    public double SpeedGain { get; init; }
    public double StaminaGain { get; init; }
    public double AgilityGain { get; init; }
    public double DurabilityGain { get; init; }
    public bool UpsetStomachOccurred { get; init; }
    public bool PreferenceDiscovered { get; init; }
    public double CurrentHappiness { get; init; }
    public bool SpeedAtCeiling { get; init; }
    public bool StaminaAtCeiling { get; init; }
    public bool AgilityAtCeiling { get; init; }
    public bool DurabilityAtCeiling { get; init; }
}
