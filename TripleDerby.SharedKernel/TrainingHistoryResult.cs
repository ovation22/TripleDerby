namespace TripleDerby.SharedKernel;

/// <summary>
/// DTO for a past training session in a horse's training history.
/// </summary>
public record TrainingHistoryResult
{
    public Guid Id { get; init; }
    public string TrainingName { get; init; } = null!;
    public DateTime SessionDate { get; init; }
    public double SpeedGain { get; init; }
    public double StaminaGain { get; init; }
    public double AgilityGain { get; init; }
    public double DurabilityGain { get; init; }
    public double HappinessChange { get; init; }
    public bool OverworkOccurred { get; init; }
    public string Result { get; init; } = null!;
}
