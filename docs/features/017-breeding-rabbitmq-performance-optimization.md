# Breeding Service and RabbitMQ Performance Optimization

## Feature Summary

Optimize the Breeding service and RabbitMQ messaging infrastructure to recover the 30% throughput regression (500 req/sec → 350 req/sec) observed after migrating to generic message consumers, and achieve target throughput of 500-1000 req/sec while maintaining strict consistency guarantees.

## Problem Statement

After implementing generic message consumers (Feature 015), the Breeding service experienced a **30% performance degradation**:
- **Before**: ~500 breeding requests/second
- **After**: ~350 breeding requests/second
- **Target**: 500-1000 req/sec with high concurrency (24 workers)

### Root Cause Analysis

Comparing the old `RabbitMqBreedingConsumer` with the new `GenericMessageConsumer` + `RabbitMqBrokerAdapter`:

#### 1. **Extra Layer of Indirection** (Minor Impact: ~5-10ms)
- **Old**: `Worker` → `RabbitMqBreedingConsumer` → Direct message handling
- **New**: `Worker` → `GenericMessageConsumer` → `RabbitMqBrokerAdapter` → Message handling
- Additional async/await overhead and delegate invocations

#### 2. **Redundant Semaphore Usage** (Medium Impact: Lock contention)
- **Old**: Single semaphore for concurrency control
- **New**: **Two semaphores** working in tandem:
  - `RabbitMqBrokerAdapter._semaphore` (controls concurrency)
  - `RabbitMqBrokerAdapter._channelLock` (protects ACK/NACK)
- RabbitMQ QoS already limits delivery, making `_semaphore` redundant

#### 3. **JSON Deserialization Twice** (Low Impact: ~1-2ms per message)
- **Old**: Deserialize once in `OnMessageAsync`
- **New**: Deserialize in `RabbitMqBrokerAdapter.SubscribeAsync`, then type is already `TMessage`
- **VERIFIED**: Only one deserialization occurs (line 102 in RabbitMqBrokerAdapter.cs)

#### 4. **Database Query in Hot Path** (HIGH Impact: ~10-50ms per breeding)
- **Location**: `BreedingExecutor.GetRandomColor` (line 94)
- **Problem**: `await repository.GetAllAsync<Color>(cancellationToken)` on **every breeding operation**
- **Impact**: With 24 concurrent workers at 350 req/sec, this generates **~350 DB queries/second** for static reference data
- **Colors table**: Static data, rarely changes, perfect candidate for caching

#### 5. **Channel-per-Publish in RabbitMqMessagePublisher** (HIGH Impact: ~20-100ms)
- **Location**: `RabbitMqMessagePublisher.PublishAsync` (line 153)
- **Problem**: `await using var channel = await _connection!.CreateChannelAsync()` on every message
- **Impact**: Channel creation/disposal is expensive; creating ~350 channels/sec wastes CPU and memory

#### 6. **Prefetch/Concurrency Mismatch** (Medium Impact: Worker starvation)
- **Config**: `Concurrency: 24`, `PrefetchCount: 10`
- **Problem**: Only 10 messages prefetched but 24 workers ready → workers wait for messages
- **Impact**: Underutilized workers, lower throughput

#### 7. **No Publisher Confirms** (Reliability Risk)
- **Problem**: Messages could be lost if RabbitMQ crashes before persisting
- **Impact**: Critical `BreedingCompleted` events may not be delivered

#### 8. **Excessive Database Queries in Transaction** (Medium Impact: Lock duration)
- **Transaction includes**:
  - Load dam + sire (2 queries via `SingleOrDefaultAsync`)
  - Load all colors (1 query - to be cached)
  - Insert foal (1 query)
  - Update parent counters (2 updates via `ExecuteUpdateAsync`)
  - Reload BreedingRequest (1 query - redundant!)
  - Update BreedingRequest (1 query)
- **Total**: 8 database roundtrips per breeding
- **Impact**: Long-held transactions increase lock contention

### Performance Impact Breakdown

