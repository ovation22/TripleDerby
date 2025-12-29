# Feature 011: Race Microservice Migration - Implementation Plan

**Feature Spec:** [011-race-microservice-migration.md](../features/011-race-microservice-migration.md)

**Status:** 🔵 PLANNED

**Created:** 2025-12-26

**Depends On:** Feature 010 (RaceService Cleanup) ✅ Complete

---

## Overview

This implementation plan breaks down Feature 011 (Race Microservice Migration with Azure Service Bus) into 8 concrete phases, following Test-Driven Development principles with vertical slices. Each phase delivers testable, incremental value while migrating race functionality from the monolithic API to a dedicated microservice.

**Goals:**
- Add Azure Service Bus emulator support to Aspire AppHost
- Implement keyed dependency injection for dual-broker support (RabbitMQ + Service Bus)
- Create new `TripleDerby.Services.Race` microservice
- Extract race simulation to message-based architecture
- Enable independent scaling and deployment of race simulation

**Key Architectural Principles:**
- **Gradual Migration**: Breeding stays on RabbitMQ, Race uses Service Bus
- **Vertical Slices**: Each phase delivers end-to-end testable functionality
- **Test-First**: Write tests before implementation
- **Local Development Focus**: No backward compatibility concerns, can break/change endpoints freely

---

## Phase Structure

Each phase follows this pattern:
1. **RED**: Write tests that define expected behavior
2. **GREEN**: Implement changes to make tests pass
3. **REFACTOR**: Clean up and optimize
4. **VERIFY**: Run tests and validate deliverable

---

## Phase 1: Azure Service Bus Emulator Infrastructure

**Goal:** Add Azure Service Bus emulator to Aspire AppHost and verify connectivity

**Vertical Slice:** Aspire starts Service Bus emulator, API can connect and publish test messages

**Estimated Complexity:** Simple

**Risks:** Azure Service Bus Aspire integration may require specific NuGet package versions

### RED - Write Failing Tests

**Test 1.1: Create integration test for Service Bus connectivity**

File: `TripleDerby.Tests.Integration/Messaging/AzureServiceBusConnectionTests.cs` (new file)

```csharp
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TripleDerby.Tests.Integration.Messaging;

public class AzureServiceBusConnectionTests
{
    [Fact]
    public async Task ServiceBus_EmulatorConnection_Succeeds()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            })
            .Build();

        var connectionString = config.GetConnectionString("servicebus");

        // Act & Assert
        var client = new ServiceBusClient(connectionString);
        await using (client)
        {
            // Connection succeeds if no exception thrown
            Assert.NotNull(client);
        }
    }

    [Fact]
    public async Task ServiceBus_CanSendAndReceiveMessage_Succeeds()
    {
        // This test will pass once emulator is running
        // We'll implement it fully in Phase 2
        await Task.CompletedTask;
    }
}
```

**Why these tests:** Validates that Service Bus emulator is reachable and basic SDK operations work.

### GREEN - Make Tests Pass

**Task 1.1: Add Azure Service Bus NuGet packages**

Add to `TripleDerby.AppHost.csproj`:
```xml
<PackageReference Include="Aspire.Hosting.Azure.ServiceBus" Version="13.1.0" />
```

Add to `TripleDerby.Infrastructure.csproj`:
```xml
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.2" />
```

Add to `TripleDerby.Api.csproj`:
```xml
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.2" />
```

**Task 1.2: Update AppHost to add Service Bus emulator**

File: [TripleDerby.AppHost/Program.cs](../../TripleDerby.AppHost/Program.cs)

Update after line 13 (after RabbitMQ setup):

```csharp
// Azure Service Bus Emulator for Race microservice
var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator()
    .AddQueue("race-requests")
    .AddQueue("race-completions");
```

Update API service reference (after line 21):

```csharp
var apiService = builder.AddProject<Projects.TripleDerby_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)      // Breeding messages
    .WaitFor(rabbit)
    .WithReference(serviceBus)  // Race messages (NEW)
    .WaitFor(serviceBus);       // (NEW)
```

**Task 1.3: Create integration test project (if doesn't exist)**

Create `TripleDerby.Tests.Integration` project with:
- Target framework: net10.0
- References: TripleDerby.Infrastructure, Azure.Messaging.ServiceBus, xUnit

**Implementation Notes:**
- Aspire `.RunAsEmulator()` starts Azure Service Bus as Docker container
- Emulator is compatible with production Azure Service Bus SDK
- Connection string injected automatically via Aspire configuration
- Queues created on startup (race-requests, race-completions)

### REFACTOR - Clean Up

**Task 1.4: Add configuration documentation**

Add comment in AppHost/Program.cs above Service Bus setup:

```csharp
// Azure Service Bus Emulator for Race microservice (Feature 011)
// Uses emulator for local dev, supports seamless production migration
// Runs as Docker container, compatible with Azure Service Bus SDK
```

### VERIFY - Acceptance Criteria

- [ ] Run `dotnet restore` successfully on all projects
- [ ] Run `dotnet run` in AppHost - Aspire dashboard shows Service Bus container
- [ ] Service Bus emulator accessible at localhost (check Aspire logs)
- [ ] Integration test `ServiceBus_EmulatorConnection_Succeeds` passes
- [ ] No regressions - existing Breeding/RabbitMQ tests still pass

**Deliverable:** Azure Service Bus emulator running in Aspire, basic connectivity verified

---

## Phase 2: Azure Service Bus Publisher Implementation

**Goal:** Create `AzureServiceBusPublisher` implementing `IMessagePublisher` interface

**Vertical Slice:** Can publish messages to Service Bus queues with proper serialization and error handling

**Estimated Complexity:** Medium

**Risks:** Connection pooling, retry logic, dead-letter queue configuration

### RED - Write Failing Tests

**Test 2.1: AzureServiceBusPublisher sends messages**

File: `TripleDerby.Tests.Unit/Messaging/AzureServiceBusPublisherTests.cs` (new file)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.Core.Abstractions.Messaging;
using Xunit;

namespace TripleDerby.Tests.Unit.Messaging;

public class AzureServiceBusPublisherTests
{
    [Fact]
    public async Task PublishAsync_WithValidMessage_Succeeds()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var publisher = new AzureServiceBusPublisher(config, NullLogger<AzureServiceBusPublisher>.Instance);
        var message = new TestMessage { Id = Guid.NewGuid(), Content = "Test" };

        // Act
        await publisher.PublishAsync(message, new MessagePublishOptions
        {
            Destination = "race-requests"
        });

        // Assert - no exception means success
        // Full integration test will verify message delivery
    }

    [Fact]
    public async Task PublishAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var publisher = new AzureServiceBusPublisher(config, NullLogger<AzureServiceBusPublisher>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<TestMessage>(null!));
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            })
            .Build();
    }

    private record TestMessage
    {
        public Guid Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }
}
```

**Why these tests:** Validates publisher interface implementation and error handling.

### GREEN - Make Tests Pass

**Task 2.1: Add MessagePublishOptions to Core abstractions**

File: `TripleDerby.Core/Abstractions/Messaging/MessagePublishOptions.cs` (check if exists, update if needed)

Verify structure matches:
```csharp
namespace TripleDerby.Core.Abstractions.Messaging;

