# Race Modifiers Refactor - Feature Specification

**Feature Number:** 003

**Status:** Implemented

## Overview
Redesign the RaceService modifier system to be simpler, more maintainable, less buggy, and easier to balance.

## Current Problems

### 1. Architecture Issues
- **Inconsistent patterns**: Mix of in-place modifications (`*=`) and return values
- **Poor separation**: Speed, stamina, and random modifiers intermingled
- **Order dependency**: Sequential application with unclear intentionality
- **Hard to test**: Interdependent modifiers difficult to isolate

### 2. Code Quality Issues
- **Magic numbers**: Hardcoded values (1.02, 5000.0) without explanation
- **Duplicate logic**: LegType affects speed in 2 places (RaceService.cs:217, RaceService.cs:223)
- **Commented code**: 200+ lines of disabled stamina logic
- **Mixed scaling**: Linear, percentage-based, and logarithmic approaches

### 3. Balance Issues
- **Unclear impact**: Difficult to reason about actual effect of each modifier
- **Compounding effects**: Unclear if modifiers stack correctly
- **Inconsistent ranges**: Some modifiers ±1%, others ±5%, with no clear rationale

## Design Goals

### Primary Objectives
1. **Simplicity**: Easy to understand what each modifier does
2. **Maintainability**: Easy to add/modify/remove modifiers
3. **Correctness**: No duplicate applications or order dependencies
4. **Testability**: Each modifier independently testable
5. **Balance**: Clear, documented effect of each modifier

### Secondary Objectives
1. **Performance**: Efficient calculation (minimal overhead)
2. **Extensibility**: Support future features (stamina, traffic, weather)
3. **Debuggability**: Easy to trace why a horse performed a certain way
4. **Configurability**: Game balance adjustments without code changes

## Proposed Architecture

### Core Concept: Modifier Pipeline with Grouped Categories

#### Category 1: Base Speed
- Calculated from race configuration and horse stats
- **Input**: Race configuration (furlongs, target time)
- **Output**: Base speed in furlongs/tick

#### Category 2: Horse Stat Modifiers (Multiplicative)
- Direct stat-based multipliers
- **Applied**: Speed, Agility
- **Formula**: Linear scaling around neutral point (50)
- **Range**: ±2% per stat (Speed 0-100 = 0.90x to 1.10x)

#### Category 3: Environmental Modifiers (Multiplicative)
- Track and race condition effects
- **Applied**: Surface, Condition
- **Formula**: Fixed multipliers per enum value
- **Range**: ±3% typical, ±5% extreme conditions

#### Category 4: Phase Modifiers (Multiplicative)
- Time-based effects during race
- **Applied**: Leg Type timing bonuses
- **Formula**: Conditional based on race progress
- **Range**: ±2% during active phase

#### Category 5: Random Variance (Multiplicative)
- Performance fluctuation
- **Formula**: Uniform distribution
- **Range**: ±1% per tick

#### Final Calculation
```
finalSpeed = baseSpeed
  × statModifiers
  × environmentalModifiers
  × phaseModifiers
  × randomVariance
```

### Modifier Configuration Structure

```csharp
public static class RaceModifierConfig
{
    // Base speed configuration
    public const double TargetTicksFor10Furlongs = 237.0;

    // Stat modifier ranges (per point from neutral 50)
    public const double SpeedModifierPerPoint = 0.002;   // ±10% total range
    public const double AgilityModifierPerPoint = 0.001; // ±5% total range

    // Environmental modifiers
    public static readonly IReadOnlyDictionary<SurfaceId, double> SurfaceModifiers;
    public static readonly IReadOnlyDictionary<ConditionId, double> ConditionModifiers;

    // Phase modifiers
    public static readonly IReadOnlyDictionary<LegTypeId, PhaseModifier> LegTypePhaseModifiers;

    // Random variance
    public const double RandomVarianceRange = 0.01; // ±1%
}
```

## Detailed Modifier Specifications

### Base Speed
**Purpose**: Calculate furlongs/tick to complete race in target time

**Formula**:
```
baseSpeed = raceFurlongs / targetTicks
targetTicks = (raceFurlongs / 10) * TargetTicksFor10Furlongs
```

**Configuration**:
- `TargetTicksFor10Furlongs = 237` (matches current implementation)

---

### Stat Modifiers

#### Speed Modifier
**Purpose**: Faster horses move more per tick

**Current Issues**:
- Uses complex formula: `1 + ((speed - 50) / 1000.0)`
- Unclear scaling (±5% for 0-100 range)

**New Design**:
```
speedMultiplier = 1.0 + ((speed - 50) * SpeedModifierPerPoint)
```

