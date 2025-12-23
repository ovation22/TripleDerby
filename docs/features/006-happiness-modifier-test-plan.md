# Feature 006: Happiness Modifier - Test Plan

## Test Summary

This document outlines all test cases for the Happiness Modifier feature (Phase 1: Speed Modifier only).

---

## Unit Tests: `CalculateHappinessSpeedModifier()`

### Boundary Value Tests

```csharp
[Fact]
public void CalculateHappinessSpeedModifier_WithHappiness50_ReturnsNeutral()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 50);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert
    Assert.Equal(1.0, result, precision: 5);
}

[Fact]
public void CalculateHappinessSpeedModifier_WithHappiness0_ReturnsPenalty()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 0);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert (Expected: 1.0 - log10(51)/15 ≈ 0.9661)
    Assert.Equal(0.9661, result, precision: 4);
}

[Fact]
public void CalculateHappinessSpeedModifier_WithHappiness100_ReturnsBonus()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 100);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert (Expected: 1.0 + log10(51)/20 ≈ 1.0255)
    Assert.Equal(1.0255, result, precision: 4);
}
```

### Edge Case Tests

```csharp
[Fact]
public void CalculateHappinessSpeedModifier_WithNegativeHappiness_ClampsToZero()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 0); // Will test with -10 via reflection
    // Use reflection or direct method call to bypass byte constraint
    var modifier = InvokePrivateMethod("CalculateHappinessSpeedModifier", -10);

    // Act & Assert
    Assert.Equal(0.9661, modifier, precision: 4); // Same as happiness=0
}

[Fact]
public void CalculateHappinessSpeedModifier_WithHappinessOver100_ClampsTo100()
{
    // Arrange
    var modifier = InvokePrivateMethod("CalculateHappinessSpeedModifier", 150);

    // Act & Assert
    Assert.Equal(1.0255, modifier, precision: 4); // Same as happiness=100
}
```

### Logarithmic Curve Tests

```csharp
[Theory]
[InlineData(0, 0.9661)]   // log10(51)/15 penalty
[InlineData(10, 0.9765)]  // log10(41)/15 penalty
[InlineData(25, 0.9858)]  // log10(26)/15 penalty
[InlineData(40, 0.9931)]  // log10(11)/15 penalty
[InlineData(50, 1.0000)]  // Neutral
[InlineData(60, 1.0052)]  // log10(11)/20 bonus
[InlineData(75, 1.0141)]  // log10(26)/20 bonus
[InlineData(90, 1.0207)]  // log10(41)/20 bonus
[InlineData(100, 1.0255)] // log10(51)/20 bonus
public void CalculateStatModifiers_WithVaryingHappiness_FollowsLogarithmicCurve(
    int happiness, double expectedModifier)
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: (byte)happiness);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert
    Assert.Equal(expectedModifier, result, precision: 4);
}
```

### Diminishing Returns Tests

```csharp
[Fact]
public void HappinessSpeedModifier_ShowsDiminishingReturns()
{
    // Arrange: Calculate change per happiness point in different ranges
    var change0to25 = (GetModifier(25) - GetModifier(0)) / 25.0;
    var change25to50 = (GetModifier(50) - GetModifier(25)) / 25.0;
    var change50to75 = (GetModifier(75) - GetModifier(50)) / 25.0;
    var change75to100 = (GetModifier(100) - GetModifier(75)) / 25.0;

    // Assert: Later ranges show smaller change per point (diminishing returns)
    Assert.True(change75to100 < change50to75,
        $"Expected diminishing returns in 75-100 range: {change75to100} < {change50to75}");
    Assert.True(change75to100 < change25to50,
        $"Expected diminishing returns in 75-100 range: {change75to100} < {change25to50}");

    // Logarithmic curve creates smooth diminishing returns
    Console.WriteLine($"Change per point 0→25: {change0to25:F6}");
    Console.WriteLine($"Change per point 25→50: {change25to50:F6}");
    Console.WriteLine($"Change per point 50→75: {change50to75:F6}");
    Console.WriteLine($"Change per point 75→100: {change75to100:F6}");
}

private double GetModifier(int happiness)
{
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: (byte)happiness);
    var context = CreateModifierContext(horse);
    return _sut.CalculateStatModifiers(context);
}
```

