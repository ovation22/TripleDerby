# Generic Messaging Infrastructure Cleanup

**Feature Number:** 030

**Status:** 🔵 Planned

**Created:** 2026-04-08

---

## Summary

A targeted cleanup of the generic messaging infrastructure introduced in Feature 021 and extended in Feature 023. Addresses six concrete issues identified during a template extraction review: a missing hosted service registration helper, a Service Bus sender-per-publish anti-pattern, an overly defensive config fallback chain, duplicated connection string parsing, a misleading no-op `ConnectAsync` on the Service Bus adapter, and a day-one backward compatibility shim that has no reason to exist.

---

## Motivation

### Current State

The messaging system (`IMessageBrokerAdapter`, `GenericMessageConsumer`, `RoutingMessagePublisher`, et al.) is functionally correct in TripleDerby, but several implementation details make it harder to use correctly and harder to teach from:

1. **No hosted service registration helper** — `GenericMessageConsumer<TMessage, TProcessor>` implements `IMessageConsumer` but there is no `AddMessageConsumer<TMessage, TProcessor>()` extension. Callers must know to call `AddHostedService<GenericMessageConsumer<...>>()` themselves, which is non-obvious and error-prone.

2. **`AzureServiceBusPublisher` creates a new `ServiceBusSender` on every `PublishAsync` call** — `ServiceBusSender` is not free to construct. The Azure SDK documentation explicitly recommends caching and reusing senders. This is a known perf anti-pattern.

3. **`GenericMessageConsumer.BuildConfiguration()` has seven fallback config key variations** — this was appropriate when migrating between config shapes, but it is now confusing noise. There should be one canonical key per provider.

4. **`RabbitMqBrokerAdapter` and `RabbitMqMessagePublisher` each contain their own copy of `BuildConnectionFactory` / `BuildFactory`** — near-identical private static methods that parse both URI and key-value connection string formats. Any bug fix or extension must be applied in two places.

5. **`ServiceBusBrokerAdapter.ConnectAsync` is a no-op** — it ends with `await Task.CompletedTask` because the Service Bus processor is actually started in `SubscribeAsync`. The `IMessageBrokerAdapter` two-step (`ConnectAsync` → `SubscribeAsync`) maps naturally to RabbitMQ but is misleading for Service Bus. A reader comparing both adapters will be confused about when the connection is actually established.

6. **`MessagePublisherExtensions` ships a backward-compatibility shim from day one** — the `PublishAsync(message, destination, subject, ct)` extension method is labeled "legacy method for backward compatibility" in a codebase that never used the old API. It is dead code that signals "there is a better way" without any historical justification.

### Goals

1. ✅ Add `AddMessageConsumer<TMessage, TProcessor>()` extension to `MessageBusExtensions` that registers both the consumer and its hosted service
2. ✅ Cache `ServiceBusSender` instances in `AzureServiceBusPublisher`, keyed by destination queue name
3. ✅ Simplify `GenericMessageConsumer.BuildConfiguration()` to one canonical config key per provider
4. ✅ Extract shared RabbitMQ connection string parsing to a single `RabbitMqConnectionStringParser` helper
5. ✅ Clarify the `ServiceBusBrokerAdapter.ConnectAsync` / `SubscribeAsync` lifecycle with explicit documentation (or collapse to a single `StartAsync`)
6. ✅ Delete `MessagePublisherExtensions` and its backward-compat overload

---

## Requirements

### FR1: Hosted Service Registration Helper

`MessageBusExtensions` must provide:

```csharp
services.AddMessageConsumer<TMessage, TProcessor>(optionalQueueOverride);
```

