using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class BreedingRequest
{
    public Guid Id { get; set; }

    public Guid SireId { get; set; }

    public Guid DamId { get; set; }

    public Guid? FoalId { get; set; }

    public Guid OwnerId { get; set; }

    public BreedingRequestStatus Status { get; set; } = BreedingRequestStatus.Pending;

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? ProcessedDate { get; set; }
}