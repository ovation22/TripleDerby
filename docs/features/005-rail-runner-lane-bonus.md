# Rail Runner Lane Position Bonus - Feature Specification

**Feature Number:** 005

**Status:** 🔵 PROPOSED - Awaiting Implementation

**Prerequisites:** Feature 003 (Race Modifiers Refactor) - ✅ Complete

---

## Summary

Redesign the **RailRunner** leg type to provide a strategic lane-based speed bonus instead of the current phase-based modifier. Rail runners will gain a speed advantage when positioned in **lane 1 (the rail)** with **clear track ahead**, rewarding tactical positioning and clean racing lines. This mechanic transforms RailRunner from a simple late-race timing bonus into a positioning-dependent strategic choice.

**Core Design Philosophy:**
- Lane position matters: Inside rail = optimal racing line = speed advantage
- Traffic awareness: Clear track ahead required (no drafting benefit behind horses)
- Conditional bonus: Must actively maintain rail position to benefit
- Strategic depth: Players must balance lane position vs traffic management

---

## Problem Statement

### Current State (Feature 003)

The RailRunner leg type currently uses a **phase-based modifier**:
- Active phase: 70-100% race progress (final 30%)
- Multiplier: 1.02x (+2% speed)
- Issue: **Does not integrate with lane/position mechanics**

