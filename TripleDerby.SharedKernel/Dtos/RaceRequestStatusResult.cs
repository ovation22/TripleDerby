using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel.Dtos;

/// <summary>
/// Result DTO for race request status queries.
/// Mirrors BreedingRequestStatusResult pattern for consistency.
/// </summary>
public record RaceRequestStatusResult(
    Guid Id,
    byte RaceId,
    Guid HorseId,
    RaceRequestStatus Status,
    Guid? RaceRunId,
    Guid OwnerId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? ProcessedDate,
    DateTimeOffset? UpdatedDate,
    string? FailureReason
);
