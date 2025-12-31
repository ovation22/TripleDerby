using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.Web.ApiClients.Abstractions;

namespace TripleDerby.Web.ApiClients;

public class BreedingApiClient(HttpClient httpClient, ILogger<BreedingApiClient> logger)
    : BaseApiClient(httpClient, logger), IBreedingApiClient
{
    /// <summary>
    /// Fetch cached dams (server returns ~10 cached items).
    /// </summary>
    public async Task<IEnumerable<HorseResult>?> GetDamsAsync(CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<List<HorseResult>>("/api/breeding/dams", cancellationToken);
        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get dams. Status: {Status} Error: {Error}", resp.StatusCode, resp.Error);

        return null;
    }

    /// <summary>
    /// Fetch cached sires (server returns ~10 cached items).
    /// </summary>
    public async Task<IEnumerable<HorseResult>?> GetSiresAsync(CancellationToken cancellationToken = default)
    {
        var resp = await SearchAsync<List<HorseResult>>("/api/breeding/sires", cancellationToken);
        if (resp.Success)
            return resp.Data;

        Logger.LogError("Unable to get sires. Status: {Status} Error: {Error}", resp.StatusCode, resp.Error);

        return null;
    }

    /// <summary>
    /// Submits a breeding request and returns the initial status.
    /// </summary>
    public async Task<BreedingRequestStatusResult?> SubmitBreedingAsync(
        Guid sireId,
        Guid damId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var request = new BreedRequest
        {
            SireId = sireId,
            DamId = damId,
            UserId = ownerId
        };

        var resp = await PostAsync<BreedRequest, Resource<BreedingRequested>>("/api/breeding/requests", request, cancellationToken);

        if (resp.Success && resp.Data != null)
        {
            // Extract the breeding request ID from the response and fetch the full status
            var breedingRequested = resp.Data.Data;

            // The BreedingRequested message contains RequestId which is the BreedingRequest.Id
            // We need to fetch the full status to get the BreedingRequestStatusResult
            var statusUrl = $"/api/breeding/requests/{breedingRequested.RequestId}";
            var statusResp = await GetAsync<Resource<BreedingRequestStatusResult>>(statusUrl, cancellationToken);

            if (statusResp.Success && statusResp.Data != null)
                return statusResp.Data.Data;

            Logger.LogError("Unable to get breeding request status after submission. RequestId: {RequestId}, Status: {Status} Error: {Error}",
                breedingRequested.RequestId, statusResp.StatusCode, statusResp.Error);
            return null;
        }

        Logger.LogError("Unable to submit breeding request. SireId: {SireId}, DamId: {DamId}, OwnerId: {OwnerId}, Status: {Status} Error: {Error}",
            sireId, damId, ownerId, resp.StatusCode, resp.Error);
        return null;
    }
}
