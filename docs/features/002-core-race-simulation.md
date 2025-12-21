# Core Race Simulation

**Feature Number:** 002

**Status:** Implemented

**Parent:** [001-race-engine](001-race-engine.md)

## Actors & Goals

| Actor | Goal |
|-------|------|
| **Player** | Enter a horse in a race and watch it compete against a field of similar-experience horses to determine placement |
| **System (Matchmaker)** | Assemble a competitive field of 8-12 horses with similar race counts |
| **System (Race Engine)** | Simulate the race tick-by-tick using horse stats, track conditions, and stamina to determine finishing order |

## Observable Behaviors

| # | Behavior |
|---|----------|
| **B1** | Player selects a race and horse; System finds 7-11 CPU horses with similar race counts and creates a field of 8-12 competitors |
| **B2** | System assigns each horse to a lane (1-12) and generates a random track condition (Fast, Good, Muddy, Sloppy) for the race |
| **B3** | System simulates the race tick-by-tick, calculating each horse's distance based on speed, stats, leg type, lane, surface, and condition modifiers |
| **B4** | Stamina depletes each tick based on effort and conditions; low stamina reduces horse speed progressively |
| **B5** | System determines final placements when all horses cross the finish line and records finishing order with times |

## Sizing Assessment

**Outcome:** Right-sized

**Verifications:**
1. **Deferral Check:** B4 (Stamina depletion) deferred with placeholder method - horses run at constant speed initially
2. **Capability Check:** Remaining behaviors (B1, B2, B3, B5) confirmed as single cohesive capability

## Thinnest Valuable Slice

**Selected:** B3 (Tick simulation)

| Criteria | Assessment |
|----------|------------|
| Validates core value proposition | Yes - tick-by-tick movement calculation IS the race engine |
| Forces critical contracts | Yes - requires Horse stats interface, Track/Surface model, Position tracking, Tick loop structure |
| Can everything else be faked | Yes - B1 can use hardcoded test horses, B2 can assign sequential lanes + "Good" condition, B5 just reads final positions |

## Minimal Implementation

**Existing Service:** `RaceService.Race(raceId, horseId)` at `TripleDerby.Core\Services\RaceService.cs:25`

### What Already Exists

| Component | Status | Location |
|-----------|--------|----------|
| Tick-based simulation loop | Done | Lines 53-99 |
| Speed modifiers (condition, surface, lane, leg type) | Done | Lines 184-317 |
| Position tracking per tick | Done | Lines 67-87 |
| Stamina depletion | Commented out | Lines 171-175 (already deferred) |
| Random condition generation | Done | Lines 510-514 |
| Overtaking/lane change logic | Done | Lines 390-455 |

### What Needs to Be Added

| Component | Description |
|-----------|-------------|
| **CPU Horse Selection** | Use `ISpecification<Horse>` to find 7-11 horses with similar race counts (line 44 only has player's horse) |
| **Lane Assignment** | Assign lanes 1-12 to field at initialization (currently not assigned) |
| **Final Placements** | Uncomment/implement `DetermineRaceResults()` to set finish order |
| **Result Mapping** | Replace TODOs in `RaceRunResult` (lines 111-122) |

### Specification for Horse Selection

```csharp
public class SimilarRaceCountSpecification : Specification<Horse>
{
    public SimilarRaceCountSpecification(int targetRaceCount, int tolerance = 2, int limit = 11)
    // Returns horses where RaceCount is within +/- tolerance of target, excluding player's horse
}
```

## Deferred Items

| Item | Reason | Placeholder |
|------|--------|-------------|
| B4 - Stamina Depletion | Simplify initial implementation; races can run without fatigue mechanics | `CalculateStamina()` returns 100% / no-op |

## Implementation Considerations

### Addressed Concerns

#### Error Handling
- **Insufficient horses:** Allow smaller field (minimum 2 horses) rather than failing
- Widen race count tolerance progressively if needed, then fill with any available
- Existing safeguards retained: race not found, horse not found, infinite loop protection

#### Data Validation
- **Minimum field size:** 2 horses (player + at least 1 opponent)
- **Preferred field size:** 4+ horses for competitive racing
- **Target field size:** 8-12 horses when available

### Skipped Concerns

| Concern | Why Not Applicable |
|---------|-------------------|
| UX/Design | Pure domain logic - no UI |
| Security | No direct user input beyond validated IDs |
| Accessibility | No visual or interactive UI |
| Observability | Existing RaceRunTick records sufficient |

## Implementation Summary

**Status:** Implemented
**Completed:** 2025-12-18

### Files Created/Modified
- `TripleDerby.Core/Specifications/SimilarRaceStartsSpecification.cs` - Finds CPU horses with similar race experience
- `TripleDerby.Core/Entities/RaceRunHorse.cs` - Added Place property
- `TripleDerby.Core/Services/RaceService.cs` - CPU horse selection, lane assignment, finish order, result mapping
- `TripleDerby.Tests.Unit/Specifications/SimilarRaceStartsSpecificationTests.cs` - Specification tests
- `TripleDerby.Tests.Unit/Services/RaceServiceTests.cs` - Service integration tests

### Test Coverage
- [x] SimilarRaceStartsSpecification filters horses within tolerance
- [x] SimilarRaceStartsSpecification excludes retired horses
- [x] SimilarRaceStartsSpecification limits results
- [x] Lane assignment (sequential lanes 1-N)
- [x] DetermineRaceResults assigns places based on distance
- [x] Result mapping with race/track/surface names
- [x] Result mapping with horse results
- [x] CPU horse integration (fetches and adds to field)
- [x] Multi-horse place assignment
- [x] Graceful handling when no CPU horses available

### Notes
- Stamina depletion (B4) remains deferred with placeholder - horses run at constant speed
- Payout set to 0 - handled by Purse Distribution sub-feature (001b)
- Projects updated to .NET 10

## Related Decisions

No architectural decisions required - implementation uses existing patterns:
- **Specification Pattern:** Ardalis.Specification (see `HorseRandomRacerSpecification` as template)
- **Test Framework:** xUnit + Moq
- **Service Pattern:** Existing `RaceService` with repository injection
