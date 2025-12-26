using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Represents a persisted race request in the microservice architecture.
/// Follows same pattern as BreedingRequest for consistency.
/// Stored in 'rac' schema (mirroring Breeding's 'brd' schema).
/// </summary>
public class RaceRequest
{
    /// <summary>
    /// Unique identifier for this race request (same as CorrelationId in messages).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The race being run (1-10).
    /// </summary>
    public byte RaceId { get; set; }

    /// <summary>
    /// The horse entered in the race.
    /// </summary>
    public Guid HorseId { get; set; }

    /// <summary>
    /// The RaceRun created after successful completion (null until completed).
    /// </summary>
    public Guid? RaceRunId { get; set; }

    /// <summary>
    /// The owner of the horse making the request.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Current status of the race request.
    /// </summary>
    public RaceRequestStatus Status { get; set; } = RaceRequestStatus.Pending;

    /// <summary>
    /// Reason for failure if Status is Failed.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// When the request was created.
    /// </summary>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// Who created the request (user ID or system).
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// When the request was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedDate { get; set; }

    /// <summary>
    /// Who last updated the request.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// When the request was completed/failed.
    /// </summary>
    public DateTimeOffset? ProcessedDate { get; set; }
}
