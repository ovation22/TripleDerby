# Generic Message Consumers with Swappable Broker Implementations

## Feature Summary

Refactor the existing `RabbitMqBreedingConsumer` and `AzureServiceBusRaceConsumer` into a generic, broker-agnostic message consumer architecture using the Adapter pattern. This will enable easy switching between RabbitMQ and Azure Service Bus (or adding new brokers) through configuration alone, without code changes.

## Problem Statement

Currently, TripleDerby has two message consumers with significant differences:

1. **RabbitMqBreedingConsumer** (Breeding service)
   - Tightly coupled to RabbitMQ APIs (`IConnection`, `IChannel`, `AsyncEventingBasicConsumer`)
   - Implements `IMessageConsumer` interface
   - Manual connection/channel management with semaphores
   - Custom connection string parsing (URI and key-value formats)
   - Manual ACK/NACK/Dead-letter handling
   - RabbitMQ-specific concepts (exchange, routing key, QoS)

2. **AzureServiceBusRaceConsumer** (Racing service)
   - Tightly coupled to Azure Service Bus (`ServiceBusClient`, `ServiceBusProcessor`)
   - Does NOT implement `IMessageConsumer` interface (only `IAsyncDisposable`)
   - Uses built-in `ServiceBusProcessor` abstraction
   - Simple connection string handling
   - Built-in ACK/NACK/Dead-letter through `ProcessMessageEventArgs`
   - **Also publishes completion messages** (additional responsibility)

### Key Issues

- **Code duplication**: Similar message handling logic implemented twice
- **Inflexible deployment**: Cannot switch brokers without code changes
- **Inconsistent interfaces**: Racing consumer doesn't implement `IMessageConsumer`
- **Mixed concerns**: Racing consumer handles both consuming and publishing
- **Testing complexity**: Hard to test broker-specific logic in isolation
- **Maintenance burden**: Bug fixes and improvements must be applied to both implementations

## Requirements

### Functional Requirements

1. **Generic Consumer**: Single `GenericMessageConsumer<TMessage, TProcessor>` that works with any broker
2. **Broker Adapters**: `IMessageBrokerAdapter` with implementations:
   - `RabbitMqBrokerAdapter`
   - `ServiceBusBrokerAdapter`
   - Extensible for future brokers (AWS SQS, Google Pub/Sub, etc.)
3. **Unified Message Processing**: All consumers delegate to `IMessageProcessor<TMessage>` interface
4. **Configuration-based switching**: Change brokers via `appsettings.json` without code changes
5. **Consistent behavior**: Same reliability guarantees (retries, dead-letter, concurrency) across brokers
6. **Backward compatibility**: Existing processors continue working without changes

### Non-Functional Requirements

1. **Performance**: No significant performance degradation vs current implementations
2. **Reliability**: Maintain existing message delivery guarantees (at-least-once)
3. **Observability**: Consistent logging and metrics across brokers
4. **Testability**: Easy to mock adapters for unit testing
5. **Extensibility**: Simple to add new broker implementations
6. **Configuration**: Abstract broker details while allowing provider-specific tuning

## Technical Approach

### Architecture: Adapter Pattern

```
┌─────────────────────────────────────────────────────────────┐
│                    BackgroundService Worker                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         GenericMessageConsumer<TMessage, TProcessor>         │
│  - Implements IMessageConsumer                               │
│  - Delegates to IMessageBrokerAdapter                        │
│  - Resolves IMessageProcessor<TMessage> from DI scope        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ IMessageBrokerAdapter │
              └──────────┬────────────┘
                         │
           ┌─────────────┴─────────────┐
           │                           │
           ▼                           ▼
┌──────────────────────┐   ┌──────────────────────┐
│ RabbitMqBrokerAdapter│   │ ServiceBusAdapter     │
│ - IConnection         │   │ - ServiceBusClient    │
│ - IChannel            │   │ - ServiceBusProcessor │
│ - AsyncEventConsumer  │   │                       │
└──────────────────────┘   └──────────────────────┘
           │                           │
           └─────────────┬─────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ IMessageProcessor<T> │
              └──────────┬────────────┘
                         │
           ┌─────────────┴─────────────┐
           │                           │
           ▼                           ▼
┌───────────────────────┐   ┌──────────────────────┐
│IBreedingRequestProc.  │   │IRaceRequestProcessor │
└───────────────────────┘   └──────────────────────┘
```

