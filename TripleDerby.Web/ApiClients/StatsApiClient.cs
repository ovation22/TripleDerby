using TripleDerby.SharedKernel.Horses;
using TripleDerby.Web.ApiClients.Abstractions;

namespace TripleDerby.Web.ApiClients;

public class StatsApiClient : IStatsApiClient
{
    private readonly HttpClient _http;

    public StatsApiClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<IEnumerable<HorseColorStats>> GetHorseColorStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<IEnumerable<HorseColorStats>>("api/stats/horses/colors", cancellationToken);
            return result ?? [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return [];
        }
    }

    public async Task<IEnumerable<HorseLegTypeStats>> GetHorseLegTypeStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<IEnumerable<HorseLegTypeStats>>("api/stats/horses/legtypes", cancellationToken);
            return result ?? [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return [];
        }
    }

    public async Task<IEnumerable<HorseGenderStats>> GetHorseGenderStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<IEnumerable<HorseGenderStats>>("api/stats/horses/genders", cancellationToken);
            return result ?? [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return [];
        }
    }
}