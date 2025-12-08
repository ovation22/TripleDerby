using System;

namespace TripleDerby.SharedKernel.Messages;

public sealed record FoalCreated(
    Guid FoalId,
    Guid RequestId,
    string Name,
    Guid SireId,
    Guid DamId,
    DateTimeOffset CreatedAt
);