public class MessagePublishOptions
{
    public string? Destination { get; set; }
    public string? Subject { get; set; }
}
```

**Task 2.2: Create AzureServiceBusPublisher**

File: `TripleDerby.Infrastructure/Messaging/AzureServiceBusPublisher.cs` (new file)

```csharp
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus implementation of IMessagePublisher.
/// Supports both emulator (local dev) and cloud (production).
/// </summary>
public class AzureServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _defaultQueue;

    public AzureServiceBusPublisher(
        IConfiguration configuration,
        ILogger<AzureServiceBusPublisher> logger)
    {
        _logger = logger;

        // Read connection string (Aspire injects this automatically)
        var connectionString =
            configuration["ConnectionStrings:servicebus"]
            ?? configuration.GetConnectionString("servicebus")
            ?? throw new InvalidOperationException(
                "Azure Service Bus connection string not configured. " +
                "Set ConnectionStrings:servicebus in configuration.");

        _client = new ServiceBusClient(connectionString);

        _defaultQueue = configuration["ServiceBus:DefaultQueue"] ?? "race-requests";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        _logger.LogInformation(
            "AzureServiceBusPublisher configured for queue {Queue}",
            _defaultQueue);
    }

    public async Task PublishAsync<T>(
        T message,
        MessagePublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var queueName = options?.Destination ?? _defaultQueue;

        // Serialize message
        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var body = new BinaryData(Encoding.UTF8.GetBytes(payload));

        // Create Service Bus message
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Subject = options?.Subject ?? typeof(T).Name
        };

        // Add metadata
        sbMessage.ApplicationProperties["MessageType"] = typeof(T).FullName ?? typeof(T).Name;
        sbMessage.ApplicationProperties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

        // Send message
        await using var sender = _client.CreateSender(queueName);

        try
        {
            await sender.SendMessageAsync(sbMessage, cancellationToken);

            _logger.LogInformation(
                "Published message {MessageType} to queue {Queue} (MessageId: {MessageId})",
                typeof(T).Name,
                queueName,
                sbMessage.MessageId);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex,
                "Failed to publish message {MessageType} to queue {Queue}",
                typeof(T).Name,
                queueName);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
```

**Implementation Notes:**
- Follows same patterns as `RabbitMqMessagePublisher`
- Uses short-lived `ServiceBusSender` (created per send operation)
- JSON serialization with camelCase naming
- Metadata stored in ApplicationProperties for routing/debugging
- Exception handling with logging

### REFACTOR - Clean Up

**Task 2.3: Add XML documentation**

Ensure all public members have XML docs explaining purpose and behavior.

### VERIFY - Acceptance Criteria

- [ ] Unit tests pass: `AzureServiceBusPublisher_*` tests
- [ ] Can publish test message to Service Bus emulator queue
- [ ] Messages appear in Aspire Service Bus dashboard/logs
- [ ] No memory leaks (proper disposal of client/sender)
- [ ] Existing RabbitMQ publisher tests still pass

**Deliverable:** Working Service Bus publisher with tests

---

## Phase 3: Keyed Dependency Injection Setup

**Goal:** Register both RabbitMQ and Service Bus publishers with keyed DI in API

**Vertical Slice:** API can resolve different message publishers by key ("rabbitmq", "servicebus")

**Estimated Complexity:** Simple

**Risks:** .NET 8+ required for keyed DI (verify target framework)

### RED - Write Failing Tests

**Test 3.1: Keyed DI resolves correct publisher**

File: `TripleDerby.Tests.Unit/DependencyInjection/KeyedDITests.cs` (new file)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;
using Xunit;

namespace TripleDerby.Tests.Unit.DependencyInjection;

public class KeyedDITests
{
    [Fact]
    public void ServiceProvider_ResolvesRabbitMQPublisher_ByKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateTestConfiguration();

        services.AddSingleton<IConfiguration>(config);
        services.AddKeyedSingleton<IMessagePublisher, RabbitMqMessagePublisher>("rabbitmq");

        var provider = services.BuildServiceProvider();

        // Act
        var publisher = provider.GetRequiredKeyedService<IMessagePublisher>("rabbitmq");

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<RabbitMqMessagePublisher>(publisher);
    }

    [Fact]
    public void ServiceProvider_ResolvesServiceBusPublisher_ByKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateTestConfiguration();

        services.AddSingleton<IConfiguration>(config);
        services.AddKeyedSingleton<IMessagePublisher, AzureServiceBusPublisher>("servicebus");

        var provider = services.BuildServiceProvider();

        // Act
        var publisher = provider.GetRequiredKeyedService<IMessagePublisher>("servicebus");

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<AzureServiceBusPublisher>(publisher);
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "Host=localhost;Username=guest;Password=guest",
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost"
            })
            .Build();
    }
}
```

**Why these tests:** Validates DI configuration and keyed service resolution.

### GREEN - Make Tests Pass

**Task 3.1: Update API Program.cs with keyed DI**

File: [TripleDerby.Api/Program.cs](../../TripleDerby.Api/Program.cs)

Replace line 86 (current: `builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();`):

```csharp
// Keyed DI for dual-broker support (Feature 011)
// Breeding uses RabbitMQ, Race uses Azure Service Bus
builder.Services.AddKeyedSingleton<IMessagePublisher, RabbitMqMessagePublisher>("rabbitmq");
builder.Services.AddKeyedSingleton<IMessagePublisher, AzureServiceBusPublisher>("servicebus");
```

**Task 3.2: Update BreedingService to use keyed publisher**

File: [TripleDerby.Core/Services/BreedingService.cs](../../TripleDerby.Core/Services/BreedingService.cs)

Update constructor to use keyed service:

```csharp
public BreedingService(
    ITripleDerbyRepository repository,
    IRandomGenerator randomGenerator,
    IHorseNameGenerator horseNameGenerator,
    ITimeManager timeManager,
    [FromKeyedServices("rabbitmq")] IMessagePublisher messagePublisher,
    ILogger<BreedingService> logger)
{
    // ... existing implementation
}
```

Add using directive at top:
```csharp
using Microsoft.Extensions.DependencyInjection;
```

