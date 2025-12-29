using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Processes race requests from the message queue.
/// </summary>
public interface IRaceRequestProcessor
{
    /// <summary>
    /// Processes a race request and returns the result.
    /// </summary>
    /// <param name="request">The race request to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The race run result</returns>
    Task<RaceRunResult> ProcessAsync(RaceRequested request, CancellationToken cancellationToken);
}
