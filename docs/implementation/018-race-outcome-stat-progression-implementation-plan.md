# Implementation Plan: Feature 018 - Race Outcome Stat Progression

**Feature Spec:** `docs/features/018-race-outcome-stat-progression.md`
**Status:** Ready for Implementation
**Approach:** TDD with Vertical Slices

---

## Overview

This implementation plan breaks down Feature 018 into 6 incremental phases. Each phase follows Test-Driven Development (TDD) methodology and delivers a complete vertical slice of functionality that can be independently tested and validated.

**Core Principles:**
- **RED-GREEN-REFACTOR**: Write failing tests first, make them pass, then clean up
- **Vertical Slices**: Each phase delivers end-to-end working functionality
- **Incremental Delivery**: Build on previous phases, adding value progressively
- **Test Coverage**: Maintain >90% coverage for all new code
- **No Schema Changes**: Leverage existing HorseStatistic and Horse entities

---

## Phase Breakdown

### Phase 1: Configuration and Career Phase System (Foundation)
**Goal**: Add stat progression configuration constants and implement career phase multiplier logic

**Vertical Slice**: Can calculate correct age multiplier for any horse based on race count

**Estimated Time**: 60-90 minutes
**Complexity**: Simple
**Risk Level**: Low

---

### Phase 2: Core Stat Growth Formula (MVP)
**Goal**: Implement base stat growth calculation with career phase integration

**Vertical Slice**: Can calculate how much a stat should grow after a race

**Estimated Time**: 90-120 minutes
**Complexity**: Medium
**Risk Level**: Medium (formula correctness critical for game balance)

---

### Phase 3: Performance Bonus System
**Goal**: Add finish-position-based growth bonuses

**Vertical Slice**: Winners develop faster than losers

**Estimated Time**: 60-90 minutes
**Complexity**: Simple
**Risk Level**: Low

---

### Phase 4: Race-Type Stat Focus
**Goal**: Different race distances develop different stats

**Vertical Slice**: Sprints develop Speed/Agility faster, distance races develop Stamina/Durability faster

**Estimated Time**: 90-120 minutes
**Complexity**: Medium
**Risk Level**: Low

---

### Phase 5: RaceExecutor Integration
**Goal**: Wire stat progression into race completion flow

**Vertical Slice**: Horses' stats actually increase after racing in the database

**Estimated Time**: 120-150 minutes
**Complexity**: Complex (integration, transaction safety)
**Risk Level**: High (must not break existing racing functionality)

---

### Phase 6: Happiness System
**Goal**: Implement race-based happiness changes with exhaustion detection

**Vertical Slice**: Happiness changes based on race performance and stamina depletion

**Estimated Time**: 90-120 minutes
**Complexity**: Medium
**Risk Level**: Low

---

## Detailed Phase Plans

---

## Phase 1: Configuration and Career Phase System

### Goal
Add configuration constants for stat progression and implement the career phase multiplier system (young/prime/veteran/old).

### Vertical Slice
Can calculate the correct development efficiency multiplier for any horse based on their race count.

### TDD Cycle

#### RED - Write Failing Tests

**File**: `TripleDerby.Tests.Unit/Racing/StatProgressionCalculatorTests.cs` (NEW)

```csharp
public class StatProgressionCalculatorTests
{
    [Fact]
    public void CalculateAgeMultiplier_WithYoungHorse_Returns0Point80()
    {
        // Arrange - Young horse has 5 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 5);

        // Assert
        Assert.Equal(0.80, result);
    }

    [Fact]
    public void CalculateAgeMultiplier_WithPrimeHorse_Returns1Point20()
    {
        // Arrange - Prime horse has 15 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 15);

        // Assert
        Assert.Equal(1.20, result);
    }

    [Fact]
    public void CalculateAgeMultiplier_WithVeteranHorse_Returns0Point60()
    {
        // Arrange - Veteran horse has 35 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 35);

        // Assert
        Assert.Equal(0.60, result);
    }

    [Fact]
    public void CalculateAgeMultiplier_WithOldHorse_Returns0Point20()
    {
        // Arrange - Old horse has 55 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 55);

        // Assert
        Assert.Equal(0.20, result);
    }

    [Theory]
    [InlineData(0, 0.80)]   // Young lower bound
    [InlineData(9, 0.80)]   // Young upper bound
    [InlineData(10, 1.20)]  // Prime lower bound
    [InlineData(29, 1.20)]  // Prime upper bound
    [InlineData(30, 0.60)]  // Veteran lower bound
    [InlineData(49, 0.60)]  // Veteran upper bound
    [InlineData(50, 0.20)]  // Old lower bound
    [InlineData(100, 0.20)] // Old edge case
    public void CalculateAgeMultiplier_WithBoundaryValues_ReturnsCorrectMultiplier(
        short raceStarts,
        double expected)
    {
        // Arrange
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts);

        // Assert
        Assert.Equal(expected, result);
    }
}
```

**Why these tests**: Define career phase boundaries (0-9, 10-29, 30-49, 50+) and validate multipliers match specification. Boundary testing ensures transitions between phases work correctly.

#### GREEN - Make Tests Pass

**Tasks:**

1. **Add configuration constants to `RaceModifierConfig.cs`**
   ```csharp
   // Stat Development Base Rates
   public const double BaseStatGrowthRate = 0.02;  // 2% of gap per race

   // Career Phase Multipliers
   public const double YoungHorseMultiplier = 0.80;     // Races 0-9
   public const double PrimeHorseMultiplier = 1.20;     // Races 10-29
   public const double VeteranHorseMultiplier = 0.60;   // Races 30-49
   public const double OldHorseMultiplier = 0.20;       // Races 50+

   // Career Phase Boundaries
   public const short PrimeCareerStartRace = 10;
   public const short VeteranCareerStartRace = 30;
   public const short OldCareerStartRace = 50;
   ```

2. **Create `StatProgressionCalculator.cs`** in `TripleDerby.Services.Racing/Racing/`
   ```csharp
   namespace TripleDerby.Services.Racing.Racing;

   /// <summary>
   /// Calculates stat progression multipliers and growth for horses after races.
   /// Implements career phase system, race-type focus, and performance bonuses.
   /// </summary>
   public class StatProgressionCalculator
   {
       /// <summary>
       /// Returns development efficiency based on career stage.
       /// Young horses learn slower, prime horses fastest, veterans slow down, old horses minimal.
       /// </summary>
       public double CalculateAgeMultiplier(short raceStarts)
       {
           if (raceStarts < RaceModifierConfig.PrimeCareerStartRace)
               return RaceModifierConfig.YoungHorseMultiplier;      // 0.80

           if (raceStarts < RaceModifierConfig.VeteranCareerStartRace)
               return RaceModifierConfig.PrimeHorseMultiplier;      // 1.20

           if (raceStarts < RaceModifierConfig.OldCareerStartRace)
               return RaceModifierConfig.VeteranHorseMultiplier;    // 0.60

           return RaceModifierConfig.OldHorseMultiplier;            // 0.20
       }
   }
   ```

