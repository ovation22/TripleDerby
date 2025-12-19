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

    [Fact]
    public void CalculateStatModifiers_ShouldReturnNeutralValue()
    {
        // Arrange
        var context = CreateModifierContext();

        // Act
        var result = _sut.CalculateStatModifiers(context);

        // Assert
        Assert.Equal(1.0, result);
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

    private static ModifierContext CreateModifierContext()
    {
        return new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 200,
            Horse: CreateHorse(),
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10m
        );
    }

    private static Horse CreateHorse()
    {
        return new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Test Horse",
            LegTypeId = LegTypeId.FrontRunner,
            Statistics = new List<HorseStatistic>
            {
                new() { Speed = 50, Stamina = 50, Agility = 50, Durability = 50, Happiness = 50 }
            }
        };
    }
}
