using TripleDerby.SharedKernel;

namespace TripleDerby.Web.ApiClients.Abstractions;

/// <summary>
/// API client for training catalog and operations.
/// </summary>
public interface ITrainingsApiClient
{
    Task<List<TrainingsResult>?> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TrainingResult?> GetByIdAsync(byte trainingId, CancellationToken cancellationToken = default);
    Task<TrainHorseResponse?> CreateRequestAsync(Guid horseId, byte trainingId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<TrainingRequestStatusResult?> GetRequestStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> ReplayRequestAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<TrainingOptionResult>?> GetTrainingOptionsAsync(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<TrainingHistoryResult>?> GetTrainingHistoryAsync(Guid horseId, int limit = 10, CancellationToken cancellationToken = default);
}

public record TrainHorseResponse(Guid SessionId, string Status);
