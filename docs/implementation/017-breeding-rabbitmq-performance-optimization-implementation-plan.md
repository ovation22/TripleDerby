# Breeding Service and RabbitMQ Performance Optimization - Implementation Plan

## Overview

This implementation plan breaks down the performance optimization feature into concrete, testable phases. Each phase follows Test-Driven Development (TDD) principles and delivers working, validated functionality.

**Feature Spec**: [017-breeding-rabbitmq-performance-optimization.md](../features/017-breeding-rabbitmq-performance-optimization.md)

**Goal**: Recover 30% throughput regression (500 req/sec → 350 req/sec) and achieve 500-1000 req/sec sustained throughput.

**Strategy**: Implement 5 high-impact optimizations in priority order, testing performance after each phase.

---

## Implementation Phases

### Phase 1: Color Cache Implementation (HIGHEST ROI - Est. 40-50% gain)

**Goal**: Eliminate ~350 database queries/second by caching Color reference data in memory

**Vertical Slice**: End-to-end color caching service integrated into breeding flow

**Expected Outcome**:
- Throughput: 490-525 req/sec (from 350 req/sec baseline)
- Database queries for Colors: < 5/sec (from ~350/sec)

---

#### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/ColorCacheTests.cs` (NEW)

- [ ] **Test**: ColorCache_FirstCall_LoadsFromRepository
  - Arrange: Mock repository with 10 colors
  - Act: Call GetColorsAsync
  - Assert: Repository.GetAllAsync called once, returns correct colors

- [ ] **Test**: ColorCache_SubsequentCalls_ReturnsFromCache
  - Arrange: Mock repository (track call count)
  - Act: Call GetColorsAsync three times
  - Assert: Repository.GetAllAsync called only once, all calls return same data

- [ ] **Test**: ColorCache_ConcurrentCalls_LoadsOnlyOnce
  - Arrange: Mock repository with 100ms delay
  - Act: Call GetColorsAsync concurrently (10 tasks)
  - Assert: Repository.GetAllAsync called exactly once

- [ ] **Test**: ColorCache_Invalidate_ClearsCache
  - Arrange: Cache loaded with data
  - Act: Call Invalidate(), then GetColorsAsync
  - Assert: Repository.GetAllAsync called twice (initial + after invalidation)

**Test File**: `TripleDerby.Tests.Unit/Breeding/BreedingExecutorTests.cs` (MODIFY)

- [ ] **Test**: Breed_UsesColorCache_NotRepository
  - Arrange: Mock ColorCache, mock repository
  - Act: Call Breed()
  - Assert: ColorCache.GetColorsAsync called, Repository.GetAllAsync<Color> NOT called

**Why these tests**: Define thread-safe caching behavior with lazy initialization and validate integration with BreedingExecutor

---

#### GREEN - Make Tests Pass

**File**: `TripleDerby.Core/Services/ColorCache.cs` (NEW)

- [ ] Create `ColorCache` class with:
  - `_lock` semaphore for thread safety
  - `_colors` nullable List<Color> field
  - `_logger` for observability
  - `GetColorsAsync()` method with double-checked locking pattern
  - `Invalidate()` method to clear cache

**File**: `TripleDerby.Services.Breeding/BreedingExecutor.cs` (MODIFY)

- [ ] Add `ColorCache` constructor parameter
- [ ] Update `GetRandomColor()` method:
  - OLD: `var colors = (await repository.GetAllAsync<Color>(cancellationToken)).ToList();`
  - NEW: `var colors = await _colorCache.GetColorsAsync(repository, cancellationToken);`

**File**: `TripleDerby.Services.Breeding/Program.cs` (MODIFY)

- [ ] Register ColorCache as singleton: `builder.Services.AddSingleton<ColorCache>();`

**Implementation Notes**:
- Use double-checked locking pattern to avoid race conditions
- Semaphore ensures only one thread loads from DB
- Cache persists for service lifetime (colors are static data)
- Logger provides visibility into cache initialization

---

#### REFACTOR - Clean Up

- [ ] Add XML documentation to ColorCache public methods
- [ ] Add structured logging with color count and timing
- [ ] Consider: Extract ICacheService interface if caching other reference data later

---

#### Acceptance Criteria

- [ ] All ColorCacheTests pass (4 tests)
- [ ] All BreedingExecutorTests pass (including new test)
- [ ] All existing Breeding tests pass (no regressions)
- [ ] Manual test: Start service, observe log "Color cache initialized with X colors" exactly once
- [ ] Performance test: Database queries for Color table < 5/sec (vs ~350/sec baseline)
- [ ] Performance test: Throughput > 450 req/sec (target: 490-525 req/sec)

