using Moq;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.SharedKernel.Enums;

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

        // Act
        var result = _sut.CalculatePhaseModifiers(context);

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

        // Act
        var result = _sut.CalculatePhaseModifiers(context);

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

        // Act
        var result = _sut.CalculatePhaseModifiers(context);

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

        // Act
        var result = _sut.CalculatePhaseModifiers(context);

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

        // Act
        var result = _sut.CalculatePhaseModifiers(context);

        // Assert
        Assert.Equal(1.03, result, precision: 5);
    }

    [Fact]
    public void CalculatePhaseModifiers_RailRunnerInPhase_ShouldReturn1Point02()
    {
        // Arrange - Tick 170/200 = 85% (within RailRunner 70-100% phase)
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

        // Act
        var result = _sut.CalculatePhaseModifiers(context);

        // Assert
        Assert.Equal(1.02, result, precision: 5);
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
        var currentTick = 50;
        var totalTicks = 200;
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

    private static Horse CreateHorseWithStats(byte speed, byte agility)
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
                    Actual = 50
                }
            }
        };
    }
}