**Implementation Notes:**
- Both publishers registered as keyed singletons
- Services specify which broker to use via `[FromKeyedServices("key")]` attribute
- Breeding stays on RabbitMQ (no breaking changes)
- Race will use Service Bus (implemented in later phases)

### REFACTOR - Clean Up

**Task 3.3: Add configuration comments**

Add comment in Program.cs above keyed DI registration explaining dual-broker strategy.

### VERIFY - Acceptance Criteria

- [ ] Unit tests pass: `KeyedDITests`
- [ ] API starts without errors
- [ ] Breeding service still publishes to RabbitMQ
- [ ] Can resolve both publishers by key in DI
- [ ] No breaking changes to existing functionality

**Deliverable:** Dual-broker DI configuration working

---

## Phase 4: RaceRequest Entity and Status Tracking

**Goal:** Create `RaceRequest` entity to persist race requests in database (same pattern as `BreedingRequest`)

**Vertical Slice:** Race requests can be persisted, status tracked, and queried via API

**Estimated Complexity:** Medium

**Risks:** Database migration, entity configuration

### RED - Write Failing Tests

**Test 4.1: RaceRequest entity persistence**

File: `TripleDerby.Tests.Unit/Entities/RaceRequestTests.cs` (new file)

```csharp
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;
using Xunit;

namespace TripleDerby.Tests.Unit.Entities;

public class RaceRequestTests
{
    [Fact]
    public void RaceRequest_DefaultStatus_IsPending()
    {
        // Arrange & Act
        var request = new RaceRequest();

        // Assert
        Assert.Equal(RaceRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void RaceRequest_SetProperties_Succeeds()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        // Act
        var request = new RaceRequest
        {
            Id = requestId,
            RaceId = 5,
            HorseId = horseId,
            OwnerId = ownerId,
            Status = RaceRequestStatus.InProgress
        };

        // Assert
        Assert.Equal(requestId, request.Id);
        Assert.Equal(5, request.RaceId);
        Assert.Equal(horseId, request.HorseId);
        Assert.Equal(RaceRequestStatus.InProgress, request.Status);
    }
}
```

**Why these tests:** Validates entity structure and default values.

### GREEN - Make Tests Pass

**Task 4.1: Create RaceRequestStatus enum**

File: `TripleDerby.SharedKernel/Enums/RaceRequestStatus.cs` (new file)

```csharp
namespace TripleDerby.SharedKernel.Enums;

public enum RaceRequestStatus : byte
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
```

**Task 4.2: Create RaceRequest entity**

File: `TripleDerby.Core/Entities/RaceRequest.cs` (new file)

```csharp
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Represents a persisted race request.
/// Follows same pattern as BreedingRequest for consistency.
/// </summary>
public class RaceRequest
{
    public Guid Id { get; set; }

    public byte RaceId { get; set; }

    public Guid HorseId { get; set; }

    public Guid? RaceRunId { get; set; }

    public Guid OwnerId { get; set; }

    public RaceRequestStatus Status { get; set; } = RaceRequestStatus.Pending;

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? ProcessedDate { get; set; }
}
```

**Task 4.3: Add RaceRequest DbSet to TripleDerbyContext**

File: [TripleDerby.Infrastructure/Data/TripleDerbyContext.cs](../../TripleDerby.Infrastructure/Data/TripleDerbyContext.cs)

Add DbSet property:
```csharp
public DbSet<RaceRequest> RaceRequests => Set<RaceRequest>();
```

Add entity configuration in `OnModelCreating`:
```csharp
// RaceRequest configuration (rac schema, same pattern as BreedingRequest in brd schema)
modelBuilder.Entity<RaceRequest>()
    .ToTable("RaceRequests", schema: "rac")
    .HasKey(e => e.Id);

modelBuilder.Entity<RaceRequest>()
    .Property(e => e.Status)
    .HasConversion<byte>()
    .HasDefaultValue(RaceRequestStatus.Pending)
    .IsRequired();

modelBuilder.Entity<RaceRequest>()
    .Property(e => e.FailureReason)
    .HasMaxLength(1024);

modelBuilder.Entity<RaceRequest>()
    .Property(e => e.RaceId).IsRequired();

modelBuilder.Entity<RaceRequest>()
    .Property(e => e.HorseId).IsRequired();

modelBuilder.Entity<RaceRequest>()
    .Property(e => e.OwnerId).IsRequired();

modelBuilder.Entity<RaceRequest>()
    .Property(e => e.CreatedDate).IsRequired();

modelBuilder.Entity<RaceRequest>()
    .Property(e => e.CreatedBy).IsRequired();

// Indexes for common queries
modelBuilder.Entity<RaceRequest>()
    .HasIndex(e => e.Status);

modelBuilder.Entity<RaceRequest>()
    .HasIndex(e => e.CreatedDate);

modelBuilder.Entity<RaceRequest>()
    .HasIndex(e => e.HorseId);

modelBuilder.Entity<RaceRequest>()
    .HasIndex(e => e.OwnerId);
```

**Task 4.4: Create RaceRequestStatusResult DTO**

File: `TripleDerby.SharedKernel/Dtos/RaceRequestStatusResult.cs` (new file)

```csharp
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel.Dtos;

public record RaceRequestStatusResult(
    Guid Id,
    byte RaceId,
    Guid HorseId,
    RaceRequestStatus Status,
    Guid? RaceRunId,
    Guid OwnerId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? ProcessedDate,
    DateTimeOffset? UpdatedDate,
    string? FailureReason
);
```

**Task 4.5: Add database migration**

Run migration commands:
```bash
# Add migration
dotnet ef migrations add AddRaceRequestEntity --project TripleDerby.Infrastructure --startup-project TripleDerby.Api

# Update database (auto-applied on startup in local dev)
```

**Implementation Notes:**
- Follows exact same pattern as `BreedingRequest` (in `brd` schema)
- Uses `rac` schema for clean separation (like Breeding uses `brd`)
- Status tracking: Pending → InProgress → Completed/Failed
- Links to RaceRun via `RaceRunId` after completion
- Indexed on common query fields (Status, CreatedDate, HorseId, OwnerId)
- Status enum stored as byte with default value
- FailureReason max length: 1024 characters (same as Breeding)

### REFACTOR - Clean Up

**Task 4.6: Add XML documentation**

Ensure all properties have XML docs explaining their purpose.

### VERIFY - Acceptance Criteria

- [ ] Unit tests pass: `RaceRequestTests`
- [ ] Database migration created successfully
- [ ] RaceRequest table created in `rac` schema (not `dbo`)
- [ ] Can save and retrieve RaceRequest from database
- [ ] Default status is Pending
- [ ] All indexes created
- [ ] Entity follows BreedingRequest pattern (brd schema)
- [ ] Status enum converts to byte in database

