using Microsoft.EntityFrameworkCore;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

internal class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;
}