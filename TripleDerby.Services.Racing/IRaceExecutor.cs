using TripleDerby.SharedKernel;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Interface for executing race simulations.
/// </summary>
public interface IRaceExecutor
{
    /// <summary>
    /// Executes a race simulation for the specified race and horse.
    /// </summary>
    /// <param name="raceId">The race identifier.</param>
    /// <param name="horseId">The horse identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete race run result including all horse results and play-by-play.</returns>
    Task<RaceRunResult> Race(byte raceId, Guid horseId, CancellationToken cancellationToken = default);
}
