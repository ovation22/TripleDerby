using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Breeding;

public class RabbitMqBreedingConsumer : IMessageConsumer
{
    private readonly ILogger<RabbitMqBreedingConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private readonly int _concurrency;
    private string? _queueName;
    private string? _exchange;
    private string? _routingKey;

    // constructor
    public RabbitMqBreedingConsumer(ILogger<RabbitMqBreedingConsumer> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        // Read concurrency from config (MessageBus:ConsumerConcurrency) or default to 4
        _concurrency = int.TryParse(_configuration["MessageBus:ConsumerConcurrency"], out var c) && c > 0 ? c : 4;
        _semaphore = new SemaphoreSlim(_concurrency, _concurrency);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolve connection string (same keys as the publisher)
        var connectionString =
            _configuration["MessageBus:RabbitMq:ConnectionString"]
            ?? _configuration["MessageBus:RabbitMq"]
            ?? _configuration["MessageBus__RabbitMq__ConnectionString"]
            ?? _configuration["MessageBus__RabbitMq"]
            ?? _configuration.GetConnectionString("RabbitMq")
            ?? _configuration.GetConnectionString("messaging");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("RabbitMQ connection string not configured. Set MessageBus:RabbitMq or ConnectionStrings:RabbitMq.");

        var factory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
        };

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

        _exchange = _configuration["MessageBus:Exchange"] ?? "triplederby.events";
        _queueName = _configuration["MessageBus:Queue"] ?? "triplederby.breeding.requests";
        _routingKey = _configuration["MessageBus:RoutingKey"] ?? nameof(BreedingRequested);

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync();

        // set prefetch to concurrency so broker delivers at most this many unacked messages
        await _channel.BasicQosAsync(0, (ushort)_concurrency, false);

        // durable topic exchange and durable queue
        await _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true);
        await _channel.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        await _channel.QueueBindAsync(queue: _queueName, exchange: _exchange, routingKey: _routingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageAsync;

        await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("RabbitMqBreedingConsumer started (queue={Queue}, routingKey={RoutingKey}, concurrency={Concurrency})", _queueName, _routingKey, _concurrency);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        await _semaphore.WaitAsync();

        try
        {
            var body = ea.Body.ToArray();
            var payload = Encoding.UTF8.GetString(body);

            BreedingRequested? request = null;
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                request = JsonSerializer.Deserialize<BreedingRequested>(payload, jsonOptions);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Received non-conforming message on {Queue}: {Payload}", _queueName, payload);
            }

            if (request != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IBreedingRequestProcessor>();
                var context = new MessageContext { CancellationToken = CancellationToken.None };
                await processor.ProcessAsync(request, context);

                await _channelLock.WaitAsync();
                try
                {
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                finally
                {
                    _channelLock.Release();
                }
            }
            else
            {
                await _channelLock.WaitAsync();
                try
                {
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                finally
                {
                    _channelLock.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
            await _channelLock.WaitAsync();
            try
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
            catch { }
            finally
            {
                _channelLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed for message {DeliveryTag}", ea.DeliveryTag);
            await _channelLock.WaitAsync();
            try
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
            catch { }
            finally
            {
                _channelLock.Release();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try { if (_channel != null) await _channel.CloseAsync(); } catch { }
        try { if (_connection != null) await _connection.CloseAsync(); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        _semaphore.Dispose();
        _channelLock.Dispose();

        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        // For synchronous disposal, we need to block on async cleanup
        // Ideally consumers should use DisposeAsync instead
        StopAsync().GetAwaiter().GetResult();

        _semaphore.Dispose();
        _channelLock.Dispose();

        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }

        GC.SuppressFinalize(this);
    }
}