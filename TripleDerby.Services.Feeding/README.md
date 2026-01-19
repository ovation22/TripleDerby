# Feeding Service

Background microservice responsible for horse feeding operations in the TripleDerby game. Consumes `FeedingRequested` messages and publishes `FeedingCompleted` events.

## Overview

The Feeding Service manages horse nutrition and health:
- Energy restoration and health maintenance
- Feed type effects (quality, nutritional value)
- Hunger and energy management
- Feeding validation and error handling

**Processing Model**: Message-driven consumer with configurable concurrency

## Architecture

```mermaid
graph TB
    subgraph "Message Broker"
        Queue[feeding-requests Queue]
    end

    subgraph "Feeding Service"
        Worker[Worker Host]
        Consumer[GenericMessageConsumer]
        Adapter[IMessageBrokerAdapter<br/>RabbitMq or ServiceBus]
        Processor[FeedingRequestProcessor]
        Executor[FeedingExecutor]
        Calculator[FeedingCalculator]
    end

    subgraph "Infrastructure"
        DB[(SQL Server)]
        Publisher[IMessagePublisher]
    end

    subgraph "Message Broker Out"
        EventQueue[triplederby.events]
    end

    Queue -->|Subscribe| Adapter
    Worker --> Consumer
    Consumer --> Adapter
    Adapter -->|FeedingRequested| Processor
    Processor --> Executor
    Executor -->|Read horse| DB
    Executor -->|Calculate nutrition| Calculator
    Executor -->|Update health/energy| DB
    Executor -->|FeedingCompleted| Publisher
    Publisher -->|Publish| EventQueue
```

## Message Flow

```mermaid
sequenceDiagram
    participant Broker as Message Broker
    participant Consumer as GenericMessageConsumer
    participant Processor as FeedingRequestProcessor
    participant Executor as FeedingExecutor
    participant Calculator as FeedingCalculator
    participant DB as Database
    participant Publisher as IMessagePublisher

    Broker->>Consumer: FeedingRequested message
    Consumer->>Processor: ProcessAsync(FeedingRequested)
    Processor->>DB: Load horse
    Processor->>Executor: ExecuteFeedingAsync(horse, feedType)
    Executor->>Calculator: CalculateNutrition(feedType)
    Calculator-->>Executor: Energy/health gains
    Executor->>DB: Update horse status
    Executor->>Publisher: PublishAsync(FeedingCompleted)
    Publisher->>Broker: Publish event
    Executor-->>Processor: Updated status
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
        "FeedingCompleted": {
          "Destination": "triplederby.events",
          "RoutingKey": "FeedingCompleted"
        }
      }
    },
    "Consumer": {
      "Queue": "triplederby.feeding.requests",
      "Concurrency": 16,
      "MaxRetries": 3,
      "PrefetchCount": 32
    },
    "RabbitMq": {
      "Exchange": "triplederby.events",
      "RoutingKey": "FeedingRequested",
      "ExchangeType": "topic"
    }
  }
}
```

### Message Bus Configuration (Azure Service Bus)

```json
{
  "ConnectionStrings": {
    "servicebus": "Endpoint=sb://your-namespace.servicebus.windows.net/;..."
  },
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus",
      "DefaultDestination": "triplederby-events-topic",
      "Routes": {
        "FeedingCompleted": {
          "Destination": "triplederby-events-topic"
        }
      }
    },
    "Consumer": {
      "Queue": "feeding-requests",
      "Concurrency": 16,
      "MaxRetries": 3,
      "PrefetchCount": 32
    }
  }
}
```

## Messages

### Consumes

**FeedingRequested**
```csharp
{
  "HorseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "FeedType": "Premium",  // Basic, Standard, Premium
  "Quantity": 1.0,
  "CorrelationId": "feed-123",
  "RequestedAt": "2026-01-18T10:00:00Z"
}
```

### Publishes

**FeedingCompleted**
```csharp
{
  "HorseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "FeedType": "Premium",
  "EnergyRestored": 25,
  "HealthRestored": 10,
  "CompletedAt": "2026-01-18T10:00:00Z"
}
```

## Feeding Mechanics

### Feed Types and Effects

**Basic Feed**:
- Energy: +10 points
- Health: +5 points
- Cost: Low

**Standard Feed**:
- Energy: +20 points
- Health: +8 points
- Cost: Medium

**Premium Feed**:
- Energy: +30 points
- Health: +12 points
- Cost: High

