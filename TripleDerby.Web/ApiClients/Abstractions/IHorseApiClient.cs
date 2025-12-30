using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IHorseApiClient
{
    Task<HorseResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedList<HorseResult>?> FilterAsync(PaginationRequest request, CancellationToken cancellationToken = default);
}