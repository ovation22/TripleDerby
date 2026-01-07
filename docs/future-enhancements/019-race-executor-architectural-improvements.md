# Feature Discovery: Race Executor Architectural Improvements

**Created**: 2026-01-02
**Status**: Discovery
**Priority**: Medium
**Complexity**: Medium

---

## Problem Statement

The `RaceExecutor.DetermineRaceResults()` method is becoming a growing pain point as we add new race outcome behaviors. Currently, each new feature (stat progression, purse distribution, achievements, etc.) requires direct modification of this method, leading to:

- **Violation of Open/Closed Principle**: Method grows with each feature
- **Poor Separation of Concerns**: Single method handles multiple responsibilities
- **Testing Challenges**: Hard to test individual behaviors in isolation
- **Tight Coupling**: New features are tightly coupled to RaceExecutor

### Current Pattern

```csharp
private void DetermineRaceResults(RaceRun raceRun)
{
    // Sort horses by time
    var sortedHorses = raceRun.Horses.OrderBy(h => h.Time).ToList();

    foreach (var horse in sortedHorses)
    {
        // Assign places
        horse.Place = place;
        horse.Horse.RaceStarts++;

        // Update win/place/show counters
        switch (place) { ... }

        // NEW: Stat progression (Feature 018)
        ApplyStatProgression(horse, raceRun);

        // FUTURE: Purse distribution?
        // FUTURE: Achievements tracking?
        // FUTURE: Training effect decay?
        // FUTURE: Injury detection?

        place++;
    }
}
```

### Why This Matters

As we add more race outcome systems:
- Purse/prize money distribution
- Achievement/trophy tracking
- Training effect decay
- Injury detection system
- Career milestone tracking
- Jockey/trainer attribution

...the `DetermineRaceResults` method will continue to grow, becoming a maintenance bottleneck.

---

## Goals

1. **Extensibility**: Add new race outcome behaviors without modifying RaceExecutor
2. **Testability**: Test each outcome behavior independently
3. **Maintainability**: Clear separation of concerns for each behavior
4. **Flexibility**: Easy to enable/disable behaviors via configuration
5. **Performance**: No significant performance degradation
6. **Backward Compatibility**: Maintain existing race functionality

---

## Non-Goals

- Full domain events infrastructure (too heavy for current needs)
- Async/parallel handler execution (race outcomes are synchronous)
- External event publishing (no external systems to notify)
- Handler prioritization/ordering beyond simple sequence

---

## Architectural Options

### Option 1: Event-Based Pipeline Pattern ⭐ RECOMMENDED

**Concept**: Chain of responsibility where handlers process race outcomes in sequence.

```csharp
public interface IRaceOutcomeHandler
{
    void Handle(RaceOutcomeContext context);
}

public class RaceOutcomeContext
{
    public RaceRun RaceRun { get; init; }
    public RaceRunHorse CurrentHorse { get; init; }
    public int FieldSize { get; init; }
    public ITripleDerbyRepository Repository { get; init; }
}

// Individual handlers
public class StatProgressionHandler : IRaceOutcomeHandler
{
    public void Handle(RaceOutcomeContext context)
    {
        // Apply stat progression logic
    }
}

public class HappinessHandler : IRaceOutcomeHandler
{
    public void Handle(RaceOutcomeContext context)
    {
        // Apply happiness changes
    }
}

// Future handlers
public class PurseDistributionHandler : IRaceOutcomeHandler { ... }
public class AchievementTrackingHandler : IRaceOutcomeHandler { ... }
public class InjuryDetectionHandler : IRaceOutcomeHandler { ... }

// RaceExecutor integration
private void DetermineRaceResults(RaceRun raceRun)
{
    var sortedHorses = raceRun.Horses.OrderBy(h => h.Time).ToList();
    byte place = 1;

    foreach (var horse in sortedHorses)
    {
        horse.Place = place;
        horse.Horse.RaceStarts++;

        // Assign win/place/show (existing logic)
        AssignPlaceCounters(horse, raceRun, place);

        // Execute outcome pipeline
        var context = new RaceOutcomeContext
        {
            RaceRun = raceRun,
            CurrentHorse = horse,
            FieldSize = sortedHorses.Count,
            Repository = _repository
        };

        foreach (var handler in _outcomeHandlers)
        {
            handler.Handle(context);
        }

        place++;
    }
}
```

