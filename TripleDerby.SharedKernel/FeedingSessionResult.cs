using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

/// <summary>
/// DTO for feeding session results.
/// Used to display feeding outcomes in the UI.
/// </summary>
public record FeedingSessionResult
{
    public Guid SessionId { get; init; }
    public string FeedingName { get; init; } = null!;
    public FeedResponse Result { get; init; }

    // Stat gains
    public double HappinessGain { get; init; }
    public double SpeedGain { get; init; }
    public double StaminaGain { get; init; }
    public double AgilityGain { get; init; }
    public double DurabilityGain { get; init; }

    // Discovery info
    public bool PreferenceDiscovered { get; init; }
    public string? DiscoveryMessage { get; init; }  // e.g., "Thunderbolt LOVES apples!"

    // Negative events
    public bool UpsetStomachOccurred { get; init; }
}