**Deliverable**: Breeding service uses in-memory color cache, eliminating DB query hot path

**Estimated Complexity**: Medium
**Risks**: Thread safety edge cases in concurrent initialization
**Mitigation**: Comprehensive concurrency tests

---

### Phase 2: Channel Pooling for Message Publisher (HIGHEST ROI - Est. 30-40% gain)

**Goal**: Eliminate ~350 channel creations/second by reusing a single long-lived channel

**Vertical Slice**: Message publishing uses pooled channel with publisher confirms

**Expected Outcome**:
- Throughput: 637-735 req/sec (from 490-525 req/sec after Phase 1)
- Channel creations: < 1/sec (from ~350/sec)
- Zero message loss (publisher confirms)

---

#### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Messaging/RabbitMqMessagePublisherTests.cs` (NEW)

- [ ] **Test**: PublishAsync_FirstCall_CreatesChannel
  - Arrange: Mock connection/channel
  - Act: PublishAsync
  - Assert: Channel created once, ConfirmSelectAsync called

- [ ] **Test**: PublishAsync_MultipleCalls_ReusesChannel
  - Arrange: Mock connection/channel
  - Act: PublishAsync three times
  - Assert: Channel created once, BasicPublishAsync called three times

- [ ] **Test**: PublishAsync_ChannelClosed_RecreatesChannel
  - Arrange: Mock channel that reports IsOpen = false on second call
  - Act: PublishAsync twice
  - Assert: Channel created twice

- [ ] **Test**: PublishAsync_ConcurrentCalls_UseSameChannel
  - Arrange: Mock connection/channel
  - Act: PublishAsync concurrently (10 tasks)
  - Assert: Channel created once, no thread safety issues

- [ ] **Test**: PublishAsync_PublisherConfirms_Enabled
  - Arrange: Mock channel
  - Act: PublishAsync
  - Assert: WaitForConfirmsAsync called after BasicPublishAsync

**Test File**: `TripleDerby.Tests.Integration/Messaging/RabbitMqPublisherIntegrationTests.cs` (NEW)

- [ ] **Test**: PublishAsync_RealRabbitMQ_MessageDelivered
  - Arrange: TestContainers RabbitMQ, create queue
  - Act: PublishAsync message
  - Assert: Message received by consumer

**Why these tests**: Ensure channel pooling thread safety and validate publisher confirms for reliability

---

#### GREEN - Make Tests Pass

**File**: `TripleDerby.Infrastructure/Messaging/RabbitMqMessagePublisher.cs` (MODIFY)

- [ ] Add field: `private IChannel? _publishChannel;`
- [ ] Add field: `private readonly SemaphoreSlim _publishLock = new(1, 1);`
- [ ] Create method: `private async Task EnsurePublishChannelAsync()`
  - Double-checked locking pattern
  - Create channel via `_connection.CreateChannelAsync()`
  - Call `await _publishChannel.ConfirmSelectAsync()`
  - Log channel creation
- [ ] Update `PublishAsync()`:
  - Remove `await using var channel = await _connection!.CreateChannelAsync()`
  - Call `await EnsurePublishChannelAsync()`
  - Move serialization outside lock
  - Acquire `_publishLock`
  - Use `_publishChannel` instead of local `channel`
  - Call `await _publishChannel.WaitForConfirmsAsync(cancellationToken)`
  - Handle confirmation failures
  - Release lock in finally block
- [ ] Update `SafeCloseConnectionAsync()`:
  - Close `_publishChannel` if exists
- [ ] Update `DisposeAsync()`:
  - Dispose `_publishLock`

**Implementation Notes**:
- Serialize/prepare message BEFORE acquiring lock (minimize lock duration)
- Lock protects channel usage (RabbitMQ channels are NOT thread-safe)
- Publisher confirms ensure message durability
- Recreate channel if connection lost

---

#### REFACTOR - Clean Up

- [ ] Extract message preparation logic to separate method
- [ ] Add structured logging for channel lifecycle events
- [ ] Add timeout for WaitForConfirmsAsync (use _publisherConfirmTimeout)
- [ ] Consider: Retry logic if publish confirmation fails

---

#### Acceptance Criteria