**Configuration**:
- `SpeedModifierPerPoint = 0.002`
- Neutral point: Speed = 50 → 1.0x
- Range: Speed 0 → 0.90x, Speed 100 → 1.10x
- Effect: ±10% total range

**Example**:
- Speed 40 → 0.98x (2% slower)
- Speed 70 → 1.04x (4% faster)

#### Agility Modifier
**Purpose**: Agile horses maintain better form/efficiency

**Current Issues**:
- Uses: `1 + ((agility - 50) / 5000.0)`
- Effect is ±1%, very subtle

**New Design**:
```
agilityMultiplier = 1.0 + ((agility - 50) * AgilityModifierPerPoint)
```

**Configuration**:
- `AgilityModifierPerPoint = 0.001`
- Range: Agility 0 → 0.95x, Agility 100 → 1.05x
- Effect: ±5% total range

#### Happiness Modifier (Future)
**Purpose**: Happy horses perform better

**Current Issues**:
- Uses logarithmic scaling: `Math.Log(1.0 + Math.Abs(normalizedHappiness)) / 5000.0`
- Overly complex for minimal effect

**New Design** (when re-enabled):
```
happinessMultiplier = 1.0 + ((happiness - 50) * HappinessModifierPerPoint)
```

**Configuration**:
- `HappinessModifierPerPoint = 0.0005`
- Range: ±2.5% total
- **Decision**: Keep linear for simplicity, or remove if effect is negligible

---

### Environmental Modifiers

#### Surface Modifier
**Purpose**: Different surfaces favor different speeds

**Current Values**:
- Dirt: 0.98x
- Turf: 1.02x
- Artificial: 1.03x

**New Design**:
- Keep similar values, but make configurable
- Proposed rebalance:
  - **Dirt**: 1.00x (neutral, most common)
  - **Turf**: 1.02x (faster, favors speed horses)
  - **Artificial**: 1.01x (consistent, less variance)

#### Condition Modifier
**Purpose**: Weather/track condition affects speed

**Current Issues**:
- 11 different conditions with scattered values
- Unclear grouping or progression

**New Design** - Grouped by category:

**Dry/Fast Conditions** (faster):
- Fast: 1.03x (ideal conditions)
- Firm: 1.02x
- Good: 1.00x (neutral baseline)

**Wet Conditions** (slower):
- WetFast: 0.99x
- Soft: 0.98x
- Yielding: 0.97x
- Muddy: 0.96x
- Sloppy: 0.95x

**Extreme Conditions** (much slower):
- Heavy: 0.93x
- Frozen: 0.92x
- Slow: 0.90x

**Rationale**: Clear progression from ideal→extreme

#### Lane Position Modifier
**Purpose**: Certain leg types favor certain lanes

**Current Issues**:
- Combined with leg type in complex logic
- Has random variance baked in (`±0.0001`)
- Applied in `AdjustSpeedForLaneAndLegType` method

**New Design** - **Remove Lane Position Speed Modifier**

**Rationale**:
1. Lane position effects are already handled by:
   - Rail position in curves (geometric, not a speed bonus)
   - Traffic interference (separate system)
2. Combining lane + leg type creates 5×8 = 40 combinations (too complex)
3. Effect is minimal (±0.8%) and has random component (defeats purpose)
4. Leg types should care about race *phase*, not physical *lane*

**Replacement**:
- Keep lane assignment based on leg type preference (rails vs outside)
- Model actual race physics (rail is shorter distance, but more traffic)
- Handle via traffic/overtaking system (future feature)

---

### Phase Modifiers

#### Leg Type Phase Modifiers
**Purpose**: Different running styles peak at different race phases

**Current Issues**:
- Applied in separate method `AdjustSpeedForLegTypeDuringRace`
- Also affects lane position (duplicate concern)
- Uses hardcoded tick thresholds (0.25, 0.75, etc.)
- Also has late-race random boost for all horses

**New Design** - Clean separation:

**StartDash** (early burst):
- Phase: First 25% of race
- Modifier: 1.04x
- Concept: Explodes from gate, fades later

**FrontRunner** (early leader):
- Phase: First 20% of race
- Modifier: 1.03x
- Concept: Sets the pace early

**StretchRunner** (mid-race):
- Phase: 40%-60% of race
- Modifier: 1.03x
- Concept: Strong in the middle stretch

**LastSpurt** (late closer):
- Phase: Last 25% of race
- Modifier: 1.04x
- Concept: Saves energy for final push

**RailRunner** (tactical):
- Phase: Last 30% of race
- Modifier: 1.02x
- Concept: Uses rail efficiency for late run

**Decision on Late-Race Random Boost**:
- **Current**: All horses get random 0-1% boost after 80% complete
- **Recommendation**: **Remove** - use consistent random variance instead
- **Rationale**: Predictable performance > arbitrary late chaos

