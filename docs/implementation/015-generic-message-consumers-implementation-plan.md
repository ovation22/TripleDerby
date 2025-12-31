# Implementation Plan: Generic Message Consumers

**Feature**: Generic Message Consumers with Swappable Broker Implementations
**Feature Spec**: [docs/features/015-generic-message-consumers.md](../features/015-generic-message-consumers.md)
**Branch**: `feature/015-generic-message-consumers`

## Overview

This implementation plan breaks down the generic message consumer feature into 5 vertical slices. Each phase follows Test-Driven Development (RED-GREEN-REFACTOR) and delivers working, testable functionality.

### Strategy

1. **Build foundation first**: Core abstractions before implementations
2. **One adapter at a time**: RabbitMQ first (more complex), then Service Bus
3. **Migrate incrementally**: Breeding service first, then Racing service
4. **Tests drive design**: Write tests before implementation
5. **Validate continuously**: Each phase must have passing tests

### Key Design Decisions

- Use **Adapter pattern** for broker abstraction
- **Generic consumer** works with any adapter
- **Processor interface** unified across message types
- **Configuration-driven** broker selection
- **Backward compatible** migration path

## Phase 1: Core Abstractions and Test Infrastructure

**Goal**: Define all core interfaces and data structures with comprehensive unit tests

**Vertical Slice**: Foundation layer that all adapters will build upon

**Estimated Complexity**: Simple
**Risks**: None - these are POCOs and interfaces

### RED - Write Failing Tests

- [ ] Test: `MessageProcessingResult.Succeeded()` creates success result
- [ ] Test: `MessageProcessingResult.Failed()` creates failure with reason
- [ ] Test: `MessageProcessingResult.FailedWithException()` captures exception
- [ ] Test: `MessageBrokerConfig` default values are sensible
- [ ] Test: `MessageBrokerConfig.ProviderSpecific` dictionary works correctly
- [ ] Test: `MessageContext` properties are read-only and set via init
- [ ] Test: `IMessageBrokerAdapter` interface is defined
- [ ] Test: `IMessageProcessor<T>` interface is defined

**Why these tests**: Validate our data structures behave correctly before using them in adapters

**Test file**: `TripleDerby.Tests.Unit/Messaging/MessageProcessingResultTests.cs`
**Test file**: `TripleDerby.Tests.Unit/Messaging/MessageBrokerConfigTests.cs`
**Test file**: `TripleDerby.Tests.Unit/Messaging/MessageContextTests.cs`

### GREEN - Make Tests Pass

- [ ] Create: `TripleDerby.Core/Abstractions/Messaging/MessageProcessingResult.cs`
  - Implement `Succeeded()`, `Failed()`, `FailedWithException()` factory methods
  - Add properties: `Success`, `Requeue`, `ErrorReason`, `Exception`
- [ ] Create: `TripleDerby.Core/Abstractions/Messaging/MessageBrokerConfig.cs`
  - Add properties with default values
  - Initialize `ProviderSpecific` dictionary
- [ ] Create: `TripleDerby.Core/Abstractions/Messaging/MessageContext.cs`
  - Add all metadata properties with `init` accessors
  - Add `internal ProviderContext` for broker-specific data
- [ ] Create: `TripleDerby.Core/Abstractions/Messaging/IMessageBrokerAdapter.cs`
  - Define `ConnectAsync`, `SubscribeAsync`, `DisconnectAsync`
- [ ] Create: `TripleDerby.Core/Abstractions/Messaging/IMessageProcessor.cs`
  - Define `ProcessAsync(TMessage, MessageContext)` returning `Task<MessageProcessingResult>`

**Implementation notes**:
- Follow existing code style in `TripleDerby.Core.Abstractions.Messaging`
- Use XML documentation comments for all public APIs
- Use nullable reference types consistently
- Keep classes simple - just data structures

### REFACTOR - Clean Up

- [ ] Add comprehensive XML documentation to all types
- [ ] Ensure nullable annotations are correct
- [ ] Verify naming follows C# conventions
- [ ] Add code examples in XML doc comments

### Acceptance Criteria

- [ ] All tests in Phase 1 pass
- [ ] Code compiles without warnings
- [ ] All public APIs have XML documentation
- [ ] Nullable reference types properly annotated
- [ ] No external dependencies (just framework types)

