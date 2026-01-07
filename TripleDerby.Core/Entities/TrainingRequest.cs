using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Entity tracking async training request status and lifecycle.
/// Part of Feature 020: Horse Training System Phase 5.
/// </summary>
public class TrainingRequest
{
    public Guid Id { get; set; }

    public Guid HorseId { get; set; }

    public byte TrainingId { get; set; }

    public Guid SessionId { get; set; }

    public Guid? TrainingSessionId { get; set; }  // Links to completed TrainingSession

    public Guid OwnerId { get; set; }

    public TrainingRequestStatus Status { get; set; } = TrainingRequestStatus.Pending;

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? ProcessedDate { get; set; }
}