**Implementation Notes**:
- Use constants from RaceModifierConfig for all thresholds (maintainability)
- Simple if-else ladder is clearest for 4 discrete phases
- No dependencies yet - pure calculation logic

#### REFACTOR - Clean Up

**Tasks:**
- Add XML documentation comments to CalculateAgeMultiplier
- Add range validation in comments (raceStarts expected to be 0+)
- Ensure constants are logically grouped in RaceModifierConfig

**Acceptance Criteria**:
- [ ] All tests pass
- [ ] Code coverage for StatProgressionCalculator = 100%
- [ ] Career phase transitions work correctly at boundaries (9→10, 29→30, 49→50)
- [ ] Configuration constants are well-documented

**Deliverable**: Can calculate correct development efficiency multiplier for any horse based on race count. Foundation laid for stat growth calculations.

**Risks**: None - pure calculation logic with no dependencies

---

## Phase 2: Core Stat Growth Formula

### Goal
Implement the base stat growth calculation that uses career phase multipliers to determine how much a stat should increase.

### Vertical Slice
Can calculate the exact stat gain for any horse/stat combination given race context.

### TDD Cycle

#### RED - Write Failing Tests

**File**: `TripleDerby.Tests.Unit/Racing/StatProgressionCalculatorTests.cs` (ADD TO EXISTING)

```csharp
[Fact]
public void GrowStat_WithYoungHorseAtMidGap_CalculatesCorrectGain()
{
    // Arrange - Young horse (5 races), Speed 42→85 potential, mid-pack finish, classic race
    var calculator = new StatProgressionCalculator();
    var stat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 42,
        DominantPotential = 85
    };
    short raceStarts = 5;
    decimal raceFurlongs = 8m;  // Classic
    byte finishPlace = 5;
    int fieldSize = 10;

    // Act
    var newActual = calculator.GrowStat(stat, raceStarts, raceFurlongs, finishPlace, fieldSize);

    // Assert
    // Gap = 85 - 42 = 43
    // BaseGain = 43 × 0.02 = 0.86
    // AgeMultiplier = 0.80 (young)
    // FocusMultiplier = 1.00 (classic, Phase 4)
    // PerfBonus = 1.00 (mid-pack, Phase 3)
    // FinalGain = 0.86 × 0.80 × 1.00 × 1.00 = 0.688 → rounds to 1
    // Expected = 42 + 1 = 43
    Assert.Equal(43, newActual);
}

[Fact]
public void GrowStat_AtCeiling_ReturnsUnchanged()
{
    // Arrange - Horse already at genetic ceiling
    var calculator = new StatProgressionCalculator();
    var stat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 85,
        DominantPotential = 85
    };
    short raceStarts = 20;
    decimal raceFurlongs = 8m;
    byte finishPlace = 1;  // Even winning doesn't help at ceiling
    int fieldSize = 10;

    // Act
    var newActual = calculator.GrowStat(stat, raceStarts, raceFurlongs, finishPlace, fieldSize);

    // Assert - No growth possible, already at ceiling
    Assert.Equal(85, newActual);
}

[Fact]
public void GrowStat_WithOnePointFromCeiling_CapsExactlyAtCeiling()
{
    // Arrange - 1 point from ceiling
    var calculator = new StatProgressionCalculator();
    var stat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 84,
        DominantPotential = 85
    };
    short raceStarts = 15;  // Prime
    decimal raceFurlongs = 6m;  // Sprint (Speed +50%)
    byte finishPlace = 1;  // Win (+50%)
    int fieldSize = 10;

    // Act
    var newActual = calculator.GrowStat(stat, raceStarts, raceFurlongs, finishPlace, fieldSize);

    // Assert - Even with max multipliers, caps at 85
    Assert.Equal(85, newActual);
}

[Fact]
public void GrowStat_WithPrimeHorse_GrowsFasterThanYoung()
{
    // Arrange - Two identical horses, different career phases
    var calculator = new StatProgressionCalculator();
    var stat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 50,
        DominantPotential = 80
    };
    decimal raceFurlongs = 8m;
    byte finishPlace = 5;
    int fieldSize = 10;

    // Act
    var youngGain = calculator.GrowStat(stat, raceStarts: 5, raceFurlongs, finishPlace, fieldSize);
    stat.Actual = 50; // Reset
    var primeGain = calculator.GrowStat(stat, raceStarts: 15, raceFurlongs, finishPlace, fieldSize);

    // Assert - Prime horse (1.20 mult) should grow more than young (0.80 mult)
    Assert.True(primeGain > youngGain,
        $"Prime horse ({primeGain}) should grow more than young horse ({youngGain})");
}
```

**Why these tests**:
- Validate core formula: gap × 0.02 × multipliers
- Test ceiling enforcement (critical for game balance)
- Test boundary condition (1 point from ceiling)
- Verify career phase integration from Phase 1

#### GREEN - Make Tests Pass

**Tasks:**

1. **Add stub implementations for Phase 3 and 4 helpers**
   ```csharp
   public class StatProgressionCalculator
   {
       // ... existing CalculateAgeMultiplier ...

       /// <summary>
       /// Calculates how much a stat should grow after a race.
       /// Respects genetic ceiling (DominantPotential).
       /// </summary>
       public byte GrowStat(
           HorseStatistic stat,
           short raceStarts,
           decimal raceFurlongs,
           byte finishPlace,
           int fieldSize)
       {
           // 1. Check if at ceiling
           if (stat.Actual >= stat.DominantPotential)
               return stat.Actual; // No growth possible

           // 2. Calculate remaining gap to ceiling
           double gap = stat.DominantPotential - stat.Actual;

           // 3. Base growth: 2% of gap
           double baseGain = gap * RaceModifierConfig.BaseStatGrowthRate;

           // 4. Apply career phase multiplier
           double ageMultiplier = CalculateAgeMultiplier(raceStarts);

           // 5. Apply race-type stat focus (Phase 4 - stub for now)
           double focusMultiplier = GetStatFocusMultiplier(raceFurlongs, stat.StatisticId);

           // 6. Apply performance bonus (Phase 3 - stub for now)
           double perfBonus = GetPerformanceBonus(finishPlace, fieldSize);

           // 7. Calculate final gain
           double totalMultiplier = ageMultiplier * focusMultiplier * perfBonus;
           double finalGain = baseGain * totalMultiplier;

           // 8. Apply gain and cap at ceiling
           byte newActual = (byte)Math.Min(
               stat.Actual + Math.Round(finalGain),
               stat.DominantPotential
           );

           return newActual;
       }

       // Stub for Phase 4 - returns neutral (1.0) for now
       private double GetStatFocusMultiplier(decimal furlongs, StatisticId stat)
       {
           return 1.0; // Classic race effect (all stats equal)
       }

       // Stub for Phase 3 - returns neutral (1.0) for now
       private double GetPerformanceBonus(byte place, int fieldSize)
       {
           return 1.0; // Mid-pack effect (no bonus/penalty)
       }
   }
   ```

