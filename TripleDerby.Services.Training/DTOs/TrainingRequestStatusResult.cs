using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Training.DTOs;

/// <summary>
/// DTO for training request status.
/// Part of Feature 020: Horse Training System.
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
