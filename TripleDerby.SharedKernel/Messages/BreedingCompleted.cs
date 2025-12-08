namespace TripleDerby.Core.Messages;

public sealed class BreedingCompleted(
    Guid RequestId,
    Guid SireId,
    Guid DamId,
    Guid FoalId,
    Guid OwnerId,
    DateTimeOffset CompletedDate
);
