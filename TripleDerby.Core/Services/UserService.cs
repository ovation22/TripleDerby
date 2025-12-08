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
        var spec = new FilterSpecification<User>(
            request,
            defaultSortBy: "Username",
            defaultSortDirection: SortDirection.Asc);
        var pagedUsers = await repository.ListAsync(spec, cancellationToken);

        var users = pagedUsers.Data.Select(x => new UserResult
        {
            Id = x.Id,
            Username = x.Username,
            Email = x.Email,
            IsActive = x.IsActive,
            IsAdmin = x.IsAdmin
        }).ToList();

        return new PagedList<UserResult>(users, pagedUsers.Total, pagedUsers.PageNumber, pagedUsers.Size);
    }
}