- [ ] All RabbitMqMessagePublisherTests pass (5 tests)
- [ ] RabbitMqPublisherIntegrationTests passes
- [ ] All existing messaging tests pass
- [ ] Manual test: Start service, observe "Publisher channel created" logged once
- [ ] Performance test: Channel creations < 1/sec (vs ~350/sec baseline)
- [ ] Performance test: Throughput > 600 req/sec (target: 637-735 req/sec)
- [ ] Reliability test: No message loss under load (publisher confirms working)

**Deliverable**: Message publisher uses pooled channel with publisher confirms

**Estimated Complexity**: Complex
**Risks**: Channel pooling thread safety, publisher confirms adding latency
**Mitigation**: Extensive concurrency tests, benchmark confirm timeout

---

### Phase 3: Prefetch Count Optimization (Medium Impact - Est. 10-20% gain)

**Goal**: Ensure workers never starve for messages by increasing prefetch to 2x concurrency

**Vertical Slice**: Configuration change with validation

**Expected Outcome**:
- Throughput: 701-882 req/sec (from 637-735 req/sec after Phase 2)
- Queue depth: Near zero under normal load
- Worker utilization: > 95%

---

#### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Integration/Messaging/RabbitMqPrefetchTests.cs` (NEW)

- [ ] **Test**: Consumer_WithPrefetch48_Concurrency24_NoWorkerStarvation
  - Arrange: RabbitMQ with 24 workers, publish 100 messages rapidly
  - Act: Consume messages, track worker idle time
  - Assert: Worker idle time < 5% (workers always have messages)

**Test File**: `TripleDerby.Tests.Unit/Messaging/RabbitMqBrokerAdapterTests.cs` (MODIFY)

- [ ] **Test**: ConnectAsync_AppliesPrefetchCount
  - Arrange: Config with PrefetchCount = 48
  - Act: ConnectAsync
  - Assert: BasicQosAsync called with prefetchCount: 48

**Why these tests**: Validate prefetch configuration is applied and workers stay busy

---

#### GREEN - Make Tests Pass

**File**: `TripleDerby.Services.Breeding/appsettings.json` (MODIFY)

- [ ] Update MessageBus.Consumer.PrefetchCount: from 10 to 48
- [ ] Add comment explaining why (2x concurrency for optimal throughput)

**Implementation Notes**:
- Prefetch = 2x concurrency ensures workers always have messages queued
- RabbitMQ QoS enforces this limit automatically
- No code changes required (configuration only)

---

#### REFACTOR - Clean Up

- [ ] Document prefetch calculation formula in comments
- [ ] Add validation: warn if prefetch < concurrency

---

#### Acceptance Criteria

- [ ] RabbitMqPrefetchTests passes
- [ ] RabbitMqBrokerAdapterTests passes
- [ ] Configuration change deployed
- [ ] Manual test: Observe in RabbitMQ management UI that consumer has prefetch=48
- [ ] Performance test: Queue depth stays near 0 under load
- [ ] Performance test: Throughput > 700 req/sec (target: 701-882 req/sec)

**Deliverable**: Prefetch optimized for worker utilization

**Estimated Complexity**: Simple
**Risks**: Memory usage if many large messages prefetched
**Mitigation**: Monitor memory, reduce prefetch if needed

---

### Phase 4: Remove Redundant Semaphore (Medium Impact - Est. 5-10% gain)

**Goal**: Eliminate lock contention by removing redundant concurrency control

**Vertical Slice**: Simplify message handler, rely on RabbitMQ QoS for concurrency

**Expected Outcome**:
- Throughput: 736-970 req/sec (from 701-882 req/sec after Phase 3)
- Reduced lock contention
- Lower latency variance

---

#### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Integration/Messaging/RabbitMqConcurrencyTests.cs` (NEW)

- [ ] **Test**: Consumer_WithoutSemaphore_RespectsQoSConcurrencyLimit
  - Arrange: RabbitMQ with concurrency=5, publish 20 messages
  - Act: Consume with slow handler (100ms each)
  - Assert: Max 5 messages processed concurrently (tracked with counter)

**Test File**: `TripleDerby.Tests.Unit/Messaging/RabbitMqBrokerAdapterTests.cs` (MODIFY)

- [ ] **Test**: SubscribeAsync_NoSemaphore_HandlerCalled
  - Arrange: Mock channel, trigger ReceivedAsync event
  - Act: SubscribeAsync, simulate message received
  - Assert: Handler called without semaphore blocking

**Why these tests**: Prove RabbitMQ QoS enforces concurrency without application-level semaphore

---

#### GREEN - Make Tests Pass

