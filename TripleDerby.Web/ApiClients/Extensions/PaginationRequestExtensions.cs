using Microsoft.AspNetCore.WebUtilities;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Web.ApiClients.Extensions;

public static class PaginationRequestExtensions
{
    public static string ToQueryString(this PaginationRequest request, string basePath)
    {
        var query = new Dictionary<string, string?>
        {
            ["Page"] = request.Page.ToString(),
            ["Size"] = request.Size.ToString(),
            ["SortBy"] = request.SortBy,
            ["Direction"] = request.Direction.ToString(),
            ["Operator"] = request.Operator.ToString()
        };

        if (request.Filters is not null)
        {
            foreach (var kvp in request.Filters)
            {
                var key = kvp.Key;
                var filter = kvp.Value;

                query[$"Filters[{key}].Operator"] = filter.Operator.ToString();

                if (filter.Operator == FilterOperator.Between)
                {
                    query[$"Filters[{key}].ValueFrom"] = filter.ValueFrom;
                    query[$"Filters[{key}].ValueTo"] = filter.ValueTo;
                }
                else
                {
                    query[$"Filters[{key}].Value"] = filter.Value;
                }
            }
        }

        var dict = query
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value!, StringComparer.Ordinal);

        return QueryHelpers.AddQueryString(basePath, dict!);
    }
}