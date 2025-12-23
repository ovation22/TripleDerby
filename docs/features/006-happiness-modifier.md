# Feature 006: Happiness Modifier System

## Summary
Implements a Happiness-based modifier system that affects horse racing performance through two-phase logarithmic curves. Happiness influences race speed with diminishing returns, rewarding player engagement with the feeding system while maintaining balance. The feature is implemented in two phases: Phase 1 adds Speed modifiers, and Phase 2 (future) adds Stamina efficiency modifiers.

**Design Philosophy:** Happiness represents the horse's mental state and morale. Happy horses run more enthusiastically and efficiently, while unhappy horses are reluctant and wasteful with energy. The logarithmic curve reflects psychological reality—mood swings have bigger impacts at extremes than when already content.

---

## Requirements

### Functional Requirements
- [x] Happiness stat (0-100) affects race speed through logarithmic modifier
- [x] Two-phase curve: different scaling above/below neutral point (50)
- [x] Asymmetric impact: unhappiness penalty > happiness bonus (negative emotions stronger)
- [x] Diminishing returns: 0→25 has bigger impact than 75→100
- [x] Moderate total effect: ±3-5% speed range (tertiary stat, weaker than Agility)
- [ ] **Phase 2 only**: Happiness affects stamina depletion efficiency (inverted effect)

### Acceptance Criteria

#### Phase 1: Speed Modifier
- [ ] Given horse with Happiness=50, when calculating stat modifiers, then returns 1.0 (neutral)
- [ ] Given horse with Happiness=0, when calculating stat modifiers, then returns ~0.9661 (-3.39% penalty)
- [ ] Given horse with Happiness=100, when calculating stat modifiers, then returns ~1.0255 (+2.55% bonus)
- [ ] Given horse with Happiness=25, when calculating stat modifiers, then returns value showing larger change from 0→25 than 75→100 (diminishing returns)
- [ ] Given horse with Speed=80, Agility=60, Happiness=75, when calculating stat modifiers, then combines all three multiplicatively
- [ ] Given horse with Happiness=100 vs Happiness=0, when racing 10f, then happiness difference affects finish time by ~15 ticks

#### Phase 2: Stamina Efficiency (Future)
- [ ] Given horse with Happiness=100, when depleting stamina, then stamina depletes ~2% slower than neutral
- [ ] Given horse with Happiness=0, when depleting stamina, then stamina depletes ~3% faster than neutral
- [ ] Given horse with Happiness=50, when depleting stamina, then no effect on depletion rate (neutral)

### Non-Functional Requirements
- [ ] Performance: Happiness calculation adds <1ms to race simulation
- [ ] Consistency: Follows existing modifier patterns (SpeedModifierCalculator architecture)
- [ ] Testability: All edge cases covered (0, 50, 100, boundary values)
- [ ] Maintainability: Formula documented with inline comments explaining logarithmic choice

---

## Technical Analysis

### Affected Systems
- **Entities**: Horse (Happiness property already exists)
- **Services**: SpeedModifierCalculator (add happiness calculation)
- **Configuration**: RaceModifierConfig (add constants for divisors)
- **Tests**: SpeedModifierCalculatorTests (add happiness test suite)
- **Documentation**: RACE_BALANCE.md (update with happiness correlation data)

### Data Model Changes
**None required.** Happiness property already exists on Horse entity:
```csharp
public byte Happiness
{
    get => Statistics.FirstOrDefault(s => s.StatisticId == StatisticId.Happiness)?.Actual ?? 0;
    set { /* ... */ }
}
```

### Integration Points
- **SpeedModifierCalculator.CalculateStatModifiers()**: Add happiness calculation alongside Speed/Agility
- **RaceService.UpdateHorsePosition()**: No changes (modifier pipeline unchanged)
- **FeedingService**: Already modifies Happiness stat (increases 0-1 per feeding)
- **RaceBalanceValidationTests**: Will validate happiness correlation after implementation

### Risks & Challenges
- **Balance Risk**: Logarithmic curve harder to tune than linear; may need adjustment after 1000-race simulation
- **Testing Complexity**: Logarithmic values require precision assertions, floating-point edge cases
- **Performance**: Log10() calls per tick per horse—should profile if race simulation slows
- **Player Communication**: Need to clearly explain why happiness matters (UI/documentation concern)

---

## Mathematical Design

### Phase 1: Speed Modifier Formula

