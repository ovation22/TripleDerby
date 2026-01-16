using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

public class UserService(ITripleDerbyRepository repository) : IUserService
{
    public async Task<UserResult> Get(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<PagedList<UserResult>> Filter(PaginationRequest request, CancellationToken cancellationToken)
    {
        var spec = new UserFilterSpecification(request);
        return await repository.ListAsync(spec, cancellationToken);
    }
}
