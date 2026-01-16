using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

/// <summary>
/// Specification for querying training session history with pagination, filtering, and sorting.
/// Projects TrainingSession entities to TrainingHistoryResult DTOs.
/// </summary>
public sealed class TrainingSessionHistorySpecification : FilterSpecification<TrainingSession, TrainingHistoryResult>
{
    public TrainingSessionHistorySpecification(Guid horseId, PaginationRequest request)
        : base(request, defaultSortBy: "SessionDate", defaultSortDirection: SortDirection.Desc)
    {
        // Filter by horse
        Query.Where(ts => ts.HorseId == horseId);

        // Project to TrainingHistoryResult
        Query.Select(ts => new TrainingHistoryResult
        {
            Id = ts.Id,
            TrainingName = ts.Training.Name,
            SessionDate = ts.SessionDate,
            SpeedGain = ts.SpeedGain,
            StaminaGain = ts.StaminaGain,
            AgilityGain = ts.AgilityGain,
            DurabilityGain = ts.DurabilityGain,
            HappinessChange = ts.HappinessChange,
            OverworkOccurred = ts.OverworkOccurred,
            Result = ts.OverworkOccurred ? "Overworked" : "Success"
        });
    }
}
