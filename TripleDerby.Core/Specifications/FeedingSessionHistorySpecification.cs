using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

/// <summary>
/// Specification for querying feeding session history with pagination, filtering, and sorting.
/// Projects FeedingSession entities to FeedingHistoryResult DTOs.
/// </summary>
public sealed class FeedingSessionHistorySpecification : FilterSpecification<FeedingSession, FeedingHistoryResult>
{
    public FeedingSessionHistorySpecification(Guid horseId, PaginationRequest request)
        : base(request, defaultSortBy: "SessionDate", defaultSortDirection: SortDirection.Desc)
    {
        // Filter by horse
        Query.Where(fs => fs.HorseId == horseId);

        // Project to FeedingHistoryResult
        Query.Select(fs => new FeedingHistoryResult
        {
            Id = fs.Id,
            FeedingName = fs.Feeding.Name,
            SessionDate = fs.SessionDate,
            Response = fs.Result,
            HappinessGain = fs.HappinessGain,
            SpeedGain = fs.SpeedGain,
            StaminaGain = fs.StaminaGain,
            AgilityGain = fs.AgilityGain,
            DurabilityGain = fs.DurabilityGain,
            UpsetStomachOccurred = fs.UpsetStomachOccurred,
            Result = fs.UpsetStomachOccurred ? "Upset Stomach" : fs.Result.ToString()
        });
    }
}
