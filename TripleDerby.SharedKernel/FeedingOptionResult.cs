using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

/// <summary>
/// DTO for a feeding option available to a horse.
/// </summary>
public record FeedingOptionResult
{
    public byte Id { get; init; }
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public FeedingCategoryId CategoryId { get; init; }
    public double HappinessMin { get; init; }
    public double HappinessMax { get; init; }
    public double SpeedMin { get; init; }
    public double SpeedMax { get; init; }
    public double StaminaMin { get; init; }
    public double StaminaMax { get; init; }
    public double AgilityMin { get; init; }
    public double AgilityMax { get; init; }
    public double DurabilityMin { get; init; }
    public double DurabilityMax { get; init; }
}
