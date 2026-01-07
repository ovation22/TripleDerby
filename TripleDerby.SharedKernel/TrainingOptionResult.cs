namespace TripleDerby.SharedKernel;

/// <summary>
/// DTO for a training option available to a horse.
/// Part of Feature 020: Horse Training System.
/// </summary>
public record TrainingOptionResult
{
    public byte Id { get; init; }
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public double SpeedModifier { get; init; }
    public double StaminaModifier { get; init; }
    public double AgilityModifier { get; init; }
    public double DurabilityModifier { get; init; }
    public double HappinessCost { get; init; }
    public double OverworkRisk { get; init; }
    public bool IsRecovery { get; init; }
}
