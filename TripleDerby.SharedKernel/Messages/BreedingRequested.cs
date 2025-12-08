namespace TripleDerby.SharedKernel.Messages;

public sealed record BreedingRequested(
    Guid RequestId,
    Guid SireId,
    Guid DamId,
    Guid OwnerId,
    DateTimeOffset RequestedDate
);