### Core Abstractions

#### 1. IMessageBrokerAdapter

```csharp
namespace TripleDerby.Core.Abstractions.Messaging;

/// <summary>
/// Adapter interface for message broker operations.
/// Implementations wrap broker-specific APIs (RabbitMQ, Azure Service Bus, etc.)
/// </summary>
public interface IMessageBrokerAdapter : IAsyncDisposable
{
    /// <summary>
    /// Connects to the message broker using provider-agnostic configuration.
    /// </summary>
    Task ConnectAsync(MessageBrokerConfig config, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to messages of type TMessage and routes them to the handler.
    /// </summary>
    Task SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the message broker and cleans up resources.
    /// </summary>
    Task DisconnectAsync();
}
```

#### 2. MessageBrokerConfig

```csharp
namespace TripleDerby.Core.Abstractions.Messaging;

/// <summary>
/// Provider-agnostic message broker configuration.
/// </summary>
public class MessageBrokerConfig
{
    /// <summary>Connection string (broker-specific format)</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Queue/topic name to consume from</summary>
    public string Queue { get; set; } = string.Empty;

    /// <summary>Number of concurrent message processors</summary>
    public int Concurrency { get; set; } = 5;

    /// <summary>Maximum retry attempts before dead-lettering</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Prefetch count (messages buffered locally)</summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Provider-specific configuration overrides.
    /// RabbitMQ: "Exchange", "RoutingKey", "ExchangeType"
    /// Azure Service Bus: "SubscriptionName", "SessionEnabled"
    /// </summary>
    public Dictionary<string, string> ProviderSpecific { get; set; } = new();
}
```

#### 3. MessageContext

```csharp
namespace TripleDerby.Core.Abstractions.Messaging;

/// <summary>
/// Message metadata and control context passed to handlers.
/// </summary>
public class MessageContext
{
    /// <summary>Unique message identifier</summary>
    public string MessageId { get; init; } = string.Empty;

    /// <summary>Correlation ID for distributed tracing</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Number of times this message has been delivered</summary>
    public int DeliveryCount { get; init; }

    /// <summary>Message headers/properties</summary>
    public IReadOnlyDictionary<string, object> Headers { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Cancellation token for processing timeout</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Provider-specific metadata (delivery tag, partition key, etc.)</summary>
    internal object? ProviderContext { get; init; }
}
```

#### 4. MessageProcessingResult

```csharp
namespace TripleDerby.Core.Abstractions.Messaging;

/// <summary>
/// Result of message processing, determines ACK/NACK/Dead-letter behavior.
/// </summary>
public class MessageProcessingResult
{
    public bool Success { get; init; }
    public bool Requeue { get; init; }
    public string? ErrorReason { get; init; }
    public Exception? Exception { get; init; }

    public static MessageProcessingResult Succeeded() =>
        new() { Success = true };

    public static MessageProcessingResult Failed(string reason, bool requeue = false) =>
        new() { Success = false, Requeue = requeue, ErrorReason = reason };

    public static MessageProcessingResult FailedWithException(Exception ex, bool requeue = false) =>
        new() { Success = false, Requeue = requeue, Exception = ex, ErrorReason = ex.Message };
}
```

#### 5. IMessageProcessor&lt;TMessage&gt;

```csharp
namespace TripleDerby.Core.Abstractions.Messaging;

/// <summary>
/// Generic message processor interface.
/// Replaces specific interfaces like IBreedingRequestProcessor, IRaceRequestProcessor.
/// </summary>
public interface IMessageProcessor<in TMessage>
{
    /// <summary>
    /// Processes a message and returns the result.
    /// </summary>
    Task<MessageProcessingResult> ProcessAsync(TMessage message, MessageContext context);
}
```

#### 6. GenericMessageConsumer&lt;TMessage, TProcessor&gt;