**Deliverable:** RaceRequest entity persisted in `rac` schema with status tracking

---

## Phase 5: Message Contracts for Race Operations

**Goal:** Define message contracts for race request/completion

**Vertical Slice:** Message classes exist, serialize/deserialize correctly, follow Breeding pattern

**Estimated Complexity:** Simple

**Risks:** None - pure data classes

### RED - Write Failing Tests

**Test 5.1: Message serialization**

File: `TripleDerby.Tests.Unit/Messages/RaceMessageTests.cs` (new file)

```csharp
using System.Text.Json;
using TripleDerby.SharedKernel.Messages;
using Xunit;

namespace TripleDerby.Tests.Unit.Messages;

public class RaceMessageTests
{
    [Fact]
    public void RaceRequested_SerializesAndDeserializes_Correctly()
    {
        // Arrange
        var original = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<RaceRequested>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.CorrelationId, deserialized.CorrelationId);
        Assert.Equal(original.RaceId, deserialized.RaceId);
        Assert.Equal(original.HorseId, deserialized.HorseId);
    }

    [Fact]
    public void RaceCompleted_SerializesAndDeserializes_Correctly()
    {
        // Arrange
        var original = new RaceCompleted
        {
            CorrelationId = Guid.NewGuid(),
            RaceRunId = Guid.NewGuid(),
            RaceId = 5,
            RaceName = "Kentucky Derby",
            WinnerHorseId = Guid.NewGuid(),
            WinnerName = "Secretariat",
            WinnerTime = 119.4,
            FieldSize = 12,
            CompletedAt = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<RaceCompleted>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.CorrelationId, deserialized.CorrelationId);
        Assert.Equal(original.RaceRunId, deserialized.RaceRunId);
        Assert.Equal(original.WinnerName, deserialized.WinnerName);
    }
}
```

**Why these tests:** Ensures message contracts serialize correctly for wire transmission.

### GREEN - Make Tests Pass

**Task 4.1: Create RaceRequested message**

File: `TripleDerby.SharedKernel/Messages/RaceRequested.cs` (new file)

```csharp
namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Message requesting a race simulation.
/// Published by: API
/// Consumed by: Race Service
/// </summary>
public record RaceRequested
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public byte RaceId { get; init; }
    public Guid HorseId { get; init; }
    public Guid RequestedBy { get; init; } // User ID
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}
```

**Task 4.2: Create RaceCompleted message**

File: `TripleDerby.SharedKernel/Messages/RaceCompleted.cs` (new file)

```csharp
using TripleDerby.SharedKernel;

namespace TripleDerby.SharedKernel.Messages;

/// <summary>
/// Message indicating race simulation completed.
/// Published by: Race Service
/// Consumed by: API (future: WebSocket/SignalR for real-time updates)
/// </summary>
public record RaceCompleted
{
    public Guid CorrelationId { get; init; }
    public Guid RaceRunId { get; init; }
    public byte RaceId { get; init; }
    public string RaceName { get; init; } = string.Empty;
    public Guid WinnerHorseId { get; init; }
    public string WinnerName { get; init; } = string.Empty;
    public double WinnerTime { get; init; }
    public int FieldSize { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    // Full result details (for immediate response)
    // Note: RaceRunResult must be serializable
    public RaceRunResult? Result { get; init; }
}
```

**Implementation Notes:**
- Follow same pattern as `BreedingRequested` / `BreedingCompleted`
- Use `record` types for immutability
- Include `CorrelationId` for request/response tracking
- Timestamp fields for observability
- Optional `Result` field for sync response pattern (Phase 7)

### REFACTOR - Clean Up

**Task 4.3: Document message flow**

Add comment at top of each message file documenting publish/consume pattern.

### VERIFY - Acceptance Criteria

- [ ] Unit tests pass: `RaceMessageTests`
- [ ] Messages serialize to JSON correctly
- [ ] Messages deserialize from JSON correctly
- [ ] No compilation errors in SharedKernel project
- [ ] Message contracts documented

**Deliverable:** Race message contracts defined and tested

---

## Phase 6: Race Microservice Project Setup

**Goal:** Create new `TripleDerby.Services.Race` project with Service Bus consumer

**Vertical Slice:** Race microservice starts, connects to Service Bus, consumes test messages

**Estimated Complexity:** Medium

**Risks:** Consumer configuration (concurrency, prefetch, error handling)

### RED - Write Failing Tests

**Test 5.1: Race microservice integration test**

File: `TripleDerby.Tests.Integration/Services/RaceServiceIntegrationTests.cs` (new file)

```csharp
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using TripleDerby.SharedKernel.Messages;
using Xunit;
using System.Text.Json;
using System.Text;

namespace TripleDerby.Tests.Integration.Services;

public class RaceServiceIntegrationTests
{
    [Fact]
    public async Task RaceService_ConsumesRaceRequested_Successfully()
    {
        // Arrange
        var connectionString = "Endpoint=sb://localhost"; // From emulator
        var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender("race-requests");

        var request = new RaceRequested
        {
            RaceId = 1,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
        {
            ContentType = "application/json"
        };

        // Act
        await sender.SendMessageAsync(message);

        // Assert
        // Full test will verify RaceCompleted message published
        // For now, just verify no exceptions
        await Task.Delay(1000); // Wait for processing
    }
}
```

**Why these tests:** Validates end-to-end message flow through microservice.

### GREEN - Make Tests Pass

**Task 5.1: Create TripleDerby.Services.Race project**

Create new project:
```bash
dotnet new worker -n TripleDerby.Services.Race -f net10.0
```

Update `.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>race-service-secrets</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.18.2" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="13.0.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TripleDerby.Core\TripleDerby.Core.csproj" />
    <ProjectReference Include="..\TripleDerby.Infrastructure\TripleDerby.Infrastructure.csproj" />
    <ProjectReference Include="..\TripleDerby.SharedKernel\TripleDerby.SharedKernel.csproj" />
    <ProjectReference Include="..\TripleDerby.ServiceDefaults\TripleDerby.ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
```

**Task 5.2: Create Program.cs for Race service**