This method must:
- Register `TProcessor` with an appropriate lifetime (scoped, to match `GenericMessageConsumer`'s `IServiceScopeFactory` usage)
- Register `GenericMessageConsumer<TMessage, TProcessor>` as `IHostedService`
- Accept an optional queue name override (defaults to `MessageBus:Consumer:Queue` config)

### FR2: ServiceBusSender Caching

`AzureServiceBusPublisher` must cache `ServiceBusSender` instances:
- Use `ConcurrentDictionary<string, ServiceBusSender>` keyed by queue name
- Create on first use, reuse on subsequent calls
- Dispose all cached senders in `DisposeAsync`

### FR3: Simplified Consumer Configuration

`GenericMessageConsumer.BuildConfiguration()` must resolve the connection string from exactly one location per provider, chosen to match what Aspire injects:

| Provider | Canonical Config Key |
|----------|---------------------|
| RabbitMQ | `ConnectionStrings:messaging` |
| Azure Service Bus | `ConnectionStrings:servicebus` |

Remove all secondary fallback keys. Update `appsettings` documentation comments accordingly.

### FR4: Shared RabbitMQ Connection String Parser

Extract the duplicated connection string parsing logic into:

```
TripleDerby.Infrastructure/Messaging/RabbitMqConnectionStringParser.cs
```

Both `RabbitMqBrokerAdapter` and `RabbitMqMessagePublisher` must delegate to this helper. The parser must support both URI format (`amqp://...`) and key-value format (`Host=...;Username=...`).

### FR5: Service Bus Adapter Lifecycle Clarity

`ServiceBusBrokerAdapter` must make the deferred-connect behavior explicit. Preferred approach: add an XML doc comment to `ConnectAsync` explaining that for Service Bus, connection is deferred to `SubscribeAsync` (processor start), and why. The comment on `ConnectAsync` in `IMessageBrokerAdapter` should also note that implementations may defer connection establishment.

Alternative (if the two-step feels wrong enough to fix): collapse `IMessageBrokerAdapter` to a single `StartAsync(config, handler, ct)` method — but this is a larger change that touches all adapter tests.

### FR6: Remove Backward-Compat Shim

Delete `MessagePublisherExtensions.cs` entirely. There are no callers. No migration shim is needed.

---

## Technical Approach

All changes are contained within `TripleDerby.Infrastructure` and `TripleDerby.Core.Abstractions.Messaging`. No domain or API changes required.

**Suggested implementation order** (each item is independently releasable):

1. FR6 — delete the shim (zero-risk, verify no callers first)
2. FR4 — extract `RabbitMqConnectionStringParser`, update both consumers
3. FR3 — simplify `BuildConfiguration`, update local dev `appsettings`
4. FR2 — cache `ServiceBusSender` in `AzureServiceBusPublisher`
5. FR5 — add lifecycle documentation to Service Bus adapter
6. FR1 — add `AddMessageConsumer` extension and update all service registrations

---

## Files Affected

| File | Change |
|------|--------|
| `TripleDerby.Core/Abstractions/Messaging/IMessagePublisher.cs` | Remove `MessagePublisherExtensions` class |
| `TripleDerby.Core/Abstractions/Messaging/IMessageBrokerAdapter.cs` | Add lifecycle note to `ConnectAsync` XML doc |
| `TripleDerby.Infrastructure/Messaging/MessageBusExtensions.cs` | Add `AddMessageConsumer<TMessage, TProcessor>()` |
| `TripleDerby.Infrastructure/Messaging/AzureServiceBusPublisher.cs` | Cache `ServiceBusSender` instances |
| `TripleDerby.Infrastructure/Messaging/GenericMessageConsumer.cs` | Simplify `BuildConfiguration()` |
| `TripleDerby.Infrastructure/Messaging/RabbitMqBrokerAdapter.cs` | Delegate to shared parser |
| `TripleDerby.Infrastructure/Messaging/RabbitMqMessagePublisher.cs` | Delegate to shared parser |
| `TripleDerby.Infrastructure/Messaging/RabbitMqConnectionStringParser.cs` | **New** — extracted parser |
| `TripleDerby.Services.Racing/` | Update consumer registration to use `AddMessageConsumer` |
| `TripleDerby.Services.Breeding/` | Update consumer registration to use `AddMessageConsumer` |

---

## Success Criteria

- [ ] No callers of `MessagePublisherExtensions` (verify before delete)
- [ ] `RabbitMqConnectionStringParser` has unit tests covering URI format, key-value format, and missing required fields
- [ ] `AzureServiceBusPublisher` reuses the same `ServiceBusSender` instance across multiple `PublishAsync` calls to the same queue
- [ ] `GenericMessageConsumer.BuildConfiguration()` reads from exactly one config key per provider
- [ ] `services.AddMessageConsumer<TMessage, TProcessor>()` is the only registration call needed in service startup — no separate `AddHostedService` required
- [ ] All existing messaging tests pass with no changes
- [ ] Both Racing and Breeding microservices start and process messages correctly after registration changes

---

## Out of Scope

- Adding an outbox pattern or saga support
- Replacing `IMessageBrokerAdapter` with a third-party library (MassTransit, etc.)
- Changing the `MessageContext` / `MessageProcessingResult` types
- Adding new broker implementations (AWS SQS, etc.)