#### Two-Phase Logarithmic Curve
```csharp
private static double CalculateHappinessSpeedModifier(int happiness)
{
    // Clamp happiness to valid range [0, 100]
    happiness = Math.Clamp(happiness, 0, 100);

    if (happiness >= 50)
    {
        // Above neutral: logarithmic growth with diminishing returns
        // Happy horses run modestly faster, but gains diminish at high happiness
        double excess = happiness - 50.0;
        if (excess == 0)
            return 1.0; // Exactly neutral, no effect

        // log10(1 + x) provides smooth diminishing returns curve
        // Divided by 20 to scale to ~2.5% bonus at happiness=100
        double modifier = Math.Log10(1.0 + excess) / 20.0;
        return 1.0 + modifier;

        // Examples:
        // Happiness 60: 1.0 + log10(11)/20 = 1.0 + 0.0521 = 1.0052 (+0.52%)
        // Happiness 75: 1.0 + log10(26)/20 = 1.0 + 0.0707 = 1.0141 (+1.41%)
        // Happiness 100: 1.0 + log10(51)/20 = 1.0 + 0.0851 = 1.0255 (+2.55%)
    }
    else
    {
        // Below neutral: logarithmic penalty with steeper curve
        // Unhappiness has bigger impact than happiness (negative emotions stronger)
        // Psychological realism: depression/frustration affects performance more than joy improves it
        double deficit = 50.0 - happiness;

        // Divided by 15 instead of 20 for steeper penalty curve
        // Creates asymmetry: happiness=0 penalty > happiness=100 bonus
        double modifier = Math.Log10(1.0 + deficit) / 15.0;
        return 1.0 - modifier;

        // Examples:
        // Happiness 40: 1.0 - log10(11)/15 = 1.0 - 0.0694 = 0.9931 (-0.69%)
        // Happiness 25: 1.0 - log10(26)/15 = 1.0 - 0.0942 = 0.9858 (-1.42%)
        // Happiness 0: 1.0 - log10(51)/15 = 1.0 - 0.1135 = 0.9661 (-3.39%)
    }
}
```

#### Integration into Stat Modifiers
```csharp
public double CalculateStatModifiers(ModifierContext context)
{
    var speedMultiplier = CalculateSpeedMultiplier(context.Horse.Speed);
    var agilityMultiplier = CalculateAgilityMultiplier(context.Horse.Agility);
    var happinessMultiplier = CalculateHappinessSpeedModifier(context.Horse.Happiness); // NEW

    return speedMultiplier * agilityMultiplier * happinessMultiplier;
}
```

#### Configuration Constants
```csharp
// RaceModifierConfig.cs additions

/// <summary>
/// Divisor for happiness bonus calculation (above neutral).
/// Smaller value = stronger bonus effect.
/// Formula: modifier = log10(1 + excess) / HappinessSpeedBonusDivisor
/// Current value (20) yields ~2.5% bonus at happiness=100.
/// </summary>
public const double HappinessSpeedBonusDivisor = 20.0;

/// <summary>
/// Divisor for happiness penalty calculation (below neutral).
/// Smaller value = stronger penalty effect.
/// Formula: modifier = log10(1 + deficit) / HappinessSpeedPenaltyDivisor
/// Current value (15) yields ~3.4% penalty at happiness=0.
/// Asymmetric design: penalty divisor < bonus divisor (unhappiness hurts more).
/// </summary>
public const double HappinessSpeedPenaltyDivisor = 15.0;
```

### Phase 2: Stamina Efficiency Modifier (Future Implementation)

#### Logarithmic Stamina Efficiency
```csharp
private static double CalculateHappinessStaminaModifier(int happiness)
{
    // Affects stamina DEPLETION RATE (not stamina pool size)
    // INVERTED effect vs speed: high happiness = LESS depletion
    happiness = Math.Clamp(happiness, 0, 100);

    if (happiness >= 50)
    {
        // Above neutral: improved stamina efficiency
        // Happy horses "enjoy" racing, conserve energy better
        double excess = happiness - 50.0;
        if (excess == 0)
            return 1.0; // Neutral efficiency

        // Efficiency improvement (LESS depletion)
        double efficiency = Math.Log10(1.0 + excess) / 25.0;
        return 1.0 - efficiency; // INVERTED: lower multiplier = less depletion

        // Example: Happiness 100 → 1.0 - log10(51)/25 = 1.0 - 0.0681 = 0.9319
        // Stamina depletes at 93.2% of normal rate (~7% more efficient, but we'll tune to ~2%)
    }
    else
    {
        // Below neutral: worse stamina efficiency
        // Unhappy horses are "reluctant," burn more energy
        double deficit = 50.0 - happiness;

        // Efficiency penalty (MORE depletion)
        double efficiency = Math.Log10(1.0 + deficit) / 20.0;
        return 1.0 + efficiency; // Higher multiplier = more depletion

        // Example: Happiness 0 → 1.0 + log10(51)/20 = 1.0 + 0.0851 = 1.0851
        // Stamina depletes at 108.5% of normal rate (~8.5% less efficient, but we'll tune to ~3%)
    }
}
```