**File**: `TripleDerby.Infrastructure/Messaging/RabbitMqBrokerAdapter.cs` (MODIFY)

- [ ] Remove field: `private SemaphoreSlim? _semaphore;`
- [ ] Remove from `ConnectAsync()`: `_semaphore = new SemaphoreSlim(config.Concurrency, config.Concurrency);`
- [ ] Update `SubscribeAsync()` event handler:
  - Remove: `await _semaphore.WaitAsync(cancellationToken);`
  - Remove from finally block: `_semaphore.Release();`
  - Keep try/catch/finally structure for ACK/NACK
- [ ] Remove from `DisposeAsync()`: `_semaphore?.Dispose();`

**Implementation Notes**:
- RabbitMQ QoS `prefetchCount` already limits unacked messages
- Application semaphore is redundant and adds overhead
- Concurrency naturally limited by number of prefetched messages
- `_channelLock` still needed to protect BasicAckAsync/BasicNackAsync (not thread-safe)

---

#### REFACTOR - Clean Up

- [ ] Simplify event handler logic without semaphore clutter
- [ ] Add comment explaining why no semaphore needed (QoS handles it)

---

#### Acceptance Criteria

- [ ] RabbitMqConcurrencyTests passes
- [ ] All RabbitMqBrokerAdapterTests pass
- [ ] All integration tests pass (no regressions)
- [ ] Manual test: Observe max concurrency still respected (monitor logs)
- [ ] Performance test: Reduced lock contention (lower p99 latency variance)
- [ ] Performance test: Throughput > 700 req/sec (target: 736-970 req/sec)

**Deliverable**: Message consumer without redundant concurrency control

**Estimated Complexity**: Medium
**Risks**: Concurrency limit not enforced if QoS misconfigured
**Mitigation**: Integration test validates concurrency limit

---

### Phase 5: Eliminate Redundant BreedingRequest Query (Low Impact - Est. 2-5% gain)

**Goal**: Remove redundant database query inside transaction to reduce lock duration

**Vertical Slice**: Optimize transaction to reuse already-loaded entity

**Expected Outcome**:
- Throughput: 751-1,019 req/sec (from 736-970 req/sec after Phase 4)
- Reduced transaction duration (~5-10ms faster)
- Less database lock contention

---

#### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Breeding/BreedingRequestProcessorTests.cs` (MODIFY)

- [ ] **Test**: ProcessAsync_UpdatesBreedingRequest_WithoutReloading
  - Arrange: Mock repository with BreedingRequest entity
  - Act: ProcessAsync
  - Assert: FindAsync<BreedingRequest> called exactly ONCE (not twice)

**Test File**: `TripleDerby.Tests.Integration/Breeding/BreedingTransactionTests.cs` (NEW)

- [ ] **Test**: Breed_UpdatesAllEntities_InSingleTransaction
  - Arrange: Real DB with sire, dam, breeding request
  - Act: Breed
  - Assert: Foal created, parents updated, breeding request updated, all in one transaction

**Why these tests**: Verify transaction optimization doesn't break entity updates or consistency

---

#### GREEN - Make Tests Pass

**File**: `TripleDerby.Services.Breeding/BreedingRequestProcessor.cs` (MODIFY)

- [ ] Update `Breed()` method inside `ExecuteInTransactionAsync`:
  - Remove lines 112-120 (redundant FindAsync and null check)
  - After breeding logic, directly update `stored` entity:
    ```csharp
    stored.FoalId = breedingResult.FoalId;
    stored.Status = BreedingRequestStatus.Completed;
    stored.ProcessedDate = timeManager.OffsetUtcNow();
    stored.UpdatedDate = timeManager.OffsetUtcNow();
    await repository.UpdateAsync(stored, cancellationToken);
    ```
  - Return `breedingResult`

**Implementation Notes**:
- `stored` entity already loaded at line 34 before transaction
- Entity Framework tracks this entity
- No need to reload inside transaction
- Reduces queries from 8 to 7 per breeding operation

---

#### REFACTOR - Clean Up

- [ ] Verify EF change tracking behavior (stored entity is tracked)
- [ ] Add comment explaining why no reload needed

---

#### Acceptance Criteria

- [ ] BreedingRequestProcessorTests passes (verify query count)
- [ ] BreedingTransactionTests passes (verify transactional consistency)
- [ ] All existing breeding tests pass
- [ ] Performance test: One fewer query per breeding operation
- [ ] Performance test: Transaction duration ~5-10ms faster
- [ ] Performance test: Throughput > 750 req/sec (target: 751-1,019 req/sec)

