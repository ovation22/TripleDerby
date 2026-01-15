namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Event published when feeding completes successfully.
/// Follows TrainingCompleted pattern.
/// </summary>
public sealed record FeedingCompleted(
    Guid RequestId,           // FeedingRequest.Id
    Guid HorseId,
    byte FeedingId,
    Guid SessionId,           // For cache linking
    Guid OwnerId,
    Guid FeedingSessionId,    // The created FeedingSession.Id
    DateTimeOffset CompletedDate
);
