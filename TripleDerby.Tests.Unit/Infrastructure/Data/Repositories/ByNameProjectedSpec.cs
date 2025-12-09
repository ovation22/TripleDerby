using Ardalis.Specification;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

internal partial class ByNameProjectedSpec : Specification<TestEntity, string>
{
    public ByNameProjectedSpec(string name)
    {
        Query.Where(e => e.Name == name).Select(e => e.Name!);
    }
}