| Issue | Impact | Estimated Overhead | Priority |
|-------|--------|-------------------|----------|
| Database query in hot path (Colors) | **CRITICAL** | 10-50ms per request | P0 |
| Channel-per-publish pattern | **CRITICAL** | 20-100ms per publish | P0 |
| Prefetch/concurrency mismatch | **HIGH** | 10-30% throughput loss | P1 |
| Redundant semaphore | **MEDIUM** | 5-10ms lock contention | P1 |
| Redundant DB query in transaction | **MEDIUM** | 5-10ms per request | P2 |
| Extra abstraction layers | **LOW** | 2-5ms per request | P3 |
| No publisher confirms | **RISK** | Data loss potential | P1 |

**Combined overhead**: Approximately **50-200ms per breeding request** from optimizable issues

## Requirements

### Functional Requirements

1. **Throughput Recovery**: Restore performance to ≥500 req/sec
2. **Throughput Target**: Achieve 500-1000 req/sec sustained throughput
3. **Strict Consistency**: Maintain exactly-once semantics for breeding operations
4. **No Regressions**: All existing tests must pass
5. **Backward Compatibility**: No breaking changes to public APIs
6. **Message Reliability**: Zero data loss for published messages

### Non-Functional Requirements

1. **Latency**: p95 latency < 100ms, p99 < 200ms
2. **Resource Efficiency**: No increase in memory or CPU usage vs baseline
3. **Observability**: Maintain existing logging and metrics
4. **Testability**: All optimizations must be testable
5. **Maintainability**: Code remains clean and follows existing patterns

## Technical Approach

### Phase 1: Color Caching (HIGHEST IMPACT - Est. 40-50% throughput gain)

**Problem**: Loading colors from database on every breeding operation

**Solution**: Implement in-memory color cache as singleton service

```csharp
namespace TripleDerby.Core.Services;

/// <summary>
/// Singleton cache for Color reference data.
/// Colors are static game data that rarely change.
/// </summary>
public class ColorCache
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<Color>? _colors;
    private readonly ILogger<ColorCache> _logger;

    public ColorCache(ILogger<ColorCache> logger)
    {
        _logger = logger;
    }

    public async Task<List<Color>> GetColorsAsync(
        ITripleDerbyRepository repository,
        CancellationToken cancellationToken = default)
    {
        // Fast path: cache hit
        if (_colors != null)
            return _colors;

        // Slow path: cache miss (only happens once per app lifetime)
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_colors != null)
                return _colors;

            _colors = (await repository.GetAllAsync<Color>(cancellationToken)).ToList();
            _logger.LogInformation("Color cache initialized with {Count} colors", _colors.Count);

            return _colors;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Invalidates cache (for admin operations that modify colors)
    /// </summary>
    public void Invalidate()
    {
        _colors = null;
        _logger.LogInformation("Color cache invalidated");
    }
}
```

**Changes to BreedingExecutor**:
```csharp
public class BreedingExecutor
{
    private readonly ColorCache _colorCache; // Injected singleton

    private async Task<Color> GetRandomColor(...)
    {
        // OLD: var colors = (await repository.GetAllAsync<Color>(cancellationToken)).ToList();
        // NEW:
        var colors = await _colorCache.GetColorsAsync(repository, cancellationToken);

        // Rest of logic unchanged...
    }
}
```

**DI Registration**:
```csharp
builder.Services.AddSingleton<ColorCache>();
```

**Impact**: Eliminates **~350 database queries/second** → **99%+ reduction in DB load from colors**

---

### Phase 2: Channel Pooling for Publisher (HIGHEST IMPACT - Est. 30-40% throughput gain)

**Problem**: Creating a new channel for every published message

**Solution**: Use a single long-lived channel with proper locking

```csharp
public class RabbitMqMessagePublisher
{
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public async Task PublishAsync<T>(T message, ...)
    {
        await EnsureConnectedAsync();
        await EnsurePublishChannelAsync(); // New method

        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(payload);

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            var props = new BasicProperties { ... };

            await _publishChannel!.BasicPublishAsync(
                exchange: ex,
                routingKey: rk,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task EnsurePublishChannelAsync()
    {
        if (_publishChannel != null && _publishChannel.IsOpen)
            return;

        await _connectionLock.WaitAsync();
        try
        {
            if (_publishChannel != null && _publishChannel.IsOpen)
                return;

            _publishChannel = await _connection!.CreateChannelAsync();

            // Enable publisher confirms for reliability
            await _publishChannel.ConfirmSelectAsync();

            _logger.LogInformation("Publisher channel created");
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
```

