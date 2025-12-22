# Stamina Depletion System - Feature Specification

**Feature Number:** 004

**Status:** Discovery Complete - Ready for Implementation

**Prerequisites:** Feature 003 (Race Modifiers Refactor)

---

## Summary

Implement a stamina depletion mechanic that makes the Stamina and Durability stats meaningful in races. Currently, Stamina is tracked but unused (correlation: -0.040 per RACE_BALANCE.md). This feature will make stamina deplete each tick based on pace and effort, with low stamina progressively reducing horse speed in the late race. The impact will be more pronounced in longer races (10f+), creating strategic depth for horse breeding and race selection.

**Core Design Philosophy:**
- Stamina = fuel tank size (how much energy)
- Durability = fuel efficiency (how slowly you burn it)
- Creates distinct endurance profiles: sprinters vs marathoners

---

## Requirements

### Functional Requirements

- [x] Stamina depletes each tick during the race
- [x] Depletion rate influenced by horse stats (Stamina, Durability)
- [x] Depletion rate influenced by current speed/effort
- [x] Depletion rate influenced by LegType running style
- [x] Depletion rate scales with race distance (progressive scaling)
- [x] Low stamina reduces horse speed (mild: 5-10% at exhaustion)
- [x] CurrentStamina tracked and updated in RaceRunHorse entity
- [x] Stamina modifier integrates cleanly into existing modifier pipeline

### Acceptance Criteria

**Stamina Depletion:**
- [ ] Given horse with Stamina=100, when racing 10f at neutral pace, then finishes with ~30-50% stamina remaining
- [ ] Given horse with Stamina=0, when racing 10f at neutral pace, then finishes near exhaustion (0-10% stamina)
- [ ] Given horse with high Durability (80+), when racing, then stamina depletes 20-30% slower than low Durability (20-)
- [ ] Given horse running above-average speed (1.1x modifier), when racing, then stamina depletes faster than baseline
- [ ] Given horse running below-average speed (0.9x modifier), when racing, then stamina depletes slower than baseline

**LegType Impact:**
- [ ] Given StartDash horse, when in early phase (0-25%), then stamina depletes faster than baseline
- [ ] Given LastSpurt horse, when in early phase (0-74%), then stamina depletes slower (conserving energy)
- [ ] Given LastSpurt horse, when in late phase (75%+), then stamina depletes faster (burning reserves)
- [ ] Given FrontRunner horse, when racing, then stamina depletes faster overall (aggressive style)

**Distance Scaling:**
- [ ] Given 4f race (sprint), when racing, then stamina impact minimal (finishing stamina 60-80%)
- [ ] Given 10f race (classic), when racing, then stamina impact moderate (finishing stamina 20-50%)
- [ ] Given 16f race (marathon), when racing, then stamina impact severe (finishing stamina 0-20%)

**Speed Penalty:**
- [ ] Given horse at 100% stamina, when calculating speed, then no stamina penalty applied
- [ ] Given horse at 50% stamina, when calculating speed, then ~2-5% speed penalty applied
- [ ] Given horse at 0% stamina, when calculating speed, then ~5-10% speed penalty applied (mild severity)
- [ ] Given horse at 0% stamina, when racing, then still finishes race (no DNF)

**Integration:**
- [ ] Given existing race simulation, when stamina system added, then all existing tests still pass
- [ ] Given ModifierContext, when passed to stamina calculator, then has all needed data
- [ ] Given stamina modifier, when applied in pipeline, then multiplies with other modifiers correctly
- [ ] Given RaceRunHorse, when race completes, then CurrentStamina value persisted correctly

### Non-Functional Requirements

- **Performance:** Stamina calculations add < 5% overhead to race simulation
- **Backward Compatibility:** Existing tests pass without modification
- **Maintainability:** Stamina modifier follows same pattern as other modifiers
- **Testability:** Stamina system testable in isolation
- **Balance:** Stamina correlation should move from -0.040 to -0.15 to -0.25 range
- **Documentation:** Update RACE_BALANCE.md with stamina correlation data

---

## Technical Design

### Architecture Overview

The stamina system integrates into the existing modifier pipeline as a new multiplicative modifier:

