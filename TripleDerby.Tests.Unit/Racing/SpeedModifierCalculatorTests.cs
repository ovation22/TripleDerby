using Moq;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.Services.Racing.Calculators;
using TripleDerby.Services.Racing;

namespace TripleDerby.Tests.Unit.Racing;

public class SpeedModifierCalculatorTests
{
    private readonly Mock<IRandomGenerator> _randomGeneratorMock;
    private readonly SpeedModifierCalculator _sut;

    public SpeedModifierCalculatorTests()
    {
        _randomGeneratorMock = new Mock<IRandomGenerator>();
        _randomGeneratorMock.Setup(r => r.NextDouble()).Returns(0.5);
        _sut = new SpeedModifierCalculator(_randomGeneratorMock.Object);
    }

    [Fact]
    public void Constructor_WithValidRandomGenerator_ShouldNotThrow()
    {
        // Arrange
        var randomGenerator = Mock.Of<IRandomGenerator>();

        // Act
        var calculator = new SpeedModifierCalculator(randomGenerator);

        // Assert
        Assert.NotNull(calculator);
    }

    // ============================================================================
    // Phase 2: Stat Modifier Tests
    // ============================================================================

    [Fact]
    public void CalculateStatModifiers_WithSpeed50_ShouldReturnNeutral()
    {
        // Arrange - Speed 50 is neutral point
        var horse = CreateHorseWithStats(speed: 50, agility: 50);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed0_ShouldReturn0Point90()
    {
        // Arrange - Speed 0 should give -10% modifier
        var horse = CreateHorseWithStats(speed: 0, agility: 50);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert - Speed 0 = 1.0 + ((0 - 50) * 0.002) = 1.0 - 0.1 = 0.90
        Assert.Equal(0.90, result, precision: 5);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed100_ShouldReturn1Point10()
    {
        // Arrange - Speed 100 should give +10% modifier
        var horse = CreateHorseWithStats(speed: 100, agility: 50);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert - Speed 100 = 1.0 + ((100 - 50) * 0.002) = 1.0 + 0.1 = 1.10
        Assert.Equal(1.10, result, precision: 5);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed70_ShouldReturn1Point04()
    {
        // Arrange - Speed 70 should give +4% modifier
        var horse = CreateHorseWithStats(speed: 70, agility: 50);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert - Speed 70 = 1.0 + ((70 - 50) * 0.002) = 1.0 + 0.04 = 1.04
        Assert.Equal(1.04, result, precision: 5);
    }

    [Fact]
    public void CalculateStatModifiers_WithAgility50_ShouldReturnNeutral()
    {
        // Arrange - Agility 50 is neutral point
        var horse = CreateHorseWithStats(speed: 50, agility: 50);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateStatModifiers_WithAgility0_ShouldReturn0Point95()
    {
        // Arrange - Agility 0 should give -5% modifier
        var horse = CreateHorseWithStats(speed: 50, agility: 0);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert - Agility 0 = 1.0 + ((0 - 50) * 0.001) = 1.0 - 0.05 = 0.95
        Assert.Equal(0.95, result, precision: 5);
    }

    [Fact]
    public void CalculateStatModifiers_WithAgility100_ShouldReturn1Point05()
    {
        // Arrange - Agility 100 should give +5% modifier
        var horse = CreateHorseWithStats(speed: 50, agility: 100);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert - Agility 100 = 1.0 + ((100 - 50) * 0.001) = 1.0 + 0.05 = 1.05
        Assert.Equal(1.05, result, precision: 5);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed80Agility60_ShouldCombineModifiers()
    {
        // Arrange - Both speed and agility above neutral
        var horse = CreateHorseWithStats(speed: 80, agility: 60);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        // Speed 80 = 1.0 + ((80 - 50) * 0.002) = 1.06
        // Agility 60 = 1.0 + ((60 - 50) * 0.001) = 1.01
        // Combined = 1.06 * 1.01 = 1.0706
        Assert.Equal(1.0706, result, precision: 4);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed40Agility30_ShouldCombineNegativeModifiers()
    {
        // Arrange - Both stats below neutral
        var horse = CreateHorseWithStats(speed: 40, agility: 30);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        // Speed 40 = 1.0 + ((40 - 50) * 0.002) = 0.98
        // Agility 30 = 1.0 + ((30 - 50) * 0.001) = 0.98
        // Combined = 0.98 * 0.98 = 0.9604
        Assert.Equal(0.9604, result, precision: 4);
    }

    // ============================================================================
    // Happiness Modifier Tests
    // ============================================================================

    [Fact]
    public void CalculateStatModifiers_WithHappiness50_ShouldReturnNeutral()
    {
        // Arrange - Happiness 50 is neutral point with Speed/Agility also neutral
        var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 50);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void CalculateStatModifiers_WithHappiness0_ShouldReturnPenalty()
    {
        // Arrange - Happiness 0 with Speed/Agility neutral
        var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 0);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert - Expected: 1.0 - log10(51)/15 ≈ 0.8862
        Assert.Equal(0.8862, result, precision: 3);
    }

    [Fact]
    public void CalculateStatModifiers_WithHappiness100_ShouldReturnBonus()
    {
        // Arrange - Happiness 100 with Speed/Agility neutral
        var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: 100);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert - Expected: 1.0 + log10(51)/20 ≈ 1.0854
        Assert.Equal(1.0854, result, precision: 4);
    }

    [Theory]
    [InlineData(0, 0.8862)]   // 1.0 - log10(51)/15 penalty
    [InlineData(10, 0.8925)]  // 1.0 - log10(41)/15 penalty
    [InlineData(25, 0.9057)]  // 1.0 - log10(26)/15 penalty
    [InlineData(40, 0.9306)]  // 1.0 - log10(11)/15 penalty
    [InlineData(50, 1.0000)]  // Neutral
    [InlineData(60, 1.0521)]  // 1.0 + log10(11)/20 bonus
    [InlineData(75, 1.0707)]  // 1.0 + log10(26)/20 bonus
    [InlineData(90, 1.0806)]  // 1.0 + log10(41)/20 bonus
    [InlineData(100, 1.0854)] // 1.0 + log10(51)/20 bonus
    public void CalculateStatModifiers_WithVaryingHappiness_FollowsLogarithmicCurve(
        int happiness, double expectedModifier)
    {
        // Arrange - Only happiness varies, Speed/Agility neutral
        var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: (byte)happiness);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        Assert.Equal(expectedModifier, result, precision: 3);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed80Agility60Happiness75_ShouldCombineAllThree()
    {
        // Arrange - All three stats above neutral
        var horse = CreateHorseWithStats(speed: 80, agility: 60, happiness: 75);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        // Speed 80: 1.0 + (80-50)*0.002 = 1.06
        // Agility 60: 1.0 + (60-50)*0.001 = 1.01
        // Happiness 75: 1.0 + log10(26)/20 ≈ 1.0707
        // Combined: 1.06 * 1.01 * 1.0707 ≈ 1.1463
        Assert.Equal(1.1463, result, precision: 4);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed100Agility100Happiness100_ShouldReturnMaxBonus()
    {
        // Arrange - All stats at maximum
        var horse = CreateHorseWithStats(speed: 100, agility: 100, happiness: 100);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        // Speed 100: 1.10
        // Agility 100: 1.05
        // Happiness 100: 1.0854
        // Combined: 1.10 * 1.05 * 1.0854 ≈ 1.2536
        Assert.Equal(1.2536, result, precision: 4);
    }

    [Fact]
    public void CalculateStatModifiers_WithSpeed0Agility0Happiness0_ShouldReturnMaxPenalty()
    {
        // Arrange - All stats at minimum
        var horse = CreateHorseWithStats(speed: 0, agility: 0, happiness: 0);
        var context = CreateModifierContext(horse);

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        // Speed 0: 0.90
        // Agility 0: 0.95
        // Happiness 0: 0.8861
        // Combined: 0.90 * 0.95 * 0.8861 ≈ 0.7577
        Assert.Equal(0.7577, result, precision: 4);
    }

    [Fact]
    public void HappinessSpeedModifier_ShowsDiminishingReturns()
    {
        // Arrange: Calculate change per happiness point in different ranges
        var change0to25 = (GetModifierForHappiness(25) - GetModifierForHappiness(0)) / 25.0;
        var change75to100 = (GetModifierForHappiness(100) - GetModifierForHappiness(75)) / 25.0;

        // Assert: Later range shows smaller change per point (diminishing returns)
        Assert.True(change75to100 < change0to25,
            $"Expected diminishing returns: {change75to100:F6} < {change0to25:F6}");
    }

    [Fact]
    public void HappinessSpeedModifier_IsAsymmetric_PenaltyExceedsBonus()
    {
        // Arrange
        var penaltyMagnitude = Math.Abs(1.0 - GetModifierForHappiness(0));   // ~11.39%
        var bonusMagnitude = Math.Abs(GetModifierForHappiness(100) - 1.0);   // ~8.54%

        // Assert: Unhappiness hurts more than happiness helps
        Assert.True(penaltyMagnitude > bonusMagnitude,
            $"Expected penalty ({penaltyMagnitude:P2}) > bonus ({bonusMagnitude:P2})");

        // Specific expectations
        Assert.Equal(0.1138, penaltyMagnitude, precision: 3);
        Assert.Equal(0.0854, bonusMagnitude, precision: 3);
    }

    private double GetModifierForHappiness(int happiness)
    {
        var horse = CreateHorseWithStats(speed: 50, agility: 50, happiness: (byte)happiness);
        var context = CreateModifierContext(horse);
        return _sut.CalculateStatModifiers(context);
    }

    // ============================================================================
    // Phase 3: Environmental Modifier Tests
    // ============================================================================

    [Fact]
    public void CalculateEnvironmentalModifiers_WithDirtAndGood_ShouldReturn1Point0()
    {
        // Arrange - Dirt (1.00) * Good (1.00) = 1.0
        var horse = CreateHorse();
        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculateEnvironmentalModifiers(context);

        // Assert
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void CalculateEnvironmentalModifiers_WithTurfAndFast_ShouldReturn1Point0506()
    {
        // Arrange - Turf (1.02) * Fast (1.03) = 1.0506
        var horse = CreateHorse();
        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Fast,
            RaceSurface: SurfaceId.Turf,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculateEnvironmentalModifiers(context);

        // Assert
        Assert.Equal(1.0506, result, precision: 4);
    }

    [Fact]
    public void CalculateEnvironmentalModifiers_WithArtificialAndHeavy_ShouldReturn0Point9393()
    {
        // Arrange - Artificial (1.01) * Heavy (0.93) = 0.9393
        var horse = CreateHorse();
        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Heavy,
            RaceSurface: SurfaceId.Artificial,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculateEnvironmentalModifiers(context);

        // Assert
        Assert.Equal(0.9393, result, precision: 4);
    }

    [Fact]
    public void CalculateEnvironmentalModifiers_WithDirtAndSlow_ShouldReturn0Point90()
    {
        // Arrange - Dirt (1.00) * Slow (0.90) = 0.90 (worst case)
        var horse = CreateHorse();
        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Slow,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculateEnvironmentalModifiers(context);

        // Assert
        Assert.Equal(0.90, result, precision: 5);
    }

    // ============================================================================
    // Phase 4: Phase Modifier Tests
    // ============================================================================

    [Fact]
    public void CalculatePhaseModifiers_StartDashInPhase_ShouldReturn1Point04()
    {
        // Arrange - Tick 50/200 = 25% (within StartDash 0-25% phase)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.StartDash;
        var context = new ModifierContext(
            CurrentTick: 50,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert
        Assert.Equal(1.04, result, precision: 5);
    }

    [Fact]
    public void CalculatePhaseModifiers_StartDashOutOfPhase_ShouldReturn1Point0()
    {
        // Arrange - Tick 150/200 = 75% (outside StartDash 0-25% phase)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.StartDash;
        var context = new ModifierContext(
            CurrentTick: 150,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void CalculatePhaseModifiers_LastSpurtInPhase_ShouldReturn1Point04()
    {
        // Arrange - Tick 170/200 = 85% (within LastSpurt 75-100% phase)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.LastSpurt;
        var context = new ModifierContext(
            CurrentTick: 170,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert
        Assert.Equal(1.04, result, precision: 5);
    }

    [Fact]
    public void CalculatePhaseModifiers_StretchRunnerInPhase_ShouldReturn1Point03()
    {
        // Arrange - Tick 140/200 = 70% (within StretchRunner 60-80% phase)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.StretchRunner;
        var context = new ModifierContext(
            CurrentTick: 140,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void CalculatePhaseModifiers_FrontRunnerInPhase_ShouldReturn1Point03()
    {
        // Arrange - Tick 30/200 = 15% (within FrontRunner 0-20% phase)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.FrontRunner;
        var context = new ModifierContext(
            CurrentTick: 30,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void CalculatePhaseModifiers_RailRunner_InLane1_WithClearPath_ShouldReturn1Point03()
    {
        // Arrange - RailRunner in lane 1, no traffic ahead (Feature 005)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.RailRunner;
        var context = new ModifierContext(
            CurrentTick: 170,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse); // Single horse, lane 1, no traffic

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Gets conditional bonus regardless of race phase
        Assert.Equal(1.03, result, precision: 5);
    }

    // ============================================================================
    // Feature 005: Rail Runner Lane Position Tests
    // ============================================================================

    [Fact]
    public void RailRunner_InLane2_WithClearPath_ShouldReturn1Point0()
    {
        // Arrange - RailRunner not on rail (lane 2)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.RailRunner;
        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);
        raceRun.Horses.First().Lane = 2; // Move to lane 2

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - No bonus when not in lane 1
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_WithTrafficAhead_ShouldReturn1Point0()
    {
        // Arrange - RailRunner in lane 1 but with traffic ahead
        var horse1 = CreateHorse(); // Our rail runner
        horse1.LegTypeId = LegTypeId.RailRunner;

        var horse2 = CreateHorse(); // Horse ahead in same lane

        var raceRun = CreateRaceRun(horse1, horse2);
        raceRun.Horses.First().Distance = 5.0m;  // RailRunner at 5f
        raceRun.Horses.Skip(1).First().Lane = 1; // Horse 2 in lane 1
        raceRun.Horses.Skip(1).First().Distance = 5.3m; // Only 0.3f ahead (< 0.5f threshold)

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse1,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - No bonus when traffic is blocking
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_WithTrafficBeyondThreshold_ShouldReturn1Point03()
    {
        // Arrange - RailRunner in lane 1 with traffic far enough ahead
        var horse1 = CreateHorse(); // Our rail runner
        horse1.LegTypeId = LegTypeId.RailRunner;

        var horse2 = CreateHorse(); // Horse ahead in same lane

        var raceRun = CreateRaceRun(horse1, horse2);
        raceRun.Horses.First().Distance = 5.0m;  // RailRunner at 5f
        raceRun.Horses.Skip(1).First().Lane = 1; // Horse 2 in lane 1
        raceRun.Horses.Skip(1).First().Distance = 5.6m; // 0.6f ahead (> 0.5f threshold)

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse1,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Gets bonus when traffic is beyond threshold
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_WithTrafficInDifferentLane_ShouldReturn1Point03()
    {
        // Arrange - RailRunner in lane 1, traffic ahead but in different lane
        var horse1 = CreateHorse(); // Our rail runner
        horse1.LegTypeId = LegTypeId.RailRunner;

        var horse2 = CreateHorse(); // Horse ahead in lane 2

        var raceRun = CreateRaceRun(horse1, horse2);
        raceRun.Horses.First().Distance = 5.0m;  // RailRunner at 5f
        raceRun.Horses.Skip(1).First().Lane = 2; // Horse 2 in lane 2 (different lane)
        raceRun.Horses.Skip(1).First().Distance = 5.2m; // Only 0.2f ahead but different lane

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse1,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Gets bonus because traffic is in different lane
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_WithTrafficBehind_ShouldReturn1Point03()
    {
        // Arrange - RailRunner in lane 1 with traffic behind (should not affect bonus)
        var horse1 = CreateHorse(); // Our rail runner
        horse1.LegTypeId = LegTypeId.RailRunner;

        var horse2 = CreateHorse(); // Horse behind in same lane

        var raceRun = CreateRaceRun(horse1, horse2);
        raceRun.Horses.First().Distance = 5.0m;  // RailRunner at 5f
        raceRun.Horses.Skip(1).First().Lane = 1; // Horse 2 in lane 1
        raceRun.Horses.Skip(1).First().Distance = 4.8m; // Behind our horse

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse1,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Gets bonus because traffic is behind
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_AtRaceStart_ShouldReturn1Point03()
    {
        // Arrange - RailRunner at start of race (tick 1)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.RailRunner;
        var context = new ModifierContext(
            CurrentTick: 1,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Bonus applies at any race phase
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_AtRaceEnd_ShouldReturn1Point03()
    {
        // Arrange - RailRunner at end of race (tick 200)
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.RailRunner;
        var context = new ModifierContext(
            CurrentTick: 200,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Bonus applies at any race phase
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_ExactlyAtThreshold_ShouldReturn1Point03()
    {
        // Arrange - Traffic exactly at 0.5f threshold
        // Logic: (5.5 - 5.0) = 0.5, which is NOT < 0.5, so path IS clear
        var horse1 = CreateHorse();
        horse1.LegTypeId = LegTypeId.RailRunner;

        var horse2 = CreateHorse();

        var raceRun = CreateRaceRun(horse1, horse2);
        raceRun.Horses.First().Distance = 5.0m;
        raceRun.Horses.Skip(1).First().Lane = 1;
        raceRun.Horses.Skip(1).First().Distance = 5.5m; // Exactly 0.5f ahead

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse1,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Gets bonus at exact threshold (< is exclusive, so 0.5 is not blocking)
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane1_MultipleHorsesAhead_ShouldCheckClosest()
    {
        // Arrange - Multiple horses ahead, one within threshold
        var horse1 = CreateHorse();
        horse1.LegTypeId = LegTypeId.RailRunner;

        var horse2 = CreateHorse(); // Close traffic
        var horse3 = CreateHorse(); // Far traffic

        var raceRun = CreateRaceRun(horse1, horse2, horse3);
        raceRun.Horses.ElementAt(0).Distance = 5.0m;  // RailRunner
        raceRun.Horses.ElementAt(1).Lane = 1;
        raceRun.Horses.ElementAt(1).Distance = 5.3m;  // 0.3f ahead (blocks)
        raceRun.Horses.ElementAt(2).Lane = 1;
        raceRun.Horses.ElementAt(2).Distance = 6.0m;  // 1.0f ahead (doesn't matter)

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse1,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Blocked by closest horse
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void RailRunner_InLane8_ShouldReturn1Point0()
    {
        // Arrange - RailRunner in outermost lane
        var horse = CreateHorse();
        horse.LegTypeId = LegTypeId.RailRunner;
        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
        var raceRun = CreateRaceRun(horse);
        raceRun.Horses.First().Lane = 8;

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - No bonus when far from rail
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void RailRunner_LeadingRace_InLane1_ShouldReturn1Point03()
    {
        // Arrange - RailRunner leading the race (integration scenario)
        var horse1 = CreateHorse(); // Leading rail runner
        horse1.LegTypeId = LegTypeId.RailRunner;

        var horse2 = CreateHorse(); // Trailing horse
        var horse3 = CreateHorse(); // Another trailing horse

        var raceRun = CreateRaceRun(horse1, horse2, horse3);
        raceRun.Horses.ElementAt(0).Lane = 1;
        raceRun.Horses.ElementAt(0).Distance = 7.0m;  // Leading
        raceRun.Horses.ElementAt(1).Lane = 2;
        raceRun.Horses.ElementAt(1).Distance = 6.5m;  // Behind in different lane
        raceRun.Horses.ElementAt(2).Lane = 3;
        raceRun.Horses.ElementAt(2).Distance = 6.3m;  // Behind in different lane

        var context = new ModifierContext(
            CurrentTick: 150,
            TotalTicks: 200,
            Horse: horse1,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Gets bonus when leading on rail
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void RailRunner_TrailingInPack_InLane1_WithTraffic_ShouldReturn1Point0()
    {
        // Arrange - RailRunner trailing in pack with traffic (integration scenario)
        var horse1 = CreateHorse(); // Leading horse
        var horse2 = CreateHorse(); // Our rail runner (trailing)
        horse2.LegTypeId = LegTypeId.RailRunner;
        var horse3 = CreateHorse(); // Another horse

        var raceRun = CreateRaceRun(horse1, horse2, horse3);
        raceRun.Horses.ElementAt(0).Lane = 1;
        raceRun.Horses.ElementAt(0).Distance = 7.0m;  // Leading in lane 1
        raceRun.Horses.ElementAt(1).Lane = 1;
        raceRun.Horses.ElementAt(1).Distance = 6.6m;  // Our rail runner, blocked by leader
        raceRun.Horses.ElementAt(2).Lane = 2;
        raceRun.Horses.ElementAt(2).Distance = 6.8m;  // Horse in lane 2

        var context = new ModifierContext(
            CurrentTick: 150,
            TotalTicks: 200,
            Horse: horse2,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - No bonus when boxed in by traffic
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void RailRunner_MidPack_ClearPath_InLane1_ShouldReturn1Point03()
    {
        // Arrange - RailRunner mid-pack with clear path ahead (integration scenario)
        var horse1 = CreateHorse(); // Leading horse in lane 2
        var horse2 = CreateHorse(); // Our rail runner mid-pack
        horse2.LegTypeId = LegTypeId.RailRunner;
        var horse3 = CreateHorse(); // Trailing horse

        var raceRun = CreateRaceRun(horse1, horse2, horse3);
        raceRun.Horses.ElementAt(0).Lane = 2;
        raceRun.Horses.ElementAt(0).Distance = 7.5m;  // Leading but not in lane 1
        raceRun.Horses.ElementAt(1).Lane = 1;
        raceRun.Horses.ElementAt(1).Distance = 7.0m;  // Our rail runner with clear path
        raceRun.Horses.ElementAt(2).Lane = 3;
        raceRun.Horses.ElementAt(2).Distance = 6.5m;  // Trailing

        var context = new ModifierContext(
            CurrentTick: 150,
            TotalTicks: 200,
            Horse: horse2,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );

        // Act
        var result = _sut.CalculatePhaseModifiers(context, raceRun);

        // Assert - Gets bonus when clear path on rail despite not leading
        Assert.Equal(1.03, result, precision: 5);
    }

    // ============================================================================
    // Phase 5: Random Variance Tests
    // ============================================================================

    [Fact]
    public void ApplyRandomVariance_WithMockedRandom0Point5_ShouldReturnNeutral()
    {
        // Arrange - Mock returns 0.5 which should give neutral modifier
        _randomGeneratorMock.Setup(r => r.NextDouble()).Returns(0.5);

        // Act
        var result = _sut.ApplyRandomVariance();

        // Assert - 0.5 * 0.02 - 0.01 = 0.0, so 1.0 + 0.0 = 1.0
        Assert.Equal(1.0, result, precision: 5);
    }

    [Fact]
    public void ApplyRandomVariance_WithMockedRandom0Point0_ShouldReturn0Point99()
    {
        // Arrange - Mock returns 0.0 which should give -1% modifier
        _randomGeneratorMock.Setup(r => r.NextDouble()).Returns(0.0);

        // Act
        var result = _sut.ApplyRandomVariance();

        // Assert - 0.0 * 0.02 - 0.01 = -0.01, so 1.0 - 0.01 = 0.99
        Assert.Equal(0.99, result, precision: 5);
    }

    [Fact]
    public void ApplyRandomVariance_WithMockedRandom1Point0_ShouldReturn1Point01()
    {
        // Arrange - Mock returns 1.0 which should give +1% modifier
        _randomGeneratorMock.Setup(r => r.NextDouble()).Returns(1.0);

        // Act
        var result = _sut.ApplyRandomVariance();

        // Assert - 1.0 * 0.02 - 0.01 = 0.01, so 1.0 + 0.01 = 1.01
        Assert.Equal(1.01, result, precision: 5);
    }

    [Fact]
    public void ApplyRandomVariance_CalledMultipleTimes_ShouldProduceDifferentResults()
    {
        // Arrange - Use actual random generator instead of mock
        var realRandomGenerator = new RandomGenerator();
        var calculator = new SpeedModifierCalculator(realRandomGenerator);

        // Act - Call multiple times
        var results = new List<double>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(calculator.ApplyRandomVariance());
        }

        // Assert - Should have at least some variation (not all the same)
        var distinctResults = results.Distinct().Count();
        Assert.True(distinctResults > 10, $"Expected diverse results but only got {distinctResults} distinct values");
    }

    [Fact]
    public void ApplyRandomVariance_Over1000Calls_ShouldBeMeanCentered()
    {
        // Arrange - Use actual random generator
        var realRandomGenerator = new RandomGenerator();
        var calculator = new SpeedModifierCalculator(realRandomGenerator);

        // Act - Call 1000 times and calculate mean
        var results = new List<double>();
        for (int i = 0; i < 1000; i++)
        {
            results.Add(calculator.ApplyRandomVariance());
        }

        var mean = results.Average();

        // Assert - Mean should be close to 1.0 (neutral), within reasonable tolerance
        Assert.InRange(mean, 0.995, 1.005);
    }

    [Fact]
    public void ModifierContext_ShouldStoreAllRequiredFields()
    {
        // Arrange
        short currentTick = 50;
        short totalTicks = 200;
        var horse = CreateHorse();
        var raceCondition = ConditionId.Good;
        var raceSurface = SurfaceId.Dirt;
        var raceFurlongs = 10m;

        // Act
        var context = new ModifierContext(
            currentTick,
            totalTicks,
            horse,
            raceCondition,
            raceSurface,
            raceFurlongs
        );

        // Assert
        Assert.Equal(currentTick, context.CurrentTick);
        Assert.Equal(totalTicks, context.TotalTicks);
        Assert.Equal(horse, context.Horse);
        Assert.Equal(raceCondition, context.RaceCondition);
        Assert.Equal(raceSurface, context.RaceSurface);
        Assert.Equal(raceFurlongs, context.RaceFurlongs);
    }

    private static ModifierContext CreateModifierContext(Horse? horse = null)
    {
        return new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: horse ?? CreateHorse(),
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
    }

    private static Horse CreateHorse()
    {
        return CreateHorseWithStats(speed: 50, agility: 50);
    }

    private static RaceRun CreateRaceRun(params Horse[] horses)
    {
        var race = new Race
        {
            Id = 1,
            Furlongs = 10m,
            SurfaceId = SurfaceId.Dirt
        };

        var raceRun = new RaceRun
        {
            Id = Guid.NewGuid(),
            RaceId = race.Id,
            Race = race,
            ConditionId = ConditionId.Good,
            Horses = new List<RaceRunHorse>()
        };

        byte lane = 1;
        foreach (var horse in horses)
        {
            raceRun.Horses.Add(new RaceRunHorse
            {
                Id = Guid.NewGuid(),
                Horse = horse,
                HorseId = horse.Id,
                Lane = lane++,
                Distance = 0m,
                InitialStamina = horse.Stamina,
                CurrentStamina = horse.Stamina
            });
        }

        return raceRun;
    }

    private static Horse CreateHorseWithStats(byte speed, byte agility, byte happiness = 50)
    {
        return new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Test Horse",
            LegTypeId = LegTypeId.FrontRunner,
            Statistics = new List<HorseStatistic>
            {
                new()
                {
                    StatisticId = StatisticId.Speed,
                    Actual = speed
                },
                new()
                {
                    StatisticId = StatisticId.Agility,
                    Actual = agility
                },
                new()
                {
                    StatisticId = StatisticId.Stamina,
                    Actual = 50
                },
                new()
                {
                    StatisticId = StatisticId.Durability,
                    Actual = 50
                },
                new()
                {
                    StatisticId = StatisticId.Happiness,
                    Actual = happiness
                }
            }
        };
    }
}
