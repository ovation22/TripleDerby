using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Specifications;

[Obsolete("Use FilterSpecification if not overriding propertyMappings, includes, or other custom logic.", true)]
public sealed class UserSearchSpecification : PaginatedSpecification<User>
{
    private static readonly Dictionary<string, string> propertyMappings = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserSearchSpecification" /> class with pagination
    ///     request.
    /// </summary>
    /// <param name="request">The pagination request.</param>
    public UserSearchSpecification(PaginationRequest request) : base(request.Page, request.Size)
    {
        if (request.Filters != null && request.Filters.Any())
        {
            if (request.Operator == LogicalOperator.And)
            {
                ApplyAndFilters(request.Filters, propertyMappings);
            }
            else
            {
                ApplyOrFilters(request.Filters, propertyMappings);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            ApplySorting(request.SortBy, request.Direction, propertyMappings);
        }
        else
        {
            ApplySorting("Username", SortDirection.Asc, propertyMappings);
        }
    }
}