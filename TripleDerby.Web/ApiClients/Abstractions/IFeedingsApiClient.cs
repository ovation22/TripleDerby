using TripleDerby.SharedKernel;

namespace TripleDerby.Web.ApiClients.Abstractions;

/// <summary>
/// API client for feeding catalog and operations.
/// </summary>
public interface IFeedingsApiClient
{
    Task<List<FeedingsResult>?> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FeedingResult?> GetByIdAsync(byte feedingId, CancellationToken cancellationToken = default);
    Task<FeedHorseResponse?> CreateRequestAsync(Guid horseId, byte feedingId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<FeedingRequestStatusResult?> GetRequestStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> ReplayRequestAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<FeedingOptionResult>?> GetFeedingOptionsAsync(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<FeedingHistoryResult>?> GetFeedingHistoryAsync(Guid horseId, int limit = 10, CancellationToken cancellationToken = default);
    Task<FeedingSessionResult?> GetFeedingSessionResultAsync(Guid feedingSessionId, CancellationToken cancellationToken = default);
}

public record FeedHorseResponse(Guid SessionId, string Status);