**Implementation Notes**:
- Use Math.Round() for final gain to get whole stat points
- Use Math.Min() to enforce ceiling (prevents overflow)
- Stub methods return 1.0 (neutral) so tests pass with just age multiplier
- Gap calculation is double precision for accuracy

#### REFACTOR - Clean Up

**Tasks:**
- Extract magic number 1.0 to named constants (NeutralMultiplier)
- Add guard clause for negative gaps (defensive programming)
- Add XML documentation explaining formula
- Consider adding validation for stat.Actual <= stat.DominantPotential

**Acceptance Criteria**:
- [ ] All tests pass
- [ ] Code coverage for GrowStat = 100%
- [ ] Genetic ceiling is never exceeded
- [ ] Young/prime/veteran horses show different growth rates
- [ ] Formula matches specification (2% base growth)

**Deliverable**: Can calculate exact stat gain for any race scenario (with neutral focus/performance for now).

**Risks**:
- Rounding errors accumulating over many races (mitigated by using double precision until final round)
- Formula balance (will validate in Phase 5 integration tests)

---

## Phase 3: Performance Bonus System

### Goal
Implement finish-position-based growth bonuses so winners develop faster than losers.

### Vertical Slice
Winners get +50% growth, losers get -25% penalty, caps enforced correctly.

### TDD Cycle

#### RED - Write Failing Tests

**File**: `TripleDerby.Tests.Unit/Racing/StatProgressionCalculatorTests.cs` (ADD TO EXISTING)

```csharp
[Fact]
public void GetPerformanceBonus_WithWin_Returns1Point50()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetPerformanceBonus(place: 1, fieldSize: 10);

    // Assert
    Assert.Equal(1.50, result);
}

[Fact]
public void GetPerformanceBonus_WithPlace_Returns1Point25()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetPerformanceBonus(place: 2, fieldSize: 10);

    // Assert
    Assert.Equal(1.25, result);
}

[Fact]
public void GetPerformanceBonus_WithShow_Returns1Point10()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetPerformanceBonus(place: 3, fieldSize: 10);

    // Assert
    Assert.Equal(1.10, result);
}

[Fact]
public void GetPerformanceBonus_WithMidPack_Returns1Point00()
{
    // Arrange - 5th in 10-horse field = top 50%
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetPerformanceBonus(place: 5, fieldSize: 10);

    // Assert
    Assert.Equal(1.00, result);
}

[Fact]
public void GetPerformanceBonus_WithBackOfPack_Returns0Point75()
{
    // Arrange - 8th in 10-horse field = bottom 50%
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetPerformanceBonus(place: 8, fieldSize: 10);

    // Assert
    Assert.Equal(0.75, result);
}

[Theory]
[InlineData(1, 12, 1.50)]   // Win
[InlineData(2, 12, 1.25)]   // Place
[InlineData(3, 12, 1.10)]   // Show
[InlineData(6, 12, 1.00)]   // Mid-pack (exactly 50%)
[InlineData(7, 12, 0.75)]   // Back of pack (51st percentile)
[InlineData(12, 12, 0.75)]  // Dead last
public void GetPerformanceBonus_WithVariousFieldSizes_CalculatesCorrectly(
    byte place,
    int fieldSize,
    double expected)
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetPerformanceBonus(place, fieldSize);

    // Assert
    Assert.Equal(expected, result);
}

[Fact]
public void GrowStat_WithWinner_GrowsFasterThanLoser()
{
    // Arrange - Two identical horses, different finish positions
    var calculator = new StatProgressionCalculator();
    var stat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 50,
        DominantPotential = 80
    };
    short raceStarts = 15;  // Prime
    decimal raceFurlongs = 8m;  // Classic
    int fieldSize = 10;

    // Act
    var winnerGrowth = calculator.GrowStat(stat, raceStarts, raceFurlongs, place: 1, fieldSize);
    stat.Actual = 50; // Reset
    var loserGrowth = calculator.GrowStat(stat, raceStarts, raceFurlongs, place: 9, fieldSize);

    // Assert - Winner (1.50x) should grow significantly more than loser (0.75x)
    Assert.True(winnerGrowth > loserGrowth,
        $"Winner ({winnerGrowth}) should grow more than loser ({loserGrowth})");
}
```

**Why these tests**:
- Validate all 5 performance tiers (win, place, show, mid-pack, back of pack)
- Test mid-pack threshold (50th percentile calculation)
- Verify integration with GrowStat formula

#### GREEN - Make Tests Pass

**Tasks:**

1. **Add configuration constants to `RaceModifierConfig.cs`**
   ```csharp
   // Performance Bonuses
   public const double WinBonus = 1.50;
   public const double PlaceBonus = 1.25;
   public const double ShowBonus = 1.10;
   public const double MidPackMultiplier = 1.00;
   public const double BackOfPackPenalty = 0.75;
   ```

2. **Implement `GetPerformanceBonus()` in `StatProgressionCalculator.cs`**
   ```csharp
   /// <summary>
   /// Returns growth multiplier based on finish position.
   /// Winners develop faster, losers still learn but slower.
   /// </summary>
   private double GetPerformanceBonus(byte place, int fieldSize)
   {
       // Top 3 finishes get explicit bonuses
       if (place == 1)
           return RaceModifierConfig.WinBonus;        // 1.50
       if (place == 2)
           return RaceModifierConfig.PlaceBonus;      // 1.25
       if (place == 3)
           return RaceModifierConfig.ShowBonus;       // 1.10

       // Mid-pack (top 50%) gets neutral development
       if (place <= fieldSize / 2)
           return RaceModifierConfig.MidPackMultiplier;  // 1.00

       // Back of pack (bottom 50%) gets reduced development
       return RaceModifierConfig.BackOfPackPenalty;   // 0.75
   }
   ```

3. **Make method public for testing**
   - Change `private` to `public` for `GetPerformanceBonus` (or use InternalsVisibleTo)

**Implementation Notes**:
- Use integer division (fieldSize / 2) for mid-pack threshold
- Explicit checks for 1st/2nd/3rd before percentile calculation
- Even last place gets 0.75x (still learns, just slower)

#### REFACTOR - Clean Up

**Tasks:**
- Add XML documentation explaining 50th percentile threshold
- Consider edge case: what if fieldSize is 1? (returns 1.0, which is correct)
- Add debug assertions for valid place/fieldSize ranges