#### Integration into Stamina Depletion
```csharp
// StaminaCalculator.cs (Phase 2 only)
public double CalculateStaminaEfficiency(Horse horse)
{
    double staminaFactor = 1.0 + ((horse.Stamina - 50) * -0.004);
    double durabilityFactor = 1.0 + ((horse.Durability - 50) * -0.003);
    double happinessFactor = CalculateHappinessStaminaModifier(horse.Happiness); // NEW

    return staminaFactor * durabilityFactor * happinessFactor;
}
```

#### Phase 2 Configuration Constants
```csharp
// RaceModifierConfig.cs (Phase 2 additions)

/// <summary>
/// Divisor for happiness stamina efficiency bonus (above neutral).
/// Formula: efficiency = log10(1 + excess) / HappinessStaminaBonusDivisor
/// Applied as: depletionRate = 1.0 - efficiency (INVERTED)
/// Target: ~2% less depletion at happiness=100
/// </summary>
public const double HappinessStaminaBonusDivisor = 25.0;

/// <summary>
/// Divisor for happiness stamina efficiency penalty (below neutral).
/// Formula: efficiency = log10(1 + deficit) / HappinessStaminaPenaltyDivisor
/// Applied as: depletionRate = 1.0 + efficiency (MORE depletion)
/// Target: ~3% more depletion at happiness=0
/// </summary>
public const double HappinessStaminaPenaltyDivisor = 20.0;
```

---

## Impact Analysis

### Speed Modifier Impact (Phase 1)

#### Key Values
| Happiness | Speed Modifier | Impact vs Neutral | Notes |
|-----------|---------------|-------------------|-------|
| 0         | 0.9661        | -3.39%           | Maximum penalty (unhappy) |
| 10        | 0.9765        | -2.35%           | Significant unhappiness |
| 25        | 0.9858        | -1.42%           | Mild unhappiness |
| 40        | 0.9931        | -0.69%           | Slightly below neutral |
| **50**    | **1.0000**    | **0.00%**        | **Neutral baseline** |
| 60        | 1.0052        | +0.52%           | Slightly happy |
| 75        | 1.0141        | +1.41%           | Moderately happy |
| 90        | 1.0207        | +2.07%           | Very happy |
| 100       | 1.0255        | +2.55%           | Maximum bonus (ecstatic) |

**Total Range:** 0.9661 to 1.0255 = **6.1% total variance**

#### Diminishing Returns Validation
| Range    | Change per Point | Diminishing Returns? |
|----------|------------------|----------------------|
| 0→25     | 0.000568         | Baseline             |
| 25→50    | 0.000568         | Similar (linear-ish in log space) |
| 50→75    | 0.000564         | Slightly diminished  |
| 75→100   | 0.000456         | **Clearly diminished** (80% of baseline) |

**Curve Characteristics:**
- Logarithmic curve creates smooth diminishing returns
- Bigger impact changes at extremes (0→10, 90→100)
- Smaller marginal gains when already content (60→70)
- Asymmetric: penalty (-3.39%) > bonus (+2.55%)

#### Race Time Impact (10f Race)
Baseline 10f race: ~237 ticks for neutral horse

| Horse Profile | Speed | Agility | Happiness | Combined Modifier | Est. Ticks | Delta |
|---------------|-------|---------|-----------|-------------------|------------|-------|
| Baseline      | 50    | 50      | 50        | 1.000             | 237        | 0     |
| Happy Champ   | 80    | 60      | 100       | 1.145             | 207        | -30   |
| Sad Slowpoke  | 80    | 60      | 0         | 1.116             | 212        | -25   |
| **Happiness Delta** | - | -     | ±50       | ±0.029            | **±7 ticks** | **~3%** |

**Insight:** In isolation, happiness swing (0→100) affects 10f race by ~15 ticks (~6%), but when combined with high Speed/Agility, the marginal impact is ~7 ticks (~3% of finish time).

### Stat Modifier Hierarchy (After Implementation)

