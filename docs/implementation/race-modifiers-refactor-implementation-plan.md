# Race Modifiers Refactor - Implementation Plan

## Overview
This document breaks down the [003-race-modifiers-refactor feature specification](../features/003-race-modifiers-refactor.md) into concrete, testable implementation phases following TDD principles with vertical slices.

**Goal**: Redesign the RaceService modifier system to be simpler, more maintainable, less buggy, and easier to balance.

**Approach**: Incremental refactoring with each phase delivering working, tested functionality. Replace the existing scattered modifier logic with a clean, configurable pipeline.

## Implementation Strategy

### TDD Vertical Slices
Each phase follows the Red-Green-Refactor cycle:
1. **RED**: Write failing tests that define expected behavior
2. **GREEN**: Implement minimum code to make tests pass
3. **REFACTOR**: Clean up, extract, organize for maintainability

### Risk Mitigation
- Phase 1 establishes infrastructure without breaking existing code
- Each subsequent phase replaces one category of modifiers
- Tests ensure behavior remains correct throughout refactoring
- Configuration values match or improve upon existing balance

### Success Metrics
- ✅ All tests pass after each phase
- ✅ No magic numbers (all values in config)
- ✅ Each modifier independently testable
- ✅ Races complete successfully with expected variance

---

## Phase 1: Infrastructure Setup

**Goal**: Create configuration, calculator class, and test infrastructure without changing RaceService behavior

**Vertical Slice**: New modifier infrastructure exists alongside old code, can be unit tested independently

**Estimated Complexity**: Medium
**Risks**: None - purely additive work

### RED - Write Failing Tests

- [ ] Test: `RaceModifierConfig` has correct constant values for base speed
- [ ] Test: `RaceModifierConfig` has stat modifier configuration (Speed, Agility per-point values)
- [ ] Test: `RaceModifierConfig` has environmental modifier dictionaries (Surface, Condition)
- [ ] Test: `RaceModifierConfig` has phase modifier configuration (LegType timing and multipliers)
- [ ] Test: `RaceModifierConfig` has random variance configuration
- [ ] Test: `SpeedModifierCalculator` can be instantiated with config and random generator
- [ ] Test: `ModifierContext` record correctly holds race state (tick, totalTicks, horse stats, race conditions)

**Why these tests**: Define the configuration structure and infrastructure components that will replace scattered magic numbers.

### GREEN - Make Tests Pass

- [ ] Create `TripleDerby.Core/Configuration/RaceModifierConfig.cs` static class
  - Add base speed constants (TargetTicksFor10Furlongs = 237.0)
  - Add stat modifier constants (SpeedModifierPerPoint = 0.002, AgilityModifierPerPoint = 0.001)
  - Add random variance constant (RandomVarianceRange = 0.01)
  - Create empty dictionaries for Surface and Condition modifiers (populated later)
  - Create empty dictionary for LegType phase modifiers (populated later)

- [ ] Create `TripleDerby.Core/Racing/ModifierContext.cs` record
  - Fields: currentTick, totalTicks, horse, raceCondition, raceSurface, raceFurlongs
  - Constructor for easy instantiation

- [ ] Create `TripleDerby.Core/Racing/SpeedModifierCalculator.cs` class
  - Constructor taking IRandomGenerator dependency
  - Empty methods: CalculateStatModifiers, CalculateEnvironmentalModifiers, CalculatePhaseModifiers, ApplyRandomVariance
  - Methods return 1.0 (neutral) for now

- [ ] Create test file `TripleDerby.Tests.Unit/Racing/RaceModifierConfigTests.cs`
  - Tests for constant values
  - Tests for config structure

- [ ] Create test file `TripleDerby.Tests.Unit/Racing/SpeedModifierCalculatorTests.cs`
  - Tests for calculator instantiation
  - Tests for ModifierContext creation

