using Microsoft.EntityFrameworkCore;

namespace TripleDerby.Tests.Unit.Infrastructure.Data.Repositories;

internal class ThrowingDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new DbUpdateException("Simulated update failure");
    }
}