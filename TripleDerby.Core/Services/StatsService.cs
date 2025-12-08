using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel.Horses;

namespace TripleDerby.Core.Services;

public class StatsService : IStatsService
{
    private readonly ITripleDerbyRepository _repository;

    public StatsService(
        ITripleDerbyRepository repository
    )
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseColorStats>> GetHorseColorStats(CancellationToken cancellationToken)
    {
        return await _repository.GetColorStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseLegTypeStats>> GetHorseLegTypeStats(CancellationToken cancellationToken)
    {
        return await _repository.GetLegTypeStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HorseGenderStats>> GetHorseGenderStats(CancellationToken cancellationToken)
    {
        return await _repository.GetGenderStatisticsAsync(cancellationToken);
    }
}