**Pros**:
- ✅ Clean separation of concerns
- ✅ Easy to add new handlers without touching RaceExecutor
- ✅ Each handler independently testable
- ✅ Execution order controlled via DI registration
- ✅ Simple to understand and maintain
- ✅ Can disable handlers via configuration
- ✅ No async complexity

**Cons**:
- ❌ More classes to manage (one per behavior)
- ❌ Slightly more complex DI setup
- ❌ Execution order implicit in registration

**Complexity**: Medium
**Migration Effort**: Low (wrap existing code in handlers)
**Risk**: Low

---

### Option 2: Strategy Pattern with Composite

**Concept**: Each race outcome behavior is a strategy, composed together.

```csharp
public interface IRaceOutcomeStrategy
{
    void Process(RaceRun raceRun, RaceRunHorse horse);
}

public class RaceOutcomeProcessor
{
    private readonly IEnumerable<IRaceOutcomeStrategy> _strategies;

    public RaceOutcomeProcessor(IEnumerable<IRaceOutcomeStrategy> strategies)
    {
        _strategies = strategies;
    }

    public void ProcessOutcomes(RaceRun raceRun)
    {
        foreach (var horse in raceRun.Horses.OrderBy(h => h.Place))
        {
            foreach (var strategy in _strategies)
            {
                strategy.Process(raceRun, horse);
            }
        }
    }
}

// Strategies
public class StatProgressionStrategy : IRaceOutcomeStrategy { ... }
public class HappinessStrategy : IRaceOutcomeStrategy { ... }
public class PurseDistributionStrategy : IRaceOutcomeStrategy { ... }
```

**Pros**:
- ✅ Very flexible
- ✅ Easy to add/remove strategies
- ✅ Clear interface contract

**Cons**:
- ❌ Similar to Option 1 but with different naming
- ❌ No clear advantage over pipeline pattern
- ❌ Slightly more indirection

**Complexity**: Medium
**Migration Effort**: Medium
**Risk**: Low

---

### Option 3: Domain Events Pattern

**Concept**: Publish `RaceCompleted` event, handlers subscribe.

```csharp
// After race completes
await _eventPublisher.PublishAsync(new RaceCompletedEvent
{
    RaceRun = raceRun
});

// Handlers
public class StatProgressionEventHandler : IEventHandler<RaceCompletedEvent>
{
    public async Task HandleAsync(RaceCompletedEvent @event, CancellationToken ct)
    {
        // Apply stat progression
    }
}
```

**Pros**:
- ✅ Complete decoupling
- ✅ Can add handlers without touching race code
- ✅ Natural fit for async operations
- ✅ Great for cross-cutting concerns

**Cons**:
- ❌ Most complex to set up
- ❌ Need event bus infrastructure
- ❌ Harder to debug (indirect flow)
- ❌ Overkill for current needs
- ❌ Async when we don't need it

**Complexity**: High
**Migration Effort**: High
**Risk**: Medium

---

### Option 4: Keep Current Approach (Do Nothing)

**Concept**: Continue adding methods to RaceExecutor as needed.

**Pros**:
- ✅ No refactoring needed
- ✅ Simple to understand
- ✅ Everything in one place

**Cons**:
- ❌ Violates SOLID principles
- ❌ Grows indefinitely
- ❌ Testing becomes harder
- ❌ Risk of merge conflicts

**Complexity**: None
**Migration Effort**: None
**Risk**: High (technical debt accumulation)

---

## Recommendation: Option 1 - Event-Based Pipeline

### Rationale