**Acceptance Criteria**:
- [ ] All tests pass
- [ ] Winners get 1.50x multiplier
- [ ] Mid-pack (50th percentile) gets 1.00x
- [ ] Back of pack gets 0.75x
- [ ] Integration test shows winners develop faster than losers

**Deliverable**: Performance bonuses fully implemented and integrated into stat growth formula.

**Risks**: None - straightforward conditional logic

---

## Phase 4: Race-Type Stat Focus

### Goal
Implement race distance-based stat focus so sprints develop Speed/Agility and distance races develop Stamina/Durability.

### Vertical Slice
6-furlong sprint develops Speed faster than Stamina, 12-furlong marathon does the opposite.

### TDD Cycle

#### RED - Write Failing Tests

**File**: `TripleDerby.Tests.Unit/Racing/StatProgressionCalculatorTests.cs` (ADD TO EXISTING)

```csharp
[Fact]
public void GetStatFocusMultiplier_WithSprintAndSpeed_Returns1Point50()
{
    // Arrange - 6f sprint, Speed stat
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetStatFocusMultiplier(furlongs: 6m, StatisticId.Speed);

    // Assert
    Assert.Equal(1.50, result);
}

[Fact]
public void GetStatFocusMultiplier_WithSprintAndAgility_Returns1Point25()
{
    // Arrange - 6f sprint, Agility stat
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetStatFocusMultiplier(furlongs: 6m, StatisticId.Agility);

    // Assert
    Assert.Equal(1.25, result);
}

[Fact]
public void GetStatFocusMultiplier_WithSprintAndStamina_Returns0Point75()
{
    // Arrange - 6f sprint, Stamina stat (not sprint-focused)
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetStatFocusMultiplier(furlongs: 6m, StatisticId.Stamina);

    // Assert
    Assert.Equal(0.75, result);
}

[Fact]
public void GetStatFocusMultiplier_WithDistanceAndStamina_Returns1Point50()
{
    // Arrange - 12f distance, Stamina stat
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetStatFocusMultiplier(furlongs: 12m, StatisticId.Stamina);

    // Assert
    Assert.Equal(1.50, result);
}

[Fact]
public void GetStatFocusMultiplier_WithDistanceAndDurability_Returns1Point25()
{
    // Arrange - 12f distance, Durability stat
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetStatFocusMultiplier(furlongs: 12m, StatisticId.Durability);

    // Assert
    Assert.Equal(1.25, result);
}

[Fact]
public void GetStatFocusMultiplier_WithDistanceAndSpeed_Returns0Point75()
{
    // Arrange - 12f distance, Speed stat (not distance-focused)
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetStatFocusMultiplier(furlongs: 12m, StatisticId.Speed);

    // Assert
    Assert.Equal(0.75, result);
}

[Fact]
public void GetStatFocusMultiplier_WithClassicRace_Returns1Point00()
{
    // Arrange - 8f classic, all stats
    var calculator = new StatProgressionCalculator();

    // Act
    var speedResult = calculator.GetStatFocusMultiplier(furlongs: 8m, StatisticId.Speed);
    var staminaResult = calculator.GetStatFocusMultiplier(furlongs: 8m, StatisticId.Stamina);

    // Assert - Classic races develop all stats equally
    Assert.Equal(1.00, speedResult);
    Assert.Equal(1.00, staminaResult);
}

[Theory]
[InlineData(5, StatisticId.Speed, 1.50)]       // Sprint boundary
[InlineData(6, StatisticId.Speed, 1.50)]       // Sprint upper bound
[InlineData(7, StatisticId.Speed, 1.00)]       // Classic lower bound
[InlineData(10, StatisticId.Speed, 1.00)]      // Classic upper bound
[InlineData(11, StatisticId.Stamina, 1.50)]    // Distance lower bound
[InlineData(15, StatisticId.Stamina, 1.50)]    // Distance upper bound
public void GetStatFocusMultiplier_WithBoundaryDistances_ReturnsCorrectly(
    decimal furlongs,
    StatisticId stat,
    double expected)
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.GetStatFocusMultiplier(furlongs, stat);

    // Assert
    Assert.Equal(expected, result);
}

[Fact]
public void GrowStat_WithSprintRace_DevelopsSpeedFasterThanStamina()
{
    // Arrange - Same horse, sprint race, different stats
    var calculator = new StatProgressionCalculator();
    var speedStat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 50,
        DominantPotential = 80
    };
    var staminaStat = new HorseStatistic
    {
        StatisticId = StatisticId.Stamina,
        Actual = 50,
        DominantPotential = 80
    };
    short raceStarts = 15;
    byte place = 5;
    int fieldSize = 10;

    // Act
    var speedGrowth = calculator.GrowStat(speedStat, raceStarts, furlongs: 6m, place, fieldSize);
    var staminaGrowth = calculator.GrowStat(staminaStat, raceStarts, furlongs: 6m, place, fieldSize);

    // Assert - Speed (1.50x) should grow more than Stamina (0.75x) in sprint
    Assert.True(speedGrowth > staminaGrowth,
        $"Speed ({speedGrowth}) should grow more than Stamina ({staminaGrowth}) in sprint");
}

[Fact]
public void GrowStat_WithDistanceRace_DevelopsStaminaFasterThanSpeed()
{
    // Arrange - Same horse, distance race, different stats
    var calculator = new StatProgressionCalculator();
    var speedStat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 50,
        DominantPotential = 80
    };
    var staminaStat = new HorseStatistic
    {
        StatisticId = StatisticId.Stamina,
        Actual = 50,
        DominantPotential = 80
    };
    short raceStarts = 15;
    byte place = 5;
    int fieldSize = 10;

    // Act
    var speedGrowth = calculator.GrowStat(speedStat, raceStarts, furlongs: 12m, place, fieldSize);
    var staminaGrowth = calculator.GrowStat(staminaStat, raceStarts, furlongs: 12m, place, fieldSize);

    // Assert - Stamina (1.50x) should grow more than Speed (0.75x) in distance race
    Assert.True(staminaGrowth > speedGrowth,
        $"Stamina ({staminaGrowth}) should grow more than Speed ({speedGrowth}) in distance race");
}
```

**Why these tests**:
- Validate all 3 race categories (sprint ≤6f, classic 7-10f, distance 11+f)
- Test all 4 performance stats affected
- Verify boundary transitions (6f→7f, 10f→11f)
- Integration tests confirm stat-specific development

#### GREEN - Make Tests Pass

**Tasks:**

1. **Add configuration constants to `RaceModifierConfig.cs`**
   ```csharp
   // Race-Type Focus Multipliers
   public const decimal SprintDistanceThreshold = 6m;    // ≤6 furlongs
   public const decimal DistanceRaceThreshold = 11m;     // ≥11 furlongs

   public const double SprintSpeedMultiplier = 1.50;
   public const double SprintAgilityMultiplier = 1.25;
   public const double SprintOtherMultiplier = 0.75;

   public const double DistanceStaminaMultiplier = 1.50;
   public const double DistanceDurabilityMultiplier = 1.25;
   public const double DistanceOtherMultiplier = 0.75;

   public const double ClassicRaceMultiplier = 1.00;  // 7-10f, all stats equal
   ```

