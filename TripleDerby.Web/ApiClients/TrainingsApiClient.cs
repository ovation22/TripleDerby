using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;
using TripleDerby.Web.ApiClients.Abstractions;

namespace TripleDerby.Web.ApiClients;

/// <summary>
/// API client for training catalog and operations.
/// </summary>
public class TrainingsApiClient(HttpClient httpClient, ILogger<TrainingsApiClient> logger)
    : BaseApiClient(httpClient, logger), ITrainingsApiClient
{
    public async Task<List<TrainingsResult>?> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<List<TrainingsResult>>("/api/trainings", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get trainings. Status: {Status} Error: {Error}", resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<TrainingResult?> GetByIdAsync(byte trainingId, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<TrainingResult>($"/api/trainings/{trainingId}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get training {Id}. Status: {Status} Error: {Error}", trainingId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<TrainHorseResponse?> CreateRequestAsync(Guid horseId, byte trainingId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var request = new { HorseId = horseId, TrainingId = trainingId, SessionId = sessionId };
        var resp = await PostAsync<object, TrainHorseResponse>("/api/trainings/requests", request, cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to create training request for horse {HorseId}. Status: {Status} Error: {Error}", horseId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<TrainingRequestStatusResult?> GetRequestStatusAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<TrainingRequestStatusResult>($"/api/trainings/requests/{sessionId}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get training request status {SessionId}. Status: {Status} Error: {Error}", sessionId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<bool> ReplayRequestAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var resp = await PostAsync<object, object>($"/api/trainings/requests/{sessionId}/replay", null!, cancellationToken);

        if (resp.Success)
            return true;

        Logger.LogError("Unable to replay training request {SessionId}. Status: {Status} Error: {Error}", sessionId, resp.StatusCode, resp.Error);
        return false;
    }

    public async Task<List<TrainingOptionResult>?> GetTrainingOptionsAsync(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<List<TrainingOptionResult>>($"/api/trainings/options?horseId={horseId}&sessionId={sessionId}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get training options for horse {HorseId}. Status: {Status} Error: {Error}", horseId, resp.StatusCode, resp.Error);
        return null;
    }

    public async Task<PagedList<TrainingHistoryResult>?> GetTrainingHistoryAsync(Guid horseId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var queryString = $"horseId={horseId}&Page={request.Page}&Size={request.Size}";

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            queryString += $"&SortBy={request.SortBy}&Direction={request.Direction}";
        }

        var resp = await SearchAsync<PagedList<TrainingHistoryResult>>($"/api/trainings/history?{queryString}", cancellationToken);

        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get training history for horse {HorseId}. Status: {Status} Error: {Error}", horseId, resp.StatusCode, resp.Error);
        return null;
    }
}
