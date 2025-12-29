# Feature 012: Realistic Horse Speed Calculation - Implementation Plan

**Feature:** Realistic Horse Speed Calculation for Traffic Response
**Specification:** [012-realistic-horse-speed-calculation.md](012-realistic-horse-speed-calculation.md)
**Status:** Ready for Implementation

---

## Quick Start

**Goal:** Replace constant speed calculation with full modifier pipeline in OvertakingManager

**Key Changes:**
1. Inject ISpeedModifierCalculator into OvertakingManager
2. Update IOvertakingManager.ApplyTrafficEffects signature
3. Implement full pipeline in CalculateHorseSpeed method
4. Add comprehensive test suite

**Estimated Time:** 4-6 hours (TDD approach with phases)

---

## Implementation Phases

### Phase 1: Infrastructure Setup ⏱️ 30 minutes

**Objective:** Prepare OvertakingManager to receive and use ISpeedModifierCalculator

#### Task 1.1: Update OvertakingManager Constructor
**File:** [OvertakingManager.cs:17-20](c:\Development\TripleDerby\TripleDerby.Core\Racing\OvertakingManager.cs#L17-L20)

**Current:**
```csharp
public class OvertakingManager : IOvertakingManager
{
    private readonly IRandomGenerator _randomGenerator;

    public OvertakingManager(IRandomGenerator randomGenerator)
    {
        _randomGenerator = randomGenerator;
    }
}
```

**Change to:**
```csharp
public class OvertakingManager : IOvertakingManager
{
    private readonly IRandomGenerator _randomGenerator;
    private readonly ISpeedModifierCalculator _speedModifierCalculator;

    public OvertakingManager(
        IRandomGenerator randomGenerator,
        ISpeedModifierCalculator speedModifierCalculator)
    {
        _randomGenerator = randomGenerator;
        _speedModifierCalculator = speedModifierCalculator;
    }
}
```

**Steps:**
1. Add using statement: `using TripleDerby.Core.Abstractions.Racing;`
2. Add private field: `private readonly ISpeedModifierCalculator _speedModifierCalculator;`
3. Add constructor parameter
4. Assign in constructor body

**Verify:** Project compiles

---

#### Task 1.2: Update IOvertakingManager Interface
**File:** [IOvertakingManager.cs:20-27](c:\Development\TripleDerby\TripleDerby.Core\Abstractions\Racing\IOvertakingManager.cs#L20-L27)

**Current:**
```csharp
/// <summary>
/// Applies leg-type-specific traffic response effects when horse is blocked.
/// Modifies speed based on traffic ahead and horse's personality.
/// </summary>
/// <param name="horse">The horse being affected</param>
/// <param name="raceRun">Current race state</param>
/// <param name="currentSpeed">Current speed to modify (passed by reference)</param>
void ApplyTrafficEffects(RaceRunHorse horse, RaceRun raceRun, ref double currentSpeed);
```

**Change to:**
```csharp
/// <summary>
/// Applies leg-type-specific traffic response effects when horse is blocked.
/// Modifies speed based on traffic ahead and horse's personality.
/// Uses actual horse speed calculation for realistic traffic dynamics.
/// </summary>
/// <param name="horse">The horse being affected</param>
/// <param name="raceRun">Current race state</param>
/// <param name="currentTick">Current race tick</param>
/// <param name="totalTicks">Total ticks in race</param>
/// <param name="currentSpeed">Current speed to modify (passed by reference)</param>
void ApplyTrafficEffects(
    RaceRunHorse horse,
    RaceRun raceRun,
    short currentTick,
    short totalTicks,
    ref double currentSpeed);
```

**Steps:**
1. Add `currentTick` parameter
2. Add `totalTicks` parameter
3. Update XML documentation

**Verify:** Project shows compilation errors (expected - need to update implementation and call sites)

---

#### Task 1.3: Update OvertakingManager.ApplyTrafficEffects Signature
**File:** [OvertakingManager.cs:65](c:\Development\TripleDerby\TripleDerby.Core\Racing\OvertakingManager.cs#L65)

**Current:**
```csharp
public void ApplyTrafficEffects(RaceRunHorse horse, RaceRun raceRun, ref double currentSpeed)
```

**Change to:**
```csharp
public void ApplyTrafficEffects(
    RaceRunHorse horse,
    RaceRun raceRun,
    short currentTick,
    short totalTicks,
    ref double currentSpeed)
```

**Steps:**
1. Update method signature
2. Update XML documentation (lines 61-64)
3. Keep method body unchanged for now

**Verify:** Compilation errors reduced (only call sites remain)

---

#### Task 1.4: Update RaceExecutor Call Site
**File:** [RaceExecutor.cs:275](c:\Development\TripleDerby\TripleDerby.Services.Racing\RaceExecutor.cs#L275)

**Current:**
```csharp
// Apply traffic response effects (speed capping / frustration)
overtakingManager.ApplyTrafficEffects(raceRunHorse, raceRun, ref baseSpeed);
```

**Change to:**
```csharp
// Apply traffic response effects (speed capping / frustration)
overtakingManager.ApplyTrafficEffects(raceRunHorse, raceRun, tick, totalTicks, ref baseSpeed);
```

**Steps:**
1. Add `tick` parameter (already available in scope)
2. Add `totalTicks` parameter (already available in scope)

**Verify:** Project compiles with no errors

---

#### Task 1.5: Verify All Tests Pass
**Command:** `dotnet test`

**Expected:** All existing tests pass (no behavior changes yet)

**If failures occur:** Review changes, ensure only signatures changed, not logic

---

### Phase 2: Core Implementation ⏱️ 45 minutes

**Objective:** Implement full modifier pipeline in CalculateHorseSpeed

#### Task 2.1: Update CalculateHorseSpeed Method Signature
**File:** [OvertakingManager.cs:360](c:\Development\TripleDerby\TripleDerby.Core\Racing\OvertakingManager.cs#L360)

**Current:**
```csharp
/// <summary>
/// Estimates the current speed of another horse for traffic response calculations.
/// TODO: Future enhancement - use actual horse speed based on Speed stat and modifiers
/// instead of average base speed for more realistic traffic dynamics.
/// </summary>
private static double CalculateHorseSpeed(RaceRunHorse horse)
{
    // Current implementation uses average base speed as conservative approximation
    // This ensures consistent traffic behavior regardless of individual horse stats
    return RaceModifierConfig.AverageBaseSpeed;
}
```

**Change to:**
```csharp
/// <summary>
/// Calculates the current speed of a horse using the full modifier pipeline.
/// Uses same calculation as UpdateHorsePosition for consistent traffic response.
/// Applies Stats → Environment → Phase → Stamina modifiers.
/// </summary>
/// <param name="horse">The horse to calculate speed for</param>
/// <param name="currentTick">Current race tick</param>
/// <param name="totalTicks">Total ticks in race</param>
/// <param name="raceRun">Current race state</param>
/// <returns>Calculated speed in furlongs per tick</returns>
private double CalculateHorseSpeed(
    RaceRunHorse horse,
    short currentTick,
    short totalTicks,
    RaceRun raceRun)
{
    // Implementation in next task
    return RaceModifierConfig.AverageBaseSpeed; // Temporary
}
```

**Steps:**
1. Remove `static` modifier (needs access to _speedModifierCalculator)
2. Add parameters: `currentTick`, `totalTicks`, `raceRun`
3. Update XML documentation
4. Remove TODO comment
5. Keep temporary return for now

**Verify:** Project compiles (will show warnings about unused parameters)

---

#### Task 2.2: Implement Full Modifier Pipeline
**File:** [OvertakingManager.cs:360-375](c:\Development\TripleDerby\TripleDerby.Core\Racing\OvertakingManager.cs#L360-L375)

**Implementation:**
```csharp
private double CalculateHorseSpeed(
    RaceRunHorse horse,
    short currentTick,
    short totalTicks,
    RaceRun raceRun)
{
    var baseSpeed = RaceModifierConfig.AverageBaseSpeed;

    // Build context for modifier calculations
    var context = new ModifierContext(
        CurrentTick: currentTick,
        TotalTicks: totalTicks,
        Horse: horse.Horse,
        RaceCondition: raceRun.ConditionId,
        RaceSurface: raceRun.Race.SurfaceId,
        RaceFurlongs: raceRun.Race.Furlongs
    );

    // Apply modifier pipeline (same order as UpdateHorsePosition)
    // Stats → Environment → Phase → Stamina
    baseSpeed *= _speedModifierCalculator.CalculateStatModifiers(context);
    baseSpeed *= _speedModifierCalculator.CalculateEnvironmentalModifiers(context);
    baseSpeed *= _speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);
    baseSpeed *= _speedModifierCalculator.CalculateStaminaModifier(horse);

    // Note: We intentionally skip:
    // - Risky lane change penalty (temporary state, not inherent speed)
    // - Random variance (too volatile for traffic comparison)
    // - Traffic effects (avoid circular dependency)

    return baseSpeed;
}
```

**Steps:**
1. Add using statement: `using TripleDerby.Core.Racing;` (for ModifierContext)
2. Replace method body with full implementation
3. Ensure modifier order matches UpdateHorsePosition

**Verify:** Project compiles with no warnings

---

#### Task 2.3: Update CalculateHorseSpeed Call Sites
**File:** [OvertakingManager.cs:86-114](c:\Development\TripleDerby\TripleDerby.Core\Racing\OvertakingManager.cs#L86-L114)

**Current pattern (5 occurrences):**
```csharp
var startDashCap = CalculateHorseSpeed(horseAhead) *
                  (1.0 - RaceModifierConfig.StartDashSpeedCapPenalty);
```

**Change to:**
```csharp
var startDashCap = CalculateHorseSpeed(horseAhead, currentTick, totalTicks, raceRun) *
                  (1.0 - RaceModifierConfig.StartDashSpeedCapPenalty);
```

**Locations to update:**
1. Line 86: StartDash leg type
2. Line 94: LastSpurt leg type
3. Line 102: StretchRunner leg type
4. Line 110: RailRunner leg type

**Steps:**
1. Find all `CalculateHorseSpeed(horseAhead)` calls
2. Add parameters: `currentTick, totalTicks, raceRun`
3. Verify all 4 call sites updated

**Verify:**
- Project compiles
- No unused parameter warnings
- All call sites pass correct context

---

#### Task 2.4: Run Existing Tests
**Command:** `dotnet test`

**Expected:** Tests may fail or show different results (race behavior changed)

**Analysis:**
- If tests are too brittle (hardcoded expectations), may need to update
- If tests use mocks, may need to configure ISpeedModifierCalculator mock
- Goal: Understand impact, not necessarily fix all tests yet

---

### Phase 3: Unit Testing ⏱️ 1-2 hours

**Objective:** Create comprehensive unit tests for CalculateHorseSpeed

#### Task 3.1: Create Test File
**File:** Create `TripleDerby.Tests.Unit\Racing\OvertakingManagerSpeedCalculationTests.cs`

**Template:**
```csharp
using Moq;
using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.SharedKernel.Enums;
using Xunit;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Tests for CalculateHorseSpeed method in OvertakingManager.
/// Validates that traffic response uses realistic horse speeds based on full modifier pipeline.
/// </summary>
public class OvertakingManagerSpeedCalculationTests
{
    private readonly Mock<IRandomGenerator> _mockRandom;
    private readonly Mock<ISpeedModifierCalculator> _mockSpeedCalc;

    public OvertakingManagerSpeedCalculationTests()
    {
        _mockRandom = new Mock<IRandomGenerator>();
        _mockSpeedCalc = new Mock<ISpeedModifierCalculator>();

        // Setup default neutral modifiers
        _mockSpeedCalc
            .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        _mockSpeedCalc
            .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        _mockSpeedCalc
            .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
            .Returns(1.0);
        _mockSpeedCalc
            .Setup(x => x.CalculateStaminaModifier(It.IsAny<RaceRunHorse>()))
            .Returns(1.0);
    }

    // Helper methods
    private OvertakingManager CreateManager() =>
        new OvertakingManager(_mockRandom.Object, _mockSpeedCalc.Object);

    private Horse CreateHorse(
        int speed = 50,
        int agility = 50,
        int stamina = 50,
        LegTypeId legType = LegTypeId.StartDash) => new Horse
    {
        Id = Guid.NewGuid(),
        Name = "Test Horse",
        Speed = speed,
        Agility = agility,
        Stamina = stamina,
        Durability = 50,
        Happiness = 50,
        LegTypeId = legType
    };

    private RaceRunHorse CreateRaceRunHorse(Horse horse, byte lane = 3, decimal distance = 0m) => new RaceRunHorse
    {
        Id = Guid.NewGuid(),
        Horse = horse,
        HorseId = horse.Id,
        Lane = lane,
        Distance = distance,
        CurrentStamina = (byte)horse.Stamina,
        InitialStamina = (byte)horse.Stamina,
        TicksSinceLastLaneChange = 10,
        SpeedPenaltyTicksRemaining = 0
    };

    private RaceRun CreateRaceRun(params RaceRunHorse[] horses) => new RaceRun
    {
        Id = Guid.NewGuid(),
        ConditionId = ConditionId.Fast,
        Race = new Race
        {
            Id = Guid.NewGuid(),
            Name = "Test Race",
            SurfaceId = SurfaceId.Dirt,
            Furlongs = 10m
        },
        Horses = horses.ToList()
    };

    // Tests will go here
}
```

**Steps:**
1. Create file in TripleDerby.Tests.Unit\Racing folder
2. Add using statements
3. Set up mocks and helper methods
4. Prepare for test implementation

**Verify:** Project compiles, test file discovered by test runner

---

#### Task 3.2: Test Speed Stat Differentiation
**Add to test file:**

```csharp
[Fact]
public void CalculateHorseSpeed_FastHorse_HigherThanSlowHorse()
{
    // Arrange
    _mockSpeedCalc
        .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
        .Returns((ModifierContext ctx) =>
            1.0 + ((ctx.Horse.Speed - 50) * RaceModifierConfig.SpeedModifierPerPoint));

    var manager = CreateManager();
    var fastHorse = CreateRaceRunHorse(CreateHorse(speed: 80));
    var slowHorse = CreateRaceRunHorse(CreateHorse(speed: 40));
    var raceRun = CreateRaceRun(fastHorse, slowHorse);

    // Act - use reflection to call private method
    var calculateMethod = typeof(OvertakingManager).GetMethod(
        "CalculateHorseSpeed",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var fastSpeed = (double)calculateMethod!.Invoke(
        manager,
        new object[] { fastHorse, (short)50, (short)100, raceRun })!;

    var slowSpeed = (double)calculateMethod.Invoke(
        manager,
        new object[] { slowHorse, (short)50, (short)100, raceRun })!;

    // Assert
    Assert.True(fastSpeed > slowSpeed, "Fast horse should have higher calculated speed");

    var speedRatio = fastSpeed / slowSpeed;
    Assert.InRange(speedRatio, 1.06, 1.10); // 40-point difference = ~8% speed difference
}

[Fact]
public void CalculateHorseSpeed_AverageSpeed_ReturnsBaseSpeed()
{
    // Arrange - all modifiers neutral
    var manager = CreateManager();
    var avgHorse = CreateRaceRunHorse(CreateHorse(speed: 50));
    var raceRun = CreateRaceRun(avgHorse);

    // Act
    var calculateMethod = typeof(OvertakingManager).GetMethod(
        "CalculateHorseSpeed",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var speed = (double)calculateMethod!.Invoke(
        manager,
        new object[] { avgHorse, (short)50, (short)100, raceRun })!;

    // Assert
    Assert.Equal(RaceModifierConfig.AverageBaseSpeed, speed, precision: 5);
}
```

**Verify:** Run tests, both should pass

---

#### Task 3.3: Test Stamina Impact
**Add to test file:**

```csharp
[Fact]
public void CalculateHorseSpeed_ExhaustedHorse_SlowerThanFreshHorse()
{
    // Arrange
    _mockSpeedCalc
        .Setup(x => x.CalculateStaminaModifier(It.IsAny<RaceRunHorse>()))
        .Returns((RaceRunHorse h) =>
        {
            var staminaPercent = h.CurrentStamina / h.InitialStamina;
            if (staminaPercent > 0.5)
                return 1.0 - ((1.0 - staminaPercent) * 0.02);
            else
            {
                var fatigueLevel = 1.0 - staminaPercent;
                var penalty = 0.01 + (fatigueLevel * fatigueLevel * 0.09);
                return 1.0 - penalty;
            }
        });

    var manager = CreateManager();

    var freshHorse = CreateRaceRunHorse(CreateHorse(stamina: 80));
    freshHorse.CurrentStamina = 80;
    freshHorse.InitialStamina = 80;

    var exhaustedHorse = CreateRaceRunHorse(CreateHorse(stamina: 80));
    exhaustedHorse.CurrentStamina = 10;
    exhaustedHorse.InitialStamina = 80;

    var raceRun = CreateRaceRun(freshHorse, exhaustedHorse);

    // Act
    var calculateMethod = typeof(OvertakingManager).GetMethod(
        "CalculateHorseSpeed",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var freshSpeed = (double)calculateMethod!.Invoke(
        manager,
        new object[] { freshHorse, (short)50, (short)100, raceRun })!;

    var exhaustedSpeed = (double)calculateMethod.Invoke(
        manager,
        new object[] { exhaustedHorse, (short)50, (short)100, raceRun })!;

    // Assert
    Assert.True(freshSpeed > exhaustedSpeed, "Fresh horse should be faster than exhausted horse");

    var speedRatio = exhaustedSpeed / freshSpeed;
    Assert.InRange(speedRatio, 0.90, 0.95); // Exhausted horse ~5-10% slower
}
```

**Verify:** Test passes

---

#### Task 3.4: Test Environmental Modifiers
**Add to test file:**

```csharp
[Fact]
public void CalculateHorseSpeed_DirtSpecialist_FasterOnDirt()
{
    // Arrange
    _mockSpeedCalc
        .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
        .Returns((ModifierContext ctx) =>
            ctx.RaceSurface == SurfaceId.Dirt ? 1.05 : 1.0);

    var manager = CreateManager();
    var horse = CreateRaceRunHorse(CreateHorse());

    var dirtRace = CreateRaceRun(horse);
    dirtRace.Race.SurfaceId = SurfaceId.Dirt;

    var turfRace = CreateRaceRun(horse);
    turfRace.Race.SurfaceId = SurfaceId.Turf;

    // Act
    var calculateMethod = typeof(OvertakingManager).GetMethod(
        "CalculateHorseSpeed",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var dirtSpeed = (double)calculateMethod!.Invoke(
        manager,
        new object[] { horse, (short)50, (short)100, dirtRace })!;

    var turfSpeed = (double)calculateMethod.Invoke(
        manager,
        new object[] { horse, (short)50, (short)100, turfRace })!;

    // Assert
    Assert.True(dirtSpeed > turfSpeed, "Horse should be faster on dirt");
    Assert.Equal(1.05, dirtSpeed / turfSpeed, precision: 2);
}
```

**Verify:** Test passes

---

#### Task 3.5: Test Phase Modifiers
**Add to test file:**

```csharp
[Fact]
public void CalculateHorseSpeed_LastSpurt_FasterLateRace()
{
    // Arrange
    _mockSpeedCalc
        .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
        .Returns((ModifierContext ctx, RaceRun rr) =>
        {
            var progress = (double)ctx.CurrentTick / ctx.TotalTicks;
            return ctx.Horse.LegTypeId == LegTypeId.LastSpurt && progress > 0.75
                ? 1.10
                : 1.0;
        });

    var manager = CreateManager();
    var lastSpurtHorse = CreateRaceRunHorse(CreateHorse(legType: LegTypeId.LastSpurt));
    var raceRun = CreateRaceRun(lastSpurtHorse);

    // Act
    var calculateMethod = typeof(OvertakingManager).GetMethod(
        "CalculateHorseSpeed",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var earlySpeed = (double)calculateMethod!.Invoke(
        manager,
        new object[] { lastSpurtHorse, (short)20, (short)100, raceRun })!;

    var lateSpeed = (double)calculateMethod.Invoke(
        manager,
        new object[] { lastSpurtHorse, (short)80, (short)100, raceRun })!;

    // Assert
    Assert.True(lateSpeed > earlySpeed, "LastSpurt should be faster late in race");
    Assert.Equal(1.10, lateSpeed / earlySpeed, precision: 2);
}
```

**Verify:** Test passes

---

#### Task 3.6: Test Combined Modifiers
**Add to test file:**

```csharp
[Fact]
public void CalculateHorseSpeed_MultipleModifiers_StackMultiplicatively()
{
    // Arrange - all modifiers active
    _mockSpeedCalc
        .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
        .Returns(1.08); // Fast horse

    _mockSpeedCalc
        .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
        .Returns(1.05); // Surface bonus

    _mockSpeedCalc
        .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
        .Returns(1.10); // Phase bonus

    _mockSpeedCalc
        .Setup(x => x.CalculateStaminaModifier(It.IsAny<RaceRunHorse>()))
        .Returns(0.95); // Slight fatigue

    var manager = CreateManager();
    var horse = CreateRaceRunHorse(CreateHorse());
    var raceRun = CreateRaceRun(horse);

    // Act
    var calculateMethod = typeof(OvertakingManager).GetMethod(
        "CalculateHorseSpeed",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var speed = (double)calculateMethod!.Invoke(
        manager,
        new object[] { horse, (short)80, (short)100, raceRun })!;

    // Assert
    var expectedMultiplier = 1.08 * 1.05 * 1.10 * 0.95; // ~1.1869
    var expectedSpeed = RaceModifierConfig.AverageBaseSpeed * expectedMultiplier;

    Assert.Equal(expectedSpeed, speed, precision: 5);
}
```

**Verify:** All 6+ unit tests pass

---

### Phase 4: Integration Testing ⏱️ 1-2 hours

**Objective:** Test traffic response behavior with realistic speed calculations

#### Task 4.1: Create Integration Test File
**File:** Create `TripleDerby.Tests.Unit\Racing\TrafficResponseIntegrationTests.cs`

**Template:**
```csharp
using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Utilities;
using TripleDerby.SharedKernel.Enums;
using Xunit;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Integration tests for traffic response with realistic speed calculations.
/// Tests that traffic caps adapt to actual horse speeds instead of constants.
/// </summary>
public class TrafficResponseIntegrationTests
{
    private readonly IRandomGenerator _randomGenerator;
    private readonly ISpeedModifierCalculator _speedModifierCalculator;
    private readonly IOvertakingManager _overtakingManager;

    public TrafficResponseIntegrationTests()
    {
        _randomGenerator = new RandomGenerator();
        _speedModifierCalculator = new SpeedModifierCalculator(_randomGenerator);
        _overtakingManager = new OvertakingManager(_randomGenerator, _speedModifierCalculator);
    }

    // Helper methods (same as unit tests)
    // Tests will go here
}
```

---

#### Task 4.2: Test Fast Horse Behind Slow Horse
**Add to test file:**

```csharp
[Fact]
public void ApplyTrafficEffects_FastHorseBehindSlowHorse_CappedLower()
{
    // Arrange
    var fastHorse = CreateHorse(speed: 80, legType: LegTypeId.StartDash);
    var slowHorse = CreateHorse(speed: 40, legType: LegTypeId.FrontRunner);

    var fastRRH = CreateRaceRunHorse(fastHorse, lane: 3, distance: 4.8m);
    var slowRRH = CreateRaceRunHorse(slowHorse, lane: 3, distance: 5.0m);

    var raceRun = CreateRaceRun(fastRRH, slowRRH);

    // Calculate expected speeds
    var slowHorseSpeed = CalculateExpectedSpeed(slowRRH, 50, 100, raceRun);
    var expectedCap = slowHorseSpeed * (1.0 - RaceModifierConfig.StartDashSpeedCapPenalty);

    var currentSpeed = 15.0; // Fast horse's natural high speed

    // Act
    _overtakingManager.ApplyTrafficEffects(fastRRH, raceRun, 50, 100, ref currentSpeed);

    // Assert
    Assert.True(currentSpeed <= expectedCap, "Fast horse should be capped to slow horse's speed");
    Assert.True(currentSpeed < 15.0, "Speed should have been reduced by traffic");
}

private double CalculateExpectedSpeed(
    RaceRunHorse horse,
    short currentTick,
    short totalTicks,
    RaceRun raceRun)
{
    var baseSpeed = RaceModifierConfig.AverageBaseSpeed;

    var context = new ModifierContext(
        CurrentTick: currentTick,
        TotalTicks: totalTicks,
        Horse: horse.Horse,
        RaceCondition: raceRun.ConditionId,
        RaceSurface: raceRun.Race.SurfaceId,
        RaceFurlongs: raceRun.Race.Furlongs
    );

    baseSpeed *= _speedModifierCalculator.CalculateStatModifiers(context);
    baseSpeed *= _speedModifierCalculator.CalculateEnvironmentalModifiers(context);
    baseSpeed *= _speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);
    baseSpeed *= _speedModifierCalculator.CalculateStaminaModifier(horse);

    return baseSpeed;
}
```

**Verify:** Test passes

---

#### Task 4.3: Test Slow Horse Behind Fast Horse
**Add to test file:**

```csharp
[Fact]
public void ApplyTrafficEffects_SlowHorseBehindFastHorse_NotPenalized()
{
    // Arrange
    var slowHorse = CreateHorse(speed: 40, legType: LegTypeId.StartDash);
    var fastHorse = CreateHorse(speed: 80, legType: LegTypeId.FrontRunner);

    var slowRRH = CreateRaceRunHorse(slowHorse, lane: 3, distance: 4.8m);
    var fastRRH = CreateRaceRunHorse(fastHorse, lane: 3, distance: 5.0m);

    var raceRun = CreateRaceRun(slowRRH, fastRRH);

    var currentSpeed = 8.0; // Slow horse's natural speed

    // Act
    _overtakingManager.ApplyTrafficEffects(slowRRH, raceRun, 50, 100, ref currentSpeed);

    // Assert - slow horse shouldn't be slowed further
    // Cap is based on fast horse ahead, which is higher than slow horse's natural speed
    Assert.Equal(8.0, currentSpeed, precision: 2);
}
```

**Verify:** Test passes

---

#### Task 4.4: Test Exhausted Horse Blocking
**Add to test file:**

```csharp
[Fact]
public void ApplyTrafficEffects_ExhaustedHorseAhead_LowerCap()
{
    // Arrange
    var freshHorse = CreateHorse(speed: 70, stamina: 80, legType: LegTypeId.StartDash);
    var tiredHorse = CreateHorse(speed: 70, stamina: 80, legType: LegTypeId.FrontRunner);

    var freshRRH = CreateRaceRunHorse(freshHorse, lane: 3, distance: 4.8m);
    var tiredRRH = CreateRaceRunHorse(tiredHorse, lane: 3, distance: 5.0m);

    // Exhaust the leader
    tiredRRH.CurrentStamina = 10;
    tiredRRH.InitialStamina = 80;

    var raceRun = CreateRaceRun(freshRRH, tiredRRH);

    // Calculate expected speeds
    var tiredHorseSpeed = CalculateExpectedSpeed(tiredRRH, 80, 100, raceRun);
    var expectedCap = tiredHorseSpeed * (1.0 - RaceModifierConfig.StartDashSpeedCapPenalty);

    var currentSpeed = 12.0;

    // Act
    _overtakingManager.ApplyTrafficEffects(freshRRH, raceRun, 80, 100, ref currentSpeed);

    // Assert
    Assert.True(currentSpeed <= expectedCap);
    // Exhausted horse's speed is lower, so cap should be lower too
}
```

**Verify:** Test passes

---

#### Task 4.5: Test Phase-Dependent Traffic
**Add to test file:**

```csharp
[Fact]
public void ApplyTrafficEffects_LastSpurtLateRace_HigherCap()
{
    // Arrange
    var blockedHorse = CreateHorse(speed: 60, legType: LegTypeId.StartDash);
    var lastSpurtHorse = CreateHorse(speed: 60, legType: LegTypeId.LastSpurt);

    var blockedRRH = CreateRaceRunHorse(blockedHorse, lane: 3, distance: 4.8m);
    var lastSpurtRRH = CreateRaceRunHorse(lastSpurtHorse, lane: 3, distance: 5.0m);

    var raceRun = CreateRaceRun(blockedRRH, lastSpurtRRH);

    var currentSpeed = 12.0;

    // Act - early race (LastSpurt no bonus)
    var earlySpeed = currentSpeed;
    _overtakingManager.ApplyTrafficEffects(blockedRRH, raceRun, 20, 100, ref earlySpeed);

    // Act - late race (LastSpurt bonus active)
    var lateSpeed = currentSpeed;
    _overtakingManager.ApplyTrafficEffects(blockedRRH, raceRun, 80, 100, ref lateSpeed);

    // Assert
    // Late race: LastSpurt horse faster → higher cap for blocked horse
    Assert.True(lateSpeed > earlySpeed, "Cap should be higher late race when LastSpurt bonus active");
}
```

**Verify:** 4+ integration tests pass

---

### Phase 5: Balance Validation ⏱️ 1-2 hours

**Objective:** Validate that faster horses perform better with realistic speed calculations

#### Task 5.1: Create Balance Validation Test File
**File:** Create `TripleDerby.Tests.Unit\Racing\HorseSpeedBalanceValidationTests.cs`

(Due to space, I'll outline the structure rather than full implementation)

**Tests to create:**
1. `BalanceValidation_FasterHorses_WinMoreOften()` - Statistical analysis
2. `BalanceValidation_TrafficEffectiveness_BySpeed()` - Traffic impact by Speed stat
3. `BalanceValidation_MixedField_RealisticResults()` - Full field simulation

---

#### Task 5.2: Implement Statistical Win Rate Test
**Key points:**
- Run 1000+ simulated races
- Vary Speed stat across horses (20-100 range)
- Measure win percentage by Speed stat
- Verify positive correlation

**Acceptance:** Higher Speed stat → higher win rate

---

#### Task 5.3: Traffic Effectiveness Analysis
**Key points:**
- Test traffic blocking with Speed=30, 50, 70, 90 horses
- Measure how much each slows down a Speed=70 horse behind them
- Verify slower horses create more blocking

**Acceptance:** Speed=30 horse slows follower more than Speed=90 horse

---

#### Task 5.4: Performance Benchmark
**Test:**
```csharp
[Fact]
public void Performance_CalculateHorseSpeed_AcceptableSpeed()
{
    var iterations = 10000;
    var stopwatch = Stopwatch.StartNew();

    for (int i = 0; i < iterations; i++)
    {
        // Call CalculateHorseSpeed via ApplyTrafficEffects
    }

    stopwatch.Stop();

    var avgMicroseconds = (stopwatch.Elapsed.TotalMilliseconds * 1000) / iterations;
    Assert.True(avgMicroseconds < 100, $"Average execution time too high: {avgMicroseconds}μs");
}
```

**Acceptance:** Average execution <100μs per call

---

## Final Checklist

### Implementation Complete When:
- ✅ OvertakingManager constructor accepts ISpeedModifierCalculator
- ✅ IOvertakingManager.ApplyTrafficEffects signature updated
- ✅ CalculateHorseSpeed implements full modifier pipeline
- ✅ All 5 call sites updated in ApplyTrafficEffects
- ✅ RaceExecutor.cs call site passes currentTick and totalTicks
- ✅ Project compiles with no errors or warnings
- ✅ XML documentation updated

### Testing Complete When:
- ✅ All existing tests pass (or updated appropriately)
- ✅ 6+ unit tests for CalculateHorseSpeed pass
- ✅ 4+ integration tests for traffic response pass
- ✅ 3+ balance validation tests pass
- ✅ Performance benchmark passes (<100μs)
- ✅ Total test count: 26-35 tests

### Documentation Complete When:
- ✅ Feature specification finalized (this document's parent)
- ✅ XML comments updated on modified methods
- ✅ TODO comment removed from CalculateHorseSpeed
- ✅ Implementation plan marked complete

---

## Troubleshooting

### Common Issues

**Issue 1: Tests failing after implementation**
- **Cause:** Existing tests may have hardcoded expectations
- **Solution:** Review test expectations, update to account for realistic speeds

**Issue 2: Performance regression**
- **Cause:** ModifierContext creation overhead
- **Solution:** Profile with BenchmarkDotNet, optimize if >10% slower

**Issue 3: Circular dependency warnings**
- **Cause:** CalculateHorseSpeed called from ApplyTrafficEffects
- **Solution:** Ensure we don't call ApplyTrafficEffects from CalculateHorseSpeed

**Issue 4: Null reference in CalculateHorseSpeed**
- **Cause:** Missing raceRun.Race or raceRun.Horses
- **Solution:** Add null checks or ensure data always populated

---

## Implementation Tips

### TDD Approach
1. Write test first (Red)
2. Implement minimal code to pass (Green)
3. Refactor for clarity (Refactor)
4. Repeat

### Reflection for Private Method Testing
```csharp
var method = typeof(OvertakingManager).GetMethod(
    "CalculateHorseSpeed",
    BindingFlags.NonPublic | BindingFlags.Instance);

var result = (double)method!.Invoke(instance, parameters)!;
```

### Mock Setup Pattern
```csharp
_mockSpeedCalc
    .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
    .Returns((ModifierContext ctx) => /* calculate based on ctx */);
```

### Integration Test Pattern
- Use real SpeedModifierCalculator (not mock)
- Create full RaceRun with Race and Horses
- Verify realistic behavior, not exact numbers

---

## Next Steps After Implementation

1. **Run full test suite:** `dotnet test --verbosity normal`
2. **Review code coverage:** Aim for >90% on modified files
3. **Conduct balance testing:** Run large-scale race simulations
4. **Update documentation:** Mark feature as complete
5. **Create PR:** Follow project PR guidelines
6. **Plan follow-up:** Identify any discovered improvements

---

## Summary

This implementation plan provides a structured, test-driven approach to implementing realistic horse speed calculations in the traffic response system. By following the phases sequentially and validating at each step, we ensure a robust, well-tested implementation that improves race realism while maintaining performance.

**Total Estimated Time:** 4-6 hours
**Complexity:** Medium
**Risk:** Low (well-defined, tested approach)

Good luck with the implementation!
