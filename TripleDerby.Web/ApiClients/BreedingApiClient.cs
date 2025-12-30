using TripleDerby.SharedKernel;
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
}