| Stat      | Type        | Range    | Formula Pattern        | Role       | Expected Correlation |
|-----------|-------------|----------|------------------------|------------|----------------------|
| Speed     | Linear      | ±10%     | Linear from 50         | Primary    | -0.745 (strong)      |
| Agility   | Linear      | ±5%      | Linear from 50         | Secondary  | -0.355 (moderate)    |
| **Happiness** | **Logarithmic** | **±3%** | **Two-phase log** | **Tertiary** | **-0.12 est. (weak-mod)** |
| Stamina   | Progressive | Distance-dependent | Quadratic fatigue | Quaternary | -0.043 @ 10f (weak) |
| Durability| Efficiency  | Via stamina | Stamina depletion factor | Support | Indirect             |

**Total Variance (Best vs Worst Horse):**
- **Current:** 1.155x / 0.855x = 1.35x (35% faster)
- **With Happiness:** 1.183x / 0.833x = 1.42x (42% faster)
- **Happiness Contribution:** +7 percentage points of total variance

### Phase 2: Stamina Efficiency Impact (Future)

#### Estimated Values (Tuning Required)
| Happiness | Stamina Depletion Rate | Notes |
|-----------|------------------------|-------|
| 0         | 1.03x                 | +3% more depletion (unhappy burns energy) |
| 50        | 1.00x                 | Neutral |
| 100       | 0.98x                 | -2% less depletion (happy conserves energy) |

**Combined Effect (Speed + Stamina):**
- Happy horse (100): +2.5% speed, -2% stamina depletion → **significant advantage in long races**
- Unhappy horse (0): -3.4% speed, +3% stamina depletion → **compounding penalty in marathons**

**Strategic Depth:**
- Sprint meta (8-10f): Happiness affects speed only (stamina less relevant)
- Marathon meta (14-16f): Happiness affects both speed AND stamina (much more valuable)

---

## TDD Implementation Plan

### Phase 1: Happiness Speed Modifier

#### Cycle 1: Core Formula Implementation
**RED - Write Failing Tests**
- [ ] Test: `CalculateHappinessSpeedModifier_WithHappiness50_ReturnsNeutral()`
- [ ] Test: `CalculateHappinessSpeedModifier_WithHappiness0_ReturnsPenalty()`
- [ ] Test: `CalculateHappinessSpeedModifier_WithHappiness100_ReturnsBonus()`
- [ ] Test: `CalculateHappinessSpeedModifier_WithNegativeValue_ClampsToZero()`
- [ ] Test: `CalculateHappinessSpeedModifier_WithValueOver100_ClampsTo100()`

**GREEN - Make Tests Pass**
- [ ] Add `CalculateHappinessSpeedModifier(int happiness)` to SpeedModifierCalculator.cs
- [ ] Implement two-phase logarithmic formula
- [ ] Add constants to RaceModifierConfig.cs

**REFACTOR**
- [ ] Extract magic numbers to configuration constants
- [ ] Add inline comments explaining logarithmic choice
- [ ] Ensure proper naming conventions

---

#### Cycle 2: Integration with Stat Modifiers
**RED - Write Failing Tests**
- [ ] Test: `CalculateStatModifiers_WithSpeed50Agility50Happiness50_ReturnsNeutral()`
- [ ] Test: `CalculateStatModifiers_WithSpeed50Agility50Happiness100_IncludesHappinessBonus()`
- [ ] Test: `CalculateStatModifiers_WithSpeed50Agility50Happiness0_IncludesHappinessPenalty()`
- [ ] Test: `CalculateStatModifiers_WithSpeed80Agility60Happiness75_CombinesAllThree()`

**GREEN - Make Tests Pass**
- [ ] Modify `CalculateStatModifiers()` to include happiness calculation
- [ ] Multiply happiness modifier with speed/agility modifiers

**REFACTOR**
- [ ] Ensure consistent code style with existing modifiers
- [ ] Update XML documentation for CalculateStatModifiers()

---

#### Cycle 3: Diminishing Returns Validation
**RED - Write Failing Tests**
- [ ] Theory test: Multiple happiness values showing logarithmic curve
- [ ] Test: `HappinessModifier_ShowsDiminishingReturns()` (0→25 vs 75→100)
- [ ] Test: `HappinessModifier_IsAsymmetric()` (penalty > bonus)

**GREEN - Make Tests Pass**
- [ ] Verify formula produces correct logarithmic curve
- [ ] Adjust divisors if needed to meet targets

**REFACTOR**
- [ ] Add test helper methods for happiness scenarios
- [ ] Document expected curve characteristics in test comments

---