---

### Random Variance

**Purpose**: Simulate tick-to-tick performance fluctuation

**Current Implementation**:
- `ApplyRandomPerformanceFluctuations`: ±1% per tick
- `ApplyRandomIncidents`: 2-8% chance of ±5% to +15% modifier (disabled)
- Late-race boost: ±1% after 80% (in phase modifiers)
- Lane position variance: ±0.0001 (in lane logic)

**Issues**:
- Random modifiers scattered across multiple methods
- Inconsistent ranges and purposes
- Some disabled, some active, unclear which matters

**New Design**:

**Tick Variance** (always applied):
```
varianceMultiplier = 1.0 + RandomRange(-0.01, 0.01)
```
- Effect: ±1% per tick
- Purpose: Natural performance fluctuation

**Incidents** (disabled for now, future feature):
- **Keep commented code** but organize it separately
- Plan for future: Major events (stumble, traffic, surge) as discrete events
- Not part of base speed calculation

**Decision**:
- Remove all other random components
- Single, consistent ±1% variance is sufficient
- Major randomness comes from stat distribution, not modifiers

---

## Implementation Plan

### Phase 1: Setup (Preparation)
**Goal**: Prepare infrastructure without breaking current functionality

**Tasks**:
1. Create `RaceModifierConfig` static class with all configuration constants
2. Create `ModifierContext` record to pass race state to modifiers
3. Create `SpeedModifierCalculator` class (new, not yet used)
4. Add unit test project for modifier calculations
5. Document existing modifier behavior with tests (characterization tests)

**Deliverable**: New classes exist, old code unchanged and still working

---

### Phase 2: Refactor Stat Modifiers
**Goal**: Replace stat-based modifiers with new implementation

**Tasks**:
1. Implement `CalculateStatModifiers(Horse)` in `SpeedModifierCalculator`
   - Speed modifier (linear scaling)
   - Agility modifier (linear scaling)
2. Write unit tests for stat modifiers
3. Replace `ApplySpeedModifier` and `GetAgilityModifier` calls in `UpdateHorsePosition`
4. Remove `GetHappinessModifier` call (disable happiness for now)
5. Test race outcomes match expected behavior

**Deliverable**: Stat modifiers use new system, races still work

---

### Phase 3: Refactor Environmental Modifiers
**Goal**: Replace environment-based modifiers with new implementation

**Tasks**:
1. Implement `CalculateEnvironmentalModifiers(RaceContext)` in `SpeedModifierCalculator`
   - Surface modifier (from config)
   - Condition modifier (from config)
2. Populate modifier config dictionaries
3. Write unit tests for environmental modifiers
4. Replace `AdjustSpeedForCondition` and `AdjustSpeedForSurface` calls
5. **Remove** `AdjustSpeedForLaneAndLegType` call
6. Test race outcomes

**Deliverable**: Environmental modifiers use new system

---

### Phase 4: Refactor Phase Modifiers
**Goal**: Replace phase-based modifiers with new implementation

**Tasks**:
1. Create `PhaseModifier` record (startPercent, endPercent, multiplier)
2. Populate leg type phase config
3. Implement `CalculatePhaseModifiers(LegTypeId, currentTick, totalTicks)`
4. Write unit tests for phase modifiers
5. Replace `AdjustSpeedForLegTypeDuringRace` call
6. Remove late-race random boost logic
7. Test race outcomes

**Deliverable**: Phase modifiers use new system

---

### Phase 5: Consolidate Random Variance
**Goal**: Single, consistent random variance

**Tasks**:
1. Implement `ApplyRandomVariance()` in `SpeedModifierCalculator`
2. Replace `ApplyRandomPerformanceFluctuations` call
3. Remove random variance from lane position logic
4. Test race variance is appropriate

**Deliverable**: Random variance consolidated

---

### Phase 6: Cleanup & Organization
**Goal**: Remove old code, organize for maintainability

**Tasks**:
1. Delete old modifier methods:
   - `ApplySpeedModifier`
   - `GetAgilityModifier`
   - `GetHappinessModifier`
   - `AdjustSpeedForCondition`
   - `AdjustSpeedForLaneAndLegType`
   - `AdjustSpeedForSurface`
   - `AdjustSpeedForLegTypeDuringRace`
   - `ApplyRandomPerformanceFluctuations`
2. Move all commented stamina code to separate region/file
3. Document `SpeedModifierCalculator` with XML comments
4. Update `UpdateHorsePosition` to use new calculator cleanly
5. Code review and cleanup

**Deliverable**: Clean, maintainable modifier system

---