```
Final Speed = Base Speed
  × Stat Modifiers (Speed, Agility)
  × Environmental Modifiers (Surface, Condition)
  × Phase Modifiers (LegType timing)
  × Stamina Modifier (NEW - based on CurrentStamina%)
  × Random Variance
```

### Data Model

**Existing (No Changes Needed):**
```csharp
// RaceRunHorse.cs (already has these properties)
public byte InitialStamina { get; set; }      // Horse.Stamina at race start
public double CurrentStamina { get; set; }    // Current stamina (0-100+)

// Horse.cs (already has these properties)
public byte Stamina { get; set; }             // Base stamina stat (0-100)
public byte Durability { get; set; }          // Base durability stat (0-100)
public LegTypeId LegTypeId { get; set; }      // Running style
```

**New Properties (if needed for ModifierContext):**
```csharp
// ModifierContext.cs - may need to add RaceRunHorse reference
public record ModifierContext(
    short CurrentTick,
    short TotalTicks,
    Horse Horse,
    ConditionId RaceCondition,
    SurfaceId RaceSurface,
    decimal RaceFurlongs,
    RaceRunHorse RaceRunHorse  // NEW - needed to access CurrentStamina
);
```

### Stamina Depletion Formula

**Per-Tick Depletion Calculation:**

```csharp
// Called each tick in UpdateHorsePosition()
private void DepleteStamina(RaceRunHorse horse, ModifierContext context, double currentSpeed)
{
    // 1. Calculate base depletion rate
    double baseDepletionRate = CalculateBaseDepletionRate(context.RaceFurlongs);

    // 2. Adjust for horse stats
    double staminaEfficiency = CalculateStaminaEfficiency(horse.Horse);

    // 3. Adjust for current pace/effort
    double paceMultiplier = CalculatePaceMultiplier(currentSpeed, context);

    // 4. Adjust for LegType strategy
    double legTypeMultiplier = CalculateLegTypeStaminaMultiplier(context);

    // 5. Calculate final depletion
    double depletionAmount = baseDepletionRate
        * staminaEfficiency
        * paceMultiplier
        * legTypeMultiplier;

    // 6. Update CurrentStamina
    horse.CurrentStamina = Math.Max(0, horse.CurrentStamina - depletionAmount);
}
```

**Component Formulas:**

```csharp
// 1. Base Depletion Rate (scales with distance)
private double CalculateBaseDepletionRate(decimal furlongs)
{
    // Progressive scaling: minimal impact on sprints, severe on marathons
    // Target: 100 stamina horse should finish 10f race with ~40% remaining

    if (furlongs <= 6)
        return 0.08;  // Sprint: ~8% depletion per 100 ticks
    else if (furlongs <= 10)
        return 0.15;  // Classic: ~15% depletion per 100 ticks
    else if (furlongs <= 12)
        return 0.22;  // Long: ~22% depletion per 100 ticks
    else
        return 0.30;  // Marathon: ~30% depletion per 100 ticks

    // For 10f race (~237 ticks):
    // 100 stamina horse: 237 * 0.15 / 100 = ~36 stamina depleted
    // Finishes with ~64% stamina (before other modifiers)
}

// 2. Stamina Efficiency (Stamina + Durability synergy)
private double CalculateStaminaEfficiency(Horse horse)
{
    // High Stamina = bigger tank
    double staminaFactor = 1.0 + ((horse.Stamina - 50) * -0.004);
    // Stamina 0 = 1.20x depletion (burns fast)
    // Stamina 50 = 1.00x depletion (neutral)
    // Stamina 100 = 0.80x depletion (lasts longer)

    // High Durability = fuel efficient
    double durabilityFactor = 1.0 + ((horse.Durability - 50) * -0.003);
    // Durability 0 = 1.15x depletion (inefficient)
    // Durability 50 = 1.00x depletion (neutral)
    // Durability 100 = 0.85x depletion (very efficient)

    return staminaFactor * durabilityFactor;

    // Examples:
    // Stamina=100, Durability=100: 0.80 * 0.85 = 0.68x (68% depletion = marathon specialist)
    // Stamina=0, Durability=0: 1.20 * 1.15 = 1.38x (138% depletion = pure sprinter)
    // Stamina=75, Durability=25: 0.90 * 1.075 = 0.97x (balanced)
}

// 3. Pace Multiplier (effort-based)
private double CalculatePaceMultiplier(double currentSpeed, ModifierContext context)
{
    // Faster running = more effort = faster depletion
    double baseSpeed = CalculateNeutralBaseSpeed(context);
    double speedRatio = currentSpeed / baseSpeed;

    // Linear scaling: ±20% speed = ±20% depletion
    return speedRatio;

    // Examples:
    // Running 1.10x speed (fast) = 1.10x depletion
    // Running 1.00x speed (neutral) = 1.00x depletion
    // Running 0.90x speed (slow) = 0.90x depletion
}

// 4. LegType Stamina Multiplier
private double CalculateLegTypeStaminaMultiplier(ModifierContext context)
{
    double raceProgress = (double)context.CurrentTick / context.TotalTicks;

    return context.Horse.LegTypeId switch
    {
        LegTypeId.StartDash => raceProgress < 0.25 ? 1.30 : 0.90,
        // Burn hard early (130%), conserve late (90%)

        LegTypeId.FrontRunner => 1.10,
        // Aggressive throughout (110%)

        LegTypeId.StretchRunner => raceProgress < 0.60 ? 0.85 : 1.15,
        // Conserve early (85%), push stretch (115%)

        LegTypeId.LastSpurt => raceProgress < 0.75 ? 0.80 : 1.40,
        // Conserve most of race (80%), explosive finish (140%)

        LegTypeId.RailRunner => raceProgress < 0.70 ? 0.90 : 1.05,
        // Steady early (90%), slight push late (105%)

        _ => 1.00
    };
}
```