**Impact**: Eliminates **~350 channel creations/second** → Reduces publish latency by 50-80%

---

### Phase 3: Optimize Prefetch Count (Medium Impact - Est. 10-20% throughput gain)

**Problem**: Prefetch (10) much lower than concurrency (24)

**Solution**: Increase prefetch to 1.5-2x concurrency

```json
{
  "MessageBus": {
    "Consumer": {
      "Concurrency": 24,
      "PrefetchCount": 48  // OLD: 10, NEW: 48 (2x concurrency)
    }
  }
}
```

**Impact**: Workers always have messages available → eliminates starvation

---

### Phase 4: Remove Redundant Semaphore (Medium Impact - Est. 5-10% throughput gain)

**Problem**: Both `_semaphore` and RabbitMQ QoS control concurrency

**Solution**: Remove `_semaphore`, rely on RabbitMQ QoS alone

**OLD** (RabbitMqBrokerAdapter.cs):
```csharp
consumer.ReceivedAsync += async (sender, ea) =>
{
    await _semaphore.WaitAsync(cancellationToken); // REMOVE THIS

    try
    {
        // Process message...
    }
    finally
    {
        _semaphore.Release(); // REMOVE THIS
    }
};
```

**NEW**:
```csharp
consumer.ReceivedAsync += async (sender, ea) =>
{
    try
    {
        // Process message...
    }
    catch (Exception ex)
    {
        // Handle errors...
    }
};
```

**Rationale**:
- RabbitMQ QoS (`prefetchCount`) already limits unacked messages to 48
- Semaphore adds unnecessary lock contention
- Concurrency is naturally limited by prefetch

**Impact**: Reduces lock contention → Lower latency variance

---

### Phase 5: Eliminate Redundant BreedingRequest Query (Low Impact - Est. 2-5% gain)

**Problem**: BreedingRequest loaded twice in transaction

**OLD** (BreedingRequestProcessor.cs):
```csharp
var result = await repository.ExecuteInTransactionAsync(async () =>
{
    // ... breeding logic ...

    // REDUNDANT: We already have 'stored' from line 34!
    var breedingRequestEntity = await repository.FindAsync<BreedingRequest>(
        request.RequestId, cancellationToken);

    if (breedingRequestEntity != null)
    {
        breedingRequestEntity.FoalId = breedingResult.FoalId;
        // ...
    }
});
```

**NEW**:
```csharp
var result = await repository.ExecuteInTransactionAsync(async () =>
{
    // ... breeding logic ...

    // Reuse 'stored' entity from outer scope
    stored.FoalId = breedingResult.FoalId;
    stored.Status = BreedingRequestStatus.Completed;
    stored.ProcessedDate = timeManager.OffsetUtcNow();
    stored.UpdatedDate = timeManager.OffsetUtcNow();
    await repository.UpdateAsync(stored, cancellationToken);
});
```

**Impact**: Reduces transaction duration by ~5-10ms → Less lock contention

---

### Phase 6: Enable Publisher Confirms (Reliability - No performance impact)

**Problem**: No acknowledgment that RabbitMQ persisted the message

**Solution**: Enable publisher confirms and wait for acknowledgment

```csharp
private async Task EnsurePublishChannelAsync()
{
    // ... create channel ...

    await _publishChannel.ConfirmSelectAsync();
}

public async Task PublishAsync<T>(...)
{
    await _publishLock.WaitAsync(cancellationToken);
    try
    {
        await _publishChannel!.BasicPublishAsync(...);

        // Wait for broker confirmation (with timeout)
        var confirmed = await _publishChannel.WaitForConfirmsAsync(
            cancellationToken: cancellationToken);

        if (!confirmed)
        {
            throw new InvalidOperationException(
                "Message publish not confirmed by broker");
        }
    }
    finally
    {
        _publishLock.Release();
    }
}
```