2. **Implement `GetStatFocusMultiplier()` in `StatProgressionCalculator.cs`**
   ```csharp
   /// <summary>
   /// Returns stat-specific multiplier based on race distance.
   /// Sprints favor Speed/Agility, distance races favor Stamina/Durability,
   /// classic races develop all stats equally.
   /// </summary>
   private double GetStatFocusMultiplier(decimal furlongs, StatisticId stat)
   {
       // Sprint races (≤6 furlongs)
       if (furlongs <= RaceModifierConfig.SprintDistanceThreshold)
       {
           return stat switch
           {
               StatisticId.Speed => RaceModifierConfig.SprintSpeedMultiplier,      // 1.50
               StatisticId.Agility => RaceModifierConfig.SprintAgilityMultiplier,  // 1.25
               _ => RaceModifierConfig.SprintOtherMultiplier                       // 0.75
           };
       }

       // Distance races (≥11 furlongs)
       if (furlongs >= RaceModifierConfig.DistanceRaceThreshold)
       {
           return stat switch
           {
               StatisticId.Stamina => RaceModifierConfig.DistanceStaminaMultiplier,       // 1.50
               StatisticId.Durability => RaceModifierConfig.DistanceDurabilityMultiplier, // 1.25
               _ => RaceModifierConfig.DistanceOtherMultiplier                            // 0.75
           };
       }

       // Classic races (7-10 furlongs) - balanced development
       return RaceModifierConfig.ClassicRaceMultiplier;  // 1.00
   }
   ```

3. **Make method public for testing**
   - Change `private` to `public` for `GetStatFocusMultiplier`