From [RaceModifierConfig.cs:126](c:\Development\TripleDerby\TripleDerby.Core\Configuration\RaceModifierConfig.cs#L126):
```csharp
{ LegTypeId.RailRunner, new PhaseModifier(0.70, 1.00, 1.02) }  // Final 30%, position advantage
```

### Current Limitations

1. **No lane interaction:** Bonus applies regardless of actual lane position
2. **No traffic awareness:** Bonus applies even when blocked by horses ahead
3. **Inconsistent with description:** Database description says "Gets 5% speed boost while in first lane" but actual mechanic is time-based
4. **Misaligned incentives:** Doesn't reward the strategic rail-hugging behavior that real rail runners exhibit

### Design Goals

✅ Make lane position strategically meaningful
✅ Reward clean racing lines and tactical positioning
✅ Create counterplay between leg types (rail vs wide runners)
✅ Integrate cleanly with existing modifier pipeline
✅ Maintain system balance (~2-3% impact range)

---

## Requirements

### Functional Requirements

- [ ] Rail runner bonus applies **only when horse is in lane 1**
- [ ] Bonus requires **clear path ahead** (0.5 furlongs minimum)
- [ ] Bonus **replaces** existing phase-based modifier (70-100% timing)
- [ ] Traffic detection checks for horses ahead in lane 1
- [ ] Bonus activates/deactivates dynamically based on lane/traffic
- [ ] Integrates with existing lane tracking in RaceRunHorse entity
- [ ] Bonus multiplier configured in RaceModifierConfig
- [ ] Compatible with future lane-changing mechanics

### Acceptance Criteria

**Lane Position Requirement:**
- [ ] Given horse in lane 1 with clear track, when calculating phase modifier, then 1.03x bonus applied
- [ ] Given horse in lane 2-8, when calculating phase modifier, then 1.0x (no bonus)
- [ ] Given horse changes from lane 1 to lane 2, when bonus was active, then bonus immediately deactivates

**Traffic Detection:**
- [ ] Given horse in lane 1 with 0.6 furlongs clear ahead, when checking eligibility, then bonus applies
- [ ] Given horse in lane 1 with horse 0.3 furlongs ahead (same lane), when checking eligibility, then bonus does NOT apply
- [ ] Given horse in lane 1 with horse 0.3 furlongs ahead (different lane), when checking eligibility, then bonus applies (traffic not blocking)

**Phase Modifier Replacement:**
- [ ] Given RailRunner horse, when race progress is 50%, then conditional bonus checked (not phase-based)
- [ ] Given RailRunner horse, when race progress is 90%, then conditional bonus checked (not phase-based)
- [ ] Given non-RailRunner horse, when calculating phase modifiers, then existing phase logic applies unchanged

**Integration:**
- [ ] Given existing phase modifier tests, when RailRunner tests updated, then all other leg type tests still pass
- [ ] Given race simulation, when rail runner bonus activates, then multiplies with stat/env/stamina modifiers correctly
- [ ] Given RACE_BALANCE.md validation, when testing rail runners, then average impact remains 1-3% range

### Non-Functional Requirements

- **Performance:** Lane/traffic checks add < 1% overhead per tick
- **Backward Compatibility:** Other leg types unaffected
- **Maintainability:** Follows existing modifier pipeline patterns
- **Testability:** Traffic detection testable in isolation
- **Balance:** Rail runner remains balanced vs other leg types
- **Clarity:** Code clearly documents conditional logic

---

## Technical Design

### Architecture Overview

The rail runner system modifies the **CalculatePhaseModifiers** method in SpeedModifierCalculator to check lane/traffic conditions for RailRunner leg type:

```
Modifier Pipeline (unchanged):
Final Speed = Base Speed
  × Stat Modifiers (Speed, Agility)
  × Environmental Modifiers (Surface, Condition)
  × Phase Modifiers (LegType timing OR conditional bonus ← MODIFIED)
  × Stamina Modifier (fatigue penalty)
  × Random Variance

Phase Modifier Logic (new conditional branch):
IF legType == RailRunner:
    IF inLane1 AND clearPathAhead:
        RETURN 1.03  // Rail position bonus
    ELSE:
        RETURN 1.0   // No bonus
ELSE:
    RETURN existing phase-based logic
```

### Data Model

**No new properties needed.** Existing infrastructure supports this feature:

```csharp
// RaceRunHorse.cs - Lane already tracked
public byte Lane { get; set; }  // Current lane (1-8)
public decimal Distance { get; set; }  // Current distance traveled

// RaceRun.cs - Horse list available for traffic detection
public virtual ICollection<RaceRunHorse> Horses { get; set; }
```

### Configuration

**Update [RaceModifierConfig.cs:119-127](c:\Development\TripleDerby\TripleDerby.Core\Configuration\RaceModifierConfig.cs#L119-L127):**

```csharp
/// <summary>
/// Phase-based modifiers for each leg type (running style).
/// RailRunner uses conditional lane/traffic bonus instead of phase timing.
/// </summary>
public static readonly IReadOnlyDictionary<LegTypeId, PhaseModifier> LegTypePhaseModifiers =
    new Dictionary<LegTypeId, PhaseModifier>
    {
        { LegTypeId.StartDash, new PhaseModifier(0.00, 0.25, 1.04) },
        { LegTypeId.FrontRunner, new PhaseModifier(0.00, 0.20, 1.03) },
        { LegTypeId.StretchRunner, new PhaseModifier(0.60, 0.80, 1.03) },
        { LegTypeId.LastSpurt, new PhaseModifier(0.75, 1.00, 1.04) },
        // RailRunner: No entry needed (conditional logic in calculator)
    };

/// <summary>
/// Rail runner bonus configuration.
/// Bonus applied when horse is in lane 1 with clear path ahead.
/// </summary>
public const double RailRunnerBonusMultiplier = 1.03;  // +3% speed on rail
public const decimal RailRunnerClearPathDistance = 0.5m;  // furlongs ahead must be clear
```

### Implementation Components

#### 1. Traffic Detection Helper

**New method in SpeedModifierCalculator:**

```csharp
/// <summary>
/// Checks if a horse has a clear path ahead in its current lane.
/// </summary>
/// <param name="horse">The horse to check</param>
/// <param name="allHorses">All horses in the race</param>
/// <param name="clearDistance">Required clear distance (furlongs)</param>
/// <returns>True if path is clear, false if blocked by traffic</returns>
private static bool HasClearPathAhead(
    RaceRunHorse horse,
    IEnumerable<RaceRunHorse> allHorses,
    decimal clearDistance)
{
    // Check for horses in same lane ahead within clearDistance
    return !allHorses.Any(h =>
        h != horse &&                              // Not the same horse
        h.Lane == horse.Lane &&                    // Same lane
        h.Distance > horse.Distance &&             // Horse is ahead
        (h.Distance - horse.Distance) < clearDistance  // Within blocking range
    );
}
```

#### 2. Modified Phase Modifier Calculator

**Update [SpeedModifierCalculator.cs:86-107](c:\Development\TripleDerby\TripleDerby.Core\Racing\SpeedModifierCalculator.cs#L86-L107):**

```csharp
/// <summary>
/// Calculates phase-based speed modifiers (LegType timing).
/// RailRunner uses conditional lane/traffic bonus instead of phase timing.
/// </summary>
public double CalculatePhaseModifiers(ModifierContext context, RaceRun raceRun)
{
    // Special case: RailRunner uses conditional lane/traffic bonus
    if (context.Horse.LegTypeId == LegTypeId.RailRunner)
    {
        return CalculateRailRunnerBonus(context, raceRun);
    }

    // All other leg types use phase-based timing
    var raceProgress = (double)context.CurrentTick / context.TotalTicks;

    if (!Configuration.RaceModifierConfig.LegTypePhaseModifiers.TryGetValue(
        context.Horse.LegTypeId, out var phaseModifier))
    {
        return 1.0; // No modifier found for this leg type
    }

    // Check if current race progress is within the active phase
    if (raceProgress >= phaseModifier.StartPercent && raceProgress <= phaseModifier.EndPercent)
    {
        return phaseModifier.Multiplier;
    }

    return 1.0; // Outside active phase, no bonus
}

/// <summary>
/// Calculates rail runner conditional bonus based on lane position and traffic.
/// </summary>
private static double CalculateRailRunnerBonus(ModifierContext context, RaceRun raceRun)
{
    // Find the RaceRunHorse entity for this horse
    var raceRunHorse = raceRun.Horses.FirstOrDefault(h => h.Horse.Id == context.Horse.Id);

    if (raceRunHorse == null)
    {
        return 1.0; // Safety fallback
    }

    // Check lane position: must be in lane 1 (the rail)
    if (raceRunHorse.Lane != 1)
    {
        return 1.0; // Not on rail, no bonus
    }

    // Check for clear path ahead
    if (!HasClearPathAhead(
        raceRunHorse,
        raceRun.Horses,
        Configuration.RaceModifierConfig.RailRunnerClearPathDistance))
    {
        return 1.0; // Traffic ahead, no bonus
    }

    // All conditions met: apply rail position bonus
    return Configuration.RaceModifierConfig.RailRunnerBonusMultiplier;
}
```

#### 3. RaceService Integration

**Update [RaceService.cs:232](c:\Development\TripleDerby\TripleDerby.Core\Services\RaceService.cs#L232):**

```csharp
// Apply phase modifiers (LegType timing or conditional bonuses)
var phaseModifier = _speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);
baseSpeed *= phaseModifier;
```

**Signature change:**
```csharp
// Old signature:
public double CalculatePhaseModifiers(ModifierContext context)

// New signature:
public double CalculatePhaseModifiers(ModifierContext context, RaceRun raceRun)
```

### Balance Considerations

**Target Impact Range:**
- Rail runner in optimal conditions: +3% speed (1.03x multiplier)
- Comparable to existing leg types:
  - StartDash: 1.04x for 25% of race
  - LastSpurt: 1.04x for 25% of race
  - StretchRunner: 1.03x for 20% of race
  - FrontRunner: 1.03x for 20% of race

**Expected Activation Frequency:**
- Early race (0-30%): High activation (~70-80% of ticks) - horses spread out, rail clear
- Mid race (30-70%): Moderate activation (~40-60%) - pack compression, traffic increases
- Late race (70-100%): Variable activation (~30-70%) - depends on position in field

**Net Effect:**
- Average activation across full race: ~50-60% of ticks
- Effective average bonus: ~1.015x (1.5% speed improvement)
- Competitive with other leg types in overall impact

**Strategic Trade-offs:**
- **Pros:** Speed advantage on rail, shorter distance around turns
- **Cons:** Vulnerable to being boxed in, limited overtaking options, traffic dependency

---

## Implementation Phases

### Phase 1: Core Logic (Estimated: 2-3 hours)

**Tasks:**
1. Add configuration constants to RaceModifierConfig
   - `RailRunnerBonusMultiplier = 1.03`
   - `RailRunnerClearPathDistance = 0.5m`
   - Remove RailRunner from LegTypePhaseModifiers dictionary

2. Implement `HasClearPathAhead` helper method
   - Traffic detection logic
   - Lane matching
   - Distance calculations

3. Implement `CalculateRailRunnerBonus` method
   - Lane position check
   - Traffic check
   - Return appropriate multiplier

4. Modify `CalculatePhaseModifiers` signature
   - Add `RaceRun raceRun` parameter
   - Add RailRunner conditional branch
   - Preserve existing phase logic for other leg types

5. Update RaceService.UpdateHorsePosition
   - Pass `raceRun` to CalculatePhaseModifiers

**Acceptance Test:**
```csharp
[Fact]
public void RailRunner_InLane1_WithClearPath_GetsSpeedBonus()
{
    // Arrange: RailRunner in lane 1, no traffic ahead
    var raceRun = CreateTestRaceRun();
    var railRunner = raceRun.Horses[0]; // Lane 1, leading
    railRunner.Horse.LegTypeId = LegTypeId.RailRunner;

    var context = CreateContext(railRunner.Horse, tick: 100, totalTicks: 200);

    // Act
    var modifier = _calculator.CalculatePhaseModifiers(context, raceRun);

    // Assert
    Assert.Equal(1.03, modifier, precision: 2);
}
```

### Phase 2: Testing (Estimated: 2-3 hours)

**Unit Tests (SpeedModifierCalculatorTests.cs):**
1. `HasClearPathAhead_NoTraffic_ReturnsTrue`
2. `HasClearPathAhead_TrafficWithinRange_ReturnsFalse`
3. `HasClearPathAhead_TrafficBeyondRange_ReturnsTrue`
4. `HasClearPathAhead_TrafficInDifferentLane_ReturnsTrue`
5. `RailRunner_Lane1_ClearPath_Returns103xBonus`
6. `RailRunner_Lane1_TrafficAhead_Returns10xNoBonus`
7. `RailRunner_Lane2_ClearPath_Returns10xNoBonus`
8. `RailRunner_EarlyRace_BonusActivates`
9. `RailRunner_LateRace_BonusActivates`
10. `RailRunner_MidRace_BonusDynamic`

**Integration Tests (RaceBalanceValidationTests.cs):**
1. `RailRunner_Modifiers_Activate_With_Lane_And_Traffic_Conditions`
   - Test across 100 simulated races
   - Verify bonus activates appropriately
   - Measure average activation rate (target: 50-60%)

2. `RailRunner_Balance_Comparable_To_Other_LegTypes`
   - Compare finish times across leg types
   - Ensure no dominant strategy
   - Rail runner competitive but not overpowered

**Regression Tests:**
- [ ] All existing leg type tests still pass (StartDash, FrontRunner, StretchRunner, LastSpurt)
- [ ] 1000-race balance validation still achieves targets
- [ ] No impact on non-RailRunner horses

### Phase 3: Balance Validation (Estimated: 1-2 hours)

**Diagnostic Tests:**
1. Run 500-race simulation with RailRunner horses
2. Collect metrics:
   - Bonus activation frequency per race phase
   - Average finish times vs other leg types
   - Win rate distribution
3. Adjust multiplier if needed (1.02x - 1.04x range)
4. Update RACE_BALANCE.md with findings

**Target Metrics:**
- Rail runner activation rate: 50-60% of race ticks
- Average finish time: Within 3% of other leg types
- Win rate: 18-22% (balanced for 5 leg types)

### Phase 4: Documentation (Estimated: 30 minutes)

**Update Files:**
1. **RACE_BALANCE.md**
   - Add section: "Rail Runner Conditional Bonus"
   - Document activation conditions
   - Include statistical analysis
   - Update modifier pipeline diagram

2. **Database seed data (ModelBuilderExtensions.cs)**
   - Update RailRunner description: "Gets 3% speed boost while in lane 1 with clear track ahead."

3. **Code comments**
   - Document traffic detection logic
   - Explain lane position requirements
   - Note future lane-changing integration

---

## Testing Strategy

### Test Coverage Requirements

**Unit Tests (≥ 90% coverage):**
- Traffic detection edge cases
- Lane position validation
- Bonus activation conditions
- Modifier pipeline integration

**Integration Tests:**
- Full race simulation with rail runners
- Multi-horse traffic scenarios
- Lane position changes during race

**Balance Tests:**
- 500+ race statistical validation
- Cross-leg-type comparison
- Activation frequency analysis

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Horse alone in race | Bonus always active (no traffic) |
| All horses in lane 1 | Leading horse gets bonus, others blocked |
| Horse switches from lane 1 to 2 | Bonus deactivates immediately |
| Horse switches from lane 2 to 1 | Bonus activates if path clear |
| Traffic exactly 0.5 furlongs ahead | No bonus (threshold is exclusive) |
| Traffic 0.51 furlongs ahead | Bonus active (beyond threshold) |
| Horse finishes race | Bonus continues to apply if conditions met |

---

## Open Questions & Future Considerations

### Resolved Questions

✅ **Lane bonus scaling:** Binary (lane 1 only) vs gradual (proximity-based)
→ **Decision:** Binary bonus (lane 1 only) for simplicity and clarity

✅ **Traffic threshold:** 0.2f vs 0.5f vs "no horses ahead"
→ **Decision:** 0.5 furlongs (balanced realism and playability)

✅ **Bonus stacking:** Keep phase modifier or replace?
→ **Decision:** Replace phase modifier with conditional bonus

### Future Enhancements

🔮 **Lane changing mechanics (Future Feature):**
- When implemented, rail runners could dynamically move to lane 1
- `AttemptLaneChange` method already exists (currently commented out in RaceService:271)
- RailRunner lane change probability already defined (0.2 in RaceService:307)
- Could add "rail-seeking" behavior: prefer moving toward lane 1 when clear

🔮 **Rail bias (track characteristic):**
- Some tracks favor inside vs outside lanes
- Could add track-specific rail multipliers
- Example: "Fast rail" = 1.04x, "Dead rail" = 1.01x

🔮 **Turn radius advantage:**
- Horses on rail travel shorter distance around turns
- Could reduce effective distance for lane 1 horses by 0.5-1%
- Requires turn/straightaway track modeling

🔮 **Draft/slipstream mechanic:**
- Currently no benefit to running behind horses
- Could add speed bonus for trailing closely (inverse of rail runner)
- Tactical choice: clear rail vs drafting benefit

---

## Success Criteria

**Feature Complete When:**
- [x] Rail runner bonus activates based on lane + traffic (not phase timing)
- [x] Bonus multiplier configurable in RaceModifierConfig
- [x] Traffic detection accurately identifies blocking horses
- [x] All unit tests passing (≥10 new tests)
- [x] Integration tests validate balance
- [x] Existing tests unchanged and passing
- [x] RACE_BALANCE.md updated with statistics
- [x] Database description updated to match mechanic

**Balance Goals Met When:**
- Average rail runner activation: 50-60% of race ticks
- Finish time variance: Within 3% of other leg types
- Win rate: Balanced (~20% for 5 leg types)
- No dominant strategy emerges

---

## References

### Related Files

**Core Logic:**
- [RaceModifierConfig.cs](c:\Development\TripleDerby\TripleDerby.Core\Configuration\RaceModifierConfig.cs) - Configuration constants
- [SpeedModifierCalculator.cs](c:\Development\TripleDerby\TripleDerby.Core\Racing\SpeedModifierCalculator.cs) - Phase modifier calculation
- [RaceService.cs](c:\Development\TripleDerby\TripleDerby.Core\Services\RaceService.cs) - Race simulation loop
- [ModifierContext.cs](c:\Development\TripleDerby\TripleDerby.Core\Racing\ModifierContext.cs) - Context data structure

**Entities:**
- [RaceRunHorse.cs](c:\Development\TripleDerby\TripleDerby.Core\Entities\RaceRunHorse.cs) - Lane tracking
- [RaceRun.cs](c:\Development\TripleDerby\TripleDerby.Core\Entities\RaceRun.cs) - Horse collection

**Testing:**
- [SpeedModifierCalculatorTests.cs](c:\Development\TripleDerby\TripleDerby.Tests.Unit\Racing\SpeedModifierCalculatorTests.cs) - Unit tests
- [RaceBalanceValidationTests.cs](c:\Development\TripleDerby\TripleDerby.Tests.Unit\Racing\RaceBalanceValidationTests.cs) - Balance tests

**Documentation:**
- [RACE_BALANCE.md](c:\Development\TripleDerby\docs\RACE_BALANCE.md) - Balance reference guide
- [004-stamina-depletion.md](c:\Development\TripleDerby\docs\features\004-stamina-depletion.md) - Similar feature pattern

### Design Patterns

**Similar Implementations:**
- Stamina modifier (conditional on stamina percentage)
- Phase modifiers (conditional on race progress)
- Environmental modifiers (conditional on track state)

**Follows Modifier Pipeline Pattern:**
1. Check conditions (lane, traffic)
2. Return multiplier (1.0 = neutral, 1.03 = bonus)
3. Multiply with other modifiers
4. Apply to base speed

---

## Changelog

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-22 | Feature Discovery Agent | Initial specification drafted |
| 2025-12-22 | Feature Discovery Agent | User decisions incorporated (binary bonus, 0.5f threshold, replace phase) |

---

**End of Feature Specification**
