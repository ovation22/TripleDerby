using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

public record BreedingResult
{
    public Guid FoalId { get; init; }

    public string FoalName { get; init; } = null!;

    public Guid SireId { get; init; }

    public Guid DamId { get; init; }

    public Guid OwnerId { get; init; }

    public bool IsMale { get; init; }

    public int ColorId { get; init; }

    public LegTypeId LegTypeId { get; init; }
}
