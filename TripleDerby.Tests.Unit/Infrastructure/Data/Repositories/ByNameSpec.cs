using Ardalis.Specification;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

internal class ByNameSpec : Specification<TestEntity>
{
    public ByNameSpec(string name)
    {
        Query.Where(e => e.Name == name);
    }
}