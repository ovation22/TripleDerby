using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Infrastructure.Messaging;

public class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable, IDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _exchange;
    private readonly object _connectionLock = new();
    private readonly TimeSpan _publisherConfirmTimeout;
    private readonly int _maxPublishRetries;
    private readonly TimeSpan _initialRetryDelay;

    public RabbitMqMessagePublisher(IConfiguration configuration, ILogger<RabbitMqMessagePublisher> logger)
    {
        _configuration = configuration;
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
        _publisherConfirmTimeout = TimeSpan.FromSeconds(5);
        _maxPublishRetries = int.TryParse(configuration["MessageBus:Publish:MaxRetries"], out var mr) ? mr : 3;
        _initialRetryDelay = TimeSpan.FromMilliseconds(int.TryParse(configuration["MessageBus:Publish:InitialDelayMs"], out var id) ? id : 200);

        _logger.LogInformation("RabbitMqMessagePublisher configured for exchange {Exchange}", _exchange);
    }

    private static ConnectionFactory BuildFactory(string connectionString)
    {
        var factory = new ConnectionFactory();

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && (uri.Scheme == "amqp" || uri.Scheme == "amqps"))
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
                if (k == "host" || k == "hostname") factory.HostName = v;
                else if (k == "username" || k == "user") factory.UserName = v;
                else if (k == "password" || k == "pwd") factory.Password = v;
                else if (k == "virtualhost" || k == "vhost") factory.VirtualHost = v;
                else if (k == "port" && int.TryParse(v, out var p)) factory.Port = p;
            }
        }

        return factory;
    }

    private void EnsureConnected()
    {
        if (_connection != null && _connection.IsOpen) return;

        lock (_connectionLock)
        {
            if (_connection != null && _connection.IsOpen) return;

            _logger.LogInformation("Creating RabbitMQ connection...");

            // CreateConnection can throw; allow caller to handle/log and possibly retry.
            _connection = _factory.CreateConnection();

            _logger.LogInformation("RabbitMQ connection established (node: {Node})", _connection.Endpoint.HostName ?? "(unknown)");

            // Ensure exchange exists using a short-lived channel
            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
        }
    }

    public async Task PublishAsync<T>(T message, string? exchange = null, string? routingKey = null, CancellationToken cancellationToken = default)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        cancellationToken.ThrowIfCancellationRequested();

        var ex = exchange ?? _exchange;
        var rk = routingKey ?? typeof(T).Name;

        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(payload);

        var attempt = 0;
        var delay = _initialRetryDelay;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                EnsureConnected();

                // create short-lived channel per publish (IModel not thread-safe)
                using var channel = _connection!.CreateModel();

                // Request confirms so we know the broker accepted the message
                channel.ConfirmSelect();

                var props = channel.CreateBasicProperties();
                props.ContentType = "application/json";
                props.DeliveryMode = 2; // persistent

                var correlationId = Activity.Current?.Tags.FirstOrDefault(t => t.Key == "client.correlation_id").Value
                                    ?? Activity.Current?.Baggage?.FirstOrDefault(kv => kv.Key == "client.correlation_id").Value
                                    ?? Activity.Current?.TraceId.ToString()
                                    ?? Activity.Current?.Id
                                    ?? Guid.NewGuid().ToString();

                props.CorrelationId = correlationId;
                props.Headers ??= new System.Collections.Generic.Dictionary<string, object?>();
                // store header values as UTF8 bytes for broader client compatibility
                props.Headers["correlation-id"] = Encoding.UTF8.GetBytes(correlationId);
                props.Headers["message-type"] = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name);
                props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                channel.BasicPublish(exchange: ex, routingKey: rk, basicProperties: props, body: body);

                // wait for broker confirm (throws on nack or timeout)
                if (!channel.WaitForConfirms(_publisherConfirmTimeout))
                {
                    throw new Exception("Publish not confirmed by RabbitMQ within the timeout.");
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
                // connection may be in an error state — dispose and allow recreation on next loop
                SafeCloseConnection();
            }
            catch (BrokerUnreachableException bue)
            {
                _logger.LogWarning(bue, "Broker unreachable while publishing message {Type} attempt {Attempt}", typeof(T).Name, attempt);

                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                SafeCloseConnection();
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, "Failed to publish message {Type} attempt {Attempt}", typeof(T).Name, attempt);
                // If last attempt, rethrow; otherwise back off and retry
                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                SafeCloseConnection();
            }
        }
    }

    private void SafeCloseConnection()
    {
        try
        {
            if (_connection is not null)
            {
                if (_connection.IsOpen)
                {
                    try { _connection.Close(); } catch { }
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

    public ValueTask DisposeAsync()
    {
        SafeCloseConnection();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        SafeCloseConnection();
        GC.SuppressFinalize(this);
    }
}