**Implementation notes**:
- Place config in new `/Configuration` folder for organization
- Place racing logic in new `/Racing` folder (separate from Services)
- Use records for immutable context data (C# 9+ feature)
- Don't modify RaceService.cs yet - this is pure infrastructure

### REFACTOR - Clean Up

- [ ] Add XML documentation comments to `RaceModifierConfig` explaining each constant's purpose and range
- [ ] Add XML documentation to `SpeedModifierCalculator` explaining the modifier pipeline
- [ ] Ensure consistent naming conventions (e.g., "Modifier" vs "Multiplier")
- [ ] Add TODO comments noting which phase will populate empty dictionaries

**Acceptance Criteria**:
- [x] All Phase 1 tests pass
- [x] New classes compile without errors
- [x] RaceService.cs unchanged and all existing tests still pass
- [x] Code coverage for new infrastructure > 90%
- [x] Configuration values documented with purpose and expected range

**Deliverable**: Infrastructure classes exist, are tested, and ready to use. RaceService still works with old modifier logic.

---

## Phase 2: Stat Modifiers (Speed & Agility)

**Goal**: Replace stat-based modifier methods with new calculator implementation

**Vertical Slice**: Speed and Agility modifiers use new system, races complete with expected outcomes

**Estimated Complexity**: Medium
**Risks**: Changing core speed calculation - must verify race outcomes don't change drastically

### RED - Write Failing Tests

- [ ] Test: `CalculateStatModifiers` with Speed=50 returns 1.0 (neutral)
- [ ] Test: `CalculateStatModifiers` with Speed=0 returns 0.90 (-10%)
- [ ] Test: `CalculateStatModifiers` with Speed=100 returns 1.10 (+10%)
- [ ] Test: `CalculateStatModifiers` with Speed=70 returns 1.04 (+4%)
- [ ] Test: `CalculateStatModifiers` with Agility=50 returns 1.0 (neutral)
- [ ] Test: `CalculateStatModifiers` with Agility=0 returns 0.95 (-5%)
- [ ] Test: `CalculateStatModifiers` with Agility=100 returns 1.05 (+5%)
- [ ] Test: `CalculateStatModifiers` with Speed=80, Agility=60 returns combined multiplier 1.06 * 1.01 = 1.0706
- [ ] Test: Integration test - horse with Speed=80 finishes faster than horse with Speed=40

**Why these tests**: Verify the new linear scaling formula produces correct modifiers across the full stat range and modifiers combine correctly.

### GREEN - Make Tests Pass

- [ ] Implement `CalculateStatModifiers(ModifierContext context)` in `SpeedModifierCalculator`
  ```csharp
  public double CalculateStatModifiers(ModifierContext context)
  {
      var speedMultiplier = 1.0 + ((context.horse.Speed - 50) * RaceModifierConfig.SpeedModifierPerPoint);
      var agilityMultiplier = 1.0 + ((context.horse.Agility - 50) * RaceModifierConfig.AgilityModifierPerPoint);
      return speedMultiplier * agilityMultiplier;
  }
  ```

- [ ] Update `UpdateHorsePosition` in `RaceService.cs` to use new calculator:
  - Instantiate `SpeedModifierCalculator` as class field
  - Create `ModifierContext` from current race state
  - Replace `ApplySpeedModifier(baseSpeed, horse.Speed)` call with `calculator.CalculateStatModifiers(context)`
  - Replace `GetAgilityModifier(horse.Agility)` call - now included in CalculateStatModifiers
  - Comment out `GetHappinessModifier` call (disable happiness for now per spec)

- [ ] Add integration test to `RaceServiceTests.cs`:
  - Create two horses with different Speed stats
  - Run race
  - Verify faster horse finishes in less time

**Implementation notes**:
- New formula: `1.0 + ((stat - 50) * perPointValue)` is simpler than existing formulas
- Speed: old was `1 + ((speed - 50) / 1000.0)` = ±5%, new is ±10% (intentional increase)
- Agility: old was `1 + ((agility - 50) / 5000.0)` = ±1%, new is ±5% (intentional increase)
- These increases make stats more impactful per spec design goals

### REFACTOR - Clean Up

- [ ] Mark `ApplySpeedModifier` method as `[Obsolete]` (don't delete yet - Phase 6 cleanup)
- [ ] Mark `GetAgilityModifier` method as `[Obsolete]`
- [ ] Mark `GetHappinessModifier` method as `[Obsolete]`
- [ ] Extract stat multiplier calculation into separate private methods for readability:
  - `CalculateSpeedMultiplier(int speed)`
  - `CalculateAgilityMultiplier(int agility)`

**Acceptance Criteria**:
- [x] All new stat modifier tests pass
- [x] All existing RaceService tests still pass
- [x] Integration test shows stat differences affect race outcomes predictably
- [x] Code coverage > 85%
- [x] Happiness modifier disabled (not called)

**Deliverable**: Speed and Agility modifiers use new clean implementation. Old methods marked obsolete. Races still work correctly.

---

## Phase 3: Environmental Modifiers (Surface & Condition)

**Goal**: Replace environment-based modifiers with configurable dictionary lookups

**Vertical Slice**: Surface and Condition modifiers use config-driven values, all race conditions handled

**Estimated Complexity**: Simple
**Risks**: Low - straightforward dictionary lookup replacement

### RED - Write Failing Tests

- [ ] Test: `RaceModifierConfig.SurfaceModifiers[SurfaceId.Dirt]` returns 1.00
- [ ] Test: `RaceModifierConfig.SurfaceModifiers[SurfaceId.Turf]` returns 1.02
- [ ] Test: `RaceModifierConfig.SurfaceModifiers[SurfaceId.Artificial]` returns 1.01
- [ ] Test: `RaceModifierConfig.ConditionModifiers[ConditionId.Good]` returns 1.00 (baseline)
- [ ] Test: `RaceModifierConfig.ConditionModifiers[ConditionId.Fast]` returns 1.03
- [ ] Test: `RaceModifierConfig.ConditionModifiers[ConditionId.Heavy]` returns 0.93
- [ ] Test: All 11 ConditionId enum values have entries in dictionary
- [ ] Test: `CalculateEnvironmentalModifiers` with Dirt/Good returns 1.0 * 1.0 = 1.0
- [ ] Test: `CalculateEnvironmentalModifiers` with Turf/Fast returns 1.02 * 1.03 = 1.0506
- [ ] Test: `CalculateEnvironmentalModifiers` with Artificial/Heavy returns 1.01 * 0.93 = 0.9393

**Why these tests**: Ensure all race conditions are configured and environmental modifiers combine correctly.

### GREEN - Make Tests Pass

- [ ] Populate `RaceModifierConfig.SurfaceModifiers` dictionary:
  ```csharp
  public static readonly IReadOnlyDictionary<SurfaceId, double> SurfaceModifiers =
      new Dictionary<SurfaceId, double>
      {
          { SurfaceId.Dirt, 1.00 },     // Neutral, most common
          { SurfaceId.Turf, 1.02 },     // Faster
          { SurfaceId.Artificial, 1.01 } // Consistent
      };
  ```

- [ ] Populate `RaceModifierConfig.ConditionModifiers` dictionary per spec:
  - Fast: 1.03, Firm: 1.02, Good: 1.00 (dry/fast conditions)
  - WetFast: 0.99, Soft: 0.98, Yielding: 0.97, Muddy: 0.96, Sloppy: 0.95 (wet conditions)
  - Heavy: 0.93, Frozen: 0.92, Slow: 0.90 (extreme conditions)

- [ ] Implement `CalculateEnvironmentalModifiers(ModifierContext context)`:
  ```csharp
  public double CalculateEnvironmentalModifiers(ModifierContext context)
  {
      var surfaceModifier = RaceModifierConfig.SurfaceModifiers[context.raceSurface];
      var conditionModifier = RaceModifierConfig.ConditionModifiers[context.raceCondition];
      return surfaceModifier * conditionModifier;
  }
  ```

- [ ] Update `UpdateHorsePosition` in `RaceService.cs`:
  - Add `raceSurface` and `raceCondition` to ModifierContext
  - Replace `AdjustSpeedForCondition(baseSpeed, conditionId)` with calculator call
  - Replace `AdjustSpeedForSurface(baseSpeed, surfaceId)` with calculator call
  - Remove `AdjustSpeedForLaneAndLegType` call (per spec: lane position speed modifier eliminated)

**Implementation notes**:
- Condition values rebalanced per spec (larger range: -10% to +3% vs old -12% to +5%)
- Surface values changed: Dirt now neutral 1.00 (was 0.98), Turf 1.02 (was 1.02), Artificial 1.01 (was 1.03)
- Lane+LegType speed modifier removed entirely - will affect lane assignment logic but not speed

### REFACTOR - Clean Up

- [ ] Mark `AdjustSpeedForCondition` as `[Obsolete]`
- [ ] Mark `AdjustSpeedForSurface` as `[Obsolete]`
- [ ] Mark `AdjustSpeedForLaneAndLegType` as `[Obsolete]`
- [ ] Add helper method `GetModifierOrDefault<TKey>(IReadOnlyDictionary<TKey, double>, TKey)` for safe dictionary access with fallback to 1.0

**Acceptance Criteria**:
- [x] All environmental modifier tests pass
- [x] All 11 condition types have config entries
- [x] All existing race tests still pass
- [x] Lane+LegType speed modifier no longer applied
- [x] Integration test shows conditions affect race speeds appropriately

**Deliverable**: Environmental modifiers use clean config-driven implementation. Lane position no longer affects speed (only affects lane assignment).

---

## Phase 4: Phase Modifiers (LegType Timing)

**Goal**: Replace phase-based modifiers with clean timing configuration

**Vertical Slice**: Each LegType gets speed boost during its designated race phase, late-race random boost removed

**Estimated Complexity**: Medium
**Risks**: Timing logic must be precise; removing late-race boost changes race dynamics

### RED - Write Failing Tests

- [ ] Test: `PhaseModifier` record stores startPercent, endPercent, multiplier correctly
- [ ] Test: `RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.StartDash]` has phase 0-25%, multiplier 1.04
- [ ] Test: `RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.FrontRunner]` has phase 0-20%, multiplier 1.03
- [x] Test: `RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.StretchRunner]` has phase 60-80%, multiplier 1.03 (adjusted for realistic stretch run timing)
- [ ] Test: `RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.LastSpurt]` has phase 75-100%, multiplier 1.04
- [ ] Test: `RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.RailRunner]` has phase 70-100%, multiplier 1.02
- [ ] Test: `CalculatePhaseModifiers` at tick 50/200 (25%) with StartDash returns 1.04 (in phase)
- [ ] Test: `CalculatePhaseModifiers` at tick 150/200 (75%) with StartDash returns 1.0 (out of phase)
- [ ] Test: `CalculatePhaseModifiers` at tick 170/200 (85%) with LastSpurt returns 1.04 (in phase)
- [x] Test: `CalculatePhaseModifiers` at tick 140/200 (70%) with StretchRunner returns 1.03 (in phase)

**Why these tests**: Ensure each LegType's timing windows and modifiers are correctly configured and applied based on race progress.

### GREEN - Make Tests Pass

- [ ] Create `TripleDerby.Core/Racing/PhaseModifier.cs` record:
  ```csharp
  public record PhaseModifier(double StartPercent, double EndPercent, double Multiplier);
  ```

- [ ] Populate `RaceModifierConfig.LegTypePhaseModifiers` dictionary:
  ```csharp
  public static readonly IReadOnlyDictionary<LegTypeId, PhaseModifier> LegTypePhaseModifiers =
      new Dictionary<LegTypeId, PhaseModifier>
      {
          { LegTypeId.StartDash, new PhaseModifier(0.00, 0.25, 1.04) },
          { LegTypeId.FrontRunner, new PhaseModifier(0.00, 0.20, 1.03) },
          { LegTypeId.StretchRunner, new PhaseModifier(0.60, 0.80, 1.03) },  // Adjusted: 60-80% more realistically represents "stretch run"
          { LegTypeId.LastSpurt, new PhaseModifier(0.75, 1.00, 1.04) },
          { LegTypeId.RailRunner, new PhaseModifier(0.70, 1.00, 1.02) }
      };
  ```

- [ ] Implement `CalculatePhaseModifiers(ModifierContext context)`:
  ```csharp
  public double CalculatePhaseModifiers(ModifierContext context)
  {
      var raceProgress = (double)context.currentTick / context.totalTicks;

      if (!RaceModifierConfig.LegTypePhaseModifiers.TryGetValue(context.horse.LegTypeId, out var phaseModifier))
          return 1.0;

      if (raceProgress >= phaseModifier.StartPercent && raceProgress <= phaseModifier.EndPercent)
          return phaseModifier.Multiplier;

      return 1.0;
  }
  ```

- [ ] Update `UpdateHorsePosition` in `RaceService.cs`:
  - Add `legTypeId` and tick info to ModifierContext
  - Replace `AdjustSpeedForLegTypeDuringRace` method call with `calculator.CalculatePhaseModifiers(context)`
  - Remove late-race random boost logic (after 80% completion) - now handled by consistent random variance

**Implementation notes**:
- Phase modifiers now clearly separated from lane position (which was removed in Phase 3)
- Modifiers increased: old was +1.5% to +2%, new is +2% to +4% (per spec, stats matter more)
- Late-race random boost eliminated - cleaner design with just consistent ±1% variance

### REFACTOR - Clean Up

- [ ] Mark `AdjustSpeedForLegTypeDuringRace` as `[Obsolete]`
- [ ] Extract phase detection logic into helper: `IsInPhase(double raceProgress, PhaseModifier phase)`
- [ ] Add XML documentation explaining each LegType's strategy and timing

**Acceptance Criteria**:
- [x] All phase modifier tests pass
- [x] Each LegType configuration tested
- [x] Existing race tests still pass
- [x] Late-race random boost removed
- [x] Integration test shows LegType horses perform better in their phases

**Deliverable**: Phase modifiers cleanly configured and applied based on race timing. Late-race chaos removed for predictable performance.

---

## Phase 5: Random Variance Consolidation

**Goal**: Single, consistent ±1% random variance per tick, removing scattered random components

**Vertical Slice**: Predictable, consistent randomness applied uniformly

**Estimated Complexity**: Simple
**Risks**: Very low - simplifying randomness

### RED - Write Failing Tests

- [ ] Test: `ApplyRandomVariance` with mocked random returning 0.5 applies neutral 1.0 modifier
- [ ] Test: `ApplyRandomVariance` with mocked random returning 0.0 applies 0.99 modifier (-1%)
- [ ] Test: `ApplyRandomVariance` with mocked random returning 1.0 applies 1.01 modifier (+1%)
- [ ] Test: `ApplyRandomVariance` called multiple times produces different results (not deterministic)
- [ ] Test: Over 1000 calls, average variance is ~0% (mean-centered distribution)

**Why these tests**: Verify random variance is properly bounded to ±1% and mean-centered.

### GREEN - Make Tests Pass

- [ ] Implement `ApplyRandomVariance()` in `SpeedModifierCalculator`:
  ```csharp
  public double ApplyRandomVariance()
  {
      var variance = _randomGenerator.NextDouble() * 2 * RaceModifierConfig.RandomVarianceRange
                     - RaceModifierConfig.RandomVarianceRange;
      return 1.0 + variance; // Range: 0.99 to 1.01
  }
  ```

- [ ] Update `UpdateHorsePosition` in `RaceService.cs`:
  - Replace `ApplyRandomPerformanceFluctuations(baseSpeed)` with `calculator.ApplyRandomVariance()`
  - Remove random variance from `AdjustSpeedForLaneAndLegType` (already removed in Phase 3)
  - Verify late-race random boost already removed (Phase 4)

**Implementation notes**:
- Old system had: ±1% per tick, lane variance ±0.0001, late-race ±1%, disabled incidents ±5-15%
- New system: just ±1% per tick, consistent and simple
- Formula: `NextDouble() * 0.02 - 0.01` produces uniform distribution in [-0.01, +0.01]

### REFACTOR - Clean Up

- [ ] Mark `ApplyRandomPerformanceFluctuations` as `[Obsolete]`
- [ ] Verify no other random modifiers exist in UpdateHorsePosition
- [ ] Add comment documenting disabled incident system for future implementation

**Acceptance Criteria**:
- [x] All random variance tests pass
- [x] Statistical test verifies mean-centered distribution
- [x] All scattered random components removed/consolidated
- [x] Existing race tests still pass

**Deliverable**: Single, clean, consistent ±1% random variance. All other random modifiers removed.

---

## Phase 6: Cleanup & Organization

**Goal**: Remove all obsolete methods, organize code for maintainability, comprehensive documentation

**Vertical Slice**: Clean, production-ready codebase with no dead code

**Estimated Complexity**: Simple
**Risks**: Must ensure no references to deleted methods exist

### RED - Write Failing Tests

- [ ] Test: Verify `RaceService.UpdateHorsePosition` uses only new calculator methods (code inspection test)
- [ ] Test: Integration test - full race with all modifier types working together
- [ ] Test: Variance analysis - run 100 races, verify outcomes are appropriately distributed
- [ ] Test: Edge case - horse with all stats at 0 completes race
- [ ] Test: Edge case - horse with all stats at 100 completes race significantly faster

**Why these tests**: Ensure the complete system works end-to-end with clean integration.

### GREEN - Make Tests Pass

- [ ] Update `UpdateHorsePosition` to use new calculator cleanly:
  ```csharp
  private void UpdateHorsePosition(RaceRunHorse raceRunHorse, byte tick, int totalTicks, RaceRun raceRun)
  {
      var context = new ModifierContext(
          currentTick: tick,
          totalTicks: totalTicks,
          horse: raceRunHorse.Horse,
          raceCondition: raceRun.ConditionId,
          raceSurface: raceRun.Race.SurfaceId,
          raceFurlongs: raceRun.Race.Furlongs
      );

      var baseSpeed = AverageBaseSpeed;
      baseSpeed *= _speedModifierCalculator.CalculateStatModifiers(context);
      baseSpeed *= _speedModifierCalculator.CalculateEnvironmentalModifiers(context);
      baseSpeed *= _speedModifierCalculator.CalculatePhaseModifiers(context);
      baseSpeed *= _speedModifierCalculator.ApplyRandomVariance();

      raceRunHorse.Distance += (decimal)baseSpeed;
  }
  ```

- [ ] Delete all obsolete modifier methods from `RaceService.cs`:
  - `ApplySpeedModifier`
  - `GetAgilityModifier`
  - `GetHappinessModifier`
  - `AdjustSpeedForCondition`
  - `AdjustSpeedForSurface`
  - `AdjustSpeedForLaneAndLegType`
  - `AdjustSpeedForLegTypeDuringRace`
  - `ApplyRandomPerformanceFluctuations`

- [ ] Move all commented stamina code to separate region or file:
  - Create `RaceService.Stamina.cs` partial class file (for future stamina implementation)
  - Move `GetStaminaConsumption`, `ApplyStaminaEffect`, stamina modifier methods
  - Add TODO comments for Phase 7 (future) stamina re-implementation

- [ ] Delete stamina modifier methods (will be reimplemented in future):
  - `GetStaminaModifierForCondition`
  - `GetStaminaModifierForSurface`
  - `GetStaminaModifierForLaneAndLegType`
  - `GetDurabilityModifier`
  - `GetHappinessStaminaModifier`

**Implementation notes**:
- UpdateHorsePosition should now be ~15 lines vs. ~60 lines (massive simplification)
- All modifier logic centralized in SpeedModifierCalculator
- All configuration centralized in RaceModifierConfig
- Stamina system disabled but preserved for future work

### REFACTOR - Clean Up

- [ ] Add comprehensive XML documentation to `SpeedModifierCalculator`:
  - Class summary explaining the modifier pipeline
  - Method summaries for each calculator method
  - Parameter documentation

- [ ] Add comprehensive XML documentation to `RaceModifierConfig`:
  - Explain each constant's purpose, typical range, and impact
  - Document modifier stacking behavior (all multiplicative)
  - Reference feature spec for design rationale

- [ ] Organize `RaceService.cs`:
  - Group remaining helper methods logically
  - Add region markers for clarity
  - Clean up commented code

- [ ] Run code formatter on all modified files

- [ ] Update test file organization:
  - Ensure tests grouped by feature (stat, environmental, phase, variance)
  - Add summary comments to test classes

**Acceptance Criteria**:
- [x] All obsolete methods deleted
- [x] No compiler warnings or errors
- [x] All tests pass (unit + integration)
- [x] Code coverage > 85% for new code
- [x] XML documentation complete for public APIs
- [x] UpdateHorsePosition method is clean and readable
- [x] Stamina code moved to separate file for future work

**Deliverable**: Clean, maintainable, well-documented modifier system. Old code completely removed. Production-ready codebase.

---

## Phase 7: Rebalancing & Validation (Optional)

**Goal**: Tune modifier values based on actual race outcomes, validate game balance

**Vertical Slice**: Races feel balanced, stats matter appropriately, conditions have visible impact

**Estimated Complexity**: Complex (requires gameplay analysis)
**Risks**: Subjective - requires playtesting and iteration

### Activities

- [ ] Create race simulation test harness:
  - Run 1000 races with varied horse stats and conditions
  - Collect finish time distributions
  - Analyze impact of each modifier type

- [ ] Statistical analysis:
  - Calculate correlation between stats and finish position
  - Verify no single modifier dominates
  - Ensure appropriate variance in outcomes

- [ ] Balance adjustments:
  - Adjust `RaceModifierConfig` constants based on analysis
  - Re-run simulations to verify improvements
  - Document balance decisions

- [ ] Edge case testing:
  - Very long races (16+ furlongs)
  - Very short races (4 furlongs)
  - Extreme stat horses (all 0s, all 100s)
  - All condition types

- [ ] Create balance documentation:
  - Expected finish time ranges by distance
  - Stat impact charts (Speed 80 vs 60 = X% faster)
  - Condition impact reference

**Acceptance Criteria**:
- [x] Races complete in reasonable time (6f in ~150 ticks, 10f in ~237 ticks)
- [x] Stats have clear, measurable impact
- [x] No dominant strategy
- [x] Conditions visibly affect outcomes
- [x] Balance documentation created

**Deliverable**: Balanced, validated modifier values. Statistical analysis confirming design goals met.

---

## Testing Strategy

### Unit Tests (Each Phase)
- Test each modifier calculation in isolation
- Test edge cases (min/max stat values, boundary conditions)
- Test configuration completeness (all enums have entries)
- Mock random generator for deterministic tests

### Integration Tests
- Full race simulation with known inputs
- Verify modifiers stack correctly (multiplicative)
- Compare new vs old system outcomes (regression tests)
- Test all race distances and conditions

### Characterization Tests (Phase 1)
- Document existing modifier behavior with tests
- Use as regression baseline during refactoring
- Ensure new system produces similar outcomes

### Statistical Tests (Phase 7)
- Run large-scale simulations (1000+ races)
- Analyze distributions and correlations
- Validate game balance assumptions

---

## Rollback Plan

If any phase causes issues:

1. **Revert commits**: Each phase is a separate commit
2. **Re-enable old methods**: Remove `[Obsolete]` attributes
3. **Conditional logic**: Add feature flag to switch between old/new systems
4. **Gradual migration**: Enable new system for specific race types first

---

## Dependencies & Prerequisites

### Before Starting
- [x] Feature spec approved and finalized
- [x] All open questions resolved
- [x] RaceService tests passing
- [x] Development environment ready

### External Dependencies
- None - purely internal refactoring

### Team Coordination
- Frontend: No changes required (API contracts unchanged)
- Database: No schema changes
- Other services: No integration changes

---

## Success Criteria Summary

### Code Quality
- ✅ No magic numbers (all in config)
- ✅ No duplicate logic (DRY principle)
- ✅ No commented-out active code paths
- ✅ Consistent patterns (all modifiers use same approach)
- ✅ Clear separation of concerns

### Maintainability
- ✅ Easy to add new modifier (just config entry)
- ✅ Easy to adjust values (no code changes)
- ✅ Each modifier testable in isolation
- ✅ Clear documentation of each modifier's purpose

### Correctness
- ✅ No order dependencies
- ✅ No unintended compounding
- ✅ Predictable behavior
- ✅ All modifiers applied exactly once

### Balance
- ✅ Clear range documentation (min/max effect)
- ✅ Reasonable total variance (stats matter)
- ✅ Each factor has measurable impact
- ✅ No dominant strategy

---

## Timeline & Effort Estimates

| Phase | Complexity | Estimated Time | Risk Level |
|-------|-----------|----------------|------------|
| Phase 1: Infrastructure | Medium | 60-90 min | None (additive) |
| Phase 2: Stat Modifiers | Medium | 60-90 min | Low |
| Phase 3: Environmental | Simple | 30-45 min | Low |
| Phase 4: Phase Modifiers | Medium | 60-90 min | Medium |
| Phase 5: Random Variance | Simple | 30-45 min | Very Low |
| Phase 6: Cleanup | Simple | 45-60 min | Low |
| Phase 7: Rebalancing | Complex | 120+ min | Medium |
| **Total** | - | **6-9 hours** | - |

**Note**: Times are estimates for focused work sessions. Actual time may vary based on testing depth and balance iteration.

---

## Next Steps

1. ✅ Review this implementation plan with team/stakeholders
2. ⏳ Get approval to proceed
3. ⏳ Add Phase 1 tasks to TodoWrite
4. ⏳ Begin implementation with Phase 1
5. ⏳ After each phase: commit, push, move to next phase

**Ready to implement!** 🏁
