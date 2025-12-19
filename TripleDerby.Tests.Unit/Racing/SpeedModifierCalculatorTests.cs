using Moq;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
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

    [Fact]
    public void CalculateEnvironmentalModifiers_ShouldReturnNeutralValue()
    {
        // Arrange
        var context = CreateModifierContext();

        // Act
        var result = _sut.CalculateEnvironmentalModifiers(context);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculatePhaseModifiers_ShouldReturnNeutralValue()
    {
        // Arrange
        var context = CreateModifierContext();

        // Act
        var result = _sut.CalculatePhaseModifiers(context);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ApplyRandomVariance_ShouldReturnNeutralValue()
    {
        // Act
        var result = _sut.ApplyRandomVariance();

        // Assert
        Assert.Equal(1.0, result);
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