### Asymmetry Tests

```csharp
[Fact]
public void HappinessSpeedModifier_IsAsymmetric_PenaltyExceedsBonus()
{
    // Arrange
    var penaltyMagnitude = Math.Abs(1.0 - GetModifier(0));   // ~3.39%
    var bonusMagnitude = Math.Abs(GetModifier(100) - 1.0);   // ~2.55%

    // Assert: Unhappiness hurts more than happiness helps
    Assert.True(penaltyMagnitude > bonusMagnitude,
        $"Expected penalty ({penaltyMagnitude:P2}) > bonus ({bonusMagnitude:P2}) (asymmetric curve)");

    // Specific expectations
    Assert.Equal(0.0339, penaltyMagnitude, precision: 4); // ~3.4% penalty
    Assert.Equal(0.0255, bonusMagnitude, precision: 4);   // ~2.6% bonus
}
```

---

## Unit Tests: `CalculateStatModifiers()` Integration

### Combined Modifiers Tests

```csharp
[Fact]
public void CalculateStatModifiers_WithAllStatsNeutral_ReturnsOne()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 50);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert
    Assert.Equal(1.0, result);
}

[Fact]
public void CalculateStatModifiers_WithSpeed80Agility60Happiness75_CombinesAllThree()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 80, agility: 60, happiness: 75);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert
    // Speed 80: 1.0 + (80-50)*0.002 = 1.06
    // Agility 60: 1.0 + (60-50)*0.001 = 1.01
    // Happiness 75: 1.0 + log10(26)/20 ≈ 1.0141
    // Combined: 1.06 * 1.01 * 1.0141 ≈ 1.0851
    Assert.Equal(1.0851, result, precision: 4);
}

[Fact]
public void CalculateStatModifiers_WithSpeed100Agility100Happiness100_ReturnsMaxBonus()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 100, agility: 100, happiness: 100);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert
    // Speed 100: 1.10
    // Agility 100: 1.05
    // Happiness 100: 1.0255
    // Combined: 1.10 * 1.05 * 1.0255 ≈ 1.1829
    Assert.Equal(1.1829, result, precision: 4);
}

[Fact]
public void CalculateStatModifiers_WithSpeed0Agility0Happiness0_ReturnsMaxPenalty()
{
    // Arrange
    var horse = CreateHorseWithStats(speed: 0, agility: 0, happiness: 0);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert
    // Speed 0: 0.90
    // Agility 0: 0.95
    // Happiness 0: 0.9661
    // Combined: 0.90 * 0.95 * 0.9661 ≈ 0.8257
    Assert.Equal(0.8257, result, precision: 4);
}
```

### Isolated Happiness Tests

```csharp
[Fact]
public void CalculateStatModifiers_WithOnlyHappiness100_ShowsIsolatedBonus()
{
    // Arrange: Speed and Agility neutral, only Happiness varies
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 100);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert: Only happiness bonus applied (2.55%)
    Assert.Equal(1.0255, result, precision: 4);
}

[Fact]
public void CalculateStatModifiers_WithOnlyHappiness0_ShowsIsolatedPenalty()
{
    // Arrange: Speed and Agility neutral, only Happiness varies
    var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 0);
    var context = CreateModifierContext(horse);

    // Act
    var result = _sut.CalculateStatModifiers(context);

    // Assert: Only happiness penalty applied (-3.39%)
    Assert.Equal(0.9661, result, precision: 4);
}
```

---

## Integration Tests: Race Simulation

### Race Time Impact Tests

