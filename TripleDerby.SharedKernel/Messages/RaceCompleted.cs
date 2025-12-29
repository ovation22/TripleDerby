namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Message indicating race simulation completed.
/// Published by: Race Service
/// Consumed by: API (future: WebSocket/SignalR for real-time updates)
/// </summary>
public record RaceCompleted
{
    /// <summary>
    /// Correlation ID linking back to the original RaceRequested message.
    /// Maps to RaceRequest.Id in the database.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The RaceRun ID created for this race.
    /// </summary>
    public Guid RaceRunId { get; init; }

    /// <summary>
    /// The race identifier.
    /// </summary>
    public byte RaceId { get; init; }

    /// <summary>
    /// The name of the race that was run.
    /// </summary>
    public string RaceName { get; init; } = string.Empty;

    /// <summary>
    /// The ID of the winning horse.
    /// </summary>
    public Guid WinnerHorseId { get; init; }

    /// <summary>
    /// The name of the winning horse.
    /// </summary>
    public string WinnerName { get; init; } = string.Empty;

    /// <summary>
    /// The winning time in seconds.
    /// </summary>
    public double WinnerTime { get; init; }

    /// <summary>
    /// Number of horses in the race.
    /// </summary>
    public int FieldSize { get; init; }

    /// <summary>
    /// When the race completed.
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Full result details (for immediate response).
    /// Note: RaceRunResult must be serializable.
    /// </summary>
    public RaceRunResult? Result { get; init; }
}