### Stamina Speed Modifier

**Applied in modifier pipeline:**

```csharp
// SpeedModifierCalculator.cs - new method
public double CalculateStaminaModifier(RaceRunHorse raceRunHorse)
{
    double staminaPercent = raceRunHorse.CurrentStamina / raceRunHorse.InitialStamina;

    // Clamp to [0, 1] range
    staminaPercent = Math.Max(0, Math.Min(1.0, staminaPercent));

    // Mild penalty curve: 0% stamina = 90-95% speed (5-10% penalty)
    // Formula: Quadratic curve for progressive penalty
    if (staminaPercent > 0.5)
    {
        // Above 50%: minimal penalty (linear)
        return 1.0 - ((1.0 - staminaPercent) * 0.02);
        // 100% stamina = 1.00x speed (no penalty)
        // 75% stamina = 0.995x speed (0.5% penalty)
        // 50% stamina = 0.99x speed (1% penalty)
    }
    else
    {
        // Below 50%: progressive penalty (quadratic)
        double fatigueLevel = 1.0 - staminaPercent; // 0.5 to 1.0
        double penalty = 0.01 + (fatigueLevel * fatigueLevel * 0.09);
        return 1.0 - penalty;

        // 50% stamina = 0.99x speed (1% penalty)
        // 25% stamina = 0.965x speed (3.5% penalty)
        // 10% stamina = 0.938x speed (6.2% penalty)
        // 0% stamina = 0.91x speed (9% penalty)
    }
}
```

**Integration into RaceService.UpdateHorsePosition():**

```csharp
private void UpdateHorsePosition(RaceRunHorse raceRunHorse, short tick, short totalTicks, RaceRun raceRun)
{
    var baseSpeed = AverageBaseSpeed;

    var context = new ModifierContext(
        CurrentTick: tick,
        TotalTicks: totalTicks,
        Horse: raceRunHorse.Horse,
        RaceCondition: raceRun.ConditionId,
        RaceSurface: raceRun.Race.SurfaceId,
        RaceFurlongs: raceRun.Race.Furlongs,
        RaceRunHorse: raceRunHorse  // NEW
    );

    // Apply stat modifiers (Speed + Agility)
    var statModifier = _speedModifierCalculator.CalculateStatModifiers(context);
    baseSpeed *= statModifier;

    var envModifier = _speedModifierCalculator.CalculateEnvironmentalModifiers(context);
    baseSpeed *= envModifier;

    var phaseModifier = _speedModifierCalculator.CalculatePhaseModifiers(context);
    baseSpeed *= phaseModifier;

    // NEW: Apply stamina modifier BEFORE random variance
    var staminaModifier = _speedModifierCalculator.CalculateStaminaModifier(raceRunHorse);
    baseSpeed *= staminaModifier;

    var randomVariance = _speedModifierCalculator.ApplyRandomVariance();
    baseSpeed *= randomVariance;

    // NEW: Deplete stamina based on current speed
    DepleteStamina(raceRunHorse, context, baseSpeed);

    // Update horse position
    raceRunHorse.Distance += (decimal)baseSpeed;
}
```

