using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripleDerby.Web.ApiClients;

public abstract class BaseApiClient(HttpClient httpClient, ILogger logger)
{
    protected readonly HttpClient HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
                var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
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

    /// <summary>
    /// Generic POST that returns an ApiResponse&lt;T&gt; so callers can inspect success/info.
    /// </summary>
    protected async Task<ApiResponse<T>> PostAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var resp = await HttpClient.PostAsync(url, null, cancellationToken);
            var status = resp.StatusCode;

            if (resp.IsSuccessStatusCode)
            {
                await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
                return new ApiResponse<T>(true, data, null, status);
            }

            var error = await resp.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogWarning("POST {Url} returned {Status}: {Error}", url, status, error);
            return new ApiResponse<T>(false, default, error, status);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POST {Url} failed", url);
            return new ApiResponse<T>(false, default, ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Generic POST with request body that returns an ApiResponse&lt;TResponse&gt; so callers can inspect success/info.
    /// </summary>
    protected async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string url,
        TRequest requestBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await HttpClient.PostAsync(url, content, cancellationToken);
            var status = resp.StatusCode;

            if (resp.IsSuccessStatusCode)
            {
                await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                var data = await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonOptions, cancellationToken);
                return new ApiResponse<TResponse>(true, data, null, status);
            }

            var error = await resp.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogWarning("POST {Url} returned {Status}: {Error}", url, status, error);
            return new ApiResponse<TResponse>(false, default, error, status);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POST {Url} failed", url);
            return new ApiResponse<TResponse>(false, default, ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

public record ApiResponse<T>(bool Success, T? Data, string? Error, HttpStatusCode StatusCode);