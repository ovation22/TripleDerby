using Ardalis.Specification;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

/// <summary>
/// Specification for querying users with pagination, filtering, and sorting.
/// Projects User entities to UserResult DTOs.
/// </summary>
public sealed class UserFilterSpecification : FilterSpecification<User, UserResult>
{
    public UserFilterSpecification(PaginationRequest request)
        : base(request, defaultSortBy: "Username", defaultSortDirection: SortDirection.Asc)
    {
        // Project to UserResult
        Query.Select(u => new UserResult
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            IsActive = u.IsActive,
            IsAdmin = u.IsAdmin
        });
    }
}