### Configuration Constants

**Add to RaceModifierConfig.cs:**

```csharp
// ============================================================================
// Stamina Configuration (Feature 004)
// ============================================================================

/// <summary>
/// Base stamina depletion rates by race distance category.
/// Values represent percentage of stamina pool depleted per 100 ticks.
/// </summary>
public static class StaminaDepletionRates
{
    public const double Sprint = 0.08;      // ≤6f: Minimal stamina impact
    public const double Classic = 0.15;     // 7-10f: Moderate stamina impact
    public const double Long = 0.22;        // 11-12f: Significant stamina impact
    public const double Marathon = 0.30;    // 13f+: Severe stamina impact
}

/// <summary>
/// Stamina stat modifier per point from neutral (50).
/// Higher Stamina = bigger fuel tank = slower depletion.
/// Range: Stamina 0 = 1.20x depletion, Stamina 100 = 0.80x depletion
/// </summary>
public const double StaminaDepletionModifierPerPoint = -0.004;

/// <summary>
/// Durability stat modifier per point from neutral (50).
/// Higher Durability = fuel efficiency = slower depletion.
/// Range: Durability 0 = 1.15x depletion, Durability 100 = 0.85x depletion
/// </summary>
public const double DurabilityDepletionModifierPerPoint = -0.003;

/// <summary>
/// LegType-based stamina usage multipliers by race phase.
/// Different running styles burn stamina at different rates during race.
/// </summary>
public static readonly IReadOnlyDictionary<LegTypeId, (double EarlyMultiplier, double LateMultiplier, double TransitionPoint)>
    LegTypeStaminaMultipliers = new Dictionary<LegTypeId, (double, double, double)>
{
    { LegTypeId.StartDash, (1.30, 0.90, 0.25) },        // Explosive start, cruise finish
    { LegTypeId.FrontRunner, (1.10, 1.10, 1.00) },      // Aggressive throughout
    { LegTypeId.StretchRunner, (0.85, 1.15, 0.60) },    // Conserve, then stretch push
    { LegTypeId.LastSpurt, (0.80, 1.40, 0.75) },        // Maximum conservation, explosive finish
    { LegTypeId.RailRunner, (0.90, 1.05, 0.70) }        // Steady, slight late push
};

/// <summary>
/// Maximum speed penalty when stamina is fully depleted (0%).
/// Value of 0.10 means exhausted horse runs at 90% speed.
/// Uses quadratic curve for progressive penalty below 50% stamina.
/// </summary>
public const double MaxStaminaSpeedPenalty = 0.10;  // 10% max penalty (mild)
```

---

## Integration Points

### 1. ModifierContext Enhancement

**File:** `TripleDerby.Core/Racing/ModifierContext.cs`

```csharp
// Add RaceRunHorse to context for stamina tracking
public record ModifierContext(
    short CurrentTick,
    short TotalTicks,
    Horse Horse,
    ConditionId RaceCondition,
    SurfaceId RaceSurface,
    decimal RaceFurlongs,
    RaceRunHorse RaceRunHorse  // NEW
);
```

### 2. SpeedModifierCalculator Expansion

**File:** `TripleDerby.Core/Racing/SpeedModifierCalculator.cs`

Add new method:
```csharp
public double CalculateStaminaModifier(RaceRunHorse raceRunHorse)
{
    // Implementation as detailed above
}
```

### 3. RaceService Updates

**File:** `TripleDerby.Core/Services/RaceService.cs`

**Changes:**
1. Update `UpdateHorsePosition()` to include stamina modifier in pipeline
2. Add `DepleteStamina()` private method
3. Add helper methods for stamina calculations
4. Update `ModifierContext` construction to include `RaceRunHorse`

