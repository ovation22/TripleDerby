using TripleDerby.Core.Entities;
using TripleDerby.Services.Training.DTOs;

namespace TripleDerby.Services.Training.Abstractions;

/// <summary>
/// Service for managing horse training sessions.
/// Orchestrates training execution, validation, and history retrieval.
/// Part of Feature 020: Horse Training System.
/// </summary>
public interface ITrainingService
{
    /// <summary>
    /// Executes a training session for a horse.
    /// Validates eligibility, calculates stat gains, applies changes, and persists session.
    /// </summary>
    /// <param name="horseId">ID of the horse to train</param>
    /// <param name="trainingId">ID of the training type to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training session result with stat changes and status</returns>
    /// <exception cref="KeyNotFoundException">Thrown if horse or training not found</exception>
    /// <exception cref="InvalidOperationException">Thrown if horse is not eligible to train</exception>
    Task<TrainingSessionResult> TrainAsync(
        Guid horseId,
        byte trainingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves training history for a horse, ordered by most recent first.
    /// </summary>
    /// <param name="horseId">ID of the horse</param>
    /// <param name="limit">Maximum number of sessions to return (default 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of training sessions</returns>
    Task<List<TrainingSession>> GetTrainingHistoryAsync(
        Guid horseId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a horse is eligible to train.
    /// Returns false if horse has already trained since last race or happiness is below minimum.
    /// </summary>
    /// <param name="horse">Horse to check</param>
    /// <returns>True if horse can train, false otherwise</returns>
    bool CanTrain(Horse horse);
}
