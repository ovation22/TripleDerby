using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IMessagesApiClient
{
    Task<MessageRequestsSummaryResult?> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<PagedList<MessageRequestSummary>?> GetAllRequestsAsync(
        PaginationRequest pagination,
        RequestStatus? status = null,
        RequestServiceType? serviceType = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReplayRequestAsync(
        RequestServiceType serviceType,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<int> ReplayAllAsync(
        RequestServiceType serviceType,
        int maxDegreeOfParallelism = 10,
        CancellationToken cancellationToken = default);
}
