using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Abstractions.Services;

/// <summary>
/// Service for querying and managing async request messages across all services.
/// Provides unified view of Breeding, Feeding, Racing, and Training requests.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Gets aggregated status counts for all services.
    /// </summary>
    Task<MessageRequestsSummaryResult> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated list of all requests across services with optional filtering.
    /// </summary>
    Task<PagedList<MessageRequestSummary>> GetAllRequestsAsync(
        PaginationRequest pagination,
        RequestStatus? statusFilter = null,
        RequestServiceType? serviceTypeFilter = null,
        CancellationToken cancellationToken = default);
}
