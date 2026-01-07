namespace TripleDerby.Services.Training.DTOs;

/// <summary>
/// Result of a training session execution.
/// Contains stat changes, happiness impact, and ceiling status flags.
/// Part of Feature 020: Horse Training System.
/// </summary>
public record TrainingSessionResult
{
    /// <summary>
    /// Unique identifier for the training session record.
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Name of the training type performed.
    /// </summary>
    public string TrainingName { get; init; } = null!;

    /// <summary>
    /// Indicates whether the training session completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Human-readable message describing the result.
    /// </summary>
    public string Message { get; init; } = null!;

    /// <summary>
    /// Speed stat gain from this training session.
    /// </summary>
    public double SpeedGain { get; init; }

    /// <summary>
    /// Stamina stat gain from this training session.
    /// </summary>
    public double StaminaGain { get; init; }

    /// <summary>
    /// Agility stat gain from this training session.
    /// </summary>
    public double AgilityGain { get; init; }

    /// <summary>
    /// Durability stat gain from this training session.
    /// </summary>
    public double DurabilityGain { get; init; }

    /// <summary>
    /// Happiness change from this training session (negative for cost, positive for recovery).
    /// </summary>
    public double HappinessChange { get; init; }

    /// <summary>
    /// Indicates whether overwork occurred during training.
    /// Overwork applies additional happiness penalty and reduces training effectiveness.
    /// </summary>
    public bool OverworkOccurred { get; init; }

    /// <summary>
    /// Horse's current happiness after training.
    /// </summary>
    public double CurrentHappiness { get; init; }

    /// <summary>
    /// Indicates whether Speed stat has reached its genetic ceiling.
    /// </summary>
    public bool SpeedAtCeiling { get; init; }

    /// <summary>
    /// Indicates whether Stamina stat has reached its genetic ceiling.
    /// </summary>
    public bool StaminaAtCeiling { get; init; }

    /// <summary>
    /// Indicates whether Agility stat has reached its genetic ceiling.
    /// </summary>
    public bool AgilityAtCeiling { get; init; }

    /// <summary>
    /// Indicates whether Durability stat has reached its genetic ceiling.
    /// </summary>
    public bool DurabilityAtCeiling { get; init; }
}