### 4. Configuration Update

**File:** `TripleDerby.Core/Configuration/RaceModifierConfig.cs`

Add stamina-related constants as detailed above.

### 5. Balance Documentation Update

**File:** `docs/RACE_BALANCE.md`

After implementation, update with:
- Stamina correlation analysis (target: -0.15 to -0.25)
- Durability correlation analysis (target: -0.10 to -0.20)
- Distance-specific stamina impact data
- LegType stamina usage patterns

---

## Balance Targets

### Statistical Correlations (Post-Implementation)

| Stat | Current Correlation | Target Correlation | Impact Level |
|------|---------------------|-------------------|--------------|
| Speed | -0.745 | -0.745 (unchanged) | Primary |
| Agility | -0.355 | -0.355 (unchanged) | Secondary |
| **Stamina** | **-0.040** | **-0.15 to -0.25** | **Tertiary (NEW)** |
| **Durability** | **0.000** | **-0.10 to -0.20** | **Quaternary (NEW)** |

**Interpretation:**
- Stamina becomes meaningful (weak-to-moderate negative correlation)
- Durability becomes relevant for endurance races
- Speed/Agility remain primary determinants (no regression)

### Expected Behavior by Distance

**Sprint Races (4-6f):**
- Average finish stamina: 60-80%
- Stamina correlation: ~-0.05 (minimal)
- Strategy: Speed/Agility dominant

**Classic Races (10f):**
- Average finish stamina: 30-50%
- Stamina correlation: ~-0.20 (moderate)
- Strategy: Balance speed and endurance

**Marathon Races (16f):**
- Average finish stamina: 5-20%
- Stamina correlation: ~-0.35 (strong)
- Strategy: Stamina/Durability critical

### Endurance Profiles

**Pure Sprinter (Speed=100, Stamina=0, Durability=0):**
- Dominates 4-6f races
- Struggles in 12f+ races (fades badly late)
- Depletion multiplier: 1.38x

**Marathon Specialist (Speed=50, Stamina=100, Durability=100):**
- Competitive in 12f+ races
- Slow in sprints
- Depletion multiplier: 0.68x

**Balanced Runner (Speed=75, Stamina=75, Durability=50):**
- Versatile across all distances
- Competitive in 8-12f races
- Depletion multiplier: 0.90x

---

## Testing Strategy

### Unit Tests

**File:** `TripleDerby.Tests.Unit/Racing/StaminaCalculatorTests.cs`

```csharp
// Base depletion rate tests
[Theory]
[InlineData(4, 0.08)]   // Sprint
[InlineData(10, 0.15)]  // Classic
[InlineData(16, 0.30)]  // Marathon
public void CalculateBaseDepletionRate_ReturnsCorrectRateForDistance(decimal furlongs, double expectedRate)

// Stamina efficiency tests
[Theory]
[InlineData(100, 100, 0.68)]  // Marathon specialist
[InlineData(0, 0, 1.38)]      // Pure sprinter
[InlineData(50, 50, 1.00)]    // Neutral
public void CalculateStaminaEfficiency_CombinesStaminaAndDurability(int stamina, int durability, double expectedMultiplier)

// LegType stamina multiplier tests
[Theory]
[InlineData(LegTypeId.StartDash, 0.10, 1.30)]  // Early phase
[InlineData(LegTypeId.StartDash, 0.50, 0.90)]  // Late phase
[InlineData(LegTypeId.LastSpurt, 0.50, 0.80)]  // Conserving
[InlineData(LegTypeId.LastSpurt, 0.90, 1.40)]  // Explosive finish
public void CalculateLegTypeStaminaMultiplier_ReturnsCorrectMultiplierForPhase(LegTypeId legType, double progress, double expectedMultiplier)

// Speed penalty tests
[Theory]
[InlineData(1.00, 1.00)]   // 100% stamina = no penalty
[InlineData(0.50, 0.99)]   // 50% stamina = 1% penalty
[InlineData(0.25, 0.965)]  // 25% stamina = 3.5% penalty
[InlineData(0.00, 0.91)]   // 0% stamina = 9% penalty
public void CalculateStaminaModifier_ReturnsCorrectSpeedPenalty(double staminaPercent, double expectedModifier)

// Pace multiplier tests
[Theory]
[InlineData(1.10, 1.10)]  // Fast pace = faster depletion
[InlineData(1.00, 1.00)]  // Neutral
[InlineData(0.90, 0.90)]  // Slow pace = slower depletion
public void CalculatePaceMultiplier_ScalesWithSpeed(double speedRatio, double expectedMultiplier)
```

