# Message Bus Abstraction - Configuration-Driven Broker Switching

**Feature Number:** 021

**Status:** 📋 **Planned**

**Created:** 2026-01-07

**Depends On:**
- Feature 011 (Race Microservice Migration)
- Feature 015 (Generic Message Consumers)
- Feature 017 (Breeding RabbitMQ Performance Optimization)
- Feature 020 (Horse Training System)

---

## Summary

Implement a configuration-driven message bus abstraction layer that enables seamless switching between RabbitMQ and Azure Service Bus through configuration changes alone, eliminating hardcoded broker references in service code and reducing migration effort to near-zero.

The implementation provides:
- **Message-Type-Based Routing**: Automatic destination resolution from message types
- **Unified Publisher Interface**: Single DI registration regardless of broker choice
- **Configuration-Driven Provider Selection**: Change brokers via `appsettings.json` only
- **Zero-Code Service Changes**: Services publish without knowing which broker is active
- **Routing Configuration**: Centralized message routing definitions

---

## Motivation

### Current State

**Three Services, Three Patterns:**

1. **RaceService** ([RaceService.cs:19](c:\Development\TripleDerby\TripleDerby.Core\Services\RaceService.cs#L19))
   ```csharp
   [FromKeyedServices("servicebus")] IMessagePublisher messagePublisher

   await messagePublisher.PublishAsync(
       message,
       new MessagePublishOptions { Destination = "race-requests" },
       cancellationToken);
   ```

2. **BreedingService** ([BreedingService.cs:27](c:\Development\TripleDerby\Core\Services\BreedingService.cs#L27))
   ```csharp
   [FromKeyedServices("rabbitmq")] IMessagePublisher messagePublisher

   await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);
   ```

3. **TrainingService** ([TrainingService.cs:20](c:\Development\TripleDerby\Core\Services\TrainingService.cs#L20))
   ```csharp
   [FromKeyedServices("rabbitmq")] IMessagePublisher messagePublisher

   await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);
   ```

### Problems

1. **Broker Coupling**: Services explicitly request broker by key (`"servicebus"`, `"rabbitmq"`)
2. **Inconsistent Publishing**: RaceService specifies `Destination`, others don't
3. **Hard-to-Switch**: Changing broker requires code changes in multiple services
4. **Configuration Fragmentation**: Routing config split between code and appsettings
5. **DI Complexity**: Keyed services add cognitive overhead

### Goals

1. **Configuration-Only Switching**: Change broker via one config setting
2. **Service Simplification**: Remove `FromKeyedServices` attributes entirely
3. **Centralized Routing**: All message routing in configuration
4. **Consistent Publishing**: Same pattern across all services
5. **Future-Proof**: Easy to add new brokers (AWS SQS, Google Pub/Sub, Kafka)

---

## Requirements

### Functional Requirements

**FR1: Configuration-Driven Provider Selection**
- Single config value determines active broker: `MessageBus:Provider`
- Supported values: `"RabbitMq"`, `"ServiceBus"`, `"Auto"` (detect from available connection strings)
- Invalid provider throws clear exception at startup

**FR2: Message-Type-Based Routing**
- Message type name (e.g., `"RaceRequested"`) maps to routing configuration
- Configuration includes:
  - Destination (queue name, exchange name, topic name)
  - Routing key (for RabbitMQ topic exchanges)
  - Subject (for Azure Service Bus filtering)
- Fallback to sensible defaults if message not configured

**FR3: Unified Publisher Registration**
- Single DI registration: `builder.Services.AddSingleton<IMessagePublisher, ...>()`
- No keyed services required in consuming code
- Services inject `IMessagePublisher` without attributes

**FR4: Backward Compatibility**
- Existing publishing code continues working
- `MessagePublishOptions` still supported for overrides
- No breaking changes to `IMessagePublisher` interface

**FR5: Provider-Specific Configuration**
- RabbitMQ: Exchange, ExchangeType, RoutingKey
- Azure Service Bus: Queue, Subject
- Extensible for future providers

### Non-Functional Requirements

**NFR1: Performance**
- Zero runtime overhead for routing resolution (cached lookups)
- No additional network calls or serialization
- Publisher creation happens once at startup

**NFR2: Developer Experience**
- Clear error messages for misconfiguration
- Validation at application startup
- IntelliSense-friendly configuration structure

**NFR3: Testability**
- Easy to mock `IMessagePublisher` without keyed services
- Configuration injectable for testing
- Clear separation between routing logic and broker implementation

**NFR4: Observability**
- Log provider selection at startup
- Log routing decisions (at debug level)
- Include routing metadata in telemetry

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                 Application Services Layer                   │
│  RaceService, BreedingService, TrainingService               │
│  Inject: IMessagePublisher (no keyed services)               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              RoutingMessagePublisher (Decorator)             │
│  - Resolves message type → routing config                    │
│  - Applies defaults if not configured                        │
│  - Delegates to inner IMessagePublisher                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │   IMessagePublisher  │
              └──────────┬────────────┘
                         │
           ┌─────────────┴─────────────┐
           │                           │
           ▼                           ▼
┌──────────────────────┐   ┌──────────────────────┐
│RabbitMqMessagePublish│   │AzureServiceBusPublish│
│ - Connection pooling  │   │ - ServiceBusClient    │
│ - Channel management  │   │ - Sender creation     │
│ - Exchange + routing  │   │ - Queue publishing    │
└──────────────────────┘   └──────────────────────┘
```

### Component Design

#### 1. MessageRoutingConfig

Provider-agnostic routing configuration with per-message-type settings.

**Location:** `TripleDerby.Core/Abstractions/Messaging/MessageRoutingConfig.cs`

```csharp
namespace TripleDerby.Core.Abstractions.Messaging;

/// <summary>
/// Configuration for message routing, independent of broker implementation.
/// Mapped from appsettings.json MessageBus:Routing section.
/// </summary>
public class MessageRoutingConfig
{
    /// <summary>
    /// Active message broker provider.
    /// Valid values: "RabbitMq", "ServiceBus", "Auto"
    /// </summary>
    public string Provider { get; init; } = "Auto";

    /// <summary>
    /// Per-message-type routing configuration.
    /// Key: Message type name (e.g., "RaceRequested")
    /// Value: Route configuration for that message
    /// </summary>
    public Dictionary<string, MessageRoute> Routes { get; init; } = new();

    /// <summary>
    /// Default destination if message type not found in Routes.
    /// </summary>
    public string? DefaultDestination { get; init; }

    /// <summary>
    /// Default routing key for RabbitMQ if not specified per message.
    /// </summary>
    public string? DefaultRoutingKey { get; init; }
}

/// <summary>
/// Routing configuration for a specific message type.
/// </summary>
public class MessageRoute
{
    /// <summary>
    /// Destination name: RabbitMQ exchange, Azure Service Bus queue/topic.
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// Routing key for RabbitMQ topic exchanges.
    /// Maps to MessagePublishOptions.Subject for broker abstraction.
    /// </summary>
    public string? RoutingKey { get; init; }

    /// <summary>
    /// Subject for Azure Service Bus filtering.
    /// Maps to MessagePublishOptions.Subject for broker abstraction.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Additional provider-specific metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
```

#### 2. RoutingMessagePublisher

Decorator that resolves routing configuration and delegates to actual publisher.

**Location:** `TripleDerby.Infrastructure/Messaging/RoutingMessagePublisher.cs`

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Infrastructure.Messaging;

/// <summary>
/// Decorator for IMessagePublisher that applies message-type-based routing configuration.
/// Resolves routing from configuration and delegates to inner publisher.
/// </summary>
public class RoutingMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IMessagePublisher _innerPublisher;
    private readonly MessageRoutingConfig _routingConfig;
    private readonly ILogger<RoutingMessagePublisher> _logger;

    // Cache resolved routing to avoid repeated lookups
    private readonly ConcurrentDictionary<Type, MessagePublishOptions> _routingCache = new();

    public RoutingMessagePublisher(
        IMessagePublisher innerPublisher,
        IOptions<MessageRoutingConfig> routingConfig,
        ILogger<RoutingMessagePublisher> logger)
    {
        _innerPublisher = innerPublisher ?? throw new ArgumentNullException(nameof(innerPublisher));
        _routingConfig = routingConfig?.Value ?? throw new ArgumentNullException(nameof(routingConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation(
            "RoutingMessagePublisher initialized with provider: {Provider}, Routes: {RouteCount}",
            _routingConfig.Provider,
            _routingConfig.Routes.Count);
    }

    public async Task PublishAsync<T>(
        T message,
        MessagePublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        // If caller provided explicit options, respect them (override routing config)
        if (options is { Destination: not null })
        {
            _logger.LogDebug(
                "Publishing {MessageType} with explicit destination: {Destination}",
                typeof(T).Name,
                options.Destination);

            await _innerPublisher.PublishAsync(message, options, cancellationToken);
            return;
        }

        // Resolve routing from configuration (cached)
        var resolvedOptions = GetOrCreateRoutingOptions<T>(options);

        _logger.LogDebug(
            "Publishing {MessageType} to {Destination} with routing key {RoutingKey}",
            typeof(T).Name,
            resolvedOptions.Destination,
            resolvedOptions.Subject);

        await _innerPublisher.PublishAsync(message, resolvedOptions, cancellationToken);
    }

    private MessagePublishOptions GetOrCreateRoutingOptions<T>(MessagePublishOptions? userOptions)
    {
        return _routingCache.GetOrAdd(typeof(T), _ =>
        {
            var messageTypeName = typeof(T).Name;

            // Try to find route config for this message type
            if (_routingConfig.Routes.TryGetValue(messageTypeName, out var route))
            {
                return new MessagePublishOptions
                {
                    Destination = route.Destination ?? _routingConfig.DefaultDestination,
                    Subject = route.RoutingKey ?? route.Subject ?? _routingConfig.DefaultRoutingKey ?? messageTypeName,
                    Metadata = userOptions?.Metadata
                };
            }

            // Fallback to defaults
            _logger.LogWarning(
                "No routing configuration found for {MessageType}, using defaults",
                messageTypeName);

            return new MessagePublishOptions
            {
                Destination = _routingConfig.DefaultDestination,
                Subject = _routingConfig.DefaultRoutingKey ?? messageTypeName,
                Metadata = userOptions?.Metadata
            };
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_innerPublisher is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_innerPublisher is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
```

#### 3. MessageBusExtensions

Registration helpers for clean DI configuration.

**Location:** `TripleDerby.Infrastructure/Messaging/MessageBusExtensions.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Infrastructure.Messaging;

/// <summary>
/// Extension methods for configuring message bus with routing abstraction.
/// </summary>
public static class MessageBusExtensions
{
    /// <summary>
    /// Adds message bus with automatic provider selection based on configuration.
    /// Provider determined by MessageBus:Provider setting ("RabbitMq", "ServiceBus", "Auto").
    /// </summary>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind routing configuration
        services.Configure<MessageRoutingConfig>(
            configuration.GetSection("MessageBus:Routing"));

        var routingConfig = configuration
            .GetSection("MessageBus:Routing")
            .Get<MessageRoutingConfig>() ?? new MessageRoutingConfig();

        var provider = routingConfig.Provider?.ToLowerInvariant() switch
        {
            "rabbitmq" => "RabbitMq",
            "servicebus" => "ServiceBus",
            "auto" => DetectProvider(configuration),
            _ => throw new InvalidOperationException(
                $"Invalid MessageBus:Routing:Provider value: '{routingConfig.Provider}'. " +
                $"Valid values: 'RabbitMq', 'ServiceBus', 'Auto'")
        };

        // Register appropriate publisher based on provider
        if (provider == "RabbitMq")
        {
            services.AddSingleton<RabbitMqMessagePublisher>();
            services.AddSingleton<IMessagePublisher>(sp =>
            {
                var innerPublisher = sp.GetRequiredService<RabbitMqMessagePublisher>();
                var routingOptions = sp.GetRequiredService<IOptions<MessageRoutingConfig>>();
                var logger = sp.GetRequiredService<ILogger<RoutingMessagePublisher>>();

                return new RoutingMessagePublisher(innerPublisher, routingOptions, logger);
            });
        }
        else if (provider == "ServiceBus")
        {
            services.AddSingleton<AzureServiceBusPublisher>();
            services.AddSingleton<IMessagePublisher>(sp =>
            {
                var innerPublisher = sp.GetRequiredService<AzureServiceBusPublisher>();
                var routingOptions = sp.GetRequiredService<IOptions<MessageRoutingConfig>>();
                var logger = sp.GetRequiredService<ILogger<RoutingMessagePublisher>>();

                return new RoutingMessagePublisher(innerPublisher, routingOptions, logger);
            });
        }

        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<MessageBusExtensions>>();
        logger.LogInformation("Message bus configured with provider: {Provider}", provider);

        return services;
    }

    /// <summary>
    /// Detects available provider by checking for connection strings.
    /// </summary>
    private static string DetectProvider(IConfiguration configuration)
    {
        var hasRabbitMq = !string.IsNullOrEmpty(configuration.GetConnectionString("messaging"))
                       || !string.IsNullOrEmpty(configuration.GetConnectionString("RabbitMq"));

        var hasServiceBus = !string.IsNullOrEmpty(configuration.GetConnectionString("servicebus"));

        if (hasServiceBus && hasRabbitMq)
        {
            // Both available - prefer RabbitMq for backward compatibility
            return "RabbitMq";
        }

        if (hasServiceBus)
            return "ServiceBus";

        if (hasRabbitMq)
            return "RabbitMq";

        throw new InvalidOperationException(
            "No message broker connection string found. " +
            "Set ConnectionStrings:messaging (RabbitMQ) or ConnectionStrings:servicebus (Azure Service Bus).");
    }
}
```

### Configuration Schema

#### Example: RabbitMQ Configuration

```json
{
  "ConnectionStrings": {
    "messaging": "amqp://guest:guest@localhost:5672/"
  },
  "MessageBus": {
    "Routing": {
      "Provider": "RabbitMq",
      "DefaultDestination": "triplederby.events",
      "DefaultRoutingKey": "default",
      "Routes": {
        "RaceRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "RaceRequested"
        },
        "BreedingRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "BreedingRequested"
        },
        "TrainingRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "TrainingRequested"
        }
      }
    }
  }
}
```

#### Example: Azure Service Bus Configuration

```json
{
  "ConnectionStrings": {
    "servicebus": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;..."
  },
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus",
      "DefaultDestination": "default-queue",
      "Routes": {
        "RaceRequested": {
          "Destination": "race-requests"
        },
        "BreedingRequested": {
          "Destination": "breeding-requests"
        },
        "TrainingRequested": {
          "Destination": "training-requests"
        }
      }
    }
  }
}
```

#### Example: Auto-Detection

```json
{
  "ConnectionStrings": {
    "messaging": "amqp://guest:guest@localhost:5672/"
  },
  "MessageBus": {
    "Routing": {
      "Provider": "Auto",
      "DefaultDestination": "triplederby.events",
      "Routes": {
        "RaceRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "RaceRequested"
        }
      }
    }
  }
}
```

### Service Registration Changes

#### Before (Current State)

**TripleDerby.Api/Program.cs:**
```csharp
// Keyed DI for dual-broker support
builder.Services.AddKeyedSingleton<IMessagePublisher, RabbitMqMessagePublisher>("rabbitmq");
builder.Services.AddKeyedSingleton<IMessagePublisher, AzureServiceBusPublisher>("servicebus");
```

**RaceService.cs:**
```csharp
public class RaceService(
    ITripleDerbyRepository repository,
    [FromKeyedServices("servicebus")] IMessagePublisher messagePublisher,  // <-- Explicit broker
    ITimeManager timeManager,
    ILogger<RaceService> logger) : IRaceService
```

#### After (Proposed State)

**TripleDerby.Api/Program.cs:**
```csharp
// Single registration - provider selected by configuration
builder.Services.AddMessageBus(builder.Configuration);
```

**RaceService.cs:**
```csharp
public class RaceService(
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,  // <-- No attribute needed!
    ITimeManager timeManager,
    ILogger<RaceService> logger) : IRaceService
{
    // Publishing - same pattern across all services
    await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);
}
```

### Migration Path

#### Phase 1: Add Routing Infrastructure (No Breaking Changes)

1. Create `MessageRoutingConfig` class
2. Create `RoutingMessagePublisher` decorator
3. Create `MessageBusExtensions` registration helpers
4. Add unit tests for routing resolution

**Status:** Infrastructure added, no existing code affected

#### Phase 2: Update API Service Registration

1. Add routing configuration to `appsettings.json`
2. Replace keyed DI with `AddMessageBus()` extension
3. Validate startup with both RabbitMQ and Service Bus configs

**Status:** DI registration changed, services still use keyed injection (backward compatible)

#### Phase 3: Remove Keyed Service Attributes from Services

1. Update `RaceService` to inject plain `IMessagePublisher`
2. Update `BreedingService` to inject plain `IMessagePublisher`
3. Update `TrainingService` to inject plain `IMessagePublisher`
4. Remove explicit `MessagePublishOptions.Destination` where redundant

**Status:** Services simplified, configuration drives routing

#### Phase 4: Update Microservices

1. Update `TripleDerby.Services.Racing` to use `AddMessageBus()`
2. Update `TripleDerby.Services.Breeding` to use `AddMessageBus()`
3. Update `TripleDerby.Services.Training` to use `AddMessageBus()`
4. Each service can independently choose broker via config

**Status:** All services use unified registration pattern

---

## Implementation Plan

### Phase 1: Core Routing Abstractions (2 hours)

**Tasks:**
1. Create `MessageRoutingConfig` class with XML documentation
2. Create `MessageRoute` class for per-message configuration
3. Add `Routing` section binding in configuration
4. Write unit tests for configuration binding
5. Validate configuration schema with example JSON

**Files Created:**
- `TripleDerby.Core/Abstractions/Messaging/MessageRoutingConfig.cs`
- `TripleDerby.Tests.Unit/Messaging/MessageRoutingConfigTests.cs`

**Acceptance Criteria:**
- Configuration binds correctly from appsettings.json
- Invalid provider values throw clear exceptions
- Default values applied when routes not configured

### Phase 2: Routing Publisher Decorator (3 hours)

**Tasks:**
1. Implement `RoutingMessagePublisher` with caching
2. Add routing resolution logic (type → config lookup)
3. Handle explicit `MessagePublishOptions` overrides
4. Add logging for routing decisions
5. Write unit tests with mock inner publisher
6. Performance test caching mechanism

**Files Created:**
- `TripleDerby.Infrastructure/Messaging/RoutingMessagePublisher.cs`
- `TripleDerby.Tests.Unit/Messaging/RoutingMessagePublisherTests.cs`

**Acceptance Criteria:**
- Routes resolved from configuration correctly
- User-provided options override config
- Caching eliminates repeated lookups
- Logging shows routing decisions at debug level

### Phase 3: DI Registration Extensions (2 hours)

**Tasks:**
1. Create `MessageBusExtensions` class
2. Implement `AddMessageBus()` extension method
3. Add provider auto-detection logic
4. Handle connection string validation
5. Add startup logging for provider selection
6. Write integration tests for DI registration

**Files Created:**
- `TripleDerby.Infrastructure/Messaging/MessageBusExtensions.cs`
- `TripleDerby.Tests.Integration/Messaging/MessageBusDITests.cs`

**Acceptance Criteria:**
- Single `AddMessageBus()` call registers all dependencies
- Provider auto-detection works correctly
- Clear error messages for missing connection strings
- Startup logs indicate active provider

### Phase 4: Configuration Updates (1 hour)

**Tasks:**
1. Add `MessageBus:Routing` section to API appsettings.json
2. Add routing config to Racing microservice appsettings.json
3. Add routing config to Breeding microservice appsettings.json
4. Add routing config to Training microservice appsettings.json
5. Document configuration schema with examples
6. Add appsettings.Development.json overrides

**Files Modified:**
- `TripleDerby.Api/appsettings.json`
- `TripleDerby.Services.Racing/appsettings.json`
- `TripleDerby.Services.Breeding/appsettings.json`
- `TripleDerby.Services.Training/appsettings.json`
- `docs/configuration/message-bus-configuration.md` (new)

**Acceptance Criteria:**
- All message types have routing configuration
- Configuration validates at startup
- Development/Production configs documented

### Phase 5: API Service Updates (2 hours)

**Tasks:**
1. Replace keyed DI in `TripleDerby.Api/Program.cs` with `AddMessageBus()`
2. Remove `[FromKeyedServices]` from `RaceService`
3. Remove `[FromKeyedServices]` from `BreedingService`
4. Remove `[FromKeyedServices]` from `TrainingService`
5. Remove explicit `Destination` from publishing calls where redundant
6. Add startup validation for routing config

**Files Modified:**
- `TripleDerby.Api/Program.cs`
- `TripleDerby.Core/Services/RaceService.cs`
- `TripleDerby.Core/Services/BreedingService.cs`
- `TripleDerby.Core/Services/TrainingService.cs`

**Acceptance Criteria:**
- Services inject plain `IMessagePublisher` without attributes
- Publishing works identically to before
- Can switch broker via config without code changes

### Phase 6: Microservice Updates (3 hours)

**Tasks:**
1. Update Racing microservice to use `AddMessageBus()`
2. Update Breeding microservice to use `AddMessageBus()`
3. Update Training microservice to use `AddMessageBus()`
4. Remove provider-specific registration code
5. Test each microservice with both RabbitMQ and Service Bus
6. Validate message flow end-to-end

**Files Modified:**
- `TripleDerby.Services.Racing/Program.cs`
- `TripleDerby.Services.Breeding/Program.cs`
- `TripleDerby.Services.Training/Program.cs`

**Acceptance Criteria:**
- Each microservice can independently choose broker
- Message publishing/consuming works with both providers
- No runtime errors or performance degradation

### Phase 7: Testing & Validation (3 hours)

**Tasks:**
1. Unit test routing resolution for all message types
2. Integration test with RabbitMQ configuration
3. Integration test with Azure Service Bus configuration
4. Integration test with auto-detection
5. Test invalid configuration scenarios
6. Performance test routing cache
7. End-to-end test: Publish → Consume → Verify

**Files Created:**
- `TripleDerby.Tests.Unit/Messaging/RoutingIntegrationTests.cs`
- `TripleDerby.Tests.Integration/Messaging/BrokerSwitchingTests.cs`

**Acceptance Criteria:**
- 100% of message types route correctly
- Switching brokers via config works without code changes
- Performance overhead < 1ms per publish call
- All existing integration tests pass

### Phase 8: Documentation (2 hours)

**Tasks:**
1. Update architecture diagrams
2. Document configuration schema with examples
3. Create migration guide from keyed DI
4. Add troubleshooting section
5. Document auto-detection behavior
6. Update README with message bus section

**Files Created/Modified:**
- `docs/architecture/message-bus-abstraction.md`
- `docs/configuration/message-bus-configuration.md`
- `docs/migration/from-keyed-di-to-routing.md`
- `README.md` (add Message Bus section)

**Acceptance Criteria:**
- Clear examples for each provider
- Migration steps documented
- Troubleshooting guide for common issues
- Architecture diagrams updated

---

## Success Criteria

### Functional Validation

- [ ] Single `Provider` config value switches entire system between brokers
- [ ] All three services (Race, Breeding, Training) publish successfully
- [ ] Message routing resolved correctly for all message types
- [ ] Auto-detection works when `Provider: "Auto"` configured
- [ ] Invalid configuration throws clear error at startup
- [ ] User-provided `MessagePublishOptions` override config (backward compatibility)
- [ ] No runtime routing errors or exceptions

### Code Quality

- [ ] Services use plain `IMessagePublisher` injection (no `FromKeyedServices`)
- [ ] Routing configuration centralized in appsettings.json
- [ ] Configuration validated at startup with actionable error messages
- [ ] XML documentation on all public APIs
- [ ] Unit test coverage > 85% for routing components
- [ ] Integration tests cover both RabbitMQ and Service Bus

### Performance

- [ ] Routing resolution overhead < 1ms per publish (cached lookups)
- [ ] No additional memory allocations per publish
- [ ] Startup time increased by < 100ms
- [ ] No impact on message throughput

### Developer Experience

- [ ] Adding new message type requires only config change
- [ ] Switching broker requires changing one config value
- [ ] Clear IntelliSense for configuration schema
- [ ] Startup logs indicate active provider and route count
- [ ] Error messages actionable (e.g., "Add MessageBus:Routing:Routes:NewMessage section")

---

## Configuration Examples

### Complete Example: RabbitMQ

```json
{
  "ConnectionStrings": {
    "sql": "Server=localhost,59944;...",
    "cache": "localhost:6379",
    "messaging": "amqp://guest:guest@localhost:5672/"
  },
  "MessageBus": {
    "Routing": {
      "Provider": "RabbitMq",
      "DefaultDestination": "triplederby.events",
      "DefaultRoutingKey": "default",
      "Routes": {
        "RaceRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "RaceRequested"
        },
        "RaceCompleted": {
          "Destination": "triplederby.events",
          "RoutingKey": "RaceCompleted"
        },
        "BreedingRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "BreedingRequested"
        },
        "BreedingCompleted": {
          "Destination": "triplederby.events",
          "RoutingKey": "BreedingCompleted"
        },
        "TrainingRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "TrainingRequested"
        },
        "TrainingCompleted": {
          "Destination": "triplederby.events",
          "RoutingKey": "TrainingCompleted"
        }
      }
    }
  }
}
```

### Complete Example: Azure Service Bus

```json
{
  "ConnectionStrings": {
    "sql": "Server=localhost,59944;...",
    "cache": "localhost:6379",
    "servicebus": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;..."
  },
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus",
      "DefaultDestination": "default-queue",
      "Routes": {
        "RaceRequested": {
          "Destination": "race-requests"
        },
        "RaceCompleted": {
          "Destination": "race-completions"
        },
        "BreedingRequested": {
          "Destination": "breeding-requests"
        },
        "BreedingCompleted": {
          "Destination": "breeding-completions"
        },
        "TrainingRequested": {
          "Destination": "training-requests"
        },
        "TrainingCompleted": {
          "Destination": "training-completions"
        }
      }
    }
  }
}
```

### Minimal Example: Auto-Detection

```json
{
  "ConnectionStrings": {
    "messaging": "amqp://guest:guest@localhost:5672/"
  },
  "MessageBus": {
    "Routing": {
      "Provider": "Auto",
      "DefaultDestination": "triplederby.events",
      "DefaultRoutingKey": "default",
      "Routes": {}
    }
  }
}
```

When `Routes` is empty, all messages use:
- **Destination**: `DefaultDestination` value
- **Routing Key**: Message type name (e.g., `"RaceRequested"`)

---

## Error Handling

### Startup Validation Errors

**Missing Provider:**
```
System.InvalidOperationException: MessageBus:Routing:Provider is required.
Set to 'RabbitMq', 'ServiceBus', or 'Auto'.
```

**Invalid Provider:**
```
System.InvalidOperationException: Invalid MessageBus:Routing:Provider value: 'Redis'.
Valid values: 'RabbitMq', 'ServiceBus', 'Auto'
```

**No Connection String:**
```
System.InvalidOperationException: No message broker connection string found.
Set ConnectionStrings:messaging (RabbitMQ) or ConnectionStrings:servicebus (Azure Service Bus).
```

### Runtime Warnings

**Missing Route:**
```
[WARN] No routing configuration found for TrainingRequested, using defaults
```

**Fallback to Defaults:**
```
[WARN] Message type 'NewMessage' not configured in MessageBus:Routing:Routes, using DefaultDestination
```

---

## Migration Guide

### From Keyed DI to Unified Registration

**Before:**
```csharp
// Program.cs
builder.Services.AddKeyedSingleton<IMessagePublisher, RabbitMqMessagePublisher>("rabbitmq");
builder.Services.AddKeyedSingleton<IMessagePublisher, AzureServiceBusPublisher>("servicebus");

// RaceService.cs
public RaceService(
    [FromKeyedServices("servicebus")] IMessagePublisher messagePublisher)
{
    await messagePublisher.PublishAsync(
        message,
        new MessagePublishOptions { Destination = "race-requests" },
        cancellationToken);
}
```

**After:**
```csharp
// Program.cs
builder.Services.AddMessageBus(builder.Configuration);

// RaceService.cs
public RaceService(IMessagePublisher messagePublisher)
{
    await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);
}

// appsettings.json
{
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus",
      "Routes": {
        "RaceRequested": {
          "Destination": "race-requests"
        }
      }
    }
  }
}
```

**Benefits:**
- Remove `[FromKeyedServices]` attributes from 3 services
- Remove explicit `Destination` from ~12 publishing call sites
- Switch entire application via 1 config value
- Routing visible in configuration (not hidden in code)

---

## Performance Considerations

### Routing Cache

**Implementation:**
```csharp
private readonly ConcurrentDictionary<Type, MessagePublishOptions> _routingCache = new();

private MessagePublishOptions GetOrCreateRoutingOptions<T>(MessagePublishOptions? userOptions)
{
    return _routingCache.GetOrAdd(typeof(T), _ =>
    {
        // Resolution logic (executed once per message type)
        // ...
    });
}
```

**Benchmarks (expected):**

| Operation | Without Cache | With Cache |
|-----------|---------------|------------|
| First publish (cold) | ~50 μs | ~50 μs |
| Subsequent publishes (hot) | ~50 μs | ~0.5 μs |

**Memory Overhead:**
- ~200 bytes per cached message type
- Expected: 8 message types = ~1.6 KB total
- Negligible impact on heap

### Comparison to Keyed DI

| Approach | Resolution Time | Memory | Code Complexity |
|----------|-----------------|---------|-----------------|
| Keyed DI | ~1-2 μs (service locator) | Registry overhead | High (attributes everywhere) |
| Routing Decorator | ~0.5 μs (cached dictionary) | Minimal | Low (config-driven) |

**Verdict:** Routing decorator performs better than keyed DI while improving code clarity.

---

## Future Enhancements

### 1. Dynamic Route Reloading

Allow changing routes without restart:

```csharp
services.Configure<MessageRoutingConfig>(
    configuration.GetSection("MessageBus:Routing"));

// Watch for configuration changes
configuration.GetReloadToken().RegisterChangeCallback(_ =>
{
    _routingCache.Clear(); // Invalidate cache
}, null);
```

### 2. Conditional Routing

Route based on message properties:

```json
{
  "Routes": {
    "RaceRequested": {
      "Rules": [
        {
          "Condition": "Priority == 'High'",
          "Destination": "race-requests-priority"
        },
        {
          "Condition": "default",
          "Destination": "race-requests"
        }
      ]
    }
  }
}
```

### 3. Multi-Broker Publishing

Publish same message to multiple brokers:

```json
{
  "Routes": {
    "RaceRequested": {
      "Destinations": [
        { "Provider": "RabbitMq", "Destination": "triplederby.events" },
        { "Provider": "ServiceBus", "Destination": "race-requests" }
      ]
    }
  }
}
```

### 4. Provider-Specific Routing

Different routes per provider:

```json
{
  "Routes": {
    "RaceRequested": {
      "RabbitMq": {
        "Destination": "triplederby.events",
        "RoutingKey": "RaceRequested"
      },
      "ServiceBus": {
        "Destination": "race-requests",
        "Subject": "high-priority"
      }
    }
  }
}
```

### 5. Schema Validation

Validate message structure at startup:

```csharp
services.AddMessageBus(configuration)
    .WithSchemaValidation()
    .ValidateOnStartup();
```

---

## Testing Strategy

### Unit Tests

**MessageRoutingConfig Binding:**
```csharp
[Fact]
public void MessageRoutingConfig_BindsFromConfiguration()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["MessageBus:Routing:Provider"] = "RabbitMq",
            ["MessageBus:Routing:Routes:RaceRequested:Destination"] = "race-queue"
        })
        .Build();

    var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

    Assert.Equal("RabbitMq", routingConfig.Provider);
    Assert.Equal("race-queue", routingConfig.Routes["RaceRequested"].Destination);
}
```

**Routing Resolution:**
```csharp
[Fact]
public async Task RoutingPublisher_ResolvesRouteFromConfig()
{
    var mockPublisher = new Mock<IMessagePublisher>();
    var routingConfig = new MessageRoutingConfig
    {
        Routes = new Dictionary<string, MessageRoute>
        {
            ["RaceRequested"] = new MessageRoute
            {
                Destination = "race-requests",
                RoutingKey = "RaceRequested"
            }
        }
    };

    var publisher = new RoutingMessagePublisher(
        mockPublisher.Object,
        Options.Create(routingConfig),
        Mock.Of<ILogger<RoutingMessagePublisher>>());

    await publisher.PublishAsync(new RaceRequested());

    mockPublisher.Verify(p => p.PublishAsync(
        It.IsAny<RaceRequested>(),
        It.Is<MessagePublishOptions>(o =>
            o.Destination == "race-requests" &&
            o.Subject == "RaceRequested"),
        It.IsAny<CancellationToken>()));
}
```

### Integration Tests

**Broker Switching:**
```csharp
[Theory]
[InlineData("RabbitMq")]
[InlineData("ServiceBus")]
public async Task CanSwitchBrokerViaConfiguration(string provider)
{
    var services = new ServiceCollection();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["ConnectionStrings:messaging"] = "amqp://localhost",
            ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost",
            ["MessageBus:Routing:Provider"] = provider
        })
        .Build();

    services.AddMessageBus(config);

    var serviceProvider = services.BuildServiceProvider();
    var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

    // Verify correct publisher type registered
    Assert.NotNull(publisher);
}
```

---

## Dependencies

### NuGet Packages

**No new packages required** - uses existing:
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.Configuration.Binder`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Options`

### Internal Dependencies

- `TripleDerby.Core.Abstractions.Messaging.IMessagePublisher` (existing)
- `TripleDerby.Core.Abstractions.Messaging.MessagePublishOptions` (existing)
- `TripleDerby.Infrastructure.Messaging.RabbitMqMessagePublisher` (existing)
- `TripleDerby.Infrastructure.Messaging.AzureServiceBusPublisher` (existing)

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Configuration errors cause runtime failures | Medium | High | Validate configuration at startup with clear error messages |
| Performance regression from routing overhead | Low | Medium | Cache routing lookups; benchmark before/after |
| Breaking changes during migration | Medium | High | Maintain backward compatibility with `MessagePublishOptions` |
| Missing routes cause silent failures | Medium | High | Log warnings for unconfigured message types; fail fast on critical paths |
| Complex configuration for developers | Low | Medium | Provide clear examples and IntelliSense; auto-detection for simple cases |

---

## Rollback Plan

If critical issues arise:

1. **Revert DI Registration:** Replace `AddMessageBus()` with keyed services
2. **Restore Attributes:** Add `[FromKeyedServices]` back to services
3. **Remove Routing Config:** Delete `MessageBus:Routing` section
4. **Redeploy:** All changes are additive; rollback is code revert

**Rollback Time:** < 30 minutes

---

## Acceptance Criteria Summary

### Must Have (MVP)

- [ ] `RoutingMessagePublisher` decorator implemented
- [ ] `MessageRoutingConfig` binding from appsettings.json
- [ ] `AddMessageBus()` extension method for DI registration
- [ ] Provider selection: `"RabbitMq"`, `"ServiceBus"`, `"Auto"`
- [ ] All 8 message types configured with routes
- [ ] Services updated to use plain `IMessagePublisher` injection
- [ ] Startup validation with actionable error messages
- [ ] Unit tests for routing resolution
- [ ] Integration tests for both RabbitMQ and Service Bus
- [ ] Documentation: architecture, configuration, migration guide

### Nice to Have (Future)

- [ ] Dynamic route reloading without restart
- [ ] Conditional routing based on message properties
- [ ] Multi-broker publishing (same message to multiple brokers)
- [ ] Schema validation at startup
- [ ] Performance monitoring/metrics for routing decisions

---

## Related Features

- **Feature 011**: Race Microservice Migration (established dual-broker pattern)
- **Feature 015**: Generic Message Consumers (consumer-side abstraction)
- **Feature 017**: Breeding RabbitMQ Performance Optimization (publisher performance)
- **Feature 020**: Horse Training System (third message-driven service)

---

## References

- [Decorator Pattern (GoF)](https://en.wikipedia.org/wiki/Decorator_pattern)
- [Options Pattern in .NET](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [RabbitMQ Topic Exchange](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html)
- [Azure Service Bus Queues](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-queues-topics-subscriptions)
- Existing Code:
  - [RaceService.cs:19](c:\Development\TripleDerby\TripleDerby.Core\Services\RaceService.cs#L19)
  - [BreedingService.cs:27](c:\Development\TripleDerby\Core\Services\BreedingService.cs#L27)
  - [TrainingService.cs:20](c:\Development\TripleDerby\Core\Services\TrainingService.cs#L20)
  - [RabbitMqMessagePublisher.cs](c:\Development\TripleDerby\TripleDerby.Infrastructure\Messaging\RabbitMqMessagePublisher.cs)
  - [AzureServiceBusPublisher.cs](c:\Development\TripleDerby\TripleDerby.Infrastructure\Messaging\AzureServiceBusPublisher.cs)

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2026-01-07 | Discovery Agent | Initial feature specification based on user request for configuration-driven broker switching |