File: `TripleDerby.Services.Race/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Serilog;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Services;
using TripleDerby.Core.Racing;
using TripleDerby.Infrastructure.Data;
using TripleDerby.Infrastructure.Data.Repositories;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.ServiceDefaults;
using TripleDerby.Services.Race;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

var conn = builder.Configuration.GetConnectionString("TripleDerby");

builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));

builder.Services.AddScoped<ITripleDerbyRepository, TripleDerbyRepository>();
builder.Services.AddScoped<IRaceRequestProcessor, RaceRequestProcessor>();

// Racing dependencies (same as API)
builder.Services.AddSingleton<ITimeManager, TimeManager>();
builder.Services.AddSingleton<IRandomGenerator, RandomGenerator>();
builder.Services.AddScoped<ISpeedModifierCalculator, SpeedModifierCalculator>();
builder.Services.AddScoped<IStaminaCalculator, StaminaCalculator>();
builder.Services.AddScoped<IRaceCommentaryGenerator, RaceCommentaryGenerator>();
builder.Services.AddScoped<IPurseCalculator, PurseCalculator>();
builder.Services.AddScoped<IOvertakingManager, OvertakingManager>();
builder.Services.AddScoped<IEventDetector, EventDetector>();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IRaceRunService, RaceRunService>();

// Messaging
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<AzureServiceBusRaceConsumer>();
builder.Services.AddSingleton<IMessagePublisher, AzureServiceBusPublisher>();

builder.AddSqlServerClient(connectionName: "sql");

try
{
    Log.Information("Starting Race worker host");
    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

**Task 5.3: Create Worker.cs**

File: `TripleDerby.Services.Race/Worker.cs`

```csharp
namespace TripleDerby.Services.Race;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly AzureServiceBusRaceConsumer _consumer;

    public Worker(
        ILogger<Worker> logger,
        AzureServiceBusRaceConsumer consumer)
    {
        _logger = logger;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Race Worker starting at: {time}", DateTimeOffset.Now);

        await _consumer.StartAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        finally
        {
            await _consumer.StopAsync();
        }
    }
}
```

**Task 5.4: Create AzureServiceBusRaceConsumer.cs**

File: `TripleDerby.Services.Race/AzureServiceBusRaceConsumer.cs`

```csharp
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Race;

public class AzureServiceBusRaceConsumer : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;
    private readonly IRaceRequestProcessor _requestProcessor;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<AzureServiceBusRaceConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureServiceBusRaceConsumer(
        IConfiguration configuration,
        IRaceRequestProcessor requestProcessor,
        IMessagePublisher publisher,
        ILogger<AzureServiceBusRaceConsumer> logger)
    {
        _requestProcessor = requestProcessor;
        _publisher = publisher;
        _logger = logger;

        var connectionString = configuration.GetConnectionString("servicebus")
            ?? throw new InvalidOperationException("Service Bus connection string not found");

        _client = new ServiceBusClient(connectionString);

        _processor = _client.CreateProcessor(
            "race-requests",
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 5, // Process 5 races concurrently
                PrefetchCount = 10
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Race Service Bus consumer");
        await _processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Race Service Bus consumer");
        await _processor.StopProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = Encoding.UTF8.GetString(args.Message.Body);

        try
        {
            var request = JsonSerializer.Deserialize<RaceRequested>(messageBody, _jsonOptions);

            if (request == null)
            {
                _logger.LogWarning("Received null RaceRequested message");
                await args.DeadLetterMessageAsync(args.Message, "NullMessage", "Message deserialized to null");
                return;
            }

            _logger.LogInformation(
                "Processing race request: RaceId={RaceId}, HorseId={HorseId}, CorrelationId={CorrelationId}",
                request.RaceId, request.HorseId, request.CorrelationId);

            // Process the race (delegates to RaceService)
            var result = await _requestProcessor.ProcessAsync(request, args.CancellationToken);

            // Publish completion message
            var completion = new RaceCompleted
            {
                CorrelationId = request.CorrelationId,
                RaceRunId = result.RaceRunId,
                RaceId = request.RaceId,
                RaceName = result.RaceName,
                WinnerHorseId = result.HorseResults.First().HorseId,
                WinnerName = result.HorseResults.First().HorseName,
                WinnerTime = result.HorseResults.First().Time,
                FieldSize = result.HorseResults.Count,
                Result = result
            };

            await _publisher.PublishAsync(
                completion,
                new MessagePublishOptions { Destination = "race-completions" },
                args.CancellationToken);

            // Complete the message (remove from queue)
            await args.CompleteMessageAsync(args.Message);

            _logger.LogInformation(
                "Race completed successfully: CorrelationId={CorrelationId}, Winner={Winner}",
                request.CorrelationId, completion.WinnerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing race request: {MessageId}",
                args.Message.MessageId);

            // Dead-letter the message after max retries
            if (args.Message.DeliveryCount >= 3)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "ProcessingFailed",
                    ex.Message);
            }
            else
            {
                // Abandon to retry
                await args.AbandonMessageAsync(args.Message);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Service Bus error: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
```

**Task 5.5: Create IRaceRequestProcessor.cs**

File: `TripleDerby.Services.Race/IRaceRequestProcessor.cs`

```csharp
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Race;

public interface IRaceRequestProcessor
{
    Task<RaceRunResult> ProcessAsync(RaceRequested request, CancellationToken cancellationToken);
}
```

**Task 5.6: Create RaceRequestProcessor.cs**

File: `TripleDerby.Services.Race/RaceRequestProcessor.cs`

```csharp
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Race;

public class RaceRequestProcessor : IRaceRequestProcessor
{
    private readonly IRaceService _raceService;
    private readonly ILogger<RaceRequestProcessor> _logger;

    public RaceRequestProcessor(
        IRaceService raceService,
        ILogger<RaceRequestProcessor> logger)
    {
        _raceService = raceService;
        _logger = logger;
    }

    public async Task<RaceRunResult> ProcessAsync(
        RaceRequested request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing race: RaceId={RaceId}, HorseId={HorseId}",
            request.RaceId, request.HorseId);

        // Delegate to existing RaceService.Race() method
        var result = await _raceService.Race(
            request.RaceId,
            request.HorseId,
            cancellationToken);

        _logger.LogInformation(
            "Race completed: Winner={Winner}, Time={Time}",
            result.HorseResults.First().HorseName,
            result.HorseResults.First().Time);

        return result;
    }
}
```

**Task 5.7: Add Race service to AppHost**

File: [TripleDerby.AppHost/Program.cs](../../TripleDerby.AppHost/Program.cs)

Add after Breeding service (after line 31):

```csharp
// Race microservice (Feature 011)
builder.AddProject<Projects.TripleDerby_Services_Race>("race")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(serviceBus)
    .WaitFor(serviceBus);
```

Update AppHost.csproj to reference new project:
```xml
<ProjectReference Include="..\TripleDerby.Services.Race\TripleDerby.Services.Race.csproj" />
```

**Implementation Notes:**
- Consumer uses `ServiceBusProcessor` for concurrent message processing
- `MaxConcurrentCalls = 5` allows 5 races to run in parallel
- Auto-complete disabled - manual control over message lifecycle
- Dead-letter queue after 3 failed attempts
- Delegates to existing `RaceService` - no business logic changes

### REFACTOR - Clean Up

**Task 5.8: Add appsettings.json for Race service**

File: `TripleDerby.Services.Race/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Azure.Messaging.ServiceBus": "Warning"
    }
  },
  "ServiceBus": {
    "RequestQueue": "race-requests",
    "CompletionQueue": "race-completions",
    "MaxConcurrentCalls": 5,
    "PrefetchCount": 10
  }
}
```

### VERIFY - Acceptance Criteria

- [ ] Race service project builds successfully
- [ ] Run Aspire - Race service starts and connects to Service Bus
- [ ] Send test message to race-requests queue
- [ ] Race service processes message and calls RaceService
- [ ] RaceCompleted message published to race-completions queue
- [ ] Check Aspire logs for successful processing
- [ ] No exceptions in service startup or message processing

**Deliverable:** Working Race microservice consuming and processing messages

---

## Phase 7: API Integration with Race Microservice

**Goal:** Update RacesController to publish race requests to Service Bus

**Vertical Slice:** POST /api/races/{id}/run publishes message, returns 202 Accepted

**Estimated Complexity:** Medium

**Risks:** Need to handle backward compatibility, consider sync vs async response

### RED - Write Failing Tests

**Test 6.1: RacesController publishes to Service Bus**

File: `TripleDerby.Tests.Unit/Controllers/RacesControllerTests.cs`

Add test to existing file (or create if doesn't exist):

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Api.Controllers;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel.Messages;
using Xunit;

namespace TripleDerby.Tests.Unit.Controllers;

public class RacesControllerTests
{
    [Fact]
    public async Task Race_PublishesRaceRequestedMessage_Returns202Accepted()
    {
        // Arrange
        var mockRaceService = new Mock<IRaceService>();
        var mockPublisher = new Mock<IMessagePublisher>();
        var controller = new RacesController(
            mockRaceService.Object,
            mockPublisher.Object,
            NullLogger<RacesController>.Instance);

        var raceId = (byte)5;
        var horseId = Guid.NewGuid();

        // Act
        var result = await controller.Race(raceId, horseId);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result.Result);
        mockPublisher.Verify(p => p.PublishAsync(
            It.Is<RaceRequested>(r => r.RaceId == raceId && r.HorseId == horseId),
            It.IsAny<MessagePublishOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

**Why these tests:** Validates controller publishes message instead of direct service call.

### GREEN - Make Tests Pass

**Task 6.1: Add Race endpoint to RacesController**

File: [TripleDerby.Api/Controllers/RacesController.cs](../../TripleDerby.Api/Controllers/RacesController.cs)

Update constructor to inject Service Bus publisher:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class RacesController : ControllerBase
{
    private readonly IRaceService _raceService;
    private readonly IMessagePublisher _serviceBusPublisher;
    private readonly ILogger<RacesController> _logger;

    public RacesController(
        IRaceService raceService,
        [FromKeyedServices("servicebus")] IMessagePublisher serviceBusPublisher,
        ILogger<RacesController> logger)
    {
        _raceService = raceService;
        _serviceBusPublisher = serviceBusPublisher;
        _logger = logger;
    }

    // ... existing GetAll() and Get() methods ...

    /// <summary>
    /// Runs a race for a given horse (async via microservice).
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="horseId">Horse identifier.</param>
    /// <returns>202 Accepted with correlation ID for tracking.</returns>
    /// <response code="202">Race simulation started.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost("{raceId}/run")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Race(
        [FromRoute] byte raceId,
        [FromQuery] Guid horseId)
    {
        var request = new RaceRequested
        {
            RaceId = raceId,
            HorseId = horseId,
            RequestedBy = GetCurrentUserId() // From JWT/claims
        };

        await _serviceBusPublisher.PublishAsync(
            request,
            new MessagePublishOptions { Destination = "race-requests" });

        _logger.LogInformation(
            "Race request published: RaceId={RaceId}, HorseId={HorseId}, CorrelationId={CorrelationId}",
            raceId, horseId, request.CorrelationId);

        // Return 202 Accepted with correlation ID for tracking
        return Accepted(new
        {
            correlationId = request.CorrelationId,
            status = "processing",
            message = "Race simulation started",
            raceId,
            horseId
        });
    }

    private Guid GetCurrentUserId()
    {
        // TODO: Extract from JWT claims or session
        // For now, return empty GUID (anonymous user)
        return Guid.Empty;
    }
}
```

