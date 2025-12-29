using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel.Dtos;

/// <summary>
/// Response DTO for race request status queries (with URLs for API responses).
/// </summary>
public record RaceRequestStatusResponse(
    Guid RequestId,
    byte RaceId,
    Guid HorseId,
    RaceRequestStatus Status,
    Guid? RaceRunId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? ProcessedDate,
    string? FailureReason,
    string? ResultUrl
);
