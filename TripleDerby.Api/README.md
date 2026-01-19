# TripleDerby API

REST API for the TripleDerby horse racing simulation game. Handles all gameplay operations including horse management, breeding, training, feeding, and race execution.

## Overview

The TripleDerby API is the primary entry point for all game operations. It publishes messages to microservices for asynchronous processing (breeding, racing, training, feeding) and provides synchronous endpoints for game data queries.

**Key Responsibilities:**
- REST endpoints for all gameplay operations
- Message publishing to background microservices
- Database queries for game state
- Response aggregation and formatting

## Architecture

```mermaid
graph TB
    subgraph "Client Applications"
        Web[Blazor Web UI]
        External[External Clients]
    end

    subgraph "API Layer"
        Controllers[API Controllers<br/>HorsesController, RacesController, etc.]
        Services[Business Services<br/>RaceService, BreedingService, etc.]
    end

    subgraph "Messaging Infrastructure"
        Publisher[IMessagePublisher<br/>RoutingMessagePublisher]

        subgraph "Configured Provider"
            RabbitPub[RabbitMqMessagePublisher]
            ServiceBusPub[AzureServiceBusPublisher]
        end
    end

    subgraph "Message Brokers"
        RabbitMQ[(RabbitMQ)]
        AzureSB[(Azure Service Bus)]
    end

    subgraph "Background Microservices"
        Racing[Racing Service]
        Breeding[Breeding Service]
        Training[Training Service]
        Feeding[Feeding Service]
    end

    subgraph "Data Layer"
        DB[(SQL Server Database)]
    end

    Web --> Controllers
    External --> Controllers
    Controllers --> Services
    Services --> Publisher
    Services --> DB

    Publisher -->|Provider: RabbitMq| RabbitPub
    Publisher -->|Provider: ServiceBus| ServiceBusPub

    RabbitPub --> RabbitMQ
    ServiceBusPub --> AzureSB

    RabbitMQ --> Racing
    RabbitMQ --> Breeding
    RabbitMQ --> Training
    RabbitMQ --> Feeding

    AzureSB -.-> Racing
    AzureSB -.-> Breeding
    AzureSB -.-> Training
    AzureSB -.-> Feeding
```

## Message Publishing Flow

When the API needs to process operations asynchronously (breeding, racing, training, feeding), it publishes messages to the configured message broker:

```mermaid
sequenceDiagram
    participant Client
    participant Controller
    participant Service
    participant Publisher as IMessagePublisher
    participant Broker as Message Broker
    participant Microservice

    Client->>Controller: POST /api/races/{id}/queue
    Controller->>Service: QueueRaceAsync(raceId)
    Service->>Publisher: PublishAsync(RaceRequested)
    Publisher->>Broker: Publish to queue
    Broker-->>Publisher: Acknowledged
    Publisher-->>Service: Success
    Service-->>Controller: Race queued
    Controller-->>Client: 202 Accepted

    Note over Broker,Microservice: Asynchronous processing
    Microservice->>Broker: Consume RaceRequested
    Microservice->>Microservice: Execute race simulation
    Microservice->>Broker: Publish RaceCompleted
```

## Configuration

### Database Connection

```json
{
  "ConnectionStrings": {
    "TripleDerby": "Server=localhost;Database=TripleDerby;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

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
        },
        "FeedingRequested": {
          "Destination": "triplederby.events",
          "RoutingKey": "FeedingRequested"
        }
      }
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
        "RaceRequested": {
          "Destination": "triplederby-events-topic"
        },
        "BreedingRequested": {
          "Destination": "triplederby-events-topic"
        },
        "TrainingRequested": {
          "Destination": "triplederby-events-topic"
        },
        "FeedingRequested": {
          "Destination": "triplederby-events-topic"
        }
      }
    }
  }
}
```

### Distributed Cache Configuration

```json
{
  "Redis": {
    "Configuration": "localhost:6379",
    "InstanceName": "TripleDerby:"
  }
}
```

## Messages Published

The API publishes the following messages to microservices:

| Message | Destination | Description |
|---------|-------------|-------------|
| `RaceRequested` | Racing Service | Triggers race simulation execution |
| `BreedingRequested` | Breeding Service | Requests horse breeding operation |
| `TrainingRequested` | Training Service | Requests horse training session |
| `FeedingRequested` | Feeding Service | Requests horse feeding operation |

## API Endpoints

### Horses

- `GET /api/horses` - List all horses
- `GET /api/horses/{id}` - Get horse details
- `POST /api/horses` - Create new horse
- `PUT /api/horses/{id}` - Update horse
- `DELETE /api/horses/{id}` - Delete horse
- `POST /api/horses/{id}/train` - Queue training (publishes `TrainingRequested`)
- `POST /api/horses/{id}/feed` - Queue feeding (publishes `FeedingRequested`)

### Races