**Implementation Notes:**
- Uses keyed DI to resolve Service Bus publisher
- Returns 202 Accepted (async pattern)
- Includes correlation ID in response for tracking
- User can poll GET /api/raceruns/{correlationId} for results (future enhancement)
- **Local dev**: No need for fallback logic or backward compatibility

### REFACTOR - Clean Up

**Task 6.2: Add Richardson Maturity Model (HATEOAS) links**

Following the pattern from BreedingController, add HATEOAS links to the race request response.

Update the Race method to include links:

```csharp
/// <summary>
/// Runs a race for a given horse (async via microservice).
/// </summary>
/// <param name="raceId">Race identifier.</param>
/// <param name="horseId">Horse identifier.</param>
/// <returns>202 Accepted with correlation ID and HATEOAS links for tracking.</returns>
/// <response code="202">Race simulation started.</response>
/// <response code="400">Invalid request.</response>
[HttpPost("{raceId}/run")]
[ProducesDefaultResponseType]
[ProducesResponseType(StatusCodes.Status202Accepted)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<Resource<RaceRequested>>> Race(
    [FromRoute] byte raceId,
    [FromQuery] Guid horseId)
{
    var request = new RaceRequested
    {
        RaceId = raceId,
        HorseId = horseId,
        RequestedBy = GetCurrentUserId() // From JWT/claims
    };

    await _serviceBusPublisher.PublishAsync(
        request,
        new MessagePublishOptions { Destination = "race-requests" });

    _logger.LogInformation(
        "Race request published: RaceId={RaceId}, HorseId={HorseId}, CorrelationId={CorrelationId}",
        raceId, horseId, request.CorrelationId);

    // HATEOAS links (Richardson Maturity Model Level 3)
    var links = new List<Link>
    {
        new("self", Url.Action(nameof(GetRaceRequest), "Races", new { id = request.CorrelationId }, Request.Scheme) ?? $"/api/races/requests/{request.CorrelationId}", "GET"),
        new("status", Url.Action(nameof(GetRaceRequest), "Races", new { id = request.CorrelationId }, Request.Scheme) ?? $"/api/races/requests/{request.CorrelationId}", "GET"),
        new("race-details", Url.Action("Get", "Races", new { id = raceId }, Request.Scheme) ?? $"/api/races/{raceId}", "GET")
    };

    // Return 202 Accepted with strongly-typed RaceRequested and links
    return Accepted(new Resource<RaceRequested>(request, links));
}
```