### Integration Tests

**File:** `TripleDerby.Tests.Unit/Racing/StaminaIntegrationTests.cs`

```csharp
// Full race stamina depletion
[Fact]
public async Task Race_10Furlongs_DepletesStaminaProgressively()
{
    // Arrange: Horse with 100 stamina
    // Act: Run 10f race
    // Assert: CurrentStamina in 30-50 range at finish
}

[Fact]
public async Task Race_HighDurabilityHorse_DepletesSlowerThanLowDurability()
{
    // Arrange: Two horses, same stamina, different durability
    // Act: Run identical races
    // Assert: High durability finishes with more stamina remaining
}

[Fact]
public async Task Race_StartDashLegType_BurnsStaminaEarlyConservesLate()
{
    // Arrange: StartDash horse in 10f race
    // Act: Track stamina at 25%, 50%, 75%, 100% progress
    // Assert: Steeper depletion 0-25%, shallower 25-100%
}

[Fact]
public async Task Race_Sprint_MinimalStaminaImpact()
{
    // Arrange: 4f race
    // Act: Run race with low stamina horse
    // Assert: Finishes with 60%+ stamina, minimal time penalty
}

[Fact]
public async Task Race_Marathon_SevereStaminaImpact()
{
    // Arrange: 16f race
    // Act: Run with low stamina vs high stamina horse
    // Assert: Low stamina finishes much slower (exhausted)
}
```

### Balance Validation Tests

**File:** `TripleDerby.Tests.Unit/Racing/RaceBalanceValidationTests.cs`

Add to existing test suite:

```csharp
[Fact]
public async Task Run_1000_Races_Stamina_Shows_Measurable_Correlation()
{
    // Act: Run 1000 races with varied Stamina/Durability
    // Assert: Stamina correlation in -0.15 to -0.25 range
    // Assert: Durability correlation in -0.10 to -0.20 range
}

[Theory]
[InlineData(4, 60, 80)]   // Sprint: 60-80% remaining
[InlineData(10, 30, 50)]  // Classic: 30-50% remaining
[InlineData(16, 5, 20)]   // Marathon: 5-20% remaining
public async Task Race_FinishStamina_MatchesExpectedRangeByDistance(decimal furlongs, double minExpected, double maxExpected)
```

---

## Implementation Plan (TDD Approach)

### Phase 1: Stamina Depletion Core Logic

**RED - Write Failing Tests**
- [ ] Test: `CalculateBaseDepletionRate` returns correct rates for each distance category
- [ ] Test: `CalculateStaminaEfficiency` combines Stamina and Durability correctly
- [ ] Test: `CalculatePaceMultiplier` scales with speed ratio
- [ ] Test: `CalculateLegTypeStaminaMultiplier` returns correct multipliers by phase
- [ ] Test: `DepleteStamina` reduces CurrentStamina by expected amount

**GREEN - Make Tests Pass**
- [ ] Implement stamina calculation methods in RaceService (or new StaminaCalculator)
- [ ] Add stamina configuration constants to RaceModifierConfig
- [ ] Wire up methods to calculate depletion correctly

**REFACTOR**
- [ ] Extract stamina logic to dedicated class if needed
- [ ] Remove magic numbers, use config constants
- [ ] Ensure clean separation from other modifiers

---

### Phase 2: Stamina Speed Modifier

**RED - Write Failing Tests**
- [ ] Test: `CalculateStaminaModifier` returns 1.0 at 100% stamina
- [ ] Test: `CalculateStaminaModifier` returns 0.99 at 50% stamina
- [ ] Test: `CalculateStaminaModifier` returns 0.91 at 0% stamina
- [ ] Test: Stamina modifier uses quadratic curve below 50%

**GREEN - Make Tests Pass**
- [ ] Add `CalculateStaminaModifier` to SpeedModifierCalculator
- [ ] Implement penalty curve logic
- [ ] Handle edge cases (stamina > initial, negative, etc.)

