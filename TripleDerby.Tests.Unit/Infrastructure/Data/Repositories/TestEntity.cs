// Simple test entity used for DbSet<T>
namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

public class TestEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