### Phase 7: Rebalancing (Optional)
**Goal**: Adjust modifier values for better gameplay

**Tasks**:
1. Run races with various configurations
2. Analyze impact of each modifier type
3. Adjust configuration values based on testing
4. Document final balance decisions

**Deliverable**: Balanced, tested modifier values

---

## Testing Strategy

### Unit Tests
- Each modifier calculation independently tested
- Edge cases (min/max stat values, boundary phases)
- Configuration validation

### Integration Tests
- Full race simulation with known inputs
- Verify modifier stacking works correctly
- Regression tests for existing behavior

### Balance Tests
- Statistical analysis of race outcomes
- Verify modifiers have intended impact
- Ensure no single modifier dominates

---

## Success Criteria

### Code Quality
- ✅ No magic numbers (all values in config)
- ✅ No duplicate logic (single source of truth)
- ✅ No commented-out code in main logic
- ✅ Consistent patterns (all modifiers use same approach)
- ✅ Clear separation of concerns

### Maintainability
- ✅ Easy to add new modifier
- ✅ Easy to adjust values without code changes
- ✅ Each modifier testable in isolation
- ✅ Clear documentation of each modifier's purpose

### Correctness
- ✅ No order dependencies
- ✅ No unintended compounding
- ✅ Predictable behavior
- ✅ All modifiers applied exactly once

### Balance
- ✅ Clear range documentation (min/max effect)
- ✅ Reasonable total variance (not too random, not too deterministic)
- ✅ Each stat/factor matters
- ✅ No dominant strategy

---

## Future Enhancements

### Stamina System (Post-Refactor)
- Re-enable stamina consumption
- Apply same modifier pattern
- Separate speed vs stamina modifiers clearly
- Stamina affects speed in late race

### Traffic & Overtaking
- Model physical interactions between horses
- Lane change logic
- Blocking and drafting effects

### Incidents & Events
- Stumbles, surges, breaks
- Jockey skill effects
- Weather changes mid-race

### Advanced Configuration
- Per-race type modifiers (stakes vs claiming)
- Track-specific characteristics
- Age/experience modifiers

---

## Open Questions

1. **Happiness Modifier**: Remove entirely or keep with linear scaling?
   - **Recommendation**: Disable for now, re-evaluate when game loop established

2. **Lane Position**: Keep assignment logic but remove speed modifier?
   - **Recommendation**: Yes, lanes matter for traffic/distance, not speed

3. **Stat Ranges**: Current stats 0-100, are all values realistic?
   - **Recommendation**: Keep 0-100, but most horses should be 30-70 range

4. **Random Variance**: ±1% enough? Too much?
   - **Recommendation**: Start with ±1%, tune based on playtesting

5. **Modifier Stacking**: Should some modifiers be additive before multiplicative?
   - **Recommendation**: All multiplicative is simpler, use for v1

---

## Appendix: Modifier Quick Reference

### Current vs. New Modifier Comparison

| Modifier | Current Range | New Range | Change |
|----------|--------------|-----------|--------|
| Speed Stat | ±5% | ±10% | Increased impact |
| Agility Stat | ±1% | ±5% | Increased impact |
| Happiness | ±0.08% (log) | Disabled | Removed complexity |
| Surface | -2% to +3% | 0% to +2% | Rebalanced |
| Condition | -5% to +2% | -10% to +3% | Increased range |
| Lane+LegType | ±0.8% | Removed | Eliminated |
| Phase (LegType) | +1.5% to +2% | +2% to +4% | Increased impact |
| Random Tick | ±1% | ±1% | Unchanged |
| Late Boost | ±1% random | Removed | Eliminated |
| Random Incidents | ±5% to +15% | Disabled | Moved to future |

### Total Variance Estimate

**Best Case Horse** (Speed 100, Agility 100, Fast/Turf, LegType in phase):
- Stats: 1.10 × 1.05 = 1.155
- Environment: 1.03 × 1.02 = 1.051
- Phase: 1.04
- Random: ~1.00 (average)
- **Total: ~1.26x base speed**

**Worst Case Horse** (Speed 0, Agility 0, Slow/Dirt, LegType out of phase):
- Stats: 0.90 × 0.95 = 0.855
- Environment: 0.90 × 1.00 = 0.90
- Phase: 1.00
- Random: ~1.00 (average)
- **Total: ~0.77x base speed**

**Variance**: 1.26 / 0.77 = ~1.64x difference (64% faster)

**Assessment**: Reasonable variance - stats and conditions matter, but not overwhelming.

---

## Document History

- **2025-12-19**: Initial specification created during discovery phase
- **Author**: Claude Code (Sonnet 4.5)
- **Status**: Draft - Pending User Approval
