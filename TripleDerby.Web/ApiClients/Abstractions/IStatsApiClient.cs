using TripleDerby.SharedKernel.Horses;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IStatsApiClient
{
    /// <summary>
    /// Retrieves horse color statistics from the API.
    /// </summary>
    Task<IEnumerable<HorseColorStats>> GetHorseColorStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves horse leg type statistics from the API.
    /// </summary>
    Task<IEnumerable<HorseLegTypeStats>> GetHorseLegTypeStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves horse gender statistics from the API.
    /// </summary>
    Task<IEnumerable<HorseGenderStats>> GetHorseGenderStatsAsync(CancellationToken cancellationToken = default);
}