using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

/// <summary>
/// DTO for feeding request status polling.
/// Follows TrainingRequestStatusResult pattern.
/// </summary>
public record FeedingRequestStatusResult
{
    public Guid Id { get; init; }
    public Guid HorseId { get; init; }
    public byte FeedingId { get; init; }
    public Guid SessionId { get; init; }
    public FeedingRequestStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset? ProcessedDate { get; init; }
}
