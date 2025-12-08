using TripleDerby.SharedKernel.Horses;

namespace TripleDerby.Core.Abstractions.Repositories;

public interface ITripleDerbyRepository : IEFRepository
{
    /// <summary>
    /// Returns counts and percentage distribution of horses per color.
    /// </summary>
    Task<IReadOnlyList<HorseColorStats>> GetColorStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns counts and percentage distribution of horses per leg type.
    /// </summary>
    Task<IReadOnlyList<HorseLegTypeStats>> GetLegTypeStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns counts and percentage distribution of horses by gender.
    /// </summary>
    Task<IReadOnlyList<HorseGenderStats>> GetGenderStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment the Parented counter for both sire and dam.
    /// This method performs a set-based update and should be called inside a transaction
    /// when paired with creating the foal to ensure atomicity.
    /// </summary>
    Task UpdateParentedAsync(Guid sireId, Guid damId, CancellationToken cancellationToken = default);
}