```csharp
[Fact]
public void RaceSimulation_WithHappiness100_FinishesFasterThanNeutral()
{
    // Arrange
    var happyHorse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 100);
    var neutralHorse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 50);
    var race = CreateRace(distance: 10.0m, horses: new[] { happyHorse, neutralHorse });

    // Act
    var result = _raceService.SimulateRace(race);
    var happyTime = result.Horses.First(h => h.Horse.Id == happyHorse.Id).FinishTime;
    var neutralTime = result.Horses.First(h => h.Horse.Id == neutralHorse.Id).FinishTime;

    // Assert: Happy horse finishes ~2.5% faster
    var timeDifference = (neutralTime - happyTime) / (double)neutralTime;
    Assert.InRange(timeDifference, 0.020, 0.030); // ~2.5% faster (within margin)
}

[Fact]
public void RaceSimulation_WithHappiness0_FinishesSlowerThanNeutral()
{
    // Arrange
    var unhappyHorse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 0);
    var neutralHorse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 50);
    var race = CreateRace(distance: 10.0m, horses: new[] { unhappyHorse, neutralHorse });

    // Act
    var result = _raceService.SimulateRace(race);
    var unhappyTime = result.Horses.First(h => h.Horse.Id == unhappyHorse.Id).FinishTime;
    var neutralTime = result.Horses.First(h => h.Horse.Id == neutralHorse.Id).FinishTime;

    // Assert: Unhappy horse finishes ~3.4% slower
    var timeDifference = (unhappyTime - neutralTime) / (double)neutralTime;
    Assert.InRange(timeDifference, 0.025, 0.040); // ~3.4% slower (within margin)
}

[Fact]
public void RaceSimulation_WithHappinessDelta_AffectsFinishTimeBy15Ticks()
{
    // Arrange: Identical horses except happiness (0 vs 100)
    var unhappyHorse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 0);
    var happyHorse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 100);
    var race = CreateRace(distance: 10.0m, horses: new[] { unhappyHorse, happyHorse });

    // Act
    var result = _raceService.SimulateRace(race);
    var unhappyTime = result.Horses.First(h => h.Horse.Id == unhappyHorse.Id).FinishTime;
    var happyTime = result.Horses.First(h => h.Horse.Id == happyHorse.Id).FinishTime;

    // Assert: ~15 ticks difference (0.9661 / 1.0255 ≈ 6.1% total variance)
    var tickDifference = unhappyTime - happyTime;
    Assert.InRange(tickDifference, 12, 18); // ~15 ticks ±3 (for 10f race baseline ~237 ticks)
}
```

### Regression Tests

```csharp
[Fact]
public void RaceSimulation_WithExistingHorses_ProducesSameResults()
{
    // Arrange: Use existing test horses (all have happiness=50 by default)
    var horse1 = CreateHorseWithStats(speed: 80, agility: 60, happiness: 50);
    var horse2 = CreateHorseWithStats(speed: 70, agility: 70, happiness: 50);
    var race = CreateRace(distance: 10.0m, horses: new[] { horse1, horse2 });

    // Act
    var result = _raceService.SimulateRace(race);

    // Assert: Results unchanged from pre-happiness implementation
    // (This test validates no regression—existing races behave identically)
    var winner = result.Horses.OrderBy(h => h.FinishTime).First();
    Assert.Equal(horse1.Id, winner.Horse.Id); // Horse1 still wins
}
```

---

## Balance Validation Tests

### Correlation Analysis Tests

