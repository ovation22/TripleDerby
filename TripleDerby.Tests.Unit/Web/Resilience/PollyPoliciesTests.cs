using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace TripleDerby.Tests.Unit.Web.Resilience;

/// <summary>
/// Tests for HTTP resilience policies using Microsoft.Extensions.Http.Resilience
/// These tests verify that retry and circuit breaker patterns work as expected
/// </summary>
public class PollyPoliciesTests
{
    [Fact]
    public async Task HttpClient_Given5xxError_Retries3TimesBeforeReturningFailedResponse()
    {
        // Arrange
        var attemptCount = 0;

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service unavailable")
            });
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(10); // Fast for testing
                options.Retry.UseJitter = false;
                options.Retry.BackoffType = DelayBackoffType.Constant;

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("test");

        // Act
        var response = await client.GetAsync("https://test.com/api/test");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(4, attemptCount); // 1 initial attempt + 3 retries
    }

    [Fact]
    public async Task HttpClient_Given404Error_DoesNotRetry()
    {
        // Arrange
        var attemptCount = 0;

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not found")
            });
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(10);

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("test");

        // Act
        var response = await client.GetAsync("https://test.com/api/test");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, attemptCount); // No retries for 4xx errors
    }

    [Fact]
    public async Task HttpClient_GivenHttpRequestException_Retries3Times()
    {
        // Arrange
        var attemptCount = 0;

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            attemptCount++;
            throw new HttpRequestException("Network error");
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(10);
                options.Retry.UseJitter = false;

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("test");

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await client.GetAsync("https://test.com/api/test");
        });

        // Assert
        Assert.Equal(4, attemptCount); // 1 initial attempt + 3 retries
        Assert.Contains("Network error", exception.Message);
    }

    [Fact]
    public async Task HttpClient_WithExponentialBackoff_IncreaseDelayBetweenRetries()
    {
        // Arrange
        var attemptTimestamps = new List<DateTimeOffset>();

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            attemptTimestamps.Add(DateTimeOffset.UtcNow);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = false;
                options.Retry.Delay = TimeSpan.FromMilliseconds(100); // Base delay

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("test");

        // Act
        await client.GetAsync("https://test.com/api/test");

        // Assert
        Assert.Equal(4, attemptTimestamps.Count);

        // Verify delays increase exponentially (approximately 100ms, 200ms, 400ms)
        if (attemptTimestamps.Count >= 3)
        {
            var delay1 = attemptTimestamps[1] - attemptTimestamps[0];
            var delay2 = attemptTimestamps[2] - attemptTimestamps[1];

            // First retry ~100ms
            Assert.InRange(delay1.TotalMilliseconds, 80, 150);

            // Second retry should be longer than first (~200ms)
            Assert.True(delay2 > delay1, "Second retry delay should be longer than first");
            Assert.InRange(delay2.TotalMilliseconds, 150, 300);
        }
    }

    [Fact]
    public async Task HttpClient_GivenConsecutiveFailures_CircuitBreakerOpens()
    {
        // Arrange
        var attemptCount = 0;

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            attemptCount++;
            throw new HttpRequestException("Service down");
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                // Fast failure for circuit breaker testing
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.Delay = TimeSpan.FromMilliseconds(1);

                // Circuit breaker configuration
                options.CircuitBreaker.FailureRatio = 0.5; // Open at 50% failure
                options.CircuitBreaker.MinimumThroughput = 3; // Need 3 attempts
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("test");

        // Act - Make several failing requests to open circuit
        Exception? circuitException = null;
        for (int i = 0; i < 5; i++) // Try more requests to ensure circuit opens
        {
            try
            {
                await client.GetAsync("https://test.com/api/test");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("circuit", StringComparison.OrdinalIgnoreCase))
                {
                    circuitException = ex;
                    break; // Circuit is open!
                }
                // Otherwise it's just the HTTP request exception, continue
            }
        }

        // Assert
        Assert.NotNull(circuitException);
        Assert.Contains("circuit", circuitException.Message, StringComparison.OrdinalIgnoreCase);

        // Circuit should now reject further calls immediately
        var beforeFastFail = attemptCount;

        var fastFailException = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await client.GetAsync("https://test.com/api/test");
        });

        // Verify circuit failed fast (no additional HTTP attempts)
        Assert.Equal(beforeFastFail, attemptCount);
        Assert.Contains("circuit", fastFailException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HttpClient_AfterCircuitBreakDuration_TransitionsToHalfOpen()
    {
        // Arrange
        var attemptCount = 0;
        var shouldSucceed = false;

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            attemptCount++;

            if (shouldSucceed)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Success")
                });
            }

            throw new HttpRequestException("Service down");
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.Delay = TimeSpan.FromMilliseconds(1);

                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 2;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromMilliseconds(500); // Short for testing

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("test");

        // Act - Open the circuit
        bool circuitOpened = false;
        for (int i = 0; i < 5 && !circuitOpened; i++)
        {
            try
            {
                await client.GetAsync("https://test.com/api/test");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("circuit", StringComparison.OrdinalIgnoreCase))
                {
                    circuitOpened = true;
                }
                // Otherwise HttpRequestException, continue
            }
        }

        Assert.True(circuitOpened, "Circuit should have opened after consecutive failures");

        // Wait for break duration to expire (circuit transitions to half-open)
        await Task.Delay(600);

        // Make requests succeed
        shouldSucceed = true;

        // This should succeed and close the circuit
        var response = await client.GetAsync("https://test.com/api/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify we made successful attempts after circuit half-opened
        Assert.True(attemptCount > 0);
    }
}

/// <summary>
/// Test message handler that allows customizing HTTP responses for testing
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await _handlerFunc(request, cancellationToken);
    }
}