**Deliverable**: Complete set of abstractions that adapters can implement

---

## Phase 2: RabbitMQ Adapter Implementation

**Goal**: Implement `RabbitMqBrokerAdapter` that wraps RabbitMQ client library

**Vertical Slice**: Can connect to RabbitMQ, subscribe to messages, and handle ACK/NACK

**Estimated Complexity**: Complex
**Risks**: Connection management, concurrency control, message acknowledgment logic

### RED - Write Failing Tests

- [ ] Test: `RabbitMqBrokerAdapter` constructor initializes correctly
- [ ] Test: `ConnectAsync` creates connection and channel
- [ ] Test: `ConnectAsync` declares exchange and queue based on config
- [ ] Test: `ConnectAsync` sets up QoS prefetch based on concurrency
- [ ] Test: `SubscribeAsync` registers message handler
- [ ] Test: Message handler deserializes JSON to TMessage
- [ ] Test: Message handler creates MessageContext from BasicDeliverEventArgs
- [ ] Test: Successful processing results in BasicAck
- [ ] Test: Failed processing (requeue=false) results in BasicNack with requeue=false
- [ ] Test: Failed processing (requeue=true) results in BasicNack with requeue=true
- [ ] Test: Concurrency semaphore limits concurrent processing
- [ ] Test: `DisconnectAsync` closes channel and connection gracefully
- [ ] Test: Connection string parsing (URI format)
- [ ] Test: Connection string parsing (key-value format)
- [ ] Test: Provider-specific config (Exchange, RoutingKey) is used

**Why these tests**: Ensure RabbitMQ adapter correctly wraps all the complex logic from `RabbitMqBreedingConsumer`

**Test file**: `TripleDerby.Tests.Unit/Messaging/RabbitMqBrokerAdapterTests.cs`

**Testing approach**: Use NSubstitute to mock `IConnection` and `IChannel`

### GREEN - Make Tests Pass

- [ ] Create: `TripleDerby.Infrastructure/Messaging/RabbitMqBrokerAdapter.cs`
- [ ] Implement constructor (no auto-connect)
- [ ] Implement `ConnectAsync`:
  - Parse connection string (extract from `RabbitMqBreedingConsumer`)
  - Create `ConnectionFactory` with resilience settings
  - Create connection and channel
  - Declare exchange (use `ProviderSpecific["Exchange"]`)
  - Declare queue
  - Bind queue to exchange with routing key
  - Set QoS prefetch to config.Concurrency
- [ ] Implement `SubscribeAsync<TMessage>`:
  - Create semaphore for concurrency control (config.Concurrency)
  - Create `AsyncEventingBasicConsumer`
  - Wire up `ReceivedAsync` event handler:
    - Wait on semaphore
    - Deserialize message body to TMessage
    - Create MessageContext (map DeliveryTag to ProviderContext)
    - Call handler function
    - Based on result: BasicAck or BasicNack
    - Release semaphore
  - Call `BasicConsumeAsync`
- [ ] Implement `DisconnectAsync`:
  - Close channel
  - Close connection
  - Dispose resources
- [ ] Implement `DisposeAsync`:
  - Call `DisconnectAsync`
  - Dispose semaphore

