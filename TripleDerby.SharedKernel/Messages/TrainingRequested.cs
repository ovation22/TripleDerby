namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Message requesting async horse training execution.
/// Follows BreedingRequested pattern.
/// </summary>
public sealed record TrainingRequested(
    Guid RequestId,     // TrainingRequest.Id for tracking
    Guid HorseId,
    byte TrainingId,
    Guid SessionId,     // For linking to cached training options
    Guid OwnerId,
    DateTimeOffset RequestedDate
);
