using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

/// <summary>
/// Unified view of a request from any service (Breeding, Feeding, Racing, Training).
/// Used for cross-service querying and management.
/// </summary>
public record MessageRequestSummary
{
    public Guid Id { get; init; }
    public RequestServiceType ServiceType { get; init; }
    public RequestStatus Status { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset? ProcessedDate { get; init; }
    public string? FailureReason { get; init; }

    /// <summary>
    /// Human-readable description of what this request is for.
    /// Examples: "Sire: FastHorse, Dam: QuickMare", "Horse: Champion, Training: Sprint"
    /// </summary>
    public string TargetDescription { get; init; } = string.Empty;

    /// <summary>
    /// Optional link to the target entity (horse, race, etc.)
    /// </summary>
    public string? TargetLink { get; init; }

    public Guid OwnerId { get; init; }

    /// <summary>
    /// ID of the completed result entity (FoalId, FeedingSessionId, RaceRunId, etc.)
    /// </summary>
    public Guid? ResultId { get; init; }

    /// <summary>
    /// Optional link to the result entity
    /// </summary>
    public string? ResultLink { get; init; }
}
