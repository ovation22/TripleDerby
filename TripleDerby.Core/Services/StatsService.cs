using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel.Horses;

namespace TripleDerby.Core.Services;

public class StatsService(ITripleDerbyRepository repository) : IStatsService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseColorStats>> GetHorseColorStats(CancellationToken cancellationToken)
    {
        return await repository.GetColorStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseLegTypeStats>> GetHorseLegTypeStats(CancellationToken cancellationToken)
    {
        return await repository.GetLegTypeStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseGenderStats>> GetHorseGenderStats(CancellationToken cancellationToken)
    {
        return await repository.GetGenderStatisticsAsync(cancellationToken);
    }
}
