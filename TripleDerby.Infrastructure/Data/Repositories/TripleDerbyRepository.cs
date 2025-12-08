using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Horses;

namespace TripleDerby.Infrastructure.Data.Repositories;

public class TripleDerbyRepository : EFRepository, ITripleDerbyRepository
{
    private readonly TripleDerbyContext _dbContext;
    /// <summary>
    ///     Initializes a new instance of the <see cref="TripleDerbyRepository" /> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger"></param>
    public TripleDerbyRepository(TripleDerbyContext dbContext, ILogger<TripleDerbyRepository> logger)
        : base(dbContext, logger)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseColorStats>> GetColorStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var horses = _dbContext.Set<Horse>().AsQueryable();
        var colors = _dbContext.Set<Color>().AsQueryable();

        var total = await horses.CountAsync(cancellationToken);
        if (total == 0) return [];

        var grouped = horses
            .GroupBy(h => h.ColorId)
            .Select(g => new { ColorId = g.Key, Count = g.Count() });

        var joined = await grouped
            .Join(colors, g => g.ColorId, c => c.Id, (g, c) => new { g.ColorId, c.Name, g.Count })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var stats = joined
            .Select(x => new HorseColorStats(x.ColorId, x.Name, x.Count, x.Count * 100.0 / total))
            .ToList();

        return stats.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseLegTypeStats>> GetLegTypeStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var horses = _dbContext.Set<Horse>().AsQueryable();
        var legTypes = _dbContext.Set<LegType>().AsQueryable();

        var total = await horses.CountAsync(cancellationToken);
        if (total == 0) return [];

        var grouped = horses
            .GroupBy(h => h.LegTypeId)
            .Select(g => new { LegTypeId = g.Key, Count = g.Count() });

        var joined = await grouped
            .Join(legTypes, g => g.LegTypeId, lt => lt.Id, (g, lt) => new { g.LegTypeId, lt.Name, g.Count })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var stats = joined
            .Select(x => new HorseLegTypeStats(x.LegTypeId, x.Name, x.Count, x.Count * 100.0 / total))
            .ToList();

        return stats.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseGenderStats>> GetGenderStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var horses = _dbContext.Set<Horse>().AsQueryable();

        var total = await horses.CountAsync(cancellationToken);
        if (total == 0) return [];

        var grouped = await horses
            .GroupBy(h => h.IsMale)
            .Select(g => new { IsMale = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var stats = grouped
            .Select(g =>
            {
                var name = g.IsMale ? "Male" : "Female";
                return new HorseGenderStats(g.IsMale, name, g.Count, g.Count * 100.0 / total);
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        return stats.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task UpdateParentedAsync(Guid sireId, Guid damId, CancellationToken cancellationToken = default)
    {
        // Use EF Core set-based update to increment Parented for both parent rows.
        // ExecuteUpdateAsync produces a single UPDATE statement and avoids read-modify-write concurrency issues.
        await _dbContext.Set<Horse>()
            .Where(h => h.Id == sireId || h.Id == damId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(h => h.Parented, h => h.Parented + 1),
                cancellationToken)
            ;
    }
}