using System.Text.Json.Serialization;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel.Dtos;

public record BreedingRequestStatusResult
{
    public Guid Id { get; init; }

    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    public BreedingRequestStatus Status { get; init; }

    public Guid? FoalId { get; init; }

    public Guid OwnerId { get; init; }

    public DateTimeOffset CreatedDate { get; init; }

    public DateTimeOffset? ProcessedDate { get; init; }

    public DateTimeOffset? UpdatedDate { get; init; }

    public string? FailureReason { get; init; }

    public BreedingRequestStatusResult() { }

    public BreedingRequestStatusResult(Guid id, BreedingRequestStatus status, Guid? foalId, Guid ownerId, DateTimeOffset createdDate, DateTimeOffset? processedDate, DateTimeOffset? updatedDate, string? failureReason)
    {
        Id = id;
        Status = status;
        FoalId = foalId;
        OwnerId = ownerId;
        CreatedDate = createdDate;
        ProcessedDate = processedDate;
        UpdatedDate = updatedDate;
        FailureReason = failureReason;
    }
}
