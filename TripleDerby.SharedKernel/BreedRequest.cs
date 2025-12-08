namespace TripleDerby.SharedKernel;

public record BreedRequest
{
    public Guid UserId { get; init; }

    public Guid SireId { get; init; }

    public Guid DamId { get; init; }
}
