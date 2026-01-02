using Moq;
using TripleDerby.Services.Racing.Abstractions;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Services.Racing.Racing;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Racing.Racing;
using static TripleDerby.Services.Racing.Racing.RaceModifierConfig;
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
        byte speed = 50,
        byte agility = 50,
        byte stamina = 50,
        byte happiness = 50,
        LegTypeId legType = LegTypeId.StartDash)
    {
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Test Horse",
            ColorId = 1,
            LegTypeId = legType,
            OwnerId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            CreatedDate = DateTimeOffset.UtcNow,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = speed },
                new() { StatisticId = StatisticId.Agility, Actual = agility },
                new() { StatisticId = StatisticId.Stamina, Actual = stamina },
                new() { StatisticId = StatisticId.Durability, Actual = 50 },
                new() { StatisticId = StatisticId.Happiness, Actual = happiness }
            }
        };
        return horse;
    }

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
            Id = 1,
            Name = "Test Race",
            Description = "Test Description",
            SurfaceId = SurfaceId.Dirt,
            Furlongs = 10m,
            TrackId = TrackId.TripleSpires,
            RaceClassId = RaceClassId.Maiden
        },
        Horses = horses.ToList()
    };

    /// <summary>
    /// Test 1: Average speed horse with all neutral modifiers returns base speed
    /// </summary>
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

    /// <summary>
    /// Test 2: Fast horse should calculate higher speed than slow horse
    /// </summary>
    [Fact]
    public void CalculateHorseSpeed_FastHorse_HigherThanSlowHorse()
    {
        // Arrange - create fresh mock with speed calculation
        var statModifierCallCount = 0;
        var lastHorseSpeed = 0;
        var lastReturnValue = 0.0;
        var mockSpeedCalc = new Mock<ISpeedModifierCalculator>();
        mockSpeedCalc
            .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
            .Returns((ModifierContext ctx) =>
            {
                statModifierCallCount++;
                lastHorseSpeed = ctx.Horse.Speed;
                lastReturnValue = 1.0 + ((ctx.Horse.Speed - 50) * RaceModifierConfig.SpeedModifierPerPoint);
                return lastReturnValue;
            });
        mockSpeedCalc
            .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculateStaminaModifier(It.IsAny<RaceRunHorse>()))
            .Returns(1.0);

        var manager = new OvertakingManager(_mockRandom.Object, mockSpeedCalc.Object);
        var fastHorseEntity = CreateHorse(speed: 80);
        var slowHorseEntity = CreateHorse(speed: 40);

        // Verify horses have correct Speed values
        Assert.Equal(80, fastHorseEntity.Speed);
        Assert.Equal(40, slowHorseEntity.Speed);

        var fastHorse = CreateRaceRunHorse(fastHorseEntity);
        var slowHorse = CreateRaceRunHorse(slowHorseEntity);
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
        Assert.True(statModifierCallCount > 0, $"Mock was called {statModifierCallCount} times - should be at least 2");
        var message = $"Fast: {fastSpeed}, Slow: {slowSpeed}, Calls: {statModifierCallCount}, LastHorseSpeed: {lastHorseSpeed}, LastReturn: {lastReturnValue}";
        Assert.True(fastSpeed > slowSpeed, message);

        var speedRatio = fastSpeed / slowSpeed;
        Assert.InRange(speedRatio, 1.06, 1.10); // 40-point difference = ~8% speed difference
    }

    /// <summary>
    /// Test 3: Exhausted horse should calculate slower speed than fresh horse
    /// </summary>
    [Fact]
    public void CalculateHorseSpeed_ExhaustedHorse_SlowerThanFreshHorse()
    {
        // Arrange - setup stamina modifier
        var mockSpeedCalc = new Mock<ISpeedModifierCalculator>();
        mockSpeedCalc
            .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
            .Returns(1.0);
        mockSpeedCalc
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

        var manager = new OvertakingManager(_mockRandom.Object, mockSpeedCalc.Object);

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

    /// <summary>
    /// Test 4: Environmental modifiers should affect speed
    /// </summary>
    [Fact]
    public void CalculateHorseSpeed_DirtSpecialist_FasterOnDirt()
    {
        // Arrange
        var mockSpeedCalc = new Mock<ISpeedModifierCalculator>();
        mockSpeedCalc
            .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
            .Returns((ModifierContext ctx) =>
                ctx.RaceSurface == SurfaceId.Dirt ? 1.05 : 1.0);
        mockSpeedCalc
            .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculateStaminaModifier(It.IsAny<RaceRunHorse>()))
            .Returns(1.0);

        var manager = new OvertakingManager(_mockRandom.Object, mockSpeedCalc.Object);
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

    /// <summary>
    /// Test 5: LastSpurt should be faster late in race
    /// </summary>
    [Fact]
    public void CalculateHorseSpeed_LastSpurt_FasterLateRace()
    {
        // Arrange
        var mockSpeedCalc = new Mock<ISpeedModifierCalculator>();
        mockSpeedCalc
            .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.0);
        mockSpeedCalc
            .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
            .Returns((ModifierContext ctx, RaceRun rr) =>
            {
                var progress = (double)ctx.CurrentTick / ctx.TotalTicks;
                return ctx.Horse.LegTypeId == LegTypeId.LastSpurt && progress > 0.75
                    ? 1.10
                    : 1.0;
            });
        mockSpeedCalc
            .Setup(x => x.CalculateStaminaModifier(It.IsAny<RaceRunHorse>()))
            .Returns(1.0);

        var manager = new OvertakingManager(_mockRandom.Object, mockSpeedCalc.Object);
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

    /// <summary>
    /// Test 6: Multiple modifiers should stack multiplicatively
    /// </summary>
    [Fact]
    public void CalculateHorseSpeed_MultipleModifiers_StackMultiplicatively()
    {
        // Arrange - all modifiers active
        var mockSpeedCalc = new Mock<ISpeedModifierCalculator>();
        mockSpeedCalc
            .Setup(x => x.CalculateStatModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.08); // Fast horse

        mockSpeedCalc
            .Setup(x => x.CalculateEnvironmentalModifiers(It.IsAny<ModifierContext>()))
            .Returns(1.05); // Surface bonus

        mockSpeedCalc
            .Setup(x => x.CalculatePhaseModifiers(It.IsAny<ModifierContext>(), It.IsAny<RaceRun>()))
            .Returns(1.10); // Phase bonus

        mockSpeedCalc
            .Setup(x => x.CalculateStaminaModifier(It.IsAny<RaceRunHorse>()))
            .Returns(0.95); // Slight fatigue

        var manager = new OvertakingManager(_mockRandom.Object, mockSpeedCalc.Object);
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
}
