namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Message requesting async horse feeding execution.
/// Follows TrainingRequested pattern.
/// </summary>
public sealed record FeedingRequested(
    Guid RequestId,     // FeedingRequest.Id for tracking
    Guid HorseId,
    byte FeedingId,
    Guid SessionId,     // For linking to cached feeding options
    Guid OwnerId,
    DateTimeOffset RequestedDate
);