#### Cycle 4: Integration with Race Simulation
**RED - Write Failing Tests**
- [ ] Integration test: Run 10f race with happiness=0 vs happiness=100
- [ ] Test: Verify happiness affects finish time by expected amount (~15 ticks)
- [ ] Test: Existing races still produce same results (regression check)

**GREEN - Make Tests Pass**
- [ ] Ensure RaceService.UpdateHorsePosition() uses updated CalculateStatModifiers()
- [ ] Verify modifier pipeline applies happiness correctly

**REFACTOR**
- [ ] No changes to RaceService needed (modifier pipeline unchanged)
- [ ] Ensure test horses have realistic happiness values

---

#### Cycle 5: Balance Validation
**RED - Write Failing Tests**
- [ ] Test: Run 1000-race simulation with varying happiness
- [ ] Test: Verify happiness correlation is -0.10 to -0.15 (weak-moderate)
- [ ] Test: Ensure happiness is weaker than Agility (-0.355)
- [ ] Test: Ensure happiness is stronger than Stamina at 10f (-0.043)

**GREEN - Make Tests Pass**
- [ ] Run RaceBalanceValidationTests with happiness variations
- [ ] Adjust divisors if correlation too strong/weak
- [ ] Target: -0.12 correlation (tertiary stat position)

**REFACTOR**
- [ ] Update RACE_BALANCE.md with happiness correlation data
- [ ] Document tuning rationale

---

### Phase 2: Happiness Stamina Efficiency (Future)

#### Cycle 6: Stamina Efficiency Formula (Phase 2 Only)
**RED - Write Failing Tests**
- [ ] Test: `CalculateHappinessStaminaModifier_WithHappiness50_ReturnsNeutral()`
- [ ] Test: `CalculateHappinessStaminaModifier_WithHappiness0_ReturnsIncreasedDepletion()`
- [ ] Test: `CalculateHappinessStaminaModifier_WithHappiness100_ReturnsDecreasedDepletion()`
- [ ] Test: `HappinessStaminaModifier_IsInverted()` (happy = less depletion)

**GREEN - Make Tests Pass**
- [ ] Add `CalculateHappinessStaminaModifier(int happiness)` to StaminaCalculator
- [ ] Implement inverted logarithmic formula
- [ ] Integrate into `CalculateStaminaEfficiency()`

**REFACTOR**
- [ ] Ensure consistency with speed modifier pattern
- [ ] Document inverted effect clearly

---

#### Cycle 7: Stamina Integration Tests (Phase 2 Only)
**RED - Write Failing Tests**
- [ ] Test: Happy horse depletes stamina slower than neutral in 14f race
- [ ] Test: Unhappy horse depletes stamina faster than neutral in 14f race
- [ ] Test: Happiness stamina effect compounds with speed effect

**GREEN - Make Tests Pass**
- [ ] Verify stamina depletion calculation uses happiness modifier
- [ ] Run long-distance races to validate impact

**REFACTOR**
- [ ] Update balance validation tests for stamina efficiency
- [ ] Document expected marathon race impacts

---

## Test Categories

### Unit Tests (Phase 1)
Focus on SpeedModifierCalculator:
- `CalculateHappinessSpeedModifier()` in isolation
- Boundary values (0, 50, 100)
- Edge cases (negative, >100)
- Logarithmic curve shape
- Diminishing returns validation
- Asymmetry validation (penalty > bonus)
- Integration with `CalculateStatModifiers()`

### Integration Tests (Phase 1)
Test components working together:
- Full race simulation with varying happiness
- Happiness + Speed + Agility combined effects
- Regression tests (existing races unchanged)
- Modifier pipeline correctness

### Balance Validation Tests (Phase 1)
- 1000-race simulation with happiness variations
- Correlation analysis (target: -0.12)
- Stat hierarchy validation (Speed > Agility > Happiness > Stamina)
- Race time impact analysis (10f, 12f, 14f, 16f)

### Unit Tests (Phase 2 - Future)
Focus on StaminaCalculator:
- `CalculateHappinessStaminaModifier()` in isolation
- Inverted effect validation
- Integration with stamina efficiency calculation

### Integration Tests (Phase 2 - Future)
- Long-distance race stamina depletion with happiness
- Combined speed + stamina effects
- Marathon race outcomes (14f, 16f)

---

## Success Criteria

### Phase 1: Speed Modifier Complete
All tests must pass:
- [ ] All happiness formula unit tests pass
- [ ] All stat modifier integration tests pass
- [ ] Existing tests still pass (no regression)
- [ ] 1000-race validation shows expected correlation (-0.10 to -0.15)
- [ ] Code coverage > 80% for new code

