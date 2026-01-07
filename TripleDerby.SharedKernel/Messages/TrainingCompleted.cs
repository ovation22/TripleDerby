namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Event published when training completes successfully.
/// Follows BreedingCompleted pattern.
/// Part of Feature 020: Horse Training System.
/// </summary>
public sealed record TrainingCompleted(
    Guid RequestId,           // TrainingRequest.Id
    Guid HorseId,
    byte TrainingId,
    Guid SessionId,           // For cache linking
    Guid OwnerId,
    Guid TrainingSessionId,   // The created TrainingSession.Id
    DateTimeOffset CompletedDate
);