**Deliverable**: Breeding transaction optimized with no redundant queries

**Estimated Complexity**: Simple
**Risks**: EF change tracking edge cases
**Mitigation**: Integration test with real DB validates behavior

---

### Phase 6: Performance Testing & Validation

**Goal**: Measure cumulative performance gains and validate targets achieved

**Vertical Slice**: Comprehensive performance test suite with baseline and optimized measurements

**Expected Outcome**:
- Documented proof of throughput recovery
- Validated latency targets (p95 < 100ms, p99 < 200ms)
- Baseline for future optimizations

---

#### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Performance/BreedingThroughputBenchmark.cs` (NEW)

- [ ] **Benchmark**: Baseline_GenericConsumer_Throughput
  - Setup: Revert to baseline (before optimizations)
  - Measure: Requests/sec over 60 seconds
  - Target: Document baseline (~350 req/sec)

- [ ] **Benchmark**: Optimized_WithAllPhases_Throughput
  - Setup: All optimizations enabled
  - Measure: Requests/sec over 60 seconds
  - Target: ≥ 750 req/sec (stretch: ≥ 800 req/sec)

- [ ] **Benchmark**: Optimized_LatencyPercentiles
  - Setup: All optimizations enabled
  - Measure: p50, p95, p99 latencies
  - Target: p95 < 100ms, p99 < 200ms

- [ ] **Benchmark**: Optimized_DatabaseLoad
  - Setup: All optimizations enabled
  - Measure: DB queries/sec, connection pool usage
  - Target: Color queries < 5/sec, connection pool < 50% utilized

**Test File**: `TripleDerby.Tests.Performance/BreedingResourceUsageTests.cs` (NEW)

- [ ] **Test**: Optimized_MemoryUsage_Stable
  - Run: 1000 breeding operations
  - Measure: Memory before/after, GC collections
  - Assert: No memory leaks, GC pressure acceptable

- [ ] **Test**: Optimized_ChannelCreation_Minimal
  - Run: 1000 breeding operations
  - Measure: RabbitMQ channel count over time
  - Assert: Channel count stays at 1-2 (vs ~1000 baseline)

**Why these tests**: Quantify performance gains and validate optimization targets achieved

---

#### GREEN - Make Tests Pass

**File**: `TripleDerby.Tests.Performance/BreedingThroughputBenchmark.cs` (IMPLEMENT)

- [ ] Use BenchmarkDotNet for accurate measurements
- [ ] Set up test RabbitMQ (TestContainers)
- [ ] Set up test database with seed data
- [ ] Implement load generator (publish messages rapidly)
- [ ] Measure throughput, latency, resource usage
- [ ] Generate markdown report with results

**File**: `docs/performance/breeding-service-optimization-results.md` (NEW)

- [ ] Document baseline measurements
- [ ] Document post-optimization measurements
- [ ] Create before/after comparison table
- [ ] Include graphs (throughput over time, latency distribution)
- [ ] Summarize key findings

**Implementation Notes**:
- Run benchmarks on consistent hardware
- Warm up before measurement period
- Collect data over sufficient duration (60+ seconds)
- Monitor system resources (CPU, memory, disk I/O)

---

#### REFACTOR - Clean Up

- [ ] Create reusable performance test harness
- [ ] Extract common setup/teardown logic
- [ ] Document how to run performance tests

---

#### Acceptance Criteria

- [ ] Baseline throughput: ~350 req/sec (documented)
- [ ] Optimized throughput: ≥ 750 req/sec (minimum target)
- [ ] Optimized throughput: ≥ 800 req/sec (stretch goal)
- [ ] p95 latency: < 100ms
- [ ] p99 latency: < 200ms
- [ ] Color DB queries: < 5/sec (from ~350/sec)
- [ ] Channel creations: < 1/sec (from ~350/sec)
- [ ] Memory usage: Stable (no leaks)
- [ ] Results documented in markdown report

**Deliverable**: Performance validation report proving optimization targets achieved

**Estimated Complexity**: Medium
**Risks**: Benchmark environment differences
**Mitigation**: Run on dedicated test hardware, multiple trials

---

## Phase Progression Strategy

### Order of Implementation

1. **Phase 1 (Color Cache)** - Highest ROI, isolated change
2. **Phase 2 (Channel Pooling)** - Highest ROI, requires careful testing
3. **Phase 3 (Prefetch)** - Quick win, configuration only
4. **Phase 4 (Remove Semaphore)** - Medium complexity, good incremental gain
5. **Phase 5 (Redundant Query)** - Low complexity, small gain
6. **Phase 6 (Performance Testing)** - Validation and documentation

