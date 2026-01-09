using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

/// <summary>
/// DTO for training request status polling.
/// </summary>
public record TrainingRequestStatusResult
{
    public Guid Id { get; init; }
    public Guid HorseId { get; init; }
    public byte TrainingId { get; init; }
    public Guid SessionId { get; init; }
    public TrainingRequestStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset? ProcessedDate { get; init; }
}