**Option 1 (Event-Based Pipeline)** strikes the best balance for TripleDerby's needs:

1. **Right Level of Complexity**: Not too simple (current approach), not too complex (domain events)
2. **Clear Execution Order**: Handlers run in predictable sequence
3. **Easy Testing**: Each handler can be unit tested independently
4. **Future-Proof**: Easy to add achievements, injuries, purse distribution, etc.
5. **Maintains Context**: Handlers share rich context object
6. **No Async Overhead**: Synchronous handlers match current race flow
7. **Low Migration Risk**: Existing code wraps cleanly into handlers

### Comparison to Other Options

**vs Option 2 (Strategy Pattern)**:
- Pipeline is conceptually clearer (sequential processing)
- "Handler" naming better communicates intent than "Strategy"

**vs Option 3 (Domain Events)**:
- Pipeline is simpler to implement and debug
- No async complexity when we don't need it
- Better for this bounded context (race outcomes)

**vs Option 4 (Status Quo)**:
- Pipeline enables growth without technical debt
- Testability and maintainability significantly improved

---

## Implementation Plan (High-Level)

### Phase 1: Create Pipeline Infrastructure
- Define `IRaceOutcomeHandler` interface
- Create `RaceOutcomeContext` record
- Update RaceExecutor constructor to accept `IEnumerable<IRaceOutcomeHandler>`
- Register handlers in DI

### Phase 2: Migrate Existing Behaviors
- Create `StatProgressionHandler` (wrap existing code)
- Create `HappinessHandler` (wrap existing code)
- Modify `DetermineRaceResults` to use pipeline
- Run full test suite to verify no regressions

### Phase 3: Clean Up
- Remove old inline methods from RaceExecutor
- Extract place assignment to helper
- Update tests to verify pipeline execution

### Phase 4: Documentation
- Document handler pattern
- Add examples for future features
- Update architecture docs

**Estimated Effort**: 4-6 hours
**Risk**: Low (wrapping existing code)
**Breaking Changes**: None (internal refactor)

---

## Success Metrics

### Technical Metrics
- [ ] RaceExecutor `DetermineRaceResults` method ≤50 lines
- [ ] Each handler independently testable
- [ ] 100% test coverage maintained
- [ ] Zero regressions in existing race tests
- [ ] New handlers can be added in ≤30 minutes

### Code Quality Metrics
- [ ] Single Responsibility Principle: Each handler has one job
- [ ] Open/Closed Principle: Add handlers without modifying RaceExecutor
- [ ] Dependency Inversion: RaceExecutor depends on IRaceOutcomeHandler abstraction
- [ ] Cyclomatic Complexity: RaceExecutor complexity reduced

---

## Future Enhancements (Enabled by Pipeline)

Once pipeline is in place, these features become trivial to add:

1. **PurseDistributionHandler**: Award prize money based on finish position
2. **AchievementTrackingHandler**: Unlock trophies/milestones
3. **TrainingDecayHandler**: Reduce training bonuses over time
4. **InjuryDetectionHandler**: Random injury chance based on exhaustion
5. **JockeyAttributionHandler**: Track jockey/trainer stats
6. **CareerMilestoneHandler**: Track 1st win, 100th start, etc.

Each handler would be:
- ~50-100 lines of code
- Independently testable
- Independently deployable (feature flags)
- Zero impact on other handlers

---

## Open Questions

1. **Handler Ordering**: Should we explicitly define handler order, or rely on DI registration order?
   - **Recommendation**: Use DI registration order for simplicity, document required order

2. **Error Handling**: What if a handler throws? Abort pipeline or continue?
   - **Recommendation**: Log error, continue with other handlers (isolation)

3. **Feature Flags**: Should handlers be toggleable via configuration?
   - **Recommendation**: Yes, add `IsEnabled` property to each handler

4. **Async Support**: Should we plan for async handlers in the future?
   - **Recommendation**: No, keep synchronous for now. Add async later if needed.