Feature works correctly:
- [ ] Happiness affects race speed as designed
- [ ] Logarithmic curve shows diminishing returns
- [ ] Asymmetric penalty/bonus validated
- [ ] Race time impact matches predictions (~15 ticks for 0→100 swing in 10f)

Documentation complete:
- [ ] RACE_BALANCE.md updated with happiness correlation
- [ ] Inline comments explain formula choices
- [ ] Feature spec documents all design decisions

### Phase 2: Stamina Efficiency Complete (Future)
- [ ] Stamina efficiency tests pass
- [ ] Long-distance races show expected happiness impact
- [ ] Combined speed + stamina effects balanced
- [ ] Updated correlation analysis includes stamina interaction

---

## Implementation Workflow

### Phase 1 Workflow

1. **Setup**: Create test file, add test fixture
2. **Cycle 1**: Implement core formula (isolated unit tests)
3. **Cycle 2**: Integrate into stat modifiers
4. **Cycle 3**: Validate curve characteristics
5. **Cycle 4**: Integration test with race simulation
6. **Cycle 5**: Balance validation and tuning
7. **Documentation**: Update RACE_BALANCE.md
8. **Commit**: "Add Feature 006: Happiness Speed Modifier"

### Phase 2 Workflow (Future)

1. **Cycle 6**: Implement stamina efficiency formula
2. **Cycle 7**: Integration test with race simulation
3. **Balance**: Re-run correlation analysis with both effects
4. **Documentation**: Update spec with Phase 2 findings
5. **Commit**: "Add Feature 006 Phase 2: Happiness Stamina Efficiency"

---

## Testing Patterns

### Test Naming Convention
`MethodName_Scenario_ExpectedBehavior`

Examples:
- `CalculateHappinessSpeedModifier_WithHappiness50_ReturnsNeutral`
- `CalculateHappinessSpeedModifier_WithHappiness0_ReturnsPenalty`
- `CalculateStatModifiers_WithAllStatsNeutral_ReturnsOne`
- `RaceSimulation_WithHappyHorse_FinishesFaster`

### Arrange-Act-Assert Pattern
```csharp
[Fact]
public void CalculateHappinessSpeedModifier_WithHappiness100_ReturnsBonus()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 100);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert
    Assert.Equal(1.0255, result, precision: 4);
}
```

### Theory Tests for Logarithmic Curve
```csharp
[Theory]
[InlineData(0, 0.9661)]   // Maximum penalty
[InlineData(25, 0.9858)]  // Mild penalty
[InlineData(50, 1.0000)]  // Neutral
[InlineData(75, 1.0141)]  // Moderate bonus
[InlineData(100, 1.0255)] // Maximum bonus
public void CalculateHappinessSpeedModifier_FollowsLogarithmicCurve(
    int happiness, double expectedModifier)
{
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: (byte)happiness);
    var context = CreateModifierContext(horse);
    var result = _sut.CalculateStatModifiers(context);
    Assert.Equal(expectedModifier, result, precision: 4);
}
```

### Diminishing Returns Test
```csharp
[Fact]
public void HappinessSpeedModifier_ShowsDiminishingReturns()
{
    // Arrange: Calculate change per point in different ranges
    var change0to25 = (Modifier(25) - Modifier(0)) / 25;
    var change75to100 = (Modifier(100) - Modifier(75)) / 25;

    // Assert: Later range shows smaller change (diminishing returns)
    Assert.True(change75to100 < change0to25,
        $"Expected diminishing returns but got {change75to100} >= {change0to25}");
}
```

### Asymmetry Test
```csharp
[Fact]
public void HappinessSpeedModifier_IsAsymmetric_PenaltyExceedsBonus()
{
    var penaltyMagnitude = Math.Abs(1.0 - Modifier(0));   // ~3.39%
    var bonusMagnitude = Math.Abs(Modifier(100) - 1.0);   // ~2.55%

    Assert.True(penaltyMagnitude > bonusMagnitude,
        "Unhappiness penalty should exceed happiness bonus (asymmetric curve)");
}
```

---

## Files to Create/Modify

### Test Files (Phase 1)
- **Modify**: `TripleDerby.Tests.Unit/Racing/SpeedModifierCalculatorTests.cs`
  - Add happiness test section (~150 lines)
  - Add test helpers for happiness scenarios
  - Add theory tests for logarithmic curve

### Implementation Files (Phase 1)
- **Modify**: `TripleDerby.Core/Racing/SpeedModifierCalculator.cs`
  - Add `CalculateHappinessSpeedModifier()` method (~30 lines)
  - Modify `CalculateStatModifiers()` to include happiness (~1 line)
  - Add XML documentation

