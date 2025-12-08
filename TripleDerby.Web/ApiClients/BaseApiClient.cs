using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TripleDerby.Web.ApiClients;

public abstract class BaseApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected BaseApiClient(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generic GET that returns an ApiResponse&lt;T&gt; so callers can inspect success/info.
    /// </summary>
    protected async Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var resp = await HttpClient.GetAsync(url, cancellationToken);
            var status = resp.StatusCode;

            if (resp.IsSuccessStatusCode)
            {
                await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                var data = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
                return new ApiResponse<T>(true, data, null, status);
            }

            var error = await resp.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogWarning("GET {Url} returned {Status}: {Error}", url, status, error);
            return new ApiResponse<T>(false, default, error, status);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GET {Url} failed", url);
            return new ApiResponse<T>(false, default, ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Convenience alias for paged/search endpoints (keeps intent clear in callers).
    /// </summary>
    protected Task<ApiResponse<T>> SearchAsync<T>(string url, CancellationToken cancellationToken = default)
        => GetAsync<T>(url, cancellationToken);
}

public record ApiResponse<T>(bool Success, T? Data, string? Error, HttpStatusCode StatusCode);