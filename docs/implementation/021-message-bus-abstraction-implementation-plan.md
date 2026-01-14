# Message Bus Abstraction - Implementation Plan

## Overview

**Feature**: [021-message-bus-abstraction](../future-enhancements/021-message-bus-abstraction.md)
**Approach**: TDD with Vertical Slices
**Total Phases**: 6

## Summary

This implementation creates a configuration-driven message bus abstraction that enables seamless switching between RabbitMQ and Azure Service Bus. The approach uses the decorator pattern - a `RoutingMessagePublisher` wraps the concrete publisher and resolves routing from configuration. Services will inject plain `IMessagePublisher` without keyed service attributes.

**Key Design Decisions:**
1. Use decorator pattern (not factory) - simpler, single registration
2. Cache routing lookups for zero overhead after first publish
3. Maintain full backward compatibility with existing `MessagePublishOptions`
4. Validate configuration at startup with clear error messages

---

## Phase 1: Core Configuration Model

**Goal**: Create the `MessageRoutingConfig` and `MessageRoute` classes that bind from appsettings.json

**Vertical Slice**: Configuration can be bound and validated without breaking existing code

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Messaging/MessageRoutingConfigTests.cs`

- [ ] Test: Configuration binds Provider, DefaultDestination, DefaultRoutingKey from appsettings
- [ ] Test: Routes dictionary binds with Destination, RoutingKey, Subject per message type
- [ ] Test: Empty configuration produces valid default object
- [ ] Test: Metadata dictionary binds correctly for extensibility

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Abstractions/Messaging/MessageRoutingConfig.cs` - Configuration model

**Tasks**:
- [ ] Create `MessageRoutingConfig` class with `Provider`, `DefaultDestination`, `DefaultRoutingKey`, `Routes`
- [ ] Create `MessageRoute` class with `Destination`, `RoutingKey`, `Subject`, `Metadata`
- [ ] Add XML documentation for all public properties
- [ ] Ensure init-only properties for immutability

### REFACTOR - Clean Up