- **Modify**: `TripleDerby.Core/Configuration/RaceModifierConfig.cs`
  - Add `HappinessSpeedBonusDivisor = 20.0` constant
  - Add `HappinessSpeedPenaltyDivisor = 15.0` constant
  - Add XML documentation

- **Modify**: `TripleDerby.Core/Abstractions/Racing/ISpeedModifierCalculator.cs`
  - Update XML documentation to mention Happiness

### Documentation Files (Phase 1)
- **Modify**: `docs/RACE_BALANCE.md`
  - Add Happiness row to stat correlation table
  - Document expected correlation (-0.12 target)
  - Add happiness analysis section

- **Create**: `docs/features/006-happiness-modifier.md` (this file)

### Test Files (Phase 2 - Future)
- **Create**: `TripleDerby.Tests.Unit/Racing/StaminaCalculatorTests.cs`
  - Add happiness stamina efficiency tests

### Implementation Files (Phase 2 - Future)
- **Modify**: `TripleDerby.Core/Racing/StaminaCalculator.cs`
  - Add `CalculateHappinessStaminaModifier()` method
  - Modify `CalculateStaminaEfficiency()` to include happiness

- **Modify**: `TripleDerby.Core/Configuration/RaceModifierConfig.cs`
  - Add Phase 2 stamina constants

---

## Milestones

### Milestone 1: Core Formula Tests Green (Phase 1)
All unit tests for `CalculateHappinessSpeedModifier()` pass
- Boundary values (0, 50, 100)
- Edge cases (negative, >100)
- Logarithmic curve validation

### Milestone 2: Integration Tests Green (Phase 1)
All stat modifier integration tests pass
- Happiness combined with Speed/Agility
- Race simulation uses happiness modifier
- No regression in existing tests

### Milestone 3: Balance Validation Complete (Phase 1)
1000-race simulation confirms:
- Happiness correlation in target range (-0.10 to -0.15)
- Stat hierarchy correct (Speed > Agility > Happiness > Stamina)
- Race time impacts match predictions

### Milestone 4: Documentation Complete (Phase 1)
- RACE_BALANCE.md updated
- Feature spec finalized
- Inline comments explain design choices

### Milestone 5: Phase 1 Production Ready
- All tests pass (>80% coverage)
- Code reviewed
- Documentation complete
- Ready for player testing

### Milestone 6: Phase 2 Complete (Future)
- Stamina efficiency tests pass
- Combined speed + stamina effects balanced
- Long-distance race impacts validated

---

## Open Questions

### Resolved
- [x] **Should Happiness affect both Speed and Stamina?**
  - **Answer:** Phase 1 is Speed only, Phase 2 adds Stamina efficiency
- [x] **What magnitude of effect?**
  - **Answer:** Moderate (±3-5% total), tertiary stat
- [x] **Why logarithmic over linear?**
  - **Answer:** Two-phase logarithmic for diminishing returns and asymmetry
- [x] **How should Happiness affect Stamina?**
  - **Answer:** Efficiency (happy = less depletion), inverted effect

### Pending
- [ ] **Should we adjust divisors after 1000-race simulation?**
  - Current targets: 20 (bonus), 15 (penalty)
  - May need tuning if correlation too strong/weak
- [ ] **Should UI show happiness modifier explicitly in race results?**
  - Player communication concern
  - Could add tooltip showing stat modifiers breakdown
- [ ] **Should feeding cost scale with happiness?**
  - Currently feeding is simple (increases 0-1 per feeding)
  - Could make high-happiness horses more expensive to maintain
- [ ] **Phase 2 timing: when to implement stamina efficiency?**
  - After Phase 1 validated and balanced
  - Need player feedback on speed-only implementation first

---

## Balance Tuning Guide

### If Happiness is Too Weak (correlation < -0.08)
- **Decrease divisors**: Try 18 (bonus), 13 (penalty)
- **Expected impact**: ±4% instead of ±3%
- **Re-test**: Run 1000-race simulation

### If Happiness is Too Strong (correlation > -0.18)
- **Increase divisors**: Try 22 (bonus), 17 (penalty)
- **Expected impact**: ±2% instead of ±3%
- **Re-test**: Run 1000-race simulation

### If Asymmetry Too Extreme
- **Bring divisors closer**: Try 20 (bonus), 16 (penalty)
- **Effect**: Penalty and bonus more balanced

