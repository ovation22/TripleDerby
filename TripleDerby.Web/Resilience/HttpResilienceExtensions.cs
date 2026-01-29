using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace TripleDerby.Web.Resilience;

/// <summary>
/// Extension methods for configuring HTTP resilience policies
/// </summary>
public static class HttpResilienceExtensions
{
    /// <summary>
    /// Adds an HTTP client with standard resilience policies (retry + circuit breaker)
    /// to the service collection.
    /// </summary>
    /// <typeparam name="TClient">The interface type for the API client</typeparam>
    /// <typeparam name="TImplementation">The implementation type for the API client</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configureClient">Action to configure the HttpClient (typically setting BaseAddress)</param>
    /// <returns>IHttpStandardResiliencePipelineBuilder for further configuration</returns>
    public static IHttpStandardResiliencePipelineBuilder AddApiClientWithResilience<TClient, TImplementation>(
        this IServiceCollection services,
        Action<HttpClient> configureClient)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services
            .AddHttpClient<TClient, TImplementation>(configureClient)
            .AddStandardApiResilience();
    }

    /// <summary>
    /// Adds standard resilience policies to an existing HTTP client builder.
    /// Configures retry (3 attempts, exponential backoff) and circuit breaker (opens after 5 failures).
    /// </summary>
    /// <param name="builder">The HTTP client builder</param>
    /// <returns>The resilience pipeline builder for further configuration</returns>
    public static IHttpStandardResiliencePipelineBuilder AddStandardApiResilience(this IHttpClientBuilder builder)
    {
        return builder.AddStandardResilienceHandler(options =>
        {
            // Retry Configuration
            // - Retry up to 3 times on transient failures (5xx, timeouts, network errors)
            // - Use exponential backoff: 1s, 2s, 4s
            // - 4xx errors are NOT retried (client errors should not be retried)
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true; // Add jitter to prevent thundering herd

            // Circuit Breaker Configuration
            // - Open circuit after 5 consecutive failures
            // - Keep circuit open for 30 seconds before allowing test requests
            // - Require at least 10 attempts in sampling window before evaluating
            options.CircuitBreaker.FailureRatio = 1.0; // 100% failure rate to open (5 failures minimum)
            options.CircuitBreaker.MinimumThroughput = 5; // Minimum 5 requests before circuit can open
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2); // Evaluate last 2 minutes
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30); // Stay open for 30 seconds

            // Timeout Configuration
            // - Each individual attempt times out after 30 seconds
            // - Total timeout across all retries handled by TotalRequestTimeout (default 30s)
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

            // Total Request Timeout
            // - Maximum time for entire request including all retries
            // - With 3 retries and exponential backoff (1s, 2s, 4s), total ~37s + request time
            // - Set to 2 minutes to allow for retries in slow network conditions
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
        });
    }
}
