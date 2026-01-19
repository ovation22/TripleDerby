# Breeding Service

Background microservice responsible for horse breeding operations in the TripleDerby game. Consumes `BreedingRequested` messages and publishes `BreedingCompleted` events.

## Overview

The Breeding Service implements genetic inheritance and foal creation:
- Punnett square-style dominant/recessive genetics
- Weighted color rarity selection
- Leg type inheritance with mutation chances
- Stat progression calculation
- Name generation for foals
- Breeding validation and error handling

**Processing Model**: Message-driven consumer with high concurrency support

## Architecture

```mermaid
graph TB
    subgraph "Message Broker"
        Queue[breeding-requests Queue]
    end

    subgraph "Breeding Service"
        Worker[Worker Host]
        Consumer[GenericMessageConsumer]
        Adapter[IMessageBrokerAdapter<br/>RabbitMq or ServiceBus]
        Processor[BreedingRequestProcessor]
        Executor[BreedingExecutor]
    end

    subgraph "Infrastructure"
        DB[(SQL Server)]
        Publisher[IMessagePublisher]
        ColorCache[ColorCache<br/>Performance Optimization]
    end

    subgraph "Message Broker Out"
        EventQueue[triplederby.events]
    end

    Queue -->|Subscribe| Adapter
    Worker --> Consumer
    Consumer --> Adapter
    Adapter -->|BreedingRequested| Processor
    Processor --> Executor
    Executor -->|Read parents| DB
    Executor -.->|Cached colors| ColorCache
    Executor -->|Generate genetics| Executor
    Executor -->|Create foal| DB
    Executor -->|BreedingCompleted| Publisher
    Publisher -->|Publish| EventQueue
```

## Message Flow

```mermaid
sequenceDiagram
    participant Broker as Message Broker
    participant Consumer as GenericMessageConsumer
    participant Processor as BreedingRequestProcessor
    participant Executor as BreedingExecutor
    participant DB as Database
    participant Publisher as IMessagePublisher

    Broker->>Consumer: BreedingRequested message
    Consumer->>Processor: ProcessAsync(BreedingRequested)
    Processor->>DB: Load mare and stallion
    Processor->>Executor: ExecuteBreedingAsync(parents)

    Note over Executor: Genetic calculation
    Executor->>Executor: Inherit color (rarity-weighted)
    Executor->>Executor: Inherit leg type (with mutation)
    Executor->>Executor: Calculate stats (genetic average)
    Executor->>Executor: Generate foal name

    Executor->>DB: Create and save foal horse
    Executor->>Publisher: PublishAsync(BreedingCompleted)
    Publisher->>Broker: Publish event
    Executor-->>Processor: Foal
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
        "BreedingCompleted": {
          "Destination": "triplederby.events",
          "RoutingKey": "BreedingCompleted"
        }
      }
    },
    "Consumer": {
      "Queue": "triplederby.breeding.requests",
      "Concurrency": 24,
      "MaxRetries": 3,
      "PrefetchCount": 48
    },
    "RabbitMq": {
      "Exchange": "triplederby.events",
      "RoutingKey": "BreedingRequested",
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
        "BreedingCompleted": {
          "Destination": "triplederby-events-topic"
        }
      }
    },
    "Consumer": {
      "Queue": "breeding-requests",
      "Concurrency": 24,
      "MaxRetries": 3,
      "PrefetchCount": 48
    },
    "ServiceBus": {
      "SubscriptionName": "breeding-service-subscription"
    }
  }
}
```

## Messages

### Consumes

**BreedingRequested**
```csharp
{
  "MareId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "StallionId": "7c8d9e0f-1234-5678-90ab-cdef12345678",
  "PlayerId": "a1b2c3d4-5678-90ab-cdef-123456789012",
  "CorrelationId": "breed-123",
  "RequestedAt": "2026-01-18T10:00:00Z"
}
```

### Publishes

**BreedingCompleted**
```csharp
{
  "FoalId": "9f8e7d6c-5b4a-3210-fedc-ba9876543210",
  "MareId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "StallionId": "7c8d9e0f-1234-5678-90ab-cdef12345678",
  "FoalName": "Midnight Storm",
  "CompletedAt": "2026-01-18T10:00:01Z"
}
```

## Breeding Genetics

### Color Inheritance

Colors use a weighted rarity system:
1. Load all horse colors with rarity weights
2. Combine mare and stallion colors into candidate pool
3. Select color based on rarity probability (rarer colors less likely)

**Rarity Tiers**:
- **Common**: 40% chance (Bay, Chestnut, Brown)
- **Uncommon**: 30% chance (Gray, Black, Palomino)
- **Rare**: 20% chance (Buckskin, Dun, Roan)
- **Very Rare**: 8% chance (Cremello, Perlino)
- **Legendary**: 2% chance (Brindle, Champagne)

### Leg Type Inheritance

Leg types use dominant/recessive genetics with mutation:
1. Each parent contributes one allele
2. Dominant alleles expressed over recessive
3. 5% chance of random mutation to any leg type

