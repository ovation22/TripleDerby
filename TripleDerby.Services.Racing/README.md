# Racing Service

Background microservice responsible for executing race simulations in the TripleDerby game. Consumes `RaceRequested` messages and publishes `RaceCompleted` events.

## Overview

The Racing Service performs all race simulation logic including:
- Horse movement and positioning
- Speed/stamina calculations with multi-phase modifiers
- Track condition effects (weather, surface)
- Overtaking mechanics and lane positioning
- Commentary generation
- Results persistence and purse distribution

**Processing Model**: Message-driven consumer with configurable concurrency

## Architecture

```mermaid
graph TB
    subgraph "Message Broker"
        Queue[race-requests Queue]
    end

    subgraph "Racing Service"
        Worker[Worker Host]
        Consumer[GenericMessageConsumer]
        Adapter[IMessageBrokerAdapter<br/>RabbitMq or ServiceBus]
        Processor[RaceRequestProcessor]
        Executor[RaceExecutor]
    end

    subgraph "Infrastructure"
        DB[(SQL Server)]
        Publisher[IMessagePublisher]
        Cache[Redis Cache]
    end

    subgraph "Message Broker Out"
        EventQueue[triplederby.events]
    end

    Queue -->|Subscribe| Adapter
    Worker --> Consumer
    Consumer --> Adapter
    Adapter -->|RaceRequested| Processor
    Processor --> Executor
    Executor -->|Read race data| DB
    Executor -->|Simulate| Executor
    Executor -->|Save results| DB
    Executor -->|RaceCompleted| Publisher
    Executor -.->|Update stats| Cache
    Publisher -->|Publish| EventQueue
```

## Message Flow

```mermaid
sequenceDiagram
    participant Broker as Message Broker
    participant Consumer as GenericMessageConsumer
    participant Processor as RaceRequestProcessor
    participant Executor as RaceExecutor
    participant DB as Database
    participant Publisher as IMessagePublisher

    Broker->>Consumer: RaceRequested message
    Consumer->>Processor: ProcessAsync(RaceRequested)
    Processor->>DB: Load race and entries
    Processor->>Executor: ExecuteRaceAsync(race)

    Note over Executor: Race simulation
    loop Each tick until finish
        Executor->>Executor: Calculate speeds/positions
        Executor->>Executor: Apply modifiers
        Executor->>Executor: Handle overtaking
    end

    Executor->>DB: Save RaceRun and results
    Executor->>Publisher: PublishAsync(RaceCompleted)
    Publisher->>Broker: Publish event
    Executor-->>Processor: RaceRun
    Processor-->>Consumer: Success
    Consumer->>Broker: ACK message
```

## Configuration

### Message Bus Configuration (RabbitMQ)

```json
{
  "ConnectionStrings": {
    "messaging": "amqp://guest:guest@localhost:5672/"
  },
  "MessageBus": {
    "Routing": {
      "Provider": "RabbitMq",
      "DefaultDestination": "triplederby.events",
      "Routes": {
        "RaceCompleted": {
          "Destination": "triplederby.events",
          "RoutingKey": "RaceCompleted"
        }
      }
    },
    "Consumer": {
      "Queue": "triplederby.racing.requests",
      "Concurrency": 24,
      "MaxRetries": 3,
      "PrefetchCount": 48
    },
    "RabbitMq": {
      "Exchange": "triplederby.events",
      "RoutingKey": "RaceRequested",
      "ExchangeType": "topic"
    }
  }
}
```

### Message Bus Configuration (Azure Service Bus)

```json
{
  "ConnectionStrings": {
    "servicebus": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY_HERE"
  },
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus",
      "DefaultDestination": "triplederby-events-topic",
      "Routes": {
        "RaceCompleted": {
          "Destination": "triplederby-events-topic"
        }
      }
    },
    "Consumer": {
      "Queue": "racing-requests",
      "Concurrency": 24,
      "MaxRetries": 3,
      "PrefetchCount": 48
    },
    "ServiceBus": {
      "SubscriptionName": "racing-service-subscription"
    }
  }
}
```

### Database Connection