```csharp
namespace TripleDerby.Infrastructure.Messaging;

/// <summary>
/// Generic message consumer that works with any broker via IMessageBrokerAdapter.
/// </summary>
public class GenericMessageConsumer<TMessage, TProcessor> : IMessageConsumer
    where TProcessor : IMessageProcessor<TMessage>
{
    private readonly IMessageBrokerAdapter _adapter;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GenericMessageConsumer<TMessage, TProcessor>> _logger;

    public GenericMessageConsumer(
        IMessageBrokerAdapter adapter,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<GenericMessageConsumer<TMessage, TProcessor>> logger)
    {
        _adapter = adapter;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = BuildConfiguration();
        await _adapter.ConnectAsync(config, cancellationToken);
        await _adapter.SubscribeAsync<TMessage>(ProcessMessageAsync, cancellationToken);

        _logger.LogInformation(
            "GenericMessageConsumer<{MessageType}> started (Queue: {Queue}, Concurrency: {Concurrency})",
            typeof(TMessage).Name, config.Queue, config.Concurrency);
    }

    private async Task<MessageProcessingResult> ProcessMessageAsync(
        TMessage message,
        MessageContext context)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<TProcessor>();

            return await processor.ProcessAsync(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception processing {MessageType} (MessageId: {MessageId})",
                typeof(TMessage).Name, context.MessageId);

            return MessageProcessingResult.FailedWithException(ex, requeue: false);
        }
    }

    private MessageBrokerConfig BuildConfiguration()
    {
        // Read configuration from IConfiguration
        // Support multiple configuration keys for flexibility
        // ...
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _adapter.DisconnectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _adapter.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
```

### Configuration Schema

```json
{
  "MessageBus": {
    "Provider": "rabbitmq",  // or "servicebus"
    "ConnectionString": "amqp://guest:guest@localhost:5672/",

    "Consumer": {
      "Queue": "triplederby.breeding.requests",
      "Concurrency": 4,
      "MaxRetries": 3,
      "PrefetchCount": 10
    },

    "RabbitMq": {
      "Exchange": "triplederby.events",
      "RoutingKey": "BreedingRequested",
      "ExchangeType": "topic"
    },

    "ServiceBus": {
      "SubscriptionName": "breeding-service"
    }
  }
}
```

### Dependency Injection Registration

```csharp
// In TripleDerby.Services.Breeding/Program.cs
var provider = builder.Configuration["MessageBus:Provider"] ?? "rabbitmq";

// Register broker adapter based on provider
if (provider == "rabbitmq")
{
    builder.Services.AddSingleton<IMessageBrokerAdapter, RabbitMqBrokerAdapter>();
    builder.AddRabbitMQClient(connectionName: "messaging");
}
else if (provider == "servicebus")
{
    builder.Services.AddSingleton<IMessageBrokerAdapter, ServiceBusAdapter>();
    builder.AddAzureServiceBusClient(connectionName: "servicebus");
}

// Register processor (unchanged)
builder.Services.AddScoped<IMessageProcessor<BreedingRequested>, BreedingRequestProcessor>();

// Register generic consumer
builder.Services.AddSingleton<IMessageConsumer,
    GenericMessageConsumer<BreedingRequested, IMessageProcessor<BreedingRequested>>>();

// Register hosted service
builder.Services.AddHostedService<Worker>();
```

### Migration Path for Existing Processors

**Current Interface (IBreedingRequestProcessor):**
```csharp
public interface IBreedingRequestProcessor
{
    Task ProcessAsync(BreedingRequested request, CancellationToken cancellationToken);
}
```

**Target Interface (IMessageProcessor&lt;BreedingRequested&gt;):**
```csharp
public interface IMessageProcessor<BreedingRequested>
{
    Task<MessageProcessingResult> ProcessAsync(
        BreedingRequested message,
        MessageContext context);
}
```

**Migration Strategy:**

1. **Phase 1: Create adapter wrapper** (no existing code changes)
   ```csharp
   public class BreedingProcessorAdapter : IMessageProcessor<BreedingRequested>
   {
       private readonly IBreedingRequestProcessor _processor;

       public async Task<MessageProcessingResult> ProcessAsync(
           BreedingRequested message,
           MessageContext context)
       {
           await _processor.ProcessAsync(message, context.CancellationToken);
           return MessageProcessingResult.Succeeded();
       }
   }
   ```