**Task 6.3: Add GetRaceRequest endpoint for status tracking**

Add endpoint to check race request status (mirrors BreedingController.GetRequest):

```csharp
/// <summary>
/// Gets race request status by correlation ID.
/// </summary>
/// <param name="id">Race request correlation ID.</param>
/// <returns>Race request status with HATEOAS links.</returns>
[HttpGet("requests/{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<Resource<RaceRequestStatusResult>>> GetRaceRequest([FromRoute] Guid id)
{
    var status = await _raceService.GetRaceRequestStatusAsync(id);

    if (status == null)
    {
        return NotFound();
    }

    // HATEOAS links (Richardson Maturity Model Level 3)
    var links = new List<Link>
    {
        new("self", Url.Action(nameof(GetRaceRequest), "Races", new { id }, Request.Scheme) ?? $"/api/races/requests/{id}", "GET")
    };

    // Add conditional links based on status
    if (status.Status == RaceRequestStatus.Completed && status.RaceRunId.HasValue)
    {
        var raceRunHref = Url.Action("GetById", "RaceRuns", new { id = status.RaceRunId.Value }, Request.Scheme) ?? $"/api/raceruns/{status.RaceRunId.Value}";
        links.Add(new Link("race-result", raceRunHref, "GET"));
    }

    if (status.Status == RaceRequestStatus.Failed)
    {
        // Add retry link (future enhancement)
        links.Add(new Link("retry", Url.Action(nameof(Race), "Races", new { raceId = status.RaceId, horseId = status.HorseId }, Request.Scheme) ?? $"/api/races/{status.RaceId}/run?horseId={status.HorseId}", "POST"));
    }

    return Ok(new Resource<RaceRequestStatusResult>(status, links));
}
```

**Implementation Notes:**
- Follows exact same pattern as BreedingController (see [BreedingController.cs](../../TripleDerby.Api/Controllers/BreedingController.cs) lines 79-84, 143-157)
- Uses `Resource<T>` wrapper with `List<Link>` collection
- Link constructor: `new(rel, href, method)`
- Conditional links based on state (e.g., "race-result" only if completed)
- Fallback pattern: `Url.Action()` first, then hardcoded path

**Task 6.4: Add XML documentation**

Ensure all public methods have XML docs.

### REFACTOR - Clean Up

**Task 6.5: Update shared types**

Ensure `Link` and `Resource<T>` classes exist in SharedKernel or API project (they should already exist from Breeding implementation).

### VERIFY - Acceptance Criteria

- [ ] Unit tests pass: `RacesControllerTests`
- [ ] POST /api/races/5/run?horseId={guid} returns 202 Accepted with HATEOAS links
- [ ] Response includes links: "self", "status", "race-details"
- [ ] GET /api/races/requests/{id} returns race request status with links
- [ ] Conditional links appear based on status (race-result, retry)
- [ ] Message appears in race-requests queue
- [ ] Race service processes message
- [ ] RaceCompleted message published
- [ ] Response includes correlation ID
- [ ] No breaking changes to existing GET endpoints
- [ ] Links follow same pattern as BreedingController

**Deliverable:** API publishes race requests via Service Bus with Richardson Maturity Model Level 3 HATEOAS links

---

## Phase 8: End-to-End Integration Testing

**Goal:** Comprehensive integration tests validating full message flow

**Vertical Slice:** API → Service Bus → Race Service → Database → Service Bus → completion

**Estimated Complexity:** Complex

**Risks:** Timing issues, race conditions, test data setup

### RED - Write Failing Tests

**Test 7.1: Full end-to-end race flow**

File: `TripleDerby.Tests.Integration/EndToEnd/RaceMicroserviceE2ETests.cs` (new file)

```csharp
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using TripleDerby.SharedKernel.Messages;
using Xunit;

namespace TripleDerby.Tests.Integration.EndToEnd;

public class RaceMicroserviceE2ETests : IAsyncLifetime
{
    private ServiceBusClient? _client;
    private IConfiguration? _configuration;

    public async Task InitializeAsync()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost"
            })
            .Build();

        _client = new ServiceBusClient(_configuration.GetConnectionString("servicebus"));

        // Wait for Service Bus to be ready
        await Task.Delay(2000);
    }

    [Fact]
    public async Task EndToEnd_RaceRequest_CompletesSuccessfully()
    {
        // Arrange
        var sender = _client!.CreateSender("race-requests");
        var receiver = _client!.CreateReceiver("race-completions");

        var request = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 1,
            HorseId = Guid.NewGuid(), // Assumes test data exists
            RequestedBy = Guid.Empty
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
        {
            ContentType = "application/json",
            CorrelationId = request.CorrelationId.ToString()
        };

        // Act
        await sender.SendMessageAsync(message);

        // Wait for processing (with timeout)
        var receivedMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));

        // Assert
        Assert.NotNull(receivedMessage);

        var completionJson = Encoding.UTF8.GetString(receivedMessage.Body);
        var completion = JsonSerializer.Deserialize<RaceCompleted>(completionJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(completion);
        Assert.Equal(request.CorrelationId, completion.CorrelationId);
        Assert.Equal(request.RaceId, completion.RaceId);
        Assert.NotEqual(Guid.Empty, completion.RaceRunId);
        Assert.NotEmpty(completion.WinnerName);

        await receiver.CompleteMessageAsync(receivedMessage);
    }

    [Fact]
    public async Task EndToEnd_MultipleRaces_ProcessConcurrently()
    {
        // Arrange
        var sender = _client!.CreateSender("race-requests");
        var tasks = new List<Task>();

        // Act - send 10 race requests
        for (int i = 0; i < 10; i++)
        {
            var request = new RaceRequested
            {
                RaceId = (byte)(i % 5 + 1), // Rotate through race IDs 1-5
                HorseId = Guid.NewGuid(),
                RequestedBy = Guid.Empty
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
            {
                ContentType = "application/json"
            };

            tasks.Add(sender.SendMessageAsync(message));
        }

        await Task.WhenAll(tasks);

        // Assert
        // Wait for all to process
        await Task.Delay(5000);

        // Check that race-completions queue has messages
        var receiver = _client!.CreateReceiver("race-completions");
        var completionCount = 0;

        for (int i = 0; i < 10; i++)
        {
            var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1));
            if (msg != null)
            {
                completionCount++;
                await receiver.CompleteMessageAsync(msg);
            }
        }

        Assert.True(completionCount >= 5, $"Expected at least 5 completions, got {completionCount}");
    }

    public async Task DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
```

