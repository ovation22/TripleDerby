using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Infrastructure.Messaging;

public class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable, IDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _exchange;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly int _maxPublishRetries;
    private readonly TimeSpan _initialRetryDelay;

    // Channel pooling for performance optimization (Phase 2)
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public RabbitMqMessagePublisher(IConfiguration configuration, ILogger<RabbitMqMessagePublisher> logger)
    {
        _logger = logger;

        // serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        // read configuration (support several shapes used by Aspire)
        string? connectionString =
            configuration["MessageBus:RabbitMq:ConnectionString"]
            ?? configuration["MessageBus:RabbitMq"]
            ?? configuration["MessageBus__RabbitMq__ConnectionString"]
            ?? configuration["MessageBus__RabbitMq"]
            ?? configuration.GetConnectionString("RabbitMq")
            ?? configuration.GetConnectionString("messaging");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("RabbitMQ connection string not configured. Set MessageBus:RabbitMq or ConnectionStrings:RabbitMq.");

        _factory = BuildFactory(connectionString);

        // resilience settings
        _factory.AutomaticRecoveryEnabled = true;
        _factory.TopologyRecoveryEnabled = true;
        _factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
        _factory.RequestedHeartbeat = TimeSpan.FromSeconds(30);

        // publish defaults - tune via config if desired
        _exchange = configuration["MessageBus:Exchange"] ?? configuration["MessageBus__Exchange"] ?? "triplederby.events";
        _maxPublishRetries = int.TryParse(configuration["MessageBus:Publish:MaxRetries"], out var mr) ? mr : 3;
        _initialRetryDelay = TimeSpan.FromMilliseconds(int.TryParse(configuration["MessageBus:Publish:InitialDelayMs"], out var id) ? id : 200);

        _logger.LogInformation("RabbitMqMessagePublisher configured for exchange {Exchange}", _exchange);
    }

    private static ConnectionFactory BuildFactory(string connectionString)
    {
        var factory = new ConnectionFactory();

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && uri.Scheme is "amqp" or "amqps")
        {
            factory.Uri = uri;
        }
        else
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;
                var k = kv[0].Trim().ToLowerInvariant();
                var v = kv[1].Trim();
                if (k is "host" or "hostname") factory.HostName = v;
                else if (k is "username" or "user") factory.UserName = v;
                else if (k is "password" or "pwd") factory.Password = v;
                else if (k is "virtualhost" or "vhost") factory.VirtualHost = v;
                else if (k == "port" && int.TryParse(v, out var p)) factory.Port = p;
            }
        }

        return factory;
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connection is { IsOpen: true }) return;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is { IsOpen: true }) return;

            _logger.LogInformation("Creating RabbitMQ connection...");

            // CreateConnectionAsync can throw; allow caller to handle/log and possibly retry.
            _connection = await _factory.CreateConnectionAsync();

            _logger.LogInformation("RabbitMQ connection established (node: {Node})", _connection.Endpoint.HostName);

            // Ensure exchange exists using a short-lived channel
            var channel = await _connection.CreateChannelAsync();
            try
            {
                await channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true);
            }
            finally
            {
                await channel.DisposeAsync();
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Ensures a dedicated publish channel is available for message publishing.
    /// Uses double-checked locking to create channel only once and reuse it for all publishes.
    /// This eliminates the overhead of creating/disposing ~350 channels/second.
    /// </summary>
    private async Task EnsurePublishChannelAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: channel exists and is open
        if (_publishChannel is { IsOpen: true })
            return;

        // Slow path: need to create channel (only happens once, or after connection loss)
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: another thread may have created while we waited
            if (_publishChannel is { IsOpen: true })
                return;

            // Close existing channel if it exists but is not open
            if (_publishChannel != null)
            {
                try { await _publishChannel.CloseAsync(cancellationToken); } catch { }
                try { _publishChannel.Dispose(); } catch { }
                _publishChannel = null;
            }

            _logger.LogInformation("Creating dedicated RabbitMQ publisher channel...");
            _publishChannel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Publisher channel created and ready for use");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task PublishAsync<T>(T message, MessagePublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        cancellationToken.ThrowIfCancellationRequested();

        var ex = options?.Destination ?? _exchange;
        var rk = options?.Subject ?? typeof(T).Name;

        // Serialize message BEFORE acquiring lock (minimize lock duration)
        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(payload);

        // Prepare message properties BEFORE acquiring lock
        var correlationId = Activity.Current?.Tags.FirstOrDefault(t => t.Key == "client.correlation_id").Value
                            ?? Activity.Current?.Baggage.FirstOrDefault(kv => kv.Key == "client.correlation_id").Value
                            ?? Activity.Current?.TraceId.ToString()
                            ?? Activity.Current?.Id
                            ?? Guid.NewGuid().ToString();

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            CorrelationId = correlationId,
            Headers = new Dictionary<string, object?>
            {
                ["correlation-id"] = Encoding.UTF8.GetBytes(correlationId),
                ["message-type"] = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name)
            },
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        var attempt = 0;
        var delay = _initialRetryDelay;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                await EnsureConnectedAsync();
                await EnsurePublishChannelAsync(cancellationToken);

                // Acquire lock to use the shared channel (channels are NOT thread-safe)
                await _publishLock.WaitAsync(cancellationToken);
                try
                {
                    await _publishChannel!.BasicPublishAsync(
                        exchange: ex,
                        routingKey: rk,
                        mandatory: false,
                        basicProperties: props,
                        body: body,
                        cancellationToken: cancellationToken);
                }
                finally
                {
                    _publishLock.Release();
                }

                _logger.LogInformation("Published message {Type} to exchange {Exchange} with routing key {RoutingKey} (attempt {Attempt})", typeof(T).Name, ex, rk, attempt);
                return;
            }
            catch (OperationInterruptedException oex)
            {
                _logger.LogWarning(oex, "Transient RabbitMQ interruption while publishing message {Type} attempt {Attempt}", typeof(T).Name, attempt);

                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                // connection may be in an error state – dispose and allow recreation on next loop
                await SafeCloseConnectionAsync();
            }
            catch (BrokerUnreachableException bue)
            {
                _logger.LogWarning(bue, "Broker unreachable while publishing message {Type} attempt {Attempt}", typeof(T).Name, attempt);

                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                await SafeCloseConnectionAsync();
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, "Failed to publish message {Type} attempt {Attempt}", typeof(T).Name, attempt);
                // If last attempt, rethrow; otherwise back off and retry
                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                await SafeCloseConnectionAsync();
            }
        }
    }

    private async Task SafeCloseConnectionAsync()
    {
        try
        {
            // Close publish channel first
            if (_publishChannel is not null)
            {
                try
                {
                    if (_publishChannel.IsOpen)
                    {
                        await _publishChannel.CloseAsync();
                    }
                    _publishChannel.Dispose();
                }
                catch { /* swallow */ }
                finally
                {
                    _publishChannel = null;
                }
            }

            // Then close connection
            if (_connection is not null)
            {
                if (_connection.IsOpen)
                {
                    try { await _connection.CloseAsync(); } catch { }
                }

                _connection.Dispose();
            }
        }
        catch { /* swallow */ }
        finally
        {
            _connection = null;
        }
    }

    /// <summary>
    /// Disposes resources asynchronously. Prefer this over Dispose() to avoid potential deadlocks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await SafeCloseConnectionAsync();
        _connectionLock.Dispose();
        _publishLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Synchronous disposal. Note: This blocks on async cleanup which may cause issues in some contexts.
    /// Prefer DisposeAsync() when possible.
    /// </summary>
    public void Dispose()
    {
        // We must block here since IDisposable.Dispose is synchronous
        // This is safe in most contexts but could deadlock in ASP.NET synchronization contexts
        // Callers should prefer DisposeAsync when possible
        try
        {
            SafeCloseConnectionAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _connectionLock.Dispose();
            _publishLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}