2. **Phase 2: Update processor implementations** (gradual migration)
   - Change `IBreedingRequestProcessor` to implement `IMessageProcessor<BreedingRequested>`
   - Update signature to return `MessageProcessingResult`
   - Use `MessageContext` for correlation IDs, delivery count, etc.

3. **Phase 3: Remove old interfaces** (cleanup)
   - Delete `IBreedingRequestProcessor`
   - Delete `IRaceRequestProcessor`
   - All processors use `IMessageProcessor<TMessage>`

### Handling Racing Service Publishing

The current `AzureServiceBusRaceConsumer` publishes completion messages. This violates single responsibility. We have two options:

**Option A: Processor publishes (recommended for now)**
```csharp
public class RaceRequestProcessor : IMessageProcessor<RaceRequested>
{
    private readonly IRaceService _raceService;
    private readonly IMessagePublisher _publisher;

    public async Task<MessageProcessingResult> ProcessAsync(
        RaceRequested message,
        MessageContext context)
    {
        var result = await _raceService.RunRaceAsync(...);

        var completion = new RaceCompleted { ... };
        await _publisher.PublishAsync(completion, new MessagePublishOptions
        {
            Destination = "race-completions"
        });

        return MessageProcessingResult.Succeeded();
    }
}
```

**Option B: Separate orchestration service** (future enhancement)
```csharp
// Consumer only consumes, orchestrator publishes
public class RaceOrchestrator
{
    // Subscribe to race requests
    // Delegate to RaceService
    // Publish completions
}
```

For this feature, we'll use **Option A** to minimize changes. Publishing can be extracted later if needed.

## Implementation Plan

### Phase 1: Core Abstractions (TripleDerby.Core)

**Files to create:**
- `TripleDerby.Core/Abstractions/Messaging/IMessageBrokerAdapter.cs`
- `TripleDerby.Core/Abstractions/Messaging/IMessageProcessor.cs`
- `TripleDerby.Core/Abstractions/Messaging/MessageBrokerConfig.cs`
- `TripleDerby.Core/Abstractions/Messaging/MessageContext.cs`
- `TripleDerby.Core/Abstractions/Messaging/MessageProcessingResult.cs`

**Tasks:**
1. Define `IMessageBrokerAdapter` interface
2. Define `IMessageProcessor<TMessage>` interface
3. Create `MessageBrokerConfig` class
4. Create `MessageContext` class
5. Create `MessageProcessingResult` class
6. Add XML documentation to all public APIs

### Phase 2: Adapter Implementations (TripleDerby.Infrastructure)

**Files to create:**
- `TripleDerby.Infrastructure/Messaging/RabbitMqBrokerAdapter.cs`
- `TripleDerby.Infrastructure/Messaging/ServiceBusAdapter.cs`
- `TripleDerby.Infrastructure/Messaging/GenericMessageConsumer.cs`

**Tasks:**
1. Implement `RabbitMqBrokerAdapter`
   - Extract connection logic from `RabbitMqBreedingConsumer`
   - Implement `ConnectAsync` with connection pooling
   - Implement `SubscribeAsync` with semaphore-based concurrency
   - Implement manual ACK/NACK/Dead-letter based on `MessageProcessingResult`
   - Handle retry count and dead-letter logic
2. Implement `ServiceBusAdapter`
   - Extract connection logic from `AzureServiceBusRaceConsumer`
   - Implement `ConnectAsync` with `ServiceBusClient`
   - Implement `SubscribeAsync` using `ServiceBusProcessor`
   - Map `ProcessMessageEventArgs` to `MessageContext`
   - Implement Complete/Abandon/DeadLetter based on `MessageProcessingResult`
3. Implement `GenericMessageConsumer<TMessage, TProcessor>`
   - Implement `IMessageConsumer` interface
   - Delegate to `IMessageBrokerAdapter`
   - Resolve processor from scoped DI container
   - Read configuration and build `MessageBrokerConfig`
   - Handle errors and logging

### Phase 3: Processor Migration

**Files to modify:**
- `TripleDerby.Services.Breeding/IBreedingRequestProcessor.cs`
- `TripleDerby.Services.Breeding/BreedingRequestProcessor.cs`
- `TripleDerby.Services.Racing/IRaceRequestProcessor.cs`
- `TripleDerby.Services.Racing/RaceRequestProcessor.cs`