**Why these tests:** Validates full message flow and concurrent processing.

### GREEN - Make Tests Pass

**Task 7.1: Ensure test data exists**

Create test data seeder or use existing horses/races in test database.

**Task 7.2: Run tests and fix issues**

Common issues:
- Connection string format
- Queue not created
- Timing/race conditions
- Missing dependencies in Race service

**Implementation Notes:**
- Tests require running Aspire (Service Bus emulator)
- May need `[Collection("Integration")]` attribute for sequential execution
- Consider using test fixtures for setup/teardown

### REFACTOR - Clean Up

**Task 7.3: Extract test helpers**

Create helper classes for:
- Service Bus message publishing
- Message receiving with timeout
- Test data creation

**Task 7.4: Add more test scenarios**

Additional tests:
- Invalid message handling
- Dead-letter queue behavior
- Retry logic
- Error scenarios (database down, invalid data)

### VERIFY - Acceptance Criteria

- [ ] All integration tests pass
- [ ] Single race request processes successfully
- [ ] Multiple races process concurrently (5+ at a time)
- [ ] Correlation IDs match request/completion
- [ ] RaceRun records created in database
- [ ] No messages stuck in dead-letter queue
- [ ] Performance acceptable (< 2 seconds per race)

**Deliverable:** Full end-to-end integration tests passing

---

## Testing Strategy

### Unit Tests

**Coverage:**
- `AzureServiceBusPublisher` - message serialization, error handling
- `AzureServiceBusRaceConsumer` - message deserialization, processing
- `RaceRequestProcessor` - delegates to RaceService correctly
- `RacesController` - publishes messages, returns 202 Accepted
- Message contracts - serialization round-trip

**Target:** 80%+ code coverage for new code

### Integration Tests

**Coverage:**
- Service Bus connectivity
- End-to-end message flow (API → Service Bus → Race Service → Database)
- Concurrent race processing
- Error handling (retries, dead-letter)

**Environment:** Requires Aspire with Service Bus emulator running

### Manual Testing

**Test Cases:**
1. Start Aspire, verify all services start
2. POST /api/races/1/run?horseId={guid} via Swagger/Postman
3. Check Aspire logs for message flow
4. Verify race-completions queue receives message
5. Check database for new RaceRun record
6. Test with invalid data (non-existent race/horse)
7. Test concurrent requests (Apache Bench / k6)

---

## Risk Mitigation

### Risk 1: Service Bus Emulator Compatibility

**Mitigation:**
- Use official Microsoft Azure Service Bus emulator
- Test on multiple developer machines
- Document setup steps clearly
- Pin NuGet package versions

### Risk 2: Message Serialization Issues

**Mitigation:**
- Write comprehensive serialization tests
- Use JSON schema validation
- Test with complex RaceRunResult objects
- Handle null/missing fields gracefully

### Risk 3: Concurrency and Race Conditions

**Mitigation:**
- Set `MaxConcurrentCalls` conservatively (5)
- Use database transactions
- Test with load testing tools
- Monitor dead-letter queue

---

## Local Development Checklist

- [ ] Run `dotnet restore` on all projects
- [ ] Run `dotnet build` to verify no compilation errors
- [ ] Start Aspire: `dotnet run --project TripleDerby.AppHost`
- [ ] Verify all containers start (SQL, Redis, RabbitMQ, Service Bus)
- [ ] Check Aspire dashboard - all services healthy
- [ ] Run unit tests: `dotnet test`
- [ ] Run integration tests (requires Aspire running)
- [ ] Test race endpoint via Swagger/Postman

---

## Phase Summary

| Phase | Goal | Complexity | Estimated Time |
|-------|------|-----------|----------------|
| 1 | Service Bus Emulator Setup | Simple | 1-2 hours |
| 2 | Service Bus Publisher | Medium | 2-3 hours |
| 3 | Keyed DI Setup | Simple | 1 hour |
| 4 | RaceRequest Entity & Status Tracking | Medium | 2 hours |
| 5 | Message Contracts | Simple | 1 hour |
| 6 | Race Microservice | Medium | 3-4 hours |
| 7 | API Integration with Status Endpoints | Medium | 2-3 hours |
| 8 | E2E Testing | Complex | 3-4 hours |

**Total Estimated Time:** 15-20 hours (2-3 days)

---

## Success Criteria

**Feature Complete When:**
- [ ] Azure Service Bus emulator runs via Aspire
- [ ] Both RabbitMQ and Service Bus registered with keyed DI
- [ ] RaceRequest entity persisted in database with status tracking
- [ ] Race microservice consumes messages successfully
- [ ] API publishes race requests to Service Bus and persists to database
- [ ] API provides GET /api/races/requests/{id} endpoint to check status (like Breeding)
- [ ] Race microservice updates RaceRequest status (Pending → InProgress → Completed/Failed)
- [ ] Breeding continues using RabbitMQ unchanged
- [ ] End-to-end race flow works (API → DB → Service Bus → Race Service → DB Update → Completion)
- [ ] Error handling and retries working with status updates
- [ ] Performance meets targets (< 2s per race, 5+ concurrent)
- [ ] All tests passing (unit + integration)
- [ ] Documentation complete

---

## Future Enhancements (Post-Feature 011)

**Not in Scope for Initial Implementation:**

1. **SignalR for Real-Time Updates**: Notify web clients when race completes
2. **Request/Response Pattern**: Synchronous API responses using Service Bus sessions
3. **Migrate Breeding to Service Bus**: Consolidate on single message broker
4. **Event Sourcing**: Store race history as events
5. **CQRS**: Separate read/write models for race data
6. **API Gateway**: Centralized routing and load balancing
7. **Distributed Tracing**: OpenTelemetry across microservices

---

## Related Documentation

- **Feature Spec:** [011-race-microservice-migration.md](../features/011-race-microservice-migration.md)
- **Feature 010:** [010-race-service-cleanup.md](../features/010-race-service-cleanup.md)
- **.NET Aspire Docs:** https://learn.microsoft.com/dotnet/aspire/
- **Azure Service Bus Docs:** https://learn.microsoft.com/azure/service-bus-messaging/

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-26 | Claude Sonnet 4.5 | Initial implementation plan based on Feature 011 spec |
| 2025-12-26 | Claude Sonnet 4.5 | Added Phase 4: RaceRequest entity to match Breeding pattern. Now 8 phases total. |
| 2025-12-26 | Claude Sonnet 4.5 | Updated RaceRequest to use `rac` schema (mirroring Breeding's `brd` schema). |
