using Ardalis.Specification;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

internal class ByStartsWithSpec : Specification<TestEntity>
{
    public ByStartsWithSpec(string startsWith)
    {
        Query.Where(e => e.Name!.StartsWith(startsWith));
    }
}