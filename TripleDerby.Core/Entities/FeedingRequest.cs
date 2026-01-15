using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Entity tracking async feeding request status and lifecycle.
/// Follows TrainingRequest pattern.
/// </summary>
public class FeedingRequest
{
    public Guid Id { get; set; }

    public Guid HorseId { get; set; }

    public byte FeedingId { get; set; }

    public Guid SessionId { get; set; }

    public Guid? FeedingSessionId { get; set; }  // Links to completed FeedingSession

    public Guid OwnerId { get; set; }

    public FeedingRequestStatus Status { get; set; } = FeedingRequestStatus.Pending;

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? ProcessedDate { get; set; }
}
