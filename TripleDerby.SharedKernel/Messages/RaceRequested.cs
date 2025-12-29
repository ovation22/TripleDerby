namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Message requesting a race simulation.
/// Published by: API
/// Consumed by: Race Service
/// </summary>
public record RaceRequested
{
    /// <summary>
    /// Correlation ID for tracking this request through the system.
    /// Maps to RaceRequest.Id in the database.
    /// </summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The race identifier (1-10).
    /// </summary>
    public byte RaceId { get; init; }

    /// <summary>
    /// The horse entering the race.
    /// </summary>
    public Guid HorseId { get; init; }

    /// <summary>
    /// The user who requested the race (from JWT/session).
    /// </summary>
    public Guid RequestedBy { get; init; }

    /// <summary>
    /// When the request was made.
    /// </summary>
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}