- [ ] Verify property naming matches JSON conventions (PascalCase for C#)

### Acceptance Criteria

- [ ] All tests pass
- [ ] Configuration binds correctly from in-memory collection
- [ ] No impact on existing code

**Deliverable**: Configuration model ready for use by routing publisher

---

## Phase 2: Routing Message Publisher

**Goal**: Create the `RoutingMessagePublisher` decorator that resolves routing from configuration

**Vertical Slice**: Messages can be published with routing resolved from config (mocked inner publisher)

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Messaging/RoutingMessagePublisherTests.cs`

- [ ] Test: Resolves destination from Routes config for known message type
- [ ] Test: Falls back to DefaultDestination when message type not in Routes
- [ ] Test: Uses DefaultRoutingKey when RoutingKey not specified per message
- [ ] Test: Respects explicit MessagePublishOptions.Destination (override behavior)
- [ ] Test: Caches routing resolution (verify mock called once for same type)
- [ ] Test: Logs warning when message type not found in Routes
- [ ] Test: Throws ArgumentNullException for null message
- [ ] Test: Disposes inner publisher when implementing IAsyncDisposable

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Infrastructure/Messaging/RoutingMessagePublisher.cs` - Decorator implementation

**Tasks**:
- [ ] Implement `RoutingMessagePublisher : IMessagePublisher, IAsyncDisposable`
- [ ] Inject `IMessagePublisher` inner, `IOptions<MessageRoutingConfig>`, `ILogger`
- [ ] Implement `PublishAsync<T>` with routing resolution
- [ ] Add `ConcurrentDictionary<Type, MessagePublishOptions>` cache
- [ ] Handle explicit options override (skip routing when Destination provided)
- [ ] Add debug logging for routing decisions
- [ ] Implement `DisposeAsync` to dispose inner publisher

### REFACTOR - Clean Up

- [ ] Extract routing resolution to private method `GetOrCreateRoutingOptions<T>`
- [ ] Ensure thread-safety with ConcurrentDictionary

### Acceptance Criteria

- [ ] All tests pass
- [ ] Routing resolution works correctly for all scenarios
- [ ] Cache eliminates repeated lookups
- [ ] Logging provides visibility into routing decisions

**Deliverable**: Routing decorator ready for DI integration

---

## Phase 3: DI Registration Extensions

**Goal**: Create `AddMessageBus()` extension method for configuration-driven provider selection

**Vertical Slice**: Single extension method registers correct publisher based on config

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Messaging/MessageBusExtensionsTests.cs`

- [ ] Test: `Provider: "RabbitMq"` registers RabbitMqMessagePublisher with routing decorator
- [ ] Test: `Provider: "ServiceBus"` registers AzureServiceBusPublisher with routing decorator
- [ ] Test: `Provider: "Auto"` with only messaging connection string selects RabbitMQ
- [ ] Test: `Provider: "Auto"` with only servicebus connection string selects ServiceBus
- [ ] Test: `Provider: "Auto"` with both connection strings prefers RabbitMQ
- [ ] Test: Invalid provider throws InvalidOperationException with clear message
- [ ] Test: Missing both connection strings throws InvalidOperationException
- [ ] Test: IMessagePublisher resolves to RoutingMessagePublisher

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Infrastructure/Messaging/MessageBusExtensions.cs` - DI registration helpers

**Tasks**:
- [ ] Create static class `MessageBusExtensions`
- [ ] Implement `AddMessageBus(IServiceCollection, IConfiguration)` extension
- [ ] Bind `MessageRoutingConfig` from `MessageBus:Routing` section
- [ ] Implement provider selection logic (switch on Provider value)
- [ ] Implement `DetectProvider` for auto-detection from connection strings
- [ ] Register concrete publisher as singleton
- [ ] Register `RoutingMessagePublisher` as `IMessagePublisher` singleton
- [ ] Add startup logging for provider selection

### REFACTOR - Clean Up

- [ ] Use pattern matching for provider switch
- [ ] Extract validation to separate method

### Acceptance Criteria

- [ ] All tests pass
- [ ] Provider selection works for all valid values
- [ ] Clear error messages for misconfiguration
- [ ] Startup logs indicate active provider

**Deliverable**: Complete DI registration system ready for application integration

---

## Phase 4: Configuration Files

**Goal**: Add MessageBus:Routing configuration to all appsettings.json files

**Vertical Slice**: Applications can start with routing configuration in place

### Tasks

**Files to Modify**:
- [ ] `TripleDerby.Api/appsettings.json` - Add MessageBus:Routing section
- [ ] `TripleDerby.Api/appsettings.Development.json` - Add development overrides (if needed)
- [ ] `TripleDerby.Services.Racing/appsettings.json` - Add MessageBus:Routing section
- [ ] `TripleDerby.Services.Breeding/appsettings.json` - Add MessageBus:Routing section
- [ ] `TripleDerby.Services.Training/appsettings.json` - Add MessageBus:Routing section

**Configuration to Add**:
```json
{
  "MessageBus": {
    "Routing": {
      "Provider": "Auto",
      "DefaultDestination": "triplederby.events",
      "DefaultRoutingKey": "default",
      "Routes": {
        "RaceRequested": {
          "Destination": "race-requests",
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

### Acceptance Criteria

- [ ] All configuration files have valid MessageBus:Routing section
- [ ] Routes defined for all message types (RaceRequested, BreedingRequested, TrainingRequested)
- [ ] Development configuration uses Auto detection

**Deliverable**: Configuration ready for service migration

---

## Phase 5: API and Service Migration

**Goal**: Replace keyed DI with `AddMessageBus()` and remove `[FromKeyedServices]` attributes

**Vertical Slice**: Services use plain `IMessagePublisher` injection with routing from config

### Tasks

**Files to Modify**:

1. **TripleDerby.Api/Program.cs**
   - [ ] Replace keyed DI registrations (lines 82-83) with `builder.Services.AddMessageBus(builder.Configuration)`
   - [ ] Remove `AddKeyedSingleton` for both publishers

2. **TripleDerby.Core/Services/RaceService.cs**
   - [ ] Remove `[FromKeyedServices("servicebus")]` attribute from constructor
   - [ ] Remove explicit `MessagePublishOptions { Destination = "race-requests" }` from publish calls (3 locations)
   - [ ] Use simple `PublishAsync(message, cancellationToken: cancellationToken)` pattern

3. **TripleDerby.Core/Services/BreedingService.cs**
   - [ ] Remove `[FromKeyedServices("rabbitmq")]` attribute from constructor
   - [ ] Verify publish calls use no explicit destination (already correct)

4. **TripleDerby.Core/Services/TrainingService.cs**
   - [ ] Remove `[FromKeyedServices("rabbitmq")]` attribute from constructor
   - [ ] Verify publish calls use no explicit destination (already correct)

5. **Microservices (optional, if they publish)**
   - [ ] Update `TripleDerby.Services.Racing/Program.cs` to use `AddMessageBus()`
   - [ ] Update `TripleDerby.Services.Breeding/Program.cs` to use `AddMessageBus()`
   - [ ] Update `TripleDerby.Services.Training/Program.cs` to use `AddMessageBus()`

### Acceptance Criteria

- [ ] No `[FromKeyedServices]` attributes in service constructors
- [ ] No explicit `Destination` in publish calls
- [ ] All services inject plain `IMessagePublisher`
- [ ] Application starts successfully
- [ ] Build succeeds with no errors

**Deliverable**: Clean service code with configuration-driven routing

---

## Phase 6: Validation and Testing

**Goal**: Verify end-to-end functionality and ensure no regressions

**Vertical Slice**: Complete message flow works with both RabbitMQ and ServiceBus configurations

### Tasks

- [ ] Run all existing unit tests - verify no regressions
- [ ] Run application with `Provider: "RabbitMq"` - verify Breeding/Training publish works
- [ ] Run application with `Provider: "ServiceBus"` - verify Race publish works
- [ ] Run application with `Provider: "Auto"` - verify correct provider detected
- [ ] Verify startup logs show provider and route count
- [ ] Test invalid configuration scenarios produce clear errors

### Manual Verification Checklist

- [ ] API starts without errors
- [ ] POST to `/api/races/{id}/queue` publishes to correct queue
- [ ] POST to `/api/breeding` publishes to correct exchange
- [ ] POST to `/api/training` publishes to correct exchange
- [ ] Log output shows routing decisions at debug level

### Acceptance Criteria

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Manual testing confirms message flow
- [ ] No regressions in existing functionality

**Deliverable**: Production-ready message bus abstraction

---

## Files Summary

### New Files
- `TripleDerby.Core/Abstractions/Messaging/MessageRoutingConfig.cs` - Configuration model
- `TripleDerby.Infrastructure/Messaging/RoutingMessagePublisher.cs` - Decorator
- `TripleDerby.Infrastructure/Messaging/MessageBusExtensions.cs` - DI helpers
- `TripleDerby.Tests.Unit/Messaging/MessageRoutingConfigTests.cs` - Config tests
- `TripleDerby.Tests.Unit/Messaging/RoutingMessagePublisherTests.cs` - Decorator tests
- `TripleDerby.Tests.Unit/Messaging/MessageBusExtensionsTests.cs` - DI tests

### Modified Files
- `TripleDerby.Api/Program.cs` - Replace keyed DI with AddMessageBus()
- `TripleDerby.Api/appsettings.json` - Add MessageBus:Routing config
- `TripleDerby.Core/Services/RaceService.cs` - Remove keyed attribute and explicit destination
- `TripleDerby.Core/Services/BreedingService.cs` - Remove keyed attribute
- `TripleDerby.Core/Services/TrainingService.cs` - Remove keyed attribute
- `TripleDerby.Services.Racing/appsettings.json` - Add MessageBus:Routing config
- `TripleDerby.Services.Breeding/appsettings.json` - Add MessageBus:Routing config
- `TripleDerby.Services.Training/appsettings.json` - Add MessageBus:Routing config

---

## Milestones

| Milestone | After Phase | What's Working |
|-----------|-------------|----------------|
| Config Model Ready | Phase 1 | Configuration classes bind from JSON |
| Routing Works | Phase 2 | Messages route via decorator (unit tests) |
| DI Complete | Phase 3 | AddMessageBus() registers correct provider |
| Config Deployed | Phase 4 | All apps have routing configuration |
| Services Clean | Phase 5 | No keyed DI, config-driven routing |
| Feature Complete | Phase 6 | Full end-to-end validation |

---

## Risks

| Risk | Mitigation | Phase |
|------|------------|-------|
| Config binding fails at runtime | Unit tests verify binding; validate at startup | 1-3 |
| Routing cache causes memory issues | Use ConcurrentDictionary (bounded by message types) | 2 |
| Breaking change during migration | Phases 1-4 are additive; only Phase 5 modifies existing code | 5 |
| Startup errors in production | Clear error messages; test all config scenarios | 3, 6 |

---

## Success Criteria

- [ ] All phases implemented
- [ ] All tests passing (unit + integration)
- [ ] No regressions in existing message flow
- [ ] Code coverage ≥ 80% for new routing code
- [ ] Services use plain IMessagePublisher (no keyed DI)
- [ ] Can switch provider via single config change