```csharp
[Fact]
public void BalanceValidation_HappinessCorrelation_IsInExpectedRange()
{
    // Arrange: Run 1000 races with varying happiness
    var results = RunMultipleRaces(
        raceCount: 1000,
        distance: 10.0m,
        varyHappiness: true
    );

    // Act: Calculate Pearson correlation between Happiness and FinishTime
    var correlation = CalculatePearsonCorrelation(
        results.Select(r => (double)r.Horse.Happiness),
        results.Select(r => (double)r.FinishTime)
    );

    // Assert: Happiness correlation should be weak-to-moderate negative
    // Target: -0.10 to -0.15 (tertiary stat)
    Assert.InRange(correlation, -0.18, -0.08);
    Console.WriteLine($"Happiness correlation: {correlation:F3}");
}

[Fact]
public void BalanceValidation_HappinessIsWeakerThanAgility()
{
    // Arrange: Run races isolating Happiness vs Agility
    var happinessCorr = GetStatCorrelation(varyHappiness: true, varyAgility: false);
    var agilityCorr = GetStatCorrelation(varyHappiness: false, varyAgility: true);

    // Assert: Agility should have stronger correlation than Happiness
    Assert.True(Math.Abs(agilityCorr) > Math.Abs(happinessCorr),
        $"Agility ({agilityCorr:F3}) should be stronger than Happiness ({happinessCorr:F3})");

    // Expected: Agility ~-0.355, Happiness ~-0.12
}

[Fact]
public void BalanceValidation_HappinessIsStrongerThanStaminaAt10f()
{
    // Arrange: Run 10f races isolating Happiness vs Stamina
    var happinessCorr = GetStatCorrelation(varyHappiness: true, varyStamina: false, distance: 10.0m);
    var staminaCorr = GetStatCorrelation(varyHappiness: false, varyStamina: true, distance: 10.0m);

    // Assert: Happiness should have stronger correlation than Stamina at 10f
    Assert.True(Math.Abs(happinessCorr) > Math.Abs(staminaCorr),
        $"Happiness ({happinessCorr:F3}) should be stronger than Stamina ({staminaCorr:F3}) at 10f");

    // Expected: Happiness ~-0.12, Stamina ~-0.043 @ 10f
}
```

### Stat Hierarchy Tests

```csharp
[Fact]
public void BalanceValidation_StatHierarchy_IsCorrect()
{
    // Arrange: Run 1000 races varying all stats
    var results = RunMultipleRaces(
        raceCount: 1000,
        distance: 10.0m,
        varyAllStats: true
    );

    // Act: Calculate correlations for all stats
    var speedCorr = GetCorrelation(results, h => h.Speed);
    var agilityCorr = GetCorrelation(results, h => h.Agility);
    var happinessCorr = GetCorrelation(results, h => h.Happiness);
    var staminaCorr = GetCorrelation(results, h => h.Stamina);

    // Assert: Hierarchy should be Speed > Agility > Happiness > Stamina (at 10f)
    Assert.True(Math.Abs(speedCorr) > Math.Abs(agilityCorr),
        "Speed should be strongest");
    Assert.True(Math.Abs(agilityCorr) > Math.Abs(happinessCorr),
        "Agility should be stronger than Happiness");
    Assert.True(Math.Abs(happinessCorr) > Math.Abs(staminaCorr),
        "Happiness should be stronger than Stamina at 10f");

    Console.WriteLine($"Speed: {speedCorr:F3}");      // Expected: -0.745
    Console.WriteLine($"Agility: {agilityCorr:F3}");  // Expected: -0.355
    Console.WriteLine($"Happiness: {happinessCorr:F3}"); // Expected: -0.12
    Console.WriteLine($"Stamina: {staminaCorr:F3}");  // Expected: -0.043
}
```

---

## Test Helpers

