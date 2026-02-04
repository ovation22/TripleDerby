namespace TripleDerby.SharedKernel.Enums;

/// <summary>
/// Unified status enum for all request types (Breeding, Feeding, Racing, Training).
/// Maps to individual service status enums with identical values.
/// </summary>
public enum RequestStatus : byte
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