**Tasks:**
1. Update `IBreedingRequestProcessor` to inherit `IMessageProcessor<BreedingRequested>`
2. Update `BreedingRequestProcessor.ProcessAsync` signature:
   - Return `Task<MessageProcessingResult>`
   - Accept `MessageContext` parameter
3. Update `IRaceRequestProcessor` to inherit `IMessageProcessor<RaceRequested>`
4. Update `RaceRequestProcessor.ProcessAsync` signature
5. Wrap processor logic in try-catch and return `MessageProcessingResult`
6. Use `context.CorrelationId` instead of extracting from message where applicable

### Phase 4: Service Registration Updates

**Files to modify:**
- `TripleDerby.Services.Breeding/Program.cs`
- `TripleDerby.Services.Racing/Program.cs`
- `TripleDerby.Services.Breeding/Worker.cs` (optional - can stay the same)
- `TripleDerby.Services.Racing/Worker.cs` (optional - can stay the same)

**Tasks:**
1. Add provider selection logic based on configuration
2. Register appropriate `IMessageBrokerAdapter` (keyed or conditional)
3. Replace concrete consumer registration with `GenericMessageConsumer<TMessage, TProcessor>`
4. Update configuration files with `MessageBus:Provider` setting
5. Test startup with RabbitMQ provider
6. Test startup with ServiceBus provider

### Phase 5: Deprecate Old Consumers

**Files to delete (or mark obsolete):**
- `TripleDerby.Services.Breeding/RabbitMqBreedingConsumer.cs`
- `TripleDerby.Services.Racing/AzureServiceBusRaceConsumer.cs`

**Tasks:**
1. Mark old consumers as `[Obsolete]`
2. Ensure all tests pass with new consumers
3. Update integration tests to use generic consumers
4. Delete old consumer files after successful migration

### Phase 6: Testing

**Files to create:**
- `TripleDerby.Tests.Unit/Messaging/RabbitMqBrokerAdapterTests.cs`
- `TripleDerby.Tests.Unit/Messaging/ServiceBusAdapterTests.cs`
- `TripleDerby.Tests.Unit/Messaging/GenericMessageConsumerTests.cs`
- `TripleDerby.Tests.Integration/Messaging/MessageConsumerIntegrationTests.cs`

**Tasks:**
1. Unit test `RabbitMqBrokerAdapter` with mock `IConnection`/`IChannel`
2. Unit test `ServiceBusAdapter` with mock `ServiceBusClient`
3. Unit test `GenericMessageConsumer` with mock adapter
4. Integration test with TestContainers (RabbitMQ)
5. Integration test with Azure Service Bus emulator
6. End-to-end test: Publish → Consume → Process → ACK
7. Test error scenarios (NACK, dead-letter, retry)
8. Test concurrency limits
9. Test graceful shutdown

### Phase 7: Documentation

**Files to create/update:**
- `docs/features/generic-message-consumers.md` (this file)
- `docs/implementation/message-consumer-migration-guide.md`
- `README.md` (update architecture section)

**Tasks:**
1. Document adapter pattern architecture
2. Document configuration schema
3. Provide migration guide for adding new brokers
4. Update architecture diagrams
5. Add code examples for common scenarios

## Success Criteria

### Functional Validation

- [ ] Breeding service consumes `BreedingRequested` messages using `GenericMessageConsumer`
- [ ] Racing service consumes `RaceRequested` messages using `GenericMessageConsumer`
- [ ] Can switch Breeding service from RabbitMQ to Azure Service Bus via config alone
- [ ] Can switch Racing service from Azure Service Bus to RabbitMQ via config alone
- [ ] Messages are ACKed when processing succeeds
- [ ] Messages are NACKed/requeued when processing fails (transient errors)
- [ ] Messages are dead-lettered after max retries
- [ ] Concurrency limits are respected (e.g., max 5 concurrent messages)
- [ ] All existing integration tests pass

### Non-Functional Validation