### Testing Strategy

- **Unit Tests**: Test each component in isolation
- **Integration Tests**: Test with real RabbitMQ and database
- **Performance Tests**: Measure throughput, latency, resource usage
- **Regression Tests**: Ensure existing functionality unchanged

### Performance Validation After Each Phase

| After Phase | Expected Throughput | How to Verify |
|-------------|-------------------|---------------|
| Baseline | ~350 req/sec | Run performance test |
| Phase 1 | 490-525 req/sec | Run performance test, check DB query count |
| Phase 2 | 637-735 req/sec | Run performance test, check channel count |
| Phase 3 | 701-882 req/sec | Run performance test, check worker utilization |
| Phase 4 | 736-970 req/sec | Run performance test, check lock contention |
| Phase 5 | 751-1,019 req/sec | Run performance test, check transaction duration |

---

## Risk Mitigation

### High-Risk Areas

1. **Channel Pooling Thread Safety**
   - **Risk**: Race conditions, channel corruption
   - **Mitigation**: Comprehensive concurrency tests, lock correctness verification

2. **Cache Invalidation**
   - **Risk**: Stale color data if colors change
   - **Mitigation**: Manual invalidation API, colors rarely change in production

3. **Publisher Confirms Latency**
   - **Risk**: Confirms add latency, reduce throughput
   - **Mitigation**: Benchmark confirms, tune timeout, acceptable for reliability

4. **Semaphore Removal**
   - **Risk**: Concurrency not enforced if QoS misconfigured
   - **Mitigation**: Integration test validates concurrency limit

### Testing Checklist

Before considering a phase complete:

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All existing tests pass (no regressions)
- [ ] Performance test shows expected improvement
- [ ] Manual testing in dev environment successful
- [ ] Code reviewed (if applicable)
- [ ] Documentation updated

---

## Success Criteria

### Overall Feature Success

- [ ] Throughput ≥ 500 req/sec (minimum requirement)
- [ ] Throughput ≥ 750 req/sec (target)
- [ ] Throughput ≥ 800 req/sec (stretch goal)
- [ ] p95 latency < 100ms
- [ ] p99 latency < 200ms
- [ ] All existing tests pass
- [ ] No data loss (publisher confirms working)
- [ ] Memory usage stable
- [ ] Database connection pool healthy

### Per-Phase Success Criteria

See individual phase acceptance criteria sections above.

---

## Dependencies and Prerequisites

### Required Before Starting

- [ ] Feature spec approved: `017-breeding-rabbitmq-performance-optimization.md`
- [ ] Test RabbitMQ environment available
- [ ] Test database environment available
- [ ] Performance testing tools installed (BenchmarkDotNet)
- [ ] Branch created: `feature/017-breeding-rabbitmq-performance-optimization`

### External Dependencies

- RabbitMQ.Client library (already installed)
- Entity Framework Core (already installed)
- BenchmarkDotNet (install for Phase 6)
- TestContainers (for integration tests)

---

## Monitoring and Observability

### Metrics to Track

- **Throughput**: Messages processed per second
- **Latency**: p50, p95, p99 processing time
- **Database**: Queries/sec, connection pool usage
- **RabbitMQ**: Channel count, connection count, queue depth
- **Application**: CPU usage, memory usage, GC pressure

### Logging Enhancements

Add structured logging for:

- Phase 1: Color cache initialization, cache hits
- Phase 2: Channel creation, publisher confirm failures
- Phase 3: Worker utilization
- Phase 4: Concurrency violations (if any)
- Phase 5: Transaction duration

---

## Future Optimizations (Not in Scope)

These optimizations are not included in this feature but may be considered later:

1. **LegType Caching**: Similar to color cache
2. **Batch Publishing**: Publish multiple messages at once
3. **Connection Pooling**: Multiple RabbitMQ connections
4. **Compiled EF Queries**: Faster database queries
5. **Read Replicas**: Offload reads to replica databases

---

## References

- [Feature Spec](../features/017-breeding-rabbitmq-performance-optimization.md)
- [Feature 015: Generic Message Consumers](../features/015-generic-message-consumers.md)
- [RabbitMQ Performance Best Practices](https://www.rabbitmq.com/best-practices.html)
- [EF Core Performance](https://learn.microsoft.com/en-us/ef/core/performance/)
