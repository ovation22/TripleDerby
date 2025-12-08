using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Abstractions.Services;

public interface IUserService
{
    Task<UserResult> Get(Guid id);
    Task<PagedList<UserResult>> Filter(PaginationRequest paginationRequest, CancellationToken cancellationToken);
}