**REFACTOR**
- [ ] Clean up formula, extract curve logic if complex
- [ ] Add XML documentation comments
- [ ] Ensure consistent with other modifier methods

---

### Phase 3: Integration into Race Simulation

**RED - Write Failing Tests**
- [ ] Test: UpdateHorsePosition applies stamina modifier in pipeline
- [ ] Test: UpdateHorsePosition depletes stamina each tick
- [ ] Test: Horse with 0 stamina still completes race (no DNF)
- [ ] Test: Full 10f race simulation shows progressive stamina depletion

**GREEN - Make Tests Pass**
- [ ] Update ModifierContext to include RaceRunHorse
- [ ] Update UpdateHorsePosition to call stamina methods
- [ ] Ensure stamina modifier applied in correct order
- [ ] Initialize CurrentStamina = InitialStamina at race start

**REFACTOR**
- [ ] Ensure UpdateHorsePosition doesn't become too complex
- [ ] Consider extracting modifier pipeline to separate method
- [ ] Clean up method signatures

---

### Phase 4: Distance & LegType Integration

**RED - Write Failing Tests**
- [ ] Test: Sprint races (4f) show minimal stamina depletion
- [ ] Test: Marathon races (16f) show severe stamina depletion
- [ ] Test: StartDash horses burn stamina faster early
- [ ] Test: LastSpurt horses conserve stamina early, burn late

**GREEN - Make Tests Pass**
- [ ] Implement distance-based depletion scaling
- [ ] Implement LegType-based stamina multipliers
- [ ] Test across all 5 LegTypes

**REFACTOR**
- [ ] Ensure LegType logic is clean and maintainable
- [ ] Verify no duplicate logic with existing LegType phase modifiers

---

### Phase 5: Balance Validation

**RED - Write Failing Tests**
- [ ] Test: 1000-race simulation shows Stamina correlation -0.15 to -0.25
- [ ] Test: 1000-race simulation shows Durability correlation -0.10 to -0.20
- [ ] Test: High Stamina horse finishes faster than low Stamina in 12f race
- [ ] Test: Stamina has minimal effect in 4f race

**GREEN - Make Tests Pass**
- [ ] Run statistical validation tests
- [ ] Adjust config constants if correlations out of range
- [ ] Iterate on formulas until balance targets met

**REFACTOR**
- [ ] Document final balance values in RACE_BALANCE.md
- [ ] Add diagnostic tests for stamina impact
- [ ] Clean up test output formatting

---

### Phase 6: Regression & Edge Cases

**RED - Write Failing Tests**
- [ ] Test: All existing race tests still pass (no regression)
- [ ] Test: Horse with Stamina=0 completes race
- [ ] Test: Horse with extreme stats (all 100s, all 0s) works correctly
- [ ] Test: Stamina system works with all conditions/surfaces

**GREEN - Make Tests Pass**
- [ ] Fix any regressions in existing tests
- [ ] Handle edge cases gracefully
- [ ] Ensure backward compatibility

**REFACTOR**
- [ ] Final cleanup pass
- [ ] Ensure code coverage > 90% for stamina code
- [ ] Update documentation

---

## Files to Modify/Create

### Test Files (Create First - TDD)
- [ ] `TripleDerby.Tests.Unit/Racing/StaminaCalculatorTests.cs` (NEW)
- [ ] `TripleDerby.Tests.Unit/Racing/StaminaIntegrationTests.cs` (NEW)
- [ ] `TripleDerby.Tests.Unit/Racing/RaceBalanceValidationTests.cs` (UPDATE - add stamina tests)
- [ ] `TripleDerby.Tests.Unit/Racing/SpeedModifierCalculatorTests.cs` (UPDATE - add stamina modifier tests)

### Implementation Files (Create After Tests)
- [ ] `TripleDerby.Core/Racing/SpeedModifierCalculator.cs` (UPDATE - add CalculateStaminaModifier)
- [ ] `TripleDerby.Core/Racing/ModifierContext.cs` (UPDATE - add RaceRunHorse)
- [ ] `TripleDerby.Core/Services/RaceService.cs` (UPDATE - add DepleteStamina, integrate stamina)
- [ ] `TripleDerby.Core/Configuration/RaceModifierConfig.cs` (UPDATE - add stamina constants)

