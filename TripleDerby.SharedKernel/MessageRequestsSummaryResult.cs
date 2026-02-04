namespace TripleDerby.SharedKernel;

/// <summary>
/// Aggregated summary of request statuses across all services.
/// Used for dashboard widgets and monitoring.
/// </summary>
public record MessageRequestsSummaryResult
{
    public ServiceSummary Breeding { get; init; } = new();
    public ServiceSummary Feeding { get; init; } = new();
    public ServiceSummary Racing { get; init; } = new();
    public ServiceSummary Training { get; init; } = new();
    public int TotalPending { get; init; }
    public int TotalFailed { get; init; }
}

/// <summary>
/// Status counts for a single service.
/// </summary>
public record ServiceSummary
{
    public int Pending { get; init; }
    public int Failed { get; init; }
    public int Completed { get; init; }
    public int InProgress { get; init; }
}
