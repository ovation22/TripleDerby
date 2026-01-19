# Training Service

Background microservice responsible for horse training operations in the TripleDerby game. Consumes `TrainingRequested` messages and publishes `TrainingCompleted` events.

## Overview

The Training Service improves horse stats through training sessions:
- Stat progression based on training type
- Random variation and improvement rates
- Training fatigue and diminishing returns (future)
- Validation and error handling

**Processing Model**: Message-driven consumer with configurable concurrency

## Architecture

```mermaid
graph TB
    subgraph "Message Broker"
        Queue[training-requests Queue]
    end

    subgraph "Training Service"
        Worker[Worker Host]
        Consumer[GenericMessageConsumer]
        Adapter[IMessageBrokerAdapter<br/>RabbitMq or ServiceBus]
        Processor[TrainingRequestProcessor]
        Executor[TrainingExecutor]
        Calculator[TrainingCalculator]
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
    Adapter -->|TrainingRequested| Processor
    Processor --> Executor
    Executor -->|Read horse| DB
    Executor -->|Calculate gains| Calculator
    Executor -->|Update stats| DB
    Executor -->|TrainingCompleted| Publisher
    Publisher -->|Publish| EventQueue
```

## Message Flow

```mermaid
sequenceDiagram
    participant Broker as Message Broker
    participant Consumer as GenericMessageConsumer
    participant Processor as TrainingRequestProcessor
    participant Executor as TrainingExecutor
    participant Calculator as TrainingCalculator
    participant DB as Database
    participant Publisher as IMessagePublisher

    Broker->>Consumer: TrainingRequested message
    Consumer->>Processor: ProcessAsync(TrainingRequested)
    Processor->>DB: Load horse
    Processor->>Executor: ExecuteTrainingAsync(horse, type)
    Executor->>Calculator: CalculateStatGain(type)
    Calculator-->>Executor: Stat improvements
    Executor->>DB: Update horse stats
    Executor->>Publisher: PublishAsync(TrainingCompleted)
    Publisher->>Broker: Publish event
    Executor-->>Processor: Updated stats
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
        "TrainingCompleted": {
          "Destination": "triplederby.events",
          "RoutingKey": "TrainingCompleted"
        }
      }
    },
    "Consumer": {
      "Queue": "triplederby.training.requests",
      "Concurrency": 16,
      "MaxRetries": 3,
      "PrefetchCount": 32
    },
    "RabbitMq": {
      "Exchange": "triplederby.events",
      "RoutingKey": "TrainingRequested",
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
        "TrainingCompleted": {
          "Destination": "triplederby-events-topic"
        }
      }
    },
    "Consumer": {
      "Queue": "training-requests",
      "Concurrency": 16,
      "MaxRetries": 3,
      "PrefetchCount": 32
    }
  }
}
```

## Messages

### Consumes

**TrainingRequested**
```csharp
{
  "HorseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "TrainingType": "Speed",  // Speed, Stamina, Acceleration
  "Intensity": "Normal",    // Light, Normal, Intense
  "CorrelationId": "train-123",
  "RequestedAt": "2026-01-18T10:00:00Z"
}
```

### Publishes

**TrainingCompleted**
```csharp
{
  "HorseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "TrainingType": "Speed",
  "StatGains": {
    "Speed": 2,
    "Stamina": 0,
    "Acceleration": 1
  },
  "CompletedAt": "2026-01-18T10:00:00Z"
}
```

## Training Mechanics

### Stat Improvements

Each training type focuses on specific stats:

**Speed Training**:
- Speed: +1 to +3 points
- Acceleration: +0 to +1 points
- Stamina: No change

**Stamina Training**:
- Stamina: +1 to +3 points
- Speed: +0 to +1 points (secondary)
- Acceleration: No change

**Acceleration Training**:
- Acceleration: +1 to +3 points
- Speed: +0 to +1 points (secondary)
- Stamina: No change

**Balanced Training**:
- All stats: +1 point each

### Intensity Modifiers

Training intensity affects gains:
- **Light**: 50% of base gains (less fatigue, future)
- **Normal**: 100% of base gains
- **Intense**: 150% of base gains (more fatigue, future)

### Stat Caps

Stats cannot exceed maximum values:
- Speed: Max 100
- Stamina: Max 100
- Acceleration: Max 100
- Adaptability: Max 100

If training would exceed cap, gain is reduced to reach cap exactly.

## Running the Service

### Prerequisites

1. **.NET 10 SDK** installed
2. **SQL Server** with TripleDerby database
3. **RabbitMQ** OR **Azure Service Bus** configured

### Standalone

```bash
cd TripleDerby.Services.Training
dotnet run
```

### With Docker

```bash
docker build -t triplederby-training -f TripleDerby.Services.Training/Dockerfile .

docker run -d \
  -e ConnectionStrings__TripleDerby="Server=host.docker.internal;..." \
  -e ConnectionStrings__messaging="amqp://host.docker.internal:5672" \
  triplederby-training
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

Training operations are lightweight:
- Simple stat calculations
- Single database update per message
- Can handle high concurrency

**Recommended Concurrency**:
- **4-core**: 8-16
- **8-core**: 16-24
- **16-core**: 24-32

## Troubleshooting

### Training not improving stats

**Symptoms**: Messages processed, but horse stats unchanged

**Common Causes**:
1. Stats already at maximum (100)
2. Training calculator returning zero gains
3. Database update not committed

**Solution**:
```sql
-- Check horse stats
SELECT Speed, Stamina, Acceleration, Adaptability
FROM Horses
WHERE Id = '...';

-- Check stat caps
SELECT * FROM Horses WHERE Speed >= 100 OR Stamina >= 100;
```

### Training gains inconsistent

**Expected Behavior**: Training uses random variation (±1 point from base).

To debug, enable verbose logging:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "TripleDerby.Services.Training": "Debug"
      }
    }
  }
}
```

### Horse not found errors

**Error**: `Horse with ID ... not found`

**Solution**: Ensure horse exists in database before publishing TrainingRequested:
```sql
SELECT * FROM Horses WHERE Id = '...';
```

## Related Documentation

- [API README](../TripleDerby.Api/README.md)
- [Racing Service README](../TripleDerby.Services.Racing/README.md)
- [Breeding Service README](../TripleDerby.Services.Breeding/README.md)
- [Feeding Service README](../TripleDerby.Services.Feeding/README.md)
- [Unified Messaging Architecture](../docs/features/023-unified-microservice-messaging-architecture.md)

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run training-specific tests
dotnet test --filter Category=Training
```

### Adding New Training Types

1. Add training type enum value
2. Implement stat calculation in `TrainingCalculator`
3. Add tests for new training type
4. Update API to accept new type
