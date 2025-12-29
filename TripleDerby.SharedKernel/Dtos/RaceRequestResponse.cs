using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel.Dtos;

/// <summary>
/// Response DTO for race request creation.
/// </summary>
public record RaceRequestResponse(
    Guid RequestId,
    RaceRequestStatus Status,
    string Message,
    string? StatusUrl = null
);