**Implementation notes**:
- Extract connection logic from [RabbitMqBreedingConsumer.cs:35-96](c:\Development\TripleDerby\TripleDerby.Services.Breeding\RabbitMqBreedingConsumer.cs#L35-L96)
- Extract message handling from [RabbitMqBreedingConsumer.cs:101-186](c:\Development\TripleDerby\TripleDerby.Services.Breeding\RabbitMqBreedingConsumer.cs#L101-L186)
- Use same JSON serialization options as `RabbitMqMessagePublisher`
- Include channel lock for thread-safe ACK/NACK

### REFACTOR - Clean Up

- [ ] Extract connection string parsing to private method
- [ ] Extract message deserialization to private method
- [ ] Extract ACK/NACK logic to private method
- [ ] Add error handling for connection failures
- [ ] Add logging for connection, subscription, and errors

### Acceptance Criteria

- [ ] All tests in Phase 2 pass
- [ ] RabbitMqBrokerAdapter implements IMessageBrokerAdapter
- [ ] Can connect to RabbitMQ (via unit test with mocks)
- [ ] Can subscribe and process messages
- [ ] ACK/NACK behavior matches current implementation
- [ ] Concurrency control works correctly
- [ ] No memory leaks (proper disposal)

**Deliverable**: Working RabbitMQ adapter that can be used by GenericMessageConsumer

---

## Phase 3: Generic Consumer and RabbitMQ Integration

**Goal**: Implement `GenericMessageConsumer<TMessage, TProcessor>` and integrate with RabbitMQ adapter

**Vertical Slice**: End-to-end message consumption with RabbitMQ

**Estimated Complexity**: Medium
**Risks**: DI scope management, configuration parsing

### RED - Write Failing Tests

- [ ] Test: `GenericMessageConsumer` constructor accepts adapter, config, scope factory, logger
- [ ] Test: `StartAsync` calls adapter.ConnectAsync with built config
- [ ] Test: `StartAsync` calls adapter.SubscribeAsync with message handler
- [ ] Test: Message handler resolves TProcessor from scoped DI
- [ ] Test: Message handler calls processor.ProcessAsync
- [ ] Test: Message handler returns processor result
- [ ] Test: Message handler catches unhandled exceptions and returns Failed result
- [ ] Test: `StopAsync` calls adapter.DisconnectAsync
- [ ] Test: `DisposeAsync` calls adapter.DisposeAsync
- [ ] Test: Configuration is built from IConfiguration with correct keys
- [ ] Test: RabbitMQ-specific config goes into ProviderSpecific dictionary

**Why these tests**: Ensure generic consumer correctly orchestrates adapter and processor

**Test file**: `TripleDerby.Tests.Unit/Messaging/GenericMessageConsumerTests.cs`

**Testing approach**: Mock `IMessageBrokerAdapter` and `IServiceProvider`

### GREEN - Make Tests Pass

- [ ] Create: `TripleDerby.Infrastructure/Messaging/GenericMessageConsumer.cs`
- [ ] Implement constructor (store dependencies)
- [ ] Implement `BuildConfiguration()` private method:
  - Read `MessageBus:Consumer:Queue`
  - Read `MessageBus:Consumer:Concurrency` (default 5)
  - Read `MessageBus:Consumer:MaxRetries` (default 3)
  - Read `MessageBus:Consumer:PrefetchCount` (default 10)
  - Read connection string from multiple locations (match publisher pattern)
  - Read RabbitMQ-specific: Exchange, RoutingKey, ExchangeType
  - Populate `ProviderSpecific` dictionary
- [ ] Implement `StartAsync`:
  - Call `BuildConfiguration()`
  - Call `_adapter.ConnectAsync(config, cancellationToken)`
  - Call `_adapter.SubscribeAsync<TMessage>(ProcessMessageAsync, cancellationToken)`
  - Log startup message
- [ ] Implement `ProcessMessageAsync`:
  - Create DI scope
  - Resolve `TProcessor` from scope
  - Call `processor.ProcessAsync(message, context)`
  - Return result
  - Catch exceptions and return Failed result
  - Log errors
- [ ] Implement `StopAsync`:
  - Call `_adapter.DisconnectAsync()`
- [ ] Implement `DisposeAsync`:
  - Call `_adapter.DisposeAsync()`
- [ ] Implement `Dispose`:
  - Call `DisposeAsync().AsTask().GetAwaiter().GetResult()`

**Implementation notes**:
- Configuration keys should match existing patterns from `RabbitMqBreedingConsumer` and `RabbitMqMessagePublisher`
- Use structured logging with message type, queue name, etc.
- Generic constraints: `where TProcessor : IMessageProcessor<TMessage>`

### REFACTOR - Clean Up

- [ ] Extract configuration reading to separate methods per section
- [ ] Add default value constants
- [ ] Improve error messages for missing configuration
- [ ] Add validation for required config values

### Acceptance Criteria

- [ ] All tests in Phase 3 pass
- [ ] GenericMessageConsumer implements IMessageConsumer
- [ ] Can start, process messages, and stop
- [ ] Configuration is read correctly from IConfiguration
- [ ] Processor is resolved from scoped DI container
- [ ] Exceptions in processor are handled gracefully
- [ ] Logs include message type, queue, and errors

**Deliverable**: Working generic consumer that can consume messages via RabbitMQ adapter

---

## Phase 4: Migrate Breeding Service to Generic Consumer

**Goal**: Replace `RabbitMqBreedingConsumer` with `GenericMessageConsumer` in Breeding service

**Vertical Slice**: Breeding service works end-to-end with new generic consumer

**Estimated Complexity**: Medium
**Risks**: Breaking existing functionality, configuration changes

### RED - Write Failing Tests

- [ ] Test: `IBreedingRequestProcessor` extends `IMessageProcessor<BreedingRequested>`
- [ ] Test: `BreedingRequestProcessor.ProcessAsync` returns `MessageProcessingResult`
- [ ] Test: Successful processing returns `MessageProcessingResult.Succeeded()`
- [ ] Test: Exception in processing returns `MessageProcessingResult.FailedWithException()`
- [ ] Test: Breeding service can start with RabbitMQ adapter configured
- [ ] Test: Breeding service can process `BreedingRequested` message end-to-end

**Why these tests**: Validate breeding processor works with new interface

**Test file**: Update `TripleDerby.Tests.Unit/Breeding/BreedingRequestProcessorTests.cs`

### GREEN - Make Tests Pass

- [ ] Modify: `TripleDerby.Services.Breeding/IBreedingRequestProcessor.cs`
  - Add inheritance: `: IMessageProcessor<BreedingRequested>`
  - Change return type to `Task<MessageProcessingResult>`
  - Change parameter from `CancellationToken` to `MessageContext context`
- [ ] Modify: `TripleDerby.Services.Breeding/BreedingRequestProcessor.cs`
  - Update signature to match interface
  - Wrap existing logic in try-catch
  - Return `MessageProcessingResult.Succeeded()` on success
  - Return `MessageProcessingResult.FailedWithException(ex)` on failure
  - Use `context.CancellationToken` instead of parameter
- [ ] Modify: `TripleDerby.Services.Breeding/Program.cs`
  - Register `RabbitMqBrokerAdapter` as `IMessageBrokerAdapter`
  - Register `GenericMessageConsumer<BreedingRequested, IBreedingRequestProcessor>` as `IMessageConsumer`
  - Remove registration of `RabbitMqBreedingConsumer`
- [ ] Modify: `TripleDerby.Services.Breeding/Worker.cs`
  - Change dependency from `RabbitMqBreedingConsumer` to `IMessageConsumer`
  - Update log messages to use generic consumer name
- [ ] Add: Configuration for RabbitMQ adapter in `appsettings.json`
  ```json
  {
    "MessageBus": {
      "Provider": "rabbitmq",
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
      }
    }
  }
  ```

**Implementation notes**:
- Keep existing `IBreedingRequestProcessor` interface name for now (backward compatibility)
- Update tests incrementally
- Test with local RabbitMQ instance if available

### REFACTOR - Clean Up

- [ ] Remove unused code from old consumer
- [ ] Update log messages for clarity
- [ ] Validate configuration at startup

### Acceptance Criteria

- [ ] All unit tests pass
- [ ] Breeding service starts successfully
- [ ] Breeding service can consume and process messages
- [ ] Existing breeding logic unchanged (just wrapped)
- [ ] Configuration is clear and documented
- [ ] No compiler warnings

**Deliverable**: Breeding service using GenericMessageConsumer with RabbitMQ

---

## Phase 5: Azure Service Bus Adapter and Racing Service Migration

**Goal**: Implement `ServiceBusAdapter` and migrate Racing service to generic consumer

**Vertical Slice**: Racing service works with Azure Service Bus via generic consumer

**Estimated Complexity**: Medium
**Risks**: Service Bus API differences, publishing logic in processor

### RED - Write Failing Tests

- [ ] Test: `ServiceBusAdapter` constructor initializes correctly
- [ ] Test: `ConnectAsync` creates ServiceBusClient and ServiceBusProcessor
- [ ] Test: `SubscribeAsync` starts processor
- [ ] Test: Message handler deserializes JSON to TMessage
- [ ] Test: Message handler creates MessageContext from ProcessMessageEventArgs
- [ ] Test: Successful processing results in CompleteMessageAsync
- [ ] Test: Failed processing (retry < max) results in AbandonMessageAsync
- [ ] Test: Failed processing (retry >= max) results in DeadLetterMessageAsync
- [ ] Test: `DisconnectAsync` stops processor
- [ ] Test: `IRaceRequestProcessor` extends `IMessageProcessor<RaceRequested>`
- [ ] Test: `RaceRequestProcessor.ProcessAsync` returns `MessageProcessingResult`
- [ ] Test: `RaceRequestProcessor` publishes completion message on success

**Why these tests**: Ensure Service Bus adapter works correctly and Racing processor handles publishing

**Test files**:
- `TripleDerby.Tests.Unit/Messaging/ServiceBusAdapterTests.cs`
- Update `TripleDerby.Tests.Unit/Services/RaceRequestProcessorTests.cs`

### GREEN - Make Tests Pass

- [ ] Create: `TripleDerby.Infrastructure/Messaging/ServiceBusAdapter.cs`
- [ ] Implement constructor (accept IConfiguration, ILogger)
- [ ] Implement `ConnectAsync`:
  - Create `ServiceBusClient` with connection string
  - Create `ServiceBusProcessor` with queue name
  - Set `MaxConcurrentCalls` from config.Concurrency
  - Set `AutoCompleteMessages = false`
  - Store processor for later use
- [ ] Implement `SubscribeAsync<TMessage>`:
  - Wire up `ProcessMessageAsync` event handler:
    - Deserialize message body to TMessage
    - Create MessageContext from args
    - Call handler function
    - Based on result: CompleteMessageAsync, AbandonMessageAsync, or DeadLetterMessageAsync
  - Wire up `ProcessErrorAsync` event handler (log errors)
  - Call `StartProcessingAsync`
- [ ] Implement `DisconnectAsync`:
  - Call `StopProcessingAsync`
- [ ] Implement `DisposeAsync`:
  - Dispose processor
  - Dispose client
- [ ] Modify: `TripleDerby.Services.Racing/IRaceRequestProcessor.cs`
  - Add inheritance: `: IMessageProcessor<RaceRequested>`
  - Change return type to `Task<MessageProcessingResult>`
  - Change parameter to `MessageContext context`
- [ ] Modify: `TripleDerby.Services.Racing/RaceRequestProcessor.cs`
  - Update signature
  - Keep existing race execution logic
  - **Keep publishing logic** (Option A from spec)
  - Wrap in try-catch
  - Return success/failure results
- [ ] Modify: `TripleDerby.Services.Racing/Program.cs`
  - Register `ServiceBusAdapter` as `IMessageBrokerAdapter`
  - Register `GenericMessageConsumer<RaceRequested, IRaceRequestProcessor>`
  - Remove registration of `AzureServiceBusRaceConsumer`
- [ ] Modify: `TripleDerby.Services.Racing/Worker.cs`
  - Change dependency to `IMessageConsumer`
- [ ] Add: Configuration for Service Bus adapter in `appsettings.json`

**Implementation notes**:
- Extract Service Bus logic from [AzureServiceBusRaceConsumer.cs:21-155](c:\Development\TripleDerby\TripleDerby.Services.Racing\AzureServiceBusRaceConsumer.cs#L21-L155)
- Keep publishing in processor for now (can refactor later)
- Use `MessageContext.DeliveryCount` for retry logic

### REFACTOR - Clean Up

- [ ] Extract message handling to private methods
- [ ] Add comprehensive logging
- [ ] Handle connection failures gracefully

### Acceptance Criteria

- [ ] All tests in Phase 5 pass
- [ ] ServiceBusAdapter implements IMessageBrokerAdapter
- [ ] Racing service starts successfully
- [ ] Racing service can consume RaceRequested messages
- [ ] Racing service publishes RaceCompleted messages
- [ ] Dead-letter logic works correctly
- [ ] No compiler warnings

**Deliverable**: Racing service using GenericMessageConsumer with Azure Service Bus

---

## Phase 6: Cleanup and Documentation

**Goal**: Remove old consumers, add tests, and document the new architecture

**Vertical Slice**: Clean, maintainable codebase with comprehensive documentation

**Estimated Complexity**: Simple
**Risks**: None - just cleanup

### Tasks

- [ ] Mark obsolete: `RabbitMqBreedingConsumer` (add `[Obsolete]` attribute)
- [ ] Mark obsolete: `AzureServiceBusRaceConsumer` (add `[Obsolete]` attribute)
- [ ] Test: Both services can switch brokers via configuration
- [ ] Test: Breeding service works with Service Bus adapter
- [ ] Test: Racing service works with RabbitMQ adapter
- [ ] Document: Configuration schema in feature spec
- [ ] Document: How to add new broker adapter
- [ ] Document: Migration guide for future processors
- [ ] Create: Architecture diagram showing adapter pattern
- [ ] Update: README with new messaging architecture section

### Deliverables

- [ ] Old consumers marked obsolete (delete in future PR)
- [ ] Cross-broker tests pass
- [ ] Documentation complete
- [ ] README updated

---

## Testing Strategy

### Unit Tests (Per Phase)

- **Phase 1**: Data structure tests (simple property tests)
- **Phase 2**: RabbitMQ adapter tests (mock IConnection/IChannel)
- **Phase 3**: Generic consumer tests (mock adapter and service provider)
- **Phase 4**: Breeding processor tests (verify new signature)
- **Phase 5**: Service Bus adapter tests (mock ServiceBusClient)

### Integration Tests (End of Implementation)

- [ ] RabbitMQ: Publish → Consume → Process → ACK (using Testcontainers)
- [ ] Service Bus: Publish → Consume → Process → Complete (using emulator)
- [ ] Error handling: NACK, dead-letter, retry
- [ ] Concurrency: Multiple messages processed in parallel
- [ ] Configuration: Switch brokers and verify behavior

### Manual Testing

- [ ] Start Breeding service with RabbitMQ
- [ ] Publish BreedingRequested message
- [ ] Verify processing succeeds
- [ ] Start Racing service with Service Bus
- [ ] Publish RaceRequested message
- [ ] Verify RaceCompleted is published
- [ ] Switch Breeding service to Service Bus (config change only)
- [ ] Verify still works

---

## Phase Completion Checklist

After each phase:

- [ ] All phase-specific tests pass
- [ ] No compiler warnings
- [ ] Code follows existing style conventions
- [ ] XML documentation on all public APIs
- [ ] Nullable reference types correctly annotated
- [ ] Git commit with clear message
- [ ] Mark phase todos as complete
- [ ] Add next phase todos to TodoWrite

---

## Rollback Plan

If issues arise during implementation:

1. **Phase 1-3 issues**: Revert commits, fix tests, re-implement
2. **Phase 4 issues**: Keep `RabbitMqBreedingConsumer` active, fix adapter
3. **Phase 5 issues**: Keep `AzureServiceBusRaceConsumer` active, fix Service Bus adapter
4. **Production issues**: Revert Program.cs registrations to old consumers

---

## Dependencies

### NuGet Packages (Already Installed)

- `RabbitMQ.Client` ≥ 6.8.0
- `Azure.Messaging.ServiceBus` ≥ 7.17.0
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`

### Test Dependencies

- `xunit`
- `NSubstitute` (for mocking)
- `FluentAssertions` (if needed)
- `Testcontainers` (for integration tests - optional)

---

## Success Metrics

- [ ] All 5 phases completed
- [ ] 100% of unit tests passing
- [ ] Both services work with both brokers
- [ ] No performance regression (< 5% latency increase)
- [ ] Code coverage > 80% for new code
- [ ] Zero compiler warnings
- [ ] Documentation complete

---

## Next Steps After Implementation

1. **Monitor in staging**: Deploy to staging environment, monitor metrics
2. **Load testing**: Verify performance under load
3. **Gradual rollout**: Production deployment with feature flags
4. **Delete old consumers**: After 1-2 releases, remove obsolete code
5. **Add more brokers**: AWS SQS, Google Pub/Sub (future)

---

## Notes

- **TDD approach**: Write tests first for each phase
- **Incremental delivery**: Each phase delivers working functionality
- **Risk management**: Most complex (RabbitMQ) first, then simpler (Service Bus)
- **Backward compatibility**: Old interfaces supported via migration path
- **Configuration-driven**: Zero code changes to switch brokers

This plan prioritizes delivering testable, working functionality incrementally while minimizing risk and maintaining backward compatibility.
