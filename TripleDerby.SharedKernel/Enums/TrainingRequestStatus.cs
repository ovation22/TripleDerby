namespace TripleDerby.SharedKernel.Enums;

/// <summary>
/// Status of an async training request.
/// Follows the same pattern as BreedingRequestStatus for consistency.
/// </summary>
public enum TrainingRequestStatus : byte
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