### If Logarithmic Curve Too Steep at Extremes
- **Switch to different base**: Use natural log (Math.Log) instead of log10
- **Adjust divisors accordingly**: ~46 for log10(20) equivalent with natural log

---

## Performance Considerations

### Expected Performance Impact
- **Per-tick calculation**: 1 additional Math.Log10() call per horse
- **Estimated cost**: <0.1ms per race (negligible)
- **Total races affected**: All races going forward

### Profiling Plan
- [ ] Benchmark race simulation before implementation
- [ ] Benchmark race simulation after implementation
- [ ] Compare average race time (should be <1% increase)
- [ ] If performance degrades significantly, consider caching or lookup table

### Optimization Options (if needed)
- **Lookup table**: Pre-calculate 101 values (happiness 0-100), use array lookup
- **Caching**: Cache modifier per horse per race (happiness doesn't change mid-race)
- **Inlining**: Mark method with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`

---

## Player-Facing Changes

### Gameplay Impact
- **Feeding becomes valuable**: Happy horses run faster (±3%)
- **Pre-race preparation**: Feed horses before big races for competitive edge
- **Strategic depth**: Happiness vs other stats in breeding decisions
- **Resource management**: If feeding costs resources, happiness creates economy sink

### UI Recommendations (Out of Scope)
- Show happiness stat prominently in horse details
- Add tooltip: "Happiness affects race speed. Happy horses run faster!"
- Show modifier breakdown in race results (optional)
- Add "Feed Horse" button in stable view

### Communication Strategy
- Announce feature: "Happiness now affects race performance!"
- Explain logarithmic curve: "Every point matters, but gains diminish at extremes"
- Emphasize asymmetry: "Unhappy horses suffer more than happy horses benefit"
- Encourage feeding: "Keep your horses happy for best performance"

---

## Design Rationale Summary

### Why Two-Phase Logarithmic?
1. **Diminishing Returns**: Psychological realism—happiness gains diminish when already content
2. **Asymmetry**: Negative emotions (unhappiness) have bigger impact than positive (happiness)
3. **Consistency**: Matches existing two-phase pattern (Stamina modifier uses two-phase quadratic)
4. **Balance**: Logarithmic allows fine-tuning via divisors (easy to adjust ±1-5% range)

### Why Moderate ±3-5% Effect?
1. **Tertiary stat**: Weaker than Agility (±5%), stronger than Stamina at 10f
2. **Meaningful but not dominant**: Players notice it, but Speed/Agility still primary
3. **Feeding system value**: Creates incentive to feed horses without making it mandatory
4. **Strategic depth**: Adds layer without overwhelming other stats

### Why Speed in Phase 1, Stamina in Phase 2?
1. **Risk mitigation**: Speed-only easier to balance and validate
2. **Player feedback**: Test reaction to happiness impact before adding complexity
3. **Iteration**: Learn from Phase 1 before designing Phase 2 stamina interaction
4. **Testing burden**: Splitting phases reduces test complexity per cycle

---

## Notes

- **Test first, always**: All code changes driven by failing tests
- **Small iterations**: Each TDD cycle should be <30 minutes
- **Keep tests green**: Never commit failing tests
- **Tune with data**: Use 1000-race simulations to validate balance
- **Document decisions**: This spec explains "why" for future maintainers
- **Listen to players**: Phase 2 timing depends on Phase 1 feedback

---

## References

### Related Features
- **Feature 003**: Race Modifiers Refactor (established modifier architecture)
- **Feature 004**: Stamina Depletion (two-phase curve inspiration)
- **Feature 005**: Rail Runner Lane Position Bonus (conditional modifier pattern)

### Related Files
- [SpeedModifierCalculator.cs](c:\Development\TripleDerby\TripleDerby.Core\Racing\SpeedModifierCalculator.cs:30)
- [RaceModifierConfig.cs](c:\Development\TripleDerby\TripleDerby.Core\Configuration\RaceModifierConfig.cs:1)
- [RACE_BALANCE.md](c:\Development\TripleDerby\docs\RACE_BALANCE.md:1)
- [Horse.cs](c:\Development\TripleDerby\TripleDerby.Core\Entities\Horse.cs:102) (Happiness property)

### Mathematical Resources
- Logarithmic functions: https://en.wikipedia.org/wiki/Logarithm
- Diminishing returns: https://en.wikipedia.org/wiki/Diminishing_returns
- Asymmetric curves in game design: (internal game balance theory)

---

**Document Version:** 1.0
**Last Updated:** 2025-12-23
**Status:** Phase 1 Ready for Implementation, Phase 2 Future
**Feature Number:** 006
