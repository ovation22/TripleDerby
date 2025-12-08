using Microsoft.AspNetCore.JsonPatch;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Abstractions.Services;

public interface IHorseService
{
    Task<HorseResult> Get(Guid id);
    Task<PagedList<HorseResult>> Filter(PaginationRequest paginationRequest, CancellationToken cancellationToken);
    Task Update(Guid id, JsonPatchDocument<HorsePatch> patch);
}