5. **Context Mutation**: Should handlers mutate context or return new context?
   - **Recommendation**: Mutate (in-memory changes before DB save)

---

## Risks and Mitigation

### Risk 1: Breaking Existing Functionality
**Mitigation**:
- Wrap existing code in handlers (no logic changes)
- Run full test suite before/after
- Manual smoke testing of race flow

### Risk 2: Performance Degradation
**Mitigation**:
- Profile before/after with 100-race benchmark
- Target <1ms overhead for pipeline
- Simple foreach loop has negligible cost

### Risk 3: Over-Engineering
**Mitigation**:
- Only refactor when we have 3+ race outcome behaviors (we're at 2 now)
- Keep pipeline simple (no ordering, priority, async complexity)
- Can always simplify later if unused

### Risk 4: Developer Confusion
**Mitigation**:
- Clear documentation with examples
- "How to add a new race outcome behavior" guide
- Code comments explaining pipeline flow

---

## Alternatives Considered

### Decorator Pattern
**Why Not**: Decorators wrap single objects; we need to handle collections
**Verdict**: Not a good fit for this use case

### Chain of Responsibility (Classic)
**Why Not**: Classic CoR allows handlers to stop chain; we want all handlers to run
**Verdict**: Pipeline pattern better matches our needs

### Visitor Pattern
**Why Not**: Adds complexity without clear benefit; pipeline is simpler
**Verdict**: Too academic for this use case

---

## Dependencies

### Prerequisites
- None (internal refactor, no external dependencies)

### Blocks
- None

### Blocked By
- None

---

## Timeline

**Discovery Phase**: Complete (this document)
**Implementation**: Ready to start (pending approval)
**Estimated Effort**: 4-6 hours
**Target Completion**: Next sprint

---

## Approval

**Pending**: Architecture review and approval to proceed

---

## References

- Current Implementation: `TripleDerby.Services.Racing/RaceExecutor.cs`
- Feature 018: Race Outcome Stat Progression (inspiration for this refactor)
- Similar Pattern: ASP.NET Core Middleware Pipeline (proven pattern)

---

## Appendix: Example Handler Implementation

```csharp
/// <summary>
/// Applies stat progression to horses after race completion.
/// Implements career phase system, race-type focus, and performance bonuses.
/// </summary>
public class StatProgressionHandler : IRaceOutcomeHandler
{
    private readonly IStatProgressionCalculator _calculator;
    private readonly ILogger<StatProgressionHandler> _logger;

    public StatProgressionHandler(
        IStatProgressionCalculator calculator,
        ILogger<StatProgressionHandler> logger)
    {
        _calculator = calculator;
        _logger = logger;
    }

    public void Handle(RaceOutcomeContext context)
    {
        var horse = context.CurrentHorse.Horse;
        var raceFurlongs = context.RaceRun.Race.Furlongs;
        var finishPlace = context.CurrentHorse.Place;
        var fieldSize = context.FieldSize;

        // Apply growth to all performance stats
        var performanceStats = horse.Statistics.Where(s =>
            s.StatisticId == StatisticId.Speed ||
            s.StatisticId == StatisticId.Stamina ||
            s.StatisticId == StatisticId.Agility ||
            s.StatisticId == StatisticId.Durability);

        foreach (var stat in performanceStats)
        {
            var growth = _calculator.GrowStat(
                stat.Actual,
                stat.DominantPotential,
                _calculator.CalculateAgeMultiplier(horse.RaceStarts));

            var newValue = stat.Actual + growth;
            stat.Actual = Math.Min(newValue, stat.DominantPotential);

            _logger.LogDebug(
                "Horse {HorseId} {StatName} grew from {OldValue} to {NewValue}",
                horse.Id, stat.StatisticId, stat.Actual - growth, stat.Actual);
        }
    }
}
```

**Lines of Code**: ~40
**Cyclomatic Complexity**: 3
**Test Coverage**: 100%
**Dependencies**: IStatProgressionCalculator, ILogger