- [ ] No significant performance degradation (< 5% latency increase)
- [ ] Memory usage comparable to current implementation
- [ ] Graceful shutdown without message loss
- [ ] Logs include correlation IDs, message types, and processing outcomes
- [ ] Unit test coverage > 80% for adapters and generic consumer
- [ ] Integration tests pass with both RabbitMQ and Azure Service Bus

### Code Quality

- [ ] All public APIs have XML documentation
- [ ] Configuration schema documented with examples
- [ ] No compiler warnings
- [ ] Passes static analysis (nullable reference types, code analysis)
- [ ] Follows existing coding conventions

## Open Questions

1. **Configuration hierarchy**: Should broker-specific config be nested under provider name, or use a flat structure with prefixes?
   - **Decision**: Nested under provider name for clarity (e.g., `MessageBus.RabbitMq.Exchange`)

2. **Processor interface migration**: Should we support both old and new interfaces during transition, or force breaking change?
   - **Decision**: Create adapter wrappers initially, then migrate processors incrementally

3. **Dead-letter queue naming**: Should dead-letter queues be auto-created with naming convention, or require explicit configuration?
   - **Decision**: Auto-create with naming convention `{queue}.deadletter` for simplicity

4. **Telemetry**: Should adapters emit metrics (message count, processing time, error rate)?
   - **Decision**: Yes, use Activity and meters for OpenTelemetry compatibility (future enhancement)

5. **Consumer lifecycle**: Should consumers support pause/resume, or only start/stop?
   - **Decision**: Start with start/stop, add pause/resume if needed later

6. **Message serialization**: Should adapters handle serialization, or should it be pluggable?
   - **Decision**: Adapters handle JSON serialization for now; make pluggable in future if needed

## Dependencies

### NuGet Packages (existing)
- `RabbitMQ.Client` (≥ 6.8.0)
- `Azure.Messaging.ServiceBus` (≥ 7.17.0)
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`

### Internal Dependencies
- `TripleDerby.Core.Abstractions.Messaging.IMessageConsumer` (existing)
- `TripleDerby.Core.Abstractions.Messaging.IMessagePublisher` (existing)
- `TripleDerby.SharedKernel.Messages.*` (existing)

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking change to processors | High | Use adapter pattern to support old interfaces during migration |
| Performance regression | Medium | Benchmark adapters vs current implementation; optimize hot paths |
| Configuration complexity | Medium | Provide clear documentation and examples; validate config at startup |
| Missing broker features | Low | Design adapters to expose provider-specific metadata when needed |
| Adapter bugs affecting both services | High | Comprehensive unit and integration tests; gradual rollout |
| Deployment issues during migration | Medium | Deploy Breeding service first, validate, then Racing service |

## Future Enhancements

1. **Additional Brokers**: AWS SQS, Google Pub/Sub, Azure Event Hubs, Kafka
2. **Batch Processing**: Handle multiple messages in one processor call for efficiency
3. **Message Middleware**: Filters, transformers, validators before reaching processor
4. **Saga/Orchestration**: Built-in support for long-running workflows
5. **Retry Policies**: Configurable exponential backoff, circuit breaker
6. **Priority Queues**: High-priority message processing
7. **Scheduled Messages**: Delay delivery until specific time
8. **Telemetry**: OpenTelemetry metrics and traces
9. **Schema Registry**: Validate messages against registered schemas
10. **Consumer Groups**: Multiple consumer instances with load balancing

## References

- [RabbitMQ .NET Client Docs](https://www.rabbitmq.com/dotnet-api-guide.html)
- [Azure Service Bus .NET SDK](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus)
- [Adapter Pattern (GoF)](https://en.wikipedia.org/wiki/Adapter_pattern)
- [Message Consumer Pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageConsumer.html)
- TripleDerby existing code:
  - [RabbitMqBreedingConsumer.cs:10](c:\Development\TripleDerby\TripleDerby.Services.Breeding\RabbitMqBreedingConsumer.cs#L10)
  - [AzureServiceBusRaceConsumer.cs:13](c:\Development\TripleDerby\TripleDerby.Services.Racing\AzureServiceBusRaceConsumer.cs#L13)
  - [IMessageConsumer.cs:6](c:\Development\TripleDerby\TripleDerby.Core\Abstractions\Messaging\IMessageConsumer.cs#L6)
