using TripleDerby.SharedKernel.Horses;

namespace TripleDerby.Core.Abstractions.Services;

public interface IStatsService
{
    /// <summary>
    /// Gets statistics for horses grouped by color.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="HorseColorStats"/> where each element contains the color identifier,
    /// optional color name, the count of horses for that color, and the percentage (0..100)
    /// that count represents of the total horse population.
    /// </returns>
    /// <remarks>
    /// Implementations should prefer server-side aggregation (database grouping/aggregation)
    /// to avoid loading all horse entities into memory.
    /// </remarks>
    Task<IReadOnlyList<HorseColorStats>> GetHorseColorStats(CancellationToken cancellationToken);

    /// <summary>
    /// Gets statistics for horses grouped by leg type.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="HorseLegTypeStats"/> where each element contains the leg type identifier,
    /// optional leg type name, the count of horses for that leg type, and the percentage (0..100)
    /// that count represents of the total horse population.
    /// </returns>
    /// <remarks>
    /// Implementations should prefer server-side aggregation (database grouping/aggregation)
    /// to avoid loading all horse entities into memory.
    /// </remarks>
    Task<IReadOnlyList<HorseLegTypeStats>> GetHorseLegTypeStats(CancellationToken cancellationToken);

    /// <summary>
    /// Gets statistics for horses grouped by gender.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="HorseGenderStats"/> where each element contains the gender flag,
    /// optional gender display name, the count of horses for that gender, and the percentage (0..100)
    /// that count represents of the total horse population.
    /// </returns>
    /// <remarks>
    /// Implementations should prefer server-side aggregation (database grouping/aggregation)
    /// to avoid loading all horse entities into memory.
    /// </remarks>
    Task<IReadOnlyList<HorseGenderStats>> GetHorseGenderStats(CancellationToken cancellationToken);
}