- `GET /api/races` - List all races
- `GET /api/races/{id}` - Get race details
- `POST /api/races` - Create new race
- `POST /api/races/{id}/queue` - Queue race for execution (publishes `RaceRequested`)
- `GET /api/races/{id}/results` - Get race results

### Breeding

- `GET /api/breeding/featured-parents` - Get featured breeding parents (cached)
- `POST /api/breeding/request` - Request breeding operation (publishes `BreedingRequested`)

### Players

- `GET /api/players` - List all players
- `GET /api/players/{id}` - Get player details
- `POST /api/players` - Create new player

For complete API documentation, run the application and navigate to `/swagger`.

## Running the API

### Prerequisites

1. **.NET 10 SDK** installed
2. **SQL Server** running (LocalDB, Express, or full)
3. **RabbitMQ** running (Docker recommended) OR **Azure Service Bus** configured
4. **Redis** (optional, for distributed caching)

### Running with RabbitMQ (Docker)

```bash
# Start RabbitMQ with management UI
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management

# Run the API
cd TripleDerby.Api
dotnet run
```

Navigate to:
- API: `https://localhost:7001` (or configured port)
- Swagger: `https://localhost:7001/swagger`
- RabbitMQ Management: `http://localhost:15672` (guest/guest)

### Running with .NET Aspire

```bash
# From solution root
cd TripleDerby.AppHost
dotnet run
```

Aspire will automatically orchestrate:
- SQL Server
- RabbitMQ
- Redis
- API
- All microservices

## Switching Message Brokers

To switch from RabbitMQ to Azure Service Bus, simply update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "servicebus": "Endpoint=sb://..."
  },
  "MessageBus": {
    "Routing": {
      "Provider": "ServiceBus"  // Change from "RabbitMq" to "ServiceBus"
    }
  }
}
```

**No code changes required!** The API automatically uses the configured provider.

## Environment Variables

Common environment variable overrides:

```bash
# Database
ConnectionStrings__TripleDerby="Server=prod-server;Database=TripleDerby;..."

# Message broker
ConnectionStrings__messaging="amqp://user:pass@rabbitmq-host:5672/"
MessageBus__Routing__Provider="RabbitMq"

# Cache
Redis__Configuration="redis-host:6379"
```

## Health Checks

The API includes health checks for:
- Database connectivity
- Message broker availability (future)

Access health checks at:
- `/health` - Overall health status
- `/health/ready` - Readiness probe (for Kubernetes)
- `/health/live` - Liveness probe (for Kubernetes)

## Troubleshooting

### API starts but can't connect to database

**Error**: `SqlException: A network-related or instance-specific error occurred`

**Solution**: Verify SQL Server is running and connection string is correct.

```bash
# Test connection
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

### Messages not being published

**Symptoms**: API accepts requests but microservices don't process them

**Common Causes**:
1. RabbitMQ not running
2. Wrong connection string
3. Message broker provider mismatch

**Solution**: Check RabbitMQ is running and verify configuration:

```bash
# Check RabbitMQ is accessible
curl http://localhost:15672/api/overview

# Check API logs for publishing errors
dotnet run --environment Development
```

### "Invalid MessageBus:Routing:Provider" error

**Error**: `Invalid MessageBus:Routing:Provider value: 'Redis'`

**Solution**: Provider must be `"RabbitMq"`, `"ServiceBus"`, or `"Auto"`.

### Auto-detection not working

**Error**: `No message broker connection string found`

**Solution**: Set explicit provider OR add connection string:

```json
{
  "ConnectionStrings": {
    "messaging": "amqp://localhost"  // For RabbitMQ
    // OR
    "servicebus": "Endpoint=sb://..."  // For Service Bus
  }
}
```

### Slow response times

**Common Causes**:
1. Database query performance
2. No caching configured
3. Synchronous operations that should be async

**Solutions**:
- Enable Redis for distributed caching
- Check database indexes
- Use message publishing for long-running operations

## Related Documentation

- [Racing Service README](../TripleDerby.Services.Racing/README.md)
- [Breeding Service README](../TripleDerby.Services.Breeding/README.md)
- [Training Service README](../TripleDerby.Services.Training/README.md)
- [Feeding Service README](../TripleDerby.Services.Feeding/README.md)
- [Message Bus Abstraction Design](../docs/features/021-message-bus-abstraction.md)
- [Unified Messaging Architecture](../docs/features/023-unified-microservice-messaging-architecture.md)

## Development

### Adding New Endpoints

1. Create controller in `Controllers/`
2. Add business logic to `Core/Services/`
3. If async processing needed, publish message via `IMessagePublisher`
4. Add corresponding microservice consumer

### Adding New Message Types

1. Define message in `TripleDerby.SharedKernel/Messages/`
2. Add routing in `appsettings.json` under `MessageBus:Routing:Routes`
3. Create corresponding microservice processor
4. Publish from API service layer

### Testing

```bash
# Run unit tests
dotnet test

# Run specific test category
dotnet test --filter Category=Api

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```