**Implementation Notes**:
- Use decimal comparison for furlong thresholds (precision)
- Switch expressions are clean for stat-based branching
- Happiness stat ignored (doesn't develop from racing)

#### REFACTOR - Clean Up

**Tasks:**
- Add XML documentation explaining race distance categories
- Consider extracting switch expressions to named methods for clarity
- Ensure Happiness stat isn't affected (it uses different system in Phase 6)

**Acceptance Criteria**:
- [ ] All tests pass
- [ ] Sprint races (≤6f) develop Speed/Agility faster
- [ ] Distance races (≥11f) develop Stamina/Durability faster
- [ ] Classic races (7-10f) develop all stats equally
- [ ] Boundary transitions work correctly (6f→7f, 10f→11f)

**Deliverable**: Race-type stat focus fully implemented. Stat growth formula now includes all multipliers (age, performance, focus).

**Risks**: None - straightforward conditional logic

---

## Phase 5: RaceExecutor Integration

### Goal
Wire stat progression into RaceExecutor so horses' stats actually increase in the database after racing.

### Vertical Slice
Can race a horse, see stats increase in database, verify with integration test.

### TDD Cycle

#### RED - Write Failing Tests

**File**: `TripleDerby.Tests.Unit/Services/RaceExecutorTests.cs` (ADD TO EXISTING)

```csharp
[Fact]
public async Task Race_AfterCompletion_IncreasesHorseStats()
{
    // Arrange
    var raceId = (byte)1;
    var playerHorseId = Guid.NewGuid();
    var race = CreateRace(raceId, furlongs: 8m);  // Classic race
    var playerHorse = CreateHorse(playerHorseId, "Player Horse");

    // Set initial stats - young horse with room to grow
    var speedStat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 42,
        DominantPotential = 85
    };
    playerHorse.Statistics.Add(speedStat);
    // ... add other stats similarly

    playerHorse.RaceStarts = 5; // Young horse

    _repositoryMock
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(race);
    _repositoryMock
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(playerHorse);

    RaceRun? capturedRaceRun = null;
    _repositoryMock
        .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
        .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
        .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

    // Act
    await _sut.Race(raceId, playerHorseId, CancellationToken.None);

    // Assert
    Assert.NotNull(capturedRaceRun);

    // Verify stats increased
    var resultingSpeedStat = playerHorse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
    Assert.True(resultingSpeedStat.Actual > 42,
        "Speed stat should have increased after race");
}

[Fact]
public async Task Race_WithHorseAtCeiling_DoesNotExceedPotential()
{
    // Arrange - Horse already at genetic ceiling
    var raceId = (byte)1;
    var playerHorseId = Guid.NewGuid();
    var race = CreateRace(raceId, furlongs: 8m);
    var playerHorse = CreateHorse(playerHorseId, "Maxed Horse");

    var speedStat = new HorseStatistic
    {
        StatisticId = StatisticId.Speed,
        Actual = 85,
        DominantPotential = 85  // Already at ceiling
    };
    playerHorse.Statistics.Add(speedStat);
    playerHorse.RaceStarts = 20;

    _repositoryMock
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(race);
    _repositoryMock
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(playerHorse);

    RaceRun? capturedRaceRun = null;
    _repositoryMock
        .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
        .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
        .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

    // Act
    await _sut.Race(raceId, playerHorseId, CancellationToken.None);

    // Assert
    var resultingSpeedStat = playerHorse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
    Assert.Equal(85, resultingSpeedStat.Actual); // Should remain at ceiling
}

[Fact]
public async Task Race_WithMultipleHorses_IncreasesAllHorsesStats()
{
    // Arrange - Multiple horses in race
    var raceId = (byte)1;
    var playerHorseId = Guid.NewGuid();
    var race = CreateRace(raceId, furlongs: 8m);
    var playerHorse = CreateHorse(playerHorseId, "Player Horse");
    var cpuHorses = new List<Horse>
    {
        CreateHorse(Guid.NewGuid(), "CPU Horse 1"),
        CreateHorse(Guid.NewGuid(), "CPU Horse 2")
    };

    // All horses start with same stats
    foreach (var horse in new[] { playerHorse }.Concat(cpuHorses))
    {
        horse.Statistics.Add(new HorseStatistic
        {
            StatisticId = StatisticId.Speed,
            Actual = 50,
            DominantPotential = 80
        });
        horse.RaceStarts = 15; // Prime career
    }

    _repositoryMock
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(race);
    _repositoryMock
        .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(playerHorse);
    _repositoryMock
        .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(cpuHorses);

    RaceRun? capturedRaceRun = null;
    _repositoryMock
        .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
        .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
        .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

    // Act
    await _sut.Race(raceId, playerHorseId, CancellationToken.None);

    // Assert - All horses should have stat increases
    Assert.NotNull(capturedRaceRun);
    foreach (var raceRunHorse in capturedRaceRun.Horses)
    {
        var speedStat = raceRunHorse.Horse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
        Assert.True(speedStat.Actual > 50,
            $"Horse {raceRunHorse.Horse.Name} should have increased Speed stat");
    }
}
```

**Why these tests**:
- Validate integration with real race flow
- Test ceiling enforcement in production scenario
- Verify all horses in field get stat updates (not just player)
- Test transaction safety (all stats updated together)

#### GREEN - Make Tests Pass

**Tasks:**

1. **Add StatProgressionCalculator to RaceExecutor DI**
   ```csharp
   // In RaceExecutor constructor
   public RaceExecutor(
       ITripleDerbyRepository repository,
       IRandomGenerator randomGenerator,
       ISpeedModifierCalculator speedModifierCalculator,
       IStaminaCalculator staminaCalculator,
       IRaceCommentaryGenerator commentaryGenerator,
       IPurseCalculator purseCalculator,
       IOvertakingManager overtakingManager,
       IEventDetector eventDetector,
       ITimeManager timeManager,
       StatProgressionCalculator statProgressionCalculator,  // NEW
       ILogger<RaceExecutor> logger)
   {
       _statProgressionCalculator = statProgressionCalculator;
       // ... other assignments
   }
   ```

2. **Modify `FinalizeResults()` in RaceExecutor.cs**
   ```csharp
   private void FinalizeResults(RaceRun raceRun)
   {
       // Existing code: sort horses by time
       var sortedHorses = raceRun.Horses.OrderBy(h => h.Time).ToList();
       byte place = 1;

       foreach (var horse in sortedHorses)
       {
           horse.Place = place;
           horse.Horse.RaceStarts++;

           // Existing code: Assign win/place/show IDs and counters
           switch (place)
           {
               case 1:
                   raceRun.WinHorseId = horse.Horse.Id;
                   horse.Horse.RaceWins++;
                   break;
               case 2:
                   raceRun.PlaceHorseId = horse.Horse.Id;
                   horse.Horse.RacePlace++;
                   break;
               case 3:
                   raceRun.ShowHorseId = horse.Horse.Id;
                   horse.Horse.RaceShow++;
                   break;
           }

           // NEW: Apply stat progression
           ApplyRaceOutcomeStatProgression(horse, raceRun);

           place++;
       }
   }
   ```

3. **Add `ApplyRaceOutcomeStatProgression()` method**
   ```csharp
   /// <summary>
   /// Applies stat progression to all performance stats after race.
   /// Stats grow toward DominantPotential based on career phase, race type, and performance.
   /// </summary>
   private void ApplyRaceOutcomeStatProgression(RaceRunHorse raceRunHorse, RaceRun raceRun)
   {
       var horse = raceRunHorse.Horse;
       var raceFurlongs = raceRun.Race.Furlongs;
       var finishPlace = raceRunHorse.Place;
       var fieldSize = raceRun.Horses.Count;

       // Apply growth to all performance stats (not Happiness)
       var performanceStats = horse.Statistics.Where(s =>
           s.StatisticId == StatisticId.Speed ||
           s.StatisticId == StatisticId.Stamina ||
           s.StatisticId == StatisticId.Agility ||
           s.StatisticId == StatisticId.Durability
       );

       foreach (var stat in performanceStats)
       {
           var newActual = _statProgressionCalculator.GrowStat(
               stat,
               horse.RaceStarts,  // Use incremented value (already updated above)
               raceFurlongs,
               finishPlace,
               fieldSize
           );

           stat.Actual = newActual;
       }
   }
   ```

4. **Update RaceExecutor test setup**
   ```csharp
   // In RaceExecutorTests constructor
   var statProgressionCalculator = new StatProgressionCalculator();

   _sut = new RaceExecutor(
       _repositoryMock.Object,
       randomGeneratorMock.Object,
       speedModifierCalculator,
       staminaCalculator,
       commentaryGenerator,
       purseCalculator,
       overtakingManager,
       eventDetector,
       timeManager.Object,
       statProgressionCalculator,  // NEW
       NullLogger<RaceExecutor>.Instance
   );
   ```

5. **Update DI registration in Program.cs (Racing service)**
   ```csharp
   // In Services.Racing/Program.cs
   builder.Services.AddSingleton<StatProgressionCalculator>();
   ```

**Implementation Notes**:
- Stats update happens AFTER RaceStarts++ (use incremented value for age multiplier)
- Only update performance stats (Speed, Stamina, Agility, Durability), NOT Happiness (Phase 6)
- All horses in field get updates (not just player horse)
- Updates happen in-memory before repository.CreateAsync(raceRun) persists

#### REFACTOR - Clean Up

**Tasks:**
- Extract performance stat filtering to helper method
- Add error handling for missing stats (defensive programming)
- Consider logging stat changes for debugging
- Add XML documentation to ApplyRaceOutcomeStatProgression

**Acceptance Criteria**:
- [ ] All tests pass
- [ ] Horses' stats increase after races
- [ ] Stats never exceed DominantPotential ceiling
- [ ] All horses in race get stat updates
- [ ] Existing race functionality unaffected (no regressions)
- [ ] Transaction safety maintained (single CreateAsync call)

**Deliverable**: Stat progression fully integrated into race flow. Horses develop through racing!

**Risks**:
- **HIGH**: Integration with existing race flow could break things
  - Mitigation: Run full test suite, verify no regressions
- **MEDIUM**: Database transaction safety (all stats must update atomically)
  - Mitigation: All updates happen in-memory before single CreateAsync
- **LOW**: Performance impact from additional calculations
  - Mitigation: Profile race completion time, ensure <5ms added

---

## Phase 6: Happiness System

### Goal
Implement race-based happiness changes with finish-position bonuses and exhaustion penalties.

### Vertical Slice
Winning increases happiness (+8), losing decreases it (-3), exhaustion compounds penalty (-5).

### TDD Cycle

#### RED - Write Failing Tests

**File**: `TripleDerby.Tests.Unit/Racing/StatProgressionCalculatorTests.cs` (ADD TO EXISTING)

```csharp
[Fact]
public void CalculateHappinessChange_WithWin_Returns8()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.CalculateHappinessChange(
        finishPlace: 1,
        fieldSize: 10,
        currentStamina: 50,
        initialStamina: 100);

    // Assert
    Assert.Equal(8, result);
}

[Fact]
public void CalculateHappinessChange_WithPlace_Returns4()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.CalculateHappinessChange(
        finishPlace: 2,
        fieldSize: 10,
        currentStamina: 50,
        initialStamina: 100);

    // Assert
    Assert.Equal(4, result);
}

[Fact]
public void CalculateHappinessChange_WithShow_Returns2()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.CalculateHappinessChange(
        finishPlace: 3,
        fieldSize: 10,
        currentStamina: 50,
        initialStamina: 100);

    // Assert
    Assert.Equal(2, result);
}

[Fact]
public void CalculateHappinessChange_WithMidPack_Returns0()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.CalculateHappinessChange(
        finishPlace: 5,
        fieldSize: 10,
        currentStamina: 50,
        initialStamina: 100);

    // Assert
    Assert.Equal(0, result);
}

[Fact]
public void CalculateHappinessChange_WithBackOfPack_ReturnsMinus3()
{
    // Arrange
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.CalculateHappinessChange(
        finishPlace: 8,
        fieldSize: 10,
        currentStamina: 50,
        initialStamina: 100);

    // Assert
    Assert.Equal(-3, result);
}

[Fact]
public void CalculateHappinessChange_WithExhaustion_AppliesMinus5Penalty()
{
    // Arrange - Finished with 5% stamina remaining
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.CalculateHappinessChange(
        finishPlace: 5,  // Mid-pack (0 base change)
        fieldSize: 10,
        currentStamina: 5,    // 5% remaining
        initialStamina: 100);

    // Assert - 0 (mid-pack) - 5 (exhaustion) = -5
    Assert.Equal(-5, result);
}

[Fact]
public void CalculateHappinessChange_WithWinButExhausted_ReturnsMinus3()
{
    // Arrange - Won but exhausted (pyrrhic victory)
    var calculator = new StatProgressionCalculator();

    // Act
    var result = calculator.CalculateHappinessChange(
        finishPlace: 1,  // Win (+8)
        fieldSize: 10,
        currentStamina: 3,    // 3% remaining (exhausted)
        initialStamina: 100);

    // Assert - 8 (win) - 5 (exhaustion) = 3
    Assert.Equal(3, result);
}

[Fact]
public void ApplyHappinessChange_IncrementsFromBaseline()
{
    // Arrange
    var calculator = new StatProgressionCalculator();
    var happinessStat = new HorseStatistic
    {
        StatisticId = StatisticId.Happiness,
        Actual = 50  // Neutral
    };

    // Act - Win increases by 8
    calculator.ApplyHappinessChange(
        happinessStat,
        finishPlace: 1,
        fieldSize: 10,
        currentStamina: 50,
        initialStamina: 100);

    // Assert
    Assert.Equal(58, happinessStat.Actual);
}

[Fact]
public void ApplyHappinessChange_CapsAt100()
{
    // Arrange - Already happy horse winning
    var calculator = new StatProgressionCalculator();
    var happinessStat = new HorseStatistic
    {
        StatisticId = StatisticId.Happiness,
        Actual = 95
    };

    // Act - Win (+8) would exceed 100
    calculator.ApplyHappinessChange(
        happinessStat,
        finishPlace: 1,
        fieldSize: 10,
        currentStamina: 50,
        initialStamina: 100);

    // Assert - Capped at 100
    Assert.Equal(100, happinessStat.Actual);
}

[Fact]
public void ApplyHappinessChange_FloorsAt0()
{
    // Arrange - Unhappy horse losing badly while exhausted
    var calculator = new StatProgressionCalculator();
    var happinessStat = new HorseStatistic
    {
        StatisticId = StatisticId.Happiness,
        Actual = 5
    };

    // Act - Back of pack (-3) + exhaustion (-5) = -8
    calculator.ApplyHappinessChange(
        happinessStat,
        finishPlace: 9,
        fieldSize: 10,
        currentStamina: 3,
        initialStamina: 100);

    // Assert - Floored at 0
    Assert.Equal(0, happinessStat.Actual);
}
```

**Why these tests**:
- Validate all finish-position tiers for happiness
- Test exhaustion detection (<10% stamina)
- Test bounds enforcement (0-100 range)
- Verify integration with stat modification pattern

#### GREEN - Make Tests Pass

**Tasks:**

1. **Add configuration constants to `RaceModifierConfig.cs`**
   ```csharp
   // Happiness Changes
   public const int WinHappinessBonus = 8;
   public const int PlaceHappinessBonus = 4;
   public const int ShowHappinessBonus = 2;
   public const int MidPackHappinessChange = 0;
   public const int BackOfPackHappinessPenalty = -3;
   public const int ExhaustionHappinessPenalty = -5;
   public const double ExhaustionStaminaThreshold = 0.10;  // <10% stamina
   ```

2. **Implement happiness methods in `StatProgressionCalculator.cs`**
   ```csharp
   /// <summary>
   /// Calculates happiness change based on race performance.
   /// Winners gain morale, losers lose it, exhaustion compounds frustration.
   /// </summary>
   public int CalculateHappinessChange(
       byte finishPlace,
       int fieldSize,
       double currentStamina,
       double initialStamina)
   {
       // Base change from finish position
       int baseChange = finishPlace switch
       {
           1 => RaceModifierConfig.WinHappinessBonus,        // +8
           2 => RaceModifierConfig.PlaceHappinessBonus,      // +4
           3 => RaceModifierConfig.ShowHappinessBonus,       // +2
           _ => finishPlace <= fieldSize / 2
               ? RaceModifierConfig.MidPackHappinessChange   // 0
               : RaceModifierConfig.BackOfPackHappinessPenalty // -3
       };

       // Exhaustion penalty: finishing with <10% stamina is traumatic
       double staminaPercent = initialStamina > 0
           ? currentStamina / initialStamina
           : 1.0;

       if (staminaPercent < RaceModifierConfig.ExhaustionStaminaThreshold)
       {
           baseChange += RaceModifierConfig.ExhaustionHappinessPenalty; // -5
       }

       return baseChange;
   }

   /// <summary>
   /// Applies happiness change to stat and clamps to valid range [0, 100].
   /// </summary>
   public void ApplyHappinessChange(
       HorseStatistic happinessStat,
       byte finishPlace,
       int fieldSize,
       double currentStamina,
       double initialStamina)
   {
       int change = CalculateHappinessChange(
           finishPlace,
           fieldSize,
           currentStamina,
           initialStamina);

       int newHappiness = Math.Clamp(
           happinessStat.Actual + change,
           0,
           100
       );

       happinessStat.Actual = (byte)newHappiness;
   }
   ```

3. **Add happiness update to RaceExecutor**
   ```csharp
   private void ApplyRaceOutcomeStatProgression(RaceRunHorse raceRunHorse, RaceRun raceRun)
   {
       var horse = raceRunHorse.Horse;

       // ... existing stat progression code ...

       // NEW: Apply happiness change
       var happinessStat = horse.Statistics.FirstOrDefault(s =>
           s.StatisticId == StatisticId.Happiness);

       if (happinessStat != null)
       {
           _statProgressionCalculator.ApplyHappinessChange(
               happinessStat,
               raceRunHorse.Place,
               raceRun.Horses.Count,
               (double)raceRunHorse.CurrentStamina,
               (double)raceRunHorse.InitialStamina
           );
       }
   }
   ```

**Implementation Notes**:
- Use Math.Clamp for bounds enforcement (clean)
- Exhaustion detection uses percentage calculation (handles zero stamina case)
- Happiness updates separately from performance stats (different logic)

#### REFACTOR - Clean Up

**Tasks:**
- Add XML documentation explaining psychological rationale (winning = confidence, losing = frustration)
- Extract magic numbers to constants
- Consider adding logging for happiness changes (debugging)

**Acceptance Criteria**:
- [ ] All tests pass
- [ ] Winning increases happiness by 8
- [ ] Losing decreases happiness by 3
- [ ] Exhaustion adds -5 penalty
- [ ] Happiness bounded to [0, 100]
- [ ] Integration test shows happiness changes after race

**Deliverable**: Happiness system fully implemented. Winners get morale boost, losers get frustrated, exhaustion compounds misery.

**Risks**: None - straightforward addition to existing stat update flow

---

## Testing Strategy

### Unit Test Coverage Goals
- **StatProgressionCalculator**: 100% coverage (pure calculation logic)
- **RaceExecutor integration**: 90%+ coverage (focus on stat progression paths)

### Test Pyramid

**Unit Tests (Fast, Isolated)**:
- All helper methods (CalculateAgeMultiplier, GetPerformanceBonus, GetStatFocusMultiplier)
- Core formula (GrowStat)
- Happiness calculation (CalculateHappinessChange, ApplyHappinessChange)
- Edge cases (ceiling, floor, boundaries)

**Integration Tests (RaceExecutor)**:
- Stats increase after race
- Ceiling enforcement
- Multiple horses updated
- Transaction safety
- No regressions in existing functionality

**Balance Validation Tests (Future)**:
- 50-race career simulation reaches 85-90% of potential
- Winners develop ~20% faster than losers over 10 races
- Sprint vs distance stat focus divergence

### Manual Testing Checklist

After all phases complete:
- [ ] Race a young horse (0-9 races), verify slow growth (0.80x)
- [ ] Race a prime horse (10-29 races), verify fast growth (1.20x)
- [ ] Race a veteran horse (30+ races), verify slowing growth
- [ ] Win a 6f sprint, verify Speed increases more than Stamina
- [ ] Win a 12f distance, verify Stamina increases more than Speed
- [ ] Finish last while exhausted, verify happiness tanks
- [ ] Verify stats cap at DominantPotential
- [ ] Check database persistence (stats saved correctly)

---

## Risk Mitigation

### High-Risk Areas

**RaceExecutor Integration (Phase 5)**:
- **Risk**: Breaking existing race functionality
- **Mitigation**:
  - Run full test suite before/after changes
  - Add integration tests first
  - Manual smoke testing of race flow
  - Feature flag if needed (can disable stat progression)

**Formula Balance**:
- **Risk**: Stat growth too fast/slow, breaks game economy
- **Mitigation**:
  - 50-race simulation tests
  - Balance validation tests
  - Configuration-driven (easy to tune without code changes)
  - Phased rollout (can adjust multipliers post-launch)

### Medium-Risk Areas

**Database Transaction Safety**:
- **Risk**: Partial stat updates if race creation fails
- **Mitigation**:
  - All stat updates in-memory before single CreateAsync
  - No additional database calls
  - Existing transaction pattern already safe

**Performance Impact**:
- **Risk**: Stat calculations slow down race completion
- **Mitigation**:
  - Profile race completion time
  - Target <5ms added (spec requirement)
  - Pure calculations (no DB/IO in hot path)

---

## Dependencies

### Prerequisites (Already Complete)
- ✅ HorseStatistic entity with Actual/DominantPotential
- ✅ RaceExecutor with FinalizeResults hook point
- ✅ RaceModifierConfig pattern established
- ✅ Test infrastructure (Moq, xUnit)

### Phase Dependencies
- Phase 1 → Foundation for all other phases
- Phase 2 → Depends on Phase 1 (uses CalculateAgeMultiplier)
- Phase 3 → Independent (can be done before/after Phase 4)
- Phase 4 → Independent (can be done before/after Phase 3)
- Phase 5 → Depends on Phases 1-4 (integrates everything)
- Phase 6 → Independent, can be done anytime (separate system)

### External Dependencies
- None (all logic self-contained within Racing service)

---

## Rollout Plan

### Phase 1-4: Feature Development (Internal)
- Build incrementally with tests
- No user-facing changes yet
- Can merge to main without affecting production

### Phase 5: Integration (Staging)
- Deploy to staging environment
- Run balance validation tests
- Observe stat progression over 100+ test races
- Tune multipliers if needed

### Phase 6: Happiness (Production)
- Deploy complete feature to production
- Monitor database transaction times
- Gather player feedback on stat progression rates
- Adjust configuration constants as needed

### Post-Launch Tuning
- **Week 1**: Monitor stat inflation metrics
- **Week 2**: Adjust multipliers based on data
- **Month 1**: Add analytics dashboard for stat progression
- **Future**: Add Happiness decay scheduler (Phase 7, not in this plan)

---

## Success Metrics

### Technical Metrics
- [ ] 100% test coverage on StatProgressionCalculator
- [ ] 90%+ test coverage on RaceExecutor stat progression paths
- [ ] Race completion time increase <5ms (performance requirement)
- [ ] Zero production errors related to stat updates
- [ ] Database transaction success rate 100%

### Game Balance Metrics
- [ ] Average 50-race career reaches 85-90% of genetic potential
- [ ] Prime horses (15 races) develop ~30% faster than young (5 races)
- [ ] Winners develop ~20% faster than consistent losers
- [ ] Sprint races show Speed > Stamina growth differential
- [ ] Distance races show Stamina > Speed growth differential

### Player Experience Metrics
- [ ] Players understand stat progression (low confusion tickets)
- [ ] Stat growth feels rewarding but not overpowered
- [ ] Retirement timing makes sense (around race 40-50)
- [ ] Breeding meta emerges (develop horses → retire → breed)

---

## Next Steps After This Plan

**Immediate** (During Implementation):
1. Execute Phase 1 (add to TodoWrite)
2. Run tests, verify RED-GREEN-REFACTOR cycle
3. Complete Phase 1, mark todos done
4. Add Phase 2 todos and repeat

**After Feature Complete**:
1. Run full balance validation suite
2. Create 50-race career simulation test
3. Tune multipliers based on results
4. Write user-facing documentation (how stat progression works)
5. Consider adding UI indicators (show stat gains in race results)

**Future Enhancements** (Not in this plan):
- Happiness decay scheduler (Phase 7)
- Career analytics dashboard
- RaceRunHorse stat delta tracking fields
- Training system stat progression (3-5% growth rates)
- Feeding system enhancements

---

**Plan Created**: 2026-01-02
**Feature Spec**: docs/features/018-race-outcome-stat-progression.md
**Total Estimated Time**: 8-12 hours (across 6 phases)
**Complexity**: Medium (integration challenges in Phase 5)
**Risk Level**: Medium (balance tuning required)