**Impact**: **Zero data loss** for published messages (slight latency increase acceptable)

---

### Phase 7: DbContext Pool Size Tuning (Optional - Only if needed)

**Current**: Default pool size (usually 128)

**If connection pool exhaustion detected**: Increase pool size

```csharp
builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")),
    poolSize: 256  // Increase if needed
);
```

**Monitor**: Connection pool usage metrics

---

## Implementation Plan

### Phase 1: Color Cache (HIGHEST ROI)
**Files to modify:**
- `TripleDerby.Core/Services/ColorCache.cs` (NEW)
- `TripleDerby.Services.Breeding/BreedingExecutor.cs`
- `TripleDerby.Services.Breeding/Program.cs`

**Tasks:**
1. Create `ColorCache` service in `TripleDerby.Core/Services/`
2. Add singleton DI registration in Breeding service
3. Update `BreedingExecutor` to inject and use `ColorCache`
4. Add unit tests for `ColorCache`
5. Add integration test verifying cache usage

**Acceptance Criteria:**
- Colors loaded from DB only once per service lifetime
- All breeding tests pass
- Performance test shows >30% throughput improvement

---

### Phase 2: Channel Pooling for Publisher (HIGHEST ROI)
**Files to modify:**
- `TripleDerby.Infrastructure/Messaging/RabbitMqMessagePublisher.cs`

**Tasks:**
1. Add `_publishChannel` and `_publishLock` fields
2. Implement `EnsurePublishChannelAsync()` method
3. Refactor `PublishAsync()` to use pooled channel
4. Enable publisher confirms
5. Add proper disposal in `DisposeAsync()`
6. Add unit tests for channel pooling
7. Add integration test for publish reliability

**Acceptance Criteria:**
- Single channel used for all publishes
- Publisher confirms enabled
- No channel creation in hot path
- All messaging tests pass

---

### Phase 3: Prefetch Optimization
**Files to modify:**
- `TripleDerby.Services.Breeding/appsettings.json`

**Tasks:**
1. Update `PrefetchCount` from 10 to 48
2. Verify in logs that prefetch is applied
3. Monitor queue depth and worker utilization

**Acceptance Criteria:**
- Workers never starve for messages
- Queue depth remains near zero under load

---

### Phase 4: Remove Redundant Semaphore
**Files to modify:**
- `TripleDerby.Infrastructure/Messaging/RabbitMqBrokerAdapter.cs`

**Tasks:**
1. Remove `_semaphore` field
2. Remove `await _semaphore.WaitAsync()` in `ReceivedAsync` handler
3. Remove `_semaphore.Release()` in `ReceivedAsync` handler
4. Remove semaphore disposal
5. Add integration test verifying concurrency still enforced by QoS

**Acceptance Criteria:**
- Concurrency still limited to configured value
- No deadlocks or race conditions
- All messaging tests pass

---

### Phase 5: Eliminate Redundant Query
**Files to modify:**
- `TripleDerby.Services.Breeding/BreedingRequestProcessor.cs`

**Tasks:**
1. Remove `FindAsync<BreedingRequest>` inside transaction
2. Reuse `stored` entity from outer scope
3. Verify transaction integrity

**Acceptance Criteria:**
- One fewer query per breeding operation
- All breeding tests pass
- Transaction semantics unchanged

---

### Phase 6: Performance Testing & Validation
**Files to create:**
- `TripleDerby.Tests.Performance/BreedingThroughputTests.cs`

**Tasks:**
1. Create load test harness
2. Baseline test: Measure current throughput (350 req/sec)
3. Test after Phase 1: Measure throughput (target: 450-500 req/sec)
4. Test after Phase 2: Measure throughput (target: 550-650 req/sec)
5. Test after all phases: Measure throughput (target: 600-800 req/sec)
6. Measure p50, p95, p99 latencies
7. Monitor CPU, memory, database connection pool

**Acceptance Criteria:**
- Throughput ≥500 req/sec (minimum)
- Throughput ≥700 req/sec (stretch goal)
- p95 latency <100ms
- p99 latency <200ms
- No resource exhaustion