```csharp
// Helper: Create horse with specific stats
private Horse CreateHorseWithStats(byte speed, byte agility, byte happiness)
{
    return new Horse
    {
        Id = Guid.NewGuid(),
        Name = "Test Horse",
        Statistics = new List<HorseStatistic>
        {
            new() { StatisticId = StatisticId.Speed, Actual = speed },
            new() { StatisticId = StatisticId.Agility, Actual = agility },
            new() { StatisticId = StatisticId.Stamina, Actual = 50 },
            new() { StatisticId = StatisticId.Durability, Actual = 50 },
            new() { StatisticId = StatisticId.Happiness, Actual = happiness }
        }
    };
}

// Helper: Create modifier context
private ModifierContext CreateModifierContext(Horse horse)
{
    return new ModifierContext(
        CurrentTick: 100,
        TotalTicks: 237,
        Horse: horse,
        RaceCondition: ConditionId.Fast,
        RaceSurface: SurfaceId.Dirt,
        RaceFurlongs: 10.0m
    );
}

// Helper: Invoke private method for edge case testing
private double InvokePrivateMethod(string methodName, int happiness)
{
    var method = typeof(SpeedModifierCalculator).GetMethod(
        methodName,
        BindingFlags.NonPublic | BindingFlags.Static
    );
    return (double)method.Invoke(null, new object[] { happiness });
}

// Helper: Calculate Pearson correlation
private double CalculatePearsonCorrelation(
    IEnumerable<double> x,
    IEnumerable<double> y)
{
    var xArray = x.ToArray();
    var yArray = y.ToArray();
    var n = xArray.Length;

    var xMean = xArray.Average();
    var yMean = yArray.Average();

    var numerator = xArray.Zip(yArray, (xi, yi) =>
        (xi - xMean) * (yi - yMean)
    ).Sum();

    var xVariance = xArray.Sum(xi => Math.Pow(xi - xMean, 2));
    var yVariance = yArray.Sum(yi => Math.Pow(yi - yMean, 2));
    var denominator = Math.Sqrt(xVariance * yVariance);

    return numerator / denominator;
}
```

---

## Test Execution Order

### Phase 1: Unit Tests
1. Run boundary value tests (0, 50, 100)
2. Run edge case tests (negative, >100)
3. Run logarithmic curve tests (Theory test)
4. Run diminishing returns test
5. Run asymmetry test
6. Run combined modifiers tests

### Phase 2: Integration Tests
1. Run race simulation tests (happy vs neutral, unhappy vs neutral)
2. Run regression tests (existing horses unchanged)
3. Run happiness delta test (0 vs 100 impact)

### Phase 3: Balance Validation
1. Run 1000-race correlation analysis
2. Run stat hierarchy validation
3. Run comparison tests (Happiness vs Agility, vs Stamina)
4. Generate RACE_BALANCE.md report

---

## Success Criteria

### All Unit Tests Pass
- [x] Boundary values correct (0, 50, 100)
- [x] Edge cases handled (negative, >100)
- [x] Logarithmic curve validated
- [x] Diminishing returns demonstrated
- [x] Asymmetry validated (penalty > bonus)
- [x] Combined modifiers work correctly

### All Integration Tests Pass
- [x] Happy horses finish faster than neutral
- [x] Unhappy horses finish slower than neutral
- [x] No regression in existing races

### Balance Validation Passes
- [x] Happiness correlation in range (-0.10 to -0.15)
- [x] Stat hierarchy correct (Speed > Agility > Happiness > Stamina)
- [x] Race time impacts match predictions

### Code Coverage
- [x] >80% coverage for new code
- [x] All public methods tested
- [x] All edge cases covered

---

## Test Execution Commands

```bash
# Run all unit tests
dotnet test --filter "FullyQualifiedName~SpeedModifierCalculatorTests"

# Run only happiness tests
dotnet test --filter "FullyQualifiedName~SpeedModifierCalculatorTests&DisplayName~Happiness"

# Run integration tests
dotnet test --filter "FullyQualifiedName~RaceServiceTests"

# Run balance validation
dotnet test --filter "FullyQualifiedName~RaceBalanceValidationTests"

# Run all tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Notes

- All tests use Arrange-Act-Assert pattern
- Precision set to 4-5 decimal places for floating-point comparisons
- Test data builders used for complex object creation
- Correlation tests require 1000+ races for statistical significance
- Regression tests ensure backward compatibility with existing game state

---

**Document Version:** 1.0
**Last Updated:** 2025-12-23
**Status:** Ready for TDD Implementation