**Leg Types**:
- **Sprinter**: Fast start, early speed
- **Endurance**: Stamina-focused
- **Balanced**: No specific advantage
- **Late Bloomer**: Strong finish
- **Tactical**: Mid-race positioning

### Stat Calculation

Foal stats are genetic averages with variation:
- **Base**: Average of mare and stallion stats
- **Variance**: ±10% random adjustment
- **Min/Max**: Clamped to valid ranges (1-100)

**Stats Inherited**:
- Speed
- Stamina
- Acceleration
- Adaptability

### Performance Optimization

**ColorCache**: In-memory cache of horse colors loaded at startup
- Eliminates database queries for color selection
- Reduces breeding execution time by ~40%
- Refreshed on service restart

## Running the Service

### Prerequisites

1. **.NET 10 SDK** installed
2. **SQL Server** with TripleDerby database
3. **RabbitMQ** running OR **Azure Service Bus** configured

### Standalone

```bash
cd TripleDerby.Services.Breeding
dotnet run
```

### With Docker

```bash
docker build -t triplederby-breeding -f TripleDerby.Services.Breeding/Dockerfile .

docker run -d \
  -e ConnectionStrings__TripleDerby="Server=host.docker.internal;..." \
  -e ConnectionStrings__messaging="amqp://host.docker.internal:5672" \
  triplederby-breeding
```

### With .NET Aspire

```bash
cd TripleDerby.AppHost
dotnet run
```

## Switching Message Brokers

Update `appsettings.json` and restart:

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

### High-Throughput Configuration

```json
{
  "MessageBus": {
    "Consumer": {
      "Concurrency": 48,      // More parallel breeding operations
      "PrefetchCount": 96     // Larger local buffer
    }
  }
}
```

Breeding is CPU-light and database-light, so can handle high concurrency.

**Recommendations**:
- **4-core**: Concurrency 16-24
- **8-core**: Concurrency 24-48
- **16-core**: Concurrency 48-96

### Database Connection Pooling

```json
{
  "ConnectionStrings": {
    "TripleDerby": "Server=...;Max Pool Size=100;..."
  }
}
```

## Monitoring

### Key Metrics

- **Breeding operations/sec**: Throughput
- **Avg breeding time**: Should be <100ms with ColorCache
- **Foal creation success rate**: Should be >99%
- **Queue depth**: Backlog indicator

### Logs

```bash
# Service logs breeding events
[10:00:01 INF] Breeding completed: Mare Lightning (ID: ...) + Stallion Thunder (ID: ...) -> Foal Midnight Storm (ID: ...)
```

## Troubleshooting

### ColorCache not loading

**Error**: `ColorCache is empty or not initialized`

**Solution**: Ensure horse colors exist in database:

```sql
SELECT COUNT(*) FROM Colors;
-- Should return > 0
```

If no colors, run database seed data.

### Breeding requests timing out

**Symptoms**: Messages retried multiple times, slow processing

**Common Causes**:
1. ColorCache not initialized (triggers database queries)
2. Database connection pool exhausted
3. Name generator taking too long

**Solution**:
```bash
# Check service logs for ColorCache initialization
grep "ColorCache" logs/breeding-service.log

# Verify database performance
SELECT TOP 100 * FROM Horses ORDER BY Id DESC
```

### Invalid parent combinations

**Error**: `Mare and Stallion cannot be the same horse`

**Solution**: Validation happens in BreedingExecutor. Check message format:
- Mare and Stallion must be different horses
- Both must exist in database
- Both must be of breeding age (if validation added)

### Foals have unexpected colors

**Expected Behavior**: Color inheritance uses rarity weighting.

Rare colors from common parents are possible but unlikely. Verify:
```csharp
// Color selection is probabilistic
var selectedColor = colorSelector.SelectColorByRarity(candidateColors);
```

To debug, enable verbose logging:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "TripleDerby.Services.Breeding.BreedingExecutor": "Verbose"
      }
    }
  }
}
```

## Related Documentation

- [API README](../TripleDerby.Api/README.md)
- [Racing Service README](../TripleDerby.Services.Racing/README.md)
- [Training Service README](../TripleDerby.Services.Training/README.md)
- [Feeding Service README](../TripleDerby.Services.Feeding/README.md)
- [Breeding Genetics Design](../docs/features/breeding-genetics.md)
- [Unified Messaging Architecture](../docs/features/023-unified-microservice-messaging-architecture.md)

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run breeding-specific tests
dotnet test --filter Category=Breeding

# Run genetics tests
dotnet test --filter FullyQualifiedName~GeneticsTests
```

### Adding New Genetic Traits

1. Add trait to `Horse` entity
2. Implement inheritance logic in `BreedingExecutor`
3. Add tests for trait inheritance
4. Update breeding documentation

### Testing Color Distribution

```csharp
// Run breeding 1000 times and analyze color distribution
var results = Enumerable.Range(0, 1000)
    .Select(_ => executor.ExecuteBreedingAsync(mare, stallion))
    .ToList();

var colorDistribution = results
    .GroupBy(f => f.ColorId)
    .Select(g => new { ColorId = g.Key, Count = g.Count() });
```
