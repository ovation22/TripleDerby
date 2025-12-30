using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

/// <summary>
/// Specification for fetching race runs for a specific race with filtering, sorting, and pagination.
/// Projects to RaceRunSummary for grid display.
/// </summary>
public sealed class RaceRunFilterSpecification : FilterSpecification<RaceRun, RaceRunSummary>
{
    private static readonly Dictionary<string, string> PropertyMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "WinnerName", "WinHorse.Name" },
        { "ConditionName", "ConditionId" },
        { "RunDate", "CreatedDate" }
    };

    public RaceRunFilterSpecification(byte raceId, PaginationRequest request)
        : base(request, PropertyMappings, defaultSortBy: "CreatedDate", defaultSortDirection: SortDirection.Desc)
    {
        // Filter by race
        Query.Where(rr => rr.RaceId == raceId);

        // Project to RaceRunSummary
        Query.Select(rr => new RaceRunSummary
        {
            RaceRunId = rr.Id,
            ConditionId = rr.ConditionId,
            ConditionName = rr.ConditionId.ToString(),
            RaceClassId = rr.Race.RaceClassId,
            RaceClass = rr.Race.RaceClass.Name,
            WinnerName = rr.WinHorse.Name,
            WinnerTime = rr.Horses.Where(h => h.Place == 1).Select(h => h.Time).FirstOrDefault(),
            FieldSize = rr.Horses.Count,
            RunDate = rr.CreatedDate,
            Purse = rr.Purse
        });
    }
}