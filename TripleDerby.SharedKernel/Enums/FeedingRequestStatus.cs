namespace TripleDerby.SharedKernel.Enums;

/// <summary>
/// Status values for async feeding request lifecycle.
/// Follows TrainingRequestStatus pattern.
/// </summary>
public enum FeedingRequestStatus : byte
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