```json
{
  "ConnectionStrings": {
    "TripleDerby": "Server=localhost;Database=TripleDerby;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Messages

### Consumes

**RaceRequested**
```csharp
{
  "RaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "CorrelationId": "request-123",
  "RequestedAt": "2026-01-18T10:00:00Z"
}
```

### Publishes

**RaceCompleted**
```csharp
{
  "RaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "RaceRunId": "8d9e1f23-4567-8901-c2de-3f4567890123",
  "CompletedAt": "2026-01-18T10:02:15Z",
  "Winner": {
    "HorseId": "...",
    "Name": "Lightning Bolt",
    "FinishTime": 135.42
  }
}
```

## Race Simulation

The service executes a tick-based race simulation with these phases:

1. **Initialization**: Load race, horses, track conditions
2. **Start Phase** (0-20%): Horses break from gate
3. **Early Phase** (20-40%): Initial positioning
4. **Middle Phase** (40-60%): Strategic running
5. **Late Phase** (60-80%): Stamina begins to factor
6. **Finish Phase** (80-100%): Final sprint

### Modifier Pipeline

Each tick applies modifiers in order:
1. **Base Stats**: Speed, Stamina, Acceleration
2. **Leg Type**: Phase-specific bonuses
3. **Track Conditions**: Weather and surface effects
4. **Stamina Depletion**: Based on remaining stamina
5. **Random Variance**: ±5% variation

### Performance Tuning

**Concurrency**: Adjust based on CPU cores and database connection pool

```json
{
  "MessageBus": {
    "Consumer": {
      "Concurrency": 24,      // Parallel race simulations
      "PrefetchCount": 48     // Messages buffered locally
    }
  }
}
```

**Recommendations**:
- **4-core**: Concurrency 8-16
- **8-core**: Concurrency 16-24
- **16-core**: Concurrency 24-48

## Running the Service

### Prerequisites

1. **.NET 10 SDK** installed
2. **SQL Server** with TripleDerby database
3. **RabbitMQ** running OR **Azure Service Bus** configured

### Standalone

```bash
cd TripleDerby.Services.Racing
dotnet run
```

### With Docker

```bash
# Build image
docker build -t triplederby-racing -f TripleDerby.Services.Racing/Dockerfile .

# Run container
docker run -d \
  -e ConnectionStrings__TripleDerby="Server=host.docker.internal;..." \
  -e ConnectionStrings__messaging="amqp://host.docker.internal:5672" \
  triplederby-racing
```

### With .NET Aspire

```bash
cd TripleDerby.AppHost
dotnet run
```

## Switching Message Brokers

To switch from RabbitMQ to Azure Service Bus:

1. Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "servicebus": "Endpoint=sb://..."
  },
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus"  // Changed from "RabbitMq"
    }
  }
}
```

2. Restart the service

**No code changes required!**

## Monitoring

### Logs

The service logs key events:
- Message consumption start/complete
- Race execution start/complete
- Errors and retries
- Performance metrics

```bash
# View logs (if using Serilog to file)
tail -f logs/racing-service.log
```

### Health Checks

Accessible at `/health` when running with ServiceDefaults.

### Metrics

Monitor these metrics:
- **Messages/sec**: Throughput
- **Avg processing time**: Performance
- **Error rate**: Reliability
- **Queue depth**: Backlog

## Troubleshooting

### Service starts but doesn't process messages

**Symptoms**: Service running, no errors, but messages stay in queue

**Common Causes**:
1. Wrong queue name in configuration
2. Message broker provider mismatch
3. Connection string incorrect

**Solution**:
```bash
# Check RabbitMQ queue exists
curl http://localhost:15672/api/queues/%2F/triplederby.racing.requests \
  -u guest:guest

# Check service logs for connection errors
dotnet run --environment Development
```

### Messages being dead-lettered

**Symptoms**: Messages disappear from queue without processing

**Common Causes**:
1. Deserialization errors (message format changed)
2. Unhandled exceptions in processor
3. Max retries exceeded

**Solution**: Check dead-letter queue and service logs:

```bash
# RabbitMQ DLQ
curl http://localhost:15672/api/queues/%2F/triplederby.racing.requests_dlq \
  -u guest:guest
```

### Race simulations taking too long

**Symptoms**: High processing time, message backlog growing

**Solutions**:
1. **Increase concurrency** (if CPU allows)
2. **Scale horizontally** (run multiple instances)
3. **Optimize database queries** (check for missing indexes)
4. **Increase prefetch** for better throughput

### Database connection pool exhausted

**Error**: `Timeout expired. The timeout period elapsed prior to obtaining a connection`

**Solution**: Increase connection pool or reduce concurrency:

```json
{
  "ConnectionStrings": {
    "TripleDerby": "Server=...;Max Pool Size=200;..."
  },
  "MessageBus": {
    "Consumer": {
      "Concurrency": 16  // Reduced from 24
    }
  }
}
```

### Memory leak / increasing memory usage

**Common Causes**:
1. Race simulations not disposing resources
2. Message context not released
3. Entity Framework tracking too many entities

**Solution**:
- Ensure DbContext is scoped (should be default)
- Use `AsNoTracking()` for read-only queries
- Monitor with profiler

## Related Documentation

- [API README](../TripleDerby.Api/README.md)
- [Breeding Service README](../TripleDerby.Services.Breeding/README.md)
- [Training Service README](../TripleDerby.Services.Training/README.md)
- [Feeding Service README](../TripleDerby.Services.Feeding/README.md)
- [Race Simulation Design](../docs/features/race-simulation.md)
- [Unified Messaging Architecture](../docs/features/023-unified-microservice-messaging-architecture.md)

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run racing-specific tests
dotnet test --filter Category=Racing

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Adding New Race Modifiers

1. Create calculator in `Calculators/`
2. Implement `ISpeedModifierCalculator` or similar interface
3. Register in `Program.cs`
4. Add to RaceExecutor modifier pipeline

### Debugging Race Simulations

Enable detailed logging:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "TripleDerby.Services.Racing.RaceExecutor": "Verbose"
      }
    }
  }
}
```