**Performance Feed** (future):
- Energy: +25 points
- Health: +10 points
- Temporary stat boost: +2 Speed
- Cost: Very High

### Energy and Health Caps

Status values cannot exceed maximums:
- **Energy**: Max 100
- **Health**: Max 100

If feeding would exceed cap, restoration is reduced to reach cap exactly.

### Feeding Frequency

Current implementation allows unlimited feeding. Future enhancements may include:
- Feeding cooldown periods
- Over-feeding penalties (health reduction)
- Satiety system (diminishing returns)

## Running the Service

### Prerequisites

1. **.NET 10 SDK** installed
2. **SQL Server** with TripleDerby database
3. **RabbitMQ** OR **Azure Service Bus** configured

### Standalone

```bash
cd TripleDerby.Services.Feeding
dotnet run
```

### With Docker

```bash
docker build -t triplederby-feeding -f TripleDerby.Services.Feeding/Dockerfile .

docker run -d \
  -e ConnectionStrings__TripleDerby="Server=host.docker.internal;..." \
  -e ConnectionStrings__messaging="amqp://host.docker.internal:5672" \
  triplederby-feeding
```

## Switching Message Brokers

Update `appsettings.json`:

```json
{
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus"  // Changed from "RabbitMq"
    }
  }
}
```

**No code changes required!**

## Performance Tuning

Feeding operations are very lightweight:
- Simple arithmetic calculations
- Single database update per message
- Can handle very high concurrency

**Recommended Concurrency**:
- **4-core**: 8-16
- **8-core**: 16-32
- **16-core**: 32-48

## Monitoring

### Key Metrics

- **Feeding operations/sec**: Throughput
- **Avg feeding time**: Should be <50ms
- **Energy/health restoration averages**: Game balance
- **Queue depth**: Backlog indicator

### Logs

```bash
# Service logs feeding events
[10:00:00 INF] Feeding completed: Horse Lightning (ID: ...) fed Premium -> Energy: +30, Health: +12
```

## Troubleshooting

### Energy/health not increasing

**Symptoms**: Messages processed, but horse status unchanged

**Common Causes**:
1. Energy/health already at maximum (100)
2. Feeding calculator returning zero restoration
3. Database update not committed

**Solution**:
```sql
-- Check horse status
SELECT Energy, Health FROM Horses WHERE Id = '...';

-- Check for horses at cap
SELECT * FROM Horses WHERE Energy >= 100 AND Health >= 100;
```

### Invalid feed type errors

**Error**: `Unknown feed type: 'UltraPremium'`

**Solution**: Ensure feed type is valid:
- Basic
- Standard
- Premium

Check `FeedingCalculator` for supported types.

### Horse not found errors

**Error**: `Horse with ID ... not found`

**Solution**: Verify horse exists before publishing FeedingRequested:
```sql
SELECT * FROM Horses WHERE Id = '...';
```

### Feeding calculations seem wrong

Enable verbose logging to debug:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "TripleDerby.Services.Feeding.FeedingCalculator": "Debug"
      }
    }
  }
}
```

## Related Documentation

- [API README](../TripleDerby.Api/README.md)
- [Racing Service README](../TripleDerby.Services.Racing/README.md)
- [Breeding Service README](../TripleDerby.Services.Breeding/README.md)
- [Training Service README](../TripleDerby.Services.Training/README.md)
- [Horse Feeding System Design](../docs/features/022-horse-feeding-system.md)
- [Unified Messaging Architecture](../docs/features/023-unified-microservice-messaging-architecture.md)

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run feeding-specific tests
dotnet test --filter Category=Feeding
```

### Adding New Feed Types

1. Add feed type enum value
2. Implement nutrition calculation in `FeedingCalculator`
3. Add tests for new feed type
4. Update API to accept new type
5. Add pricing (future) for new feed

### Testing Feed Effects

```csharp
// Test feed type effects
var horse = new Horse { Energy = 50, Health = 70 };
var result = calculator.CalculateNutrition(FeedType.Premium);

Assert.Equal(30, result.EnergyRestored);
Assert.Equal(12, result.HealthRestored);
```

## Future Enhancements

1. **Feeding Schedule**: Automatic daily feeding
2. **Over-feeding Penalties**: Health reduction if fed too frequently
3. **Nutrition Tracking**: Historical feeding data for analytics
4. **Feed Inventory**: Player-owned feed stocks
5. **Feed Market**: Dynamic feed pricing based on demand
6. **Special Feeds**: Seasonal or event-specific feeds with unique effects