---

### Phase 7: Documentation
**Files to create/update:**
- `docs/features/017-breeding-rabbitmq-performance-optimization.md` (this file)
- `docs/performance/breeding-service-optimization-results.md`

**Tasks:**
1. Document optimization techniques
2. Publish before/after performance metrics
3. Update architecture diagrams
4. Add performance tuning guide

---

## Success Criteria

### Performance Metrics

- [x] **Throughput Recovery**: ≥500 req/sec (from 350 req/sec baseline)
- [ ] **Stretch Goal**: ≥700 req/sec sustained throughput
- [ ] **Latency**: p95 <100ms, p99 <200ms
- [ ] **Database Load**: <50 queries/sec for Color table (from ~350/sec)
- [ ] **Channel Churn**: <1 channel creation/sec (from ~350/sec)

### Functional Validation

- [ ] All existing unit tests pass
- [ ] All existing integration tests pass
- [ ] No breeding data loss under load
- [ ] Publisher confirms working
- [ ] Idempotency maintained

### Non-Functional Validation

- [ ] Memory usage stable (no leaks)
- [ ] CPU usage proportional to load
- [ ] Database connection pool healthy
- [ ] Graceful shutdown without message loss

---

## Expected Performance Gains

| Phase | Optimization | Expected Gain | Cumulative Throughput |
|-------|--------------|---------------|----------------------|
| Baseline | Generic Consumer (current) | - | 350 req/sec |
| Phase 1 | Color Caching | +40-50% | 490-525 req/sec |
| Phase 2 | Channel Pooling | +30-40% | 637-735 req/sec |
| Phase 3 | Prefetch Optimization | +10-20% | 701-882 req/sec |
| Phase 4 | Remove Semaphore | +5-10% | 736-970 req/sec |
| Phase 5 | Remove Redundant Query | +2-5% | 751-1,019 req/sec |

**Conservative Estimate**: 650-750 req/sec (86-114% improvement)
**Optimistic Estimate**: 800-1,000 req/sec (129-186% improvement)

---

## Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Cache invalidation issues | High | Low | Manual invalidation API; colors rarely change |
| Channel pooling thread safety | High | Medium | Comprehensive locking; integration tests |
| Publisher confirms add latency | Medium | High | Acceptable for reliability; tune timeout |
| Prefetch causes memory spike | Medium | Low | Monitor memory; reduce if needed |
| Semaphore removal breaks concurrency | High | Low | QoS already enforces limit; test thoroughly |

---

## Future Enhancements

1. **Multi-Level Caching**: Cache other reference data (LegTypes, etc.)
2. **Batch Publishing**: Publish multiple messages in one channel operation
3. **Connection Pooling**: Multiple RabbitMQ connections for higher throughput
4. **Compiled Queries**: Use EF Core compiled queries for hot paths
5. **Read Replicas**: Route read queries to database replicas
6. **Asynchronous Publishing**: Fire-and-forget publishing with background worker
7. **Metrics Dashboard**: Real-time throughput, latency, and resource monitoring

---

## Monitoring and Observability

### Key Metrics to Track

1. **Throughput**: Messages processed per second
2. **Latency**: p50, p95, p99 processing time
3. **Queue Depth**: Messages waiting in RabbitMQ queue
4. **Database**:
   - Queries per second
   - Connection pool usage
   - Transaction duration
5. **RabbitMQ**:
   - Channel count
   - Connection count
   - Message publish rate
   - Consumer prefetch utilization
6. **Application**:
   - CPU usage
   - Memory usage
   - GC frequency

### Logging

Add structured logging for:
- Color cache hits/misses
- Channel creation events
- Publisher confirm timeouts
- Transaction durations
- Concurrency utilization

---

## References

- [Feature 015: Generic Message Consumers](015-generic-message-consumers.md)
- [RabbitMQ Performance Tuning](https://www.rabbitmq.com/best-practices.html)
- [EF Core Performance](https://learn.microsoft.com/en-us/ef/core/performance/)
- Git comparison: `e2e1130` (old consumer) vs `0c69ebf` (new consumer)