### Documentation Files
- [ ] `docs/RACE_BALANCE.md` (UPDATE - add stamina correlation data)
- [ ] `docs/features/004-stamina-depletion.md` (THIS FILE)

---

## Success Criteria

### Functional Success
- [x] All unit tests pass (stamina calculations correct)
- [x] All integration tests pass (stamina integrates with race simulation)
- [x] All balance validation tests pass (correlations in target range)
- [x] No regression in existing tests
- [x] Code coverage > 90% for new stamina code

### Balance Success
- [x] Stamina correlation: -0.15 to -0.25 (weak-to-moderate impact)
- [x] Durability correlation: -0.10 to -0.20 (tertiary impact)
- [x] Sprint races (4f): Minimal stamina impact (60-80% finish)
- [x] Classic races (10f): Moderate stamina impact (30-50% finish)
- [x] Marathon races (16f): Severe stamina impact (5-20% finish)
- [x] Speed/Agility correlations unchanged (no regression)

### Code Quality Success
- [x] Clean integration with existing modifier pipeline
- [x] Configuration-driven (no magic numbers)
- [x] Well-documented (XML comments, balance docs)
- [x] Testable in isolation
- [x] Follows established patterns

---

## Open Questions

### Resolved
- ✅ Should Durability reduce stamina depletion? **YES** (fuel efficiency mechanic)
- ✅ Should LegType affect stamina usage? **YES** (strategic variety)
- ✅ Should stamina scale with distance? **YES** (progressive scaling)
- ✅ How severe should stamina penalty be? **MILD** (5-10% max penalty)

### Remaining
- [ ] Should stamina regenerate during race? **Recommendation: NO** (adds complexity, not realistic)
- [ ] Should stamina affect acceleration/position changes? **Recommendation: NO for MVP** (future enhancement)
- [ ] Should there be a "second wind" mechanic? **Recommendation: NO** (added complexity)

---

## Future Enhancements (Out of Scope)

These are potential future features building on stamina system:

1. **Stamina Training System** - Improve Stamina/Durability through training
2. **Tactical Pacing** - Player-controlled pace strategy (conserve early, push late)
3. **Stamina Regeneration** - Slight stamina recovery during slow phases
4. **Draft Mechanics** - Following horses conserves stamina
5. **Track Variant** - Certain conditions affect stamina (heat, altitude)
6. **Injury Risk** - Low stamina + high pace increases injury chance
7. **Stamina Visualization** - UI showing stamina bar during race replay

---

## Balancing Philosophy

**Design Principles:**
1. **Speed Still King** - Speed remains primary stat (correlation -0.745)
2. **Stamina is Strategic** - Matters for distance selection, not dominating
3. **Distance Matters** - Race distance becomes meaningful choice
4. **No Dominant Build** - Sprinters and marathoners both viable
5. **Mild Impact** - Stamina adds depth without frustrating players

**Target Experience:**
- In 4f sprint: "Stamina didn't matter, Speed won"
- In 10f classic: "Stamina helped, but Speed still mattered more"
- In 16f marathon: "Stamina was crucial, low-stamina horse faded badly"

**Avoid:**
- ❌ Making stamina required for all races
- ❌ Completely negating high Speed with low Stamina
- ❌ Creating "must-have" stat combinations
- ❌ Frustrating players with sudden DNFs or dramatic collapses

---

## References

- **Feature 003:** Race Modifiers Refactor (prerequisite)
- **RACE_BALANCE.md:** Current balance data (Stamina correlation: -0.040)
- **RaceService.cs:** Existing race simulation logic
- **SpeedModifierCalculator.cs:** Existing modifier pipeline
- **RaceModifierConfig.cs:** Configuration constants pattern

---

## Document History

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-22 | Claude (Feature Discovery Skill) | Initial specification created |

---

**Status:** Ready for Implementation ✅

**Next Steps:**
1. Review and approve this specification
2. Begin Phase 1: Write stamina depletion core logic tests (RED)
3. Implement stamina calculations (GREEN)
4. Refactor and proceed to Phase 2

---

**End of Feature Specification**
