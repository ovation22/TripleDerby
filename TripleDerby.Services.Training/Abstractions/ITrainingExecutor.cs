using TripleDerby.Services.Training.DTOs;

namespace TripleDerby.Services.Training.Abstractions;

/// <summary>
/// Interface for training execution logic.
/// </summary>
public interface ITrainingExecutor
{
    /// <summary>
    /// Executes a training session for a horse.
    /// </summary>
    Task<TrainingSessionResult> ExecuteTrainingAsync(
        Guid horseId,
        byte trainingId,
        CancellationToken cancellationToken = default);
}
