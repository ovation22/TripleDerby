namespace TripleDerby.SharedKernel.Enums;

/// <summary>
/// Status of a race request in the microservice architecture.
/// Follows the same pattern as BreedingRequestStatus for consistency.
/// </summary>
public enum RaceRequestStatus : byte
{
    /// <summary>
    /// Request has been created but not yet picked up by the race service.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Request is currently being processed by the race service.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Request has been completed successfully and RaceRun created.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Request failed due to an error during processing.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Request was cancelled by the user or system.
    /// </summary>
    Cancelled = 4
}
