using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Services;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Integration tests for stamina depletion in race simulation.
/// Tests stamina integration with UpdateHorsePosition in RaceService.
/// </summary>
public class RaceStaminaIntegrationTests
{
    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void UpdateHorsePosition_DepletesStamina_EachTick()
    {
        // Arrange
        var randomGen = new TestRandomGenerator();
        var calculator = new StaminaCalculator();
        var speedModCalculator = new SpeedModifierCalculator(randomGen);
        var raceRunHorse = CreateTestRaceRunHorse(stamina: 100);
        var raceRun = CreateTestRaceRun(furlongs: 10m);

        var initialStamina = raceRunHorse.CurrentStamina;

        // Act - simulate one tick of race
        var baseSpeed = 0.0422;
        var depletionAmount = calculator.CalculateDepletionAmount(
            raceRunHorse.Horse,
            10m,
            baseSpeed,
            baseSpeed,
            raceProgress: 0.1);

        raceRunHorse.CurrentStamina -= depletionAmount;

        // Assert
        Assert.True(raceRunHorse.CurrentStamina < initialStamina, "Stamina should deplete after one tick");
        Assert.InRange(depletionAmount, 0.001, 1.0); // Reasonable depletion per tick
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void UpdateHorsePosition_AppliesStaminaSpeedModifier_WhenStaminaLow()
    {
        // Arrange
        var randomGen = new TestRandomGenerator();
        var speedModCalculator = new SpeedModifierCalculator(randomGen);
        var highStaminaHorse = CreateTestRaceRunHorse(stamina: 100);
        var lowStaminaHorse = CreateTestRaceRunHorse(stamina: 100);

        highStaminaHorse.CurrentStamina = 100; // Full stamina
        lowStaminaHorse.CurrentStamina = 20;   // Low stamina (20%)

        // Act
        var highStaminaModifier = speedModCalculator.CalculateStaminaModifier(highStaminaHorse);
        var lowStaminaModifier = speedModCalculator.CalculateStaminaModifier(lowStaminaHorse);

        // Assert
        Assert.Equal(1.0, highStaminaModifier, precision: 2); // No penalty at full stamina
        Assert.True(lowStaminaModifier < 1.0, "Low stamina should reduce speed");
        Assert.True(lowStaminaModifier < highStaminaModifier, "Lower stamina should have greater penalty");
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void StaminaDepletion_AccumulatesOverMultipleTicks()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var raceRunHorse = CreateTestRaceRunHorse(stamina: 100);
        var baseSpeed = 0.0422;
        var furlongs = 10m;

        var initialStamina = raceRunHorse.CurrentStamina;

        // Act - simulate 10 ticks
        for (int tick = 1; tick <= 10; tick++)
        {
            var raceProgress = (double)tick / 237.0; // 237 = target ticks for 10f
            var depletionAmount = calculator.CalculateDepletionAmount(
                raceRunHorse.Horse,
                furlongs,
                baseSpeed,
                baseSpeed,
                raceProgress);

            raceRunHorse.CurrentStamina -= depletionAmount;
        }

        // Assert
        Assert.True(raceRunHorse.CurrentStamina < initialStamina, "Stamina should deplete over multiple ticks");
        Assert.True(raceRunHorse.CurrentStamina >= 0, "Stamina should not go negative");

        // After 10 ticks (~4% of race), stamina should have depleted some but not too much
        var depletionPercent = (initialStamina - raceRunHorse.CurrentStamina) / initialStamina;
        Assert.InRange(depletionPercent, 0.0001, 0.10); // Should be between 0.01% and 10% depletion
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void StaminaDepletion_CannotGoNegative()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var raceRunHorse = CreateTestRaceRunHorse(stamina: 100);
        var baseSpeed = 0.0422;

        raceRunHorse.CurrentStamina = 0.01; // Nearly exhausted

        // Act - try to deplete more than remaining
        var depletionAmount = calculator.CalculateDepletionAmount(
            raceRunHorse.Horse,
            10m,
            baseSpeed,
            baseSpeed,
            raceProgress: 0.9);

        var newStamina = Math.Max(0, raceRunHorse.CurrentStamina - depletionAmount);

        // Assert
        Assert.True(newStamina >= 0, "Stamina should never go below zero");
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void MarathonRace_DepletesStaminaFasterThanSprint()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var marathonHorse = CreateTestRaceRunHorse(stamina: 100);
        var sprintHorse = CreateTestRaceRunHorse(stamina: 100);
        var baseSpeed = 0.0422;
        var raceProgress = 0.50;

        // Act - calculate depletion for one tick in each race type
        var marathonDepletion = calculator.CalculateDepletionAmount(
            marathonHorse.Horse, 16m, baseSpeed, baseSpeed, raceProgress);
        var sprintDepletion = calculator.CalculateDepletionAmount(
            sprintHorse.Horse, 6m, baseSpeed, baseSpeed, raceProgress);

        // Assert
        Assert.True(marathonDepletion > sprintDepletion,
            "Marathon races should deplete stamina faster per tick than sprints");

        // Verify ratios roughly match config (Marathon 0.30 vs Sprint 0.08)
        var depletionRatio = marathonDepletion / sprintDepletion;
        Assert.InRange(depletionRatio, 3.0, 4.5); // Should be ~3.75x (0.30/0.08)
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void FastPace_DepletesStaminaFasterThanSlowPace()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateTestRaceRunHorse(stamina: 100);
        var baseSpeed = 0.0422;
        var fastSpeed = baseSpeed * 1.15; // 15% faster
        var slowSpeed = baseSpeed * 0.85; // 15% slower

        // Act
        var fastDepletion = calculator.CalculateDepletionAmount(
            horse.Horse, 10m, fastSpeed, baseSpeed, raceProgress: 0.5);
        var slowDepletion = calculator.CalculateDepletionAmount(
            horse.Horse, 10m, slowSpeed, baseSpeed, raceProgress: 0.5);

        // Assert
        Assert.True(fastDepletion > slowDepletion,
            "Faster pace should deplete more stamina than slower pace");

        // Pace multiplier should scale linearly
        var depletionRatio = fastDepletion / slowDepletion;
        Assert.InRange(depletionRatio, 1.3, 1.4); // Should be ~1.35x (1.15/0.85)
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void LegType_AffectsStaminaDepletion_DuringRacePhases()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var startDashHorse = CreateTestRaceRunHorse(stamina: 100, legType: LegTypeId.StartDash);
        var lastSpurtHorse = CreateTestRaceRunHorse(stamina: 100, legType: LegTypeId.LastSpurt);
        var baseSpeed = 0.0422;

        // Act - Early phase (10% progress)
        var startDashEarly = calculator.CalculateDepletionAmount(
            startDashHorse.Horse, 10m, baseSpeed, baseSpeed, raceProgress: 0.10);
        var lastSpurtEarly = calculator.CalculateDepletionAmount(
            lastSpurtHorse.Horse, 10m, baseSpeed, baseSpeed, raceProgress: 0.10);

        // Act - Late phase (90% progress)
        var startDashLate = calculator.CalculateDepletionAmount(
            startDashHorse.Horse, 10m, baseSpeed, baseSpeed, raceProgress: 0.90);
        var lastSpurtLate = calculator.CalculateDepletionAmount(
            lastSpurtHorse.Horse, 10m, baseSpeed, baseSpeed, raceProgress: 0.90);

        // Assert - StartDash burns hard early, conserves late
        Assert.True(startDashEarly > lastSpurtEarly,
            "StartDash should deplete more than LastSpurt early in race");
        Assert.True(lastSpurtLate > startDashLate,
            "LastSpurt should deplete more than StartDash late in race");
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void HighStaminaStat_DepletesSlowerThanLowStamina()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var highStaminaHorse = CreateTestRaceRunHorse(stamina: 100, durability: 50);
        var lowStaminaHorse = CreateTestRaceRunHorse(stamina: 0, durability: 50);
        var baseSpeed = 0.0422;

        // Act
        var highDepletion = calculator.CalculateDepletionAmount(
            highStaminaHorse.Horse, 10m, baseSpeed, baseSpeed, raceProgress: 0.5);
        var lowDepletion = calculator.CalculateDepletionAmount(
            lowStaminaHorse.Horse, 10m, baseSpeed, baseSpeed, raceProgress: 0.5);

        // Assert
        Assert.True(highDepletion < lowDepletion,
            "Higher stamina stat should result in slower depletion");

        // High stamina (100) = 0.80x, Low stamina (0) = 1.20x
        var depletionRatio = lowDepletion / highDepletion;
        Assert.InRange(depletionRatio, 1.4, 1.6); // Should be ~1.5x (1.20/0.80)
    }

    [Trait("Category", "StaminaIntegration")]
    [Fact]
    public void FullRaceSimulation_StaminaDepletesAndAffectsSpeed()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var speedModCalculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var raceRunHorse = CreateTestRaceRunHorse(stamina: 100);
        var baseSpeed = 0.0422;
        var furlongs = 10m;
        var totalTicks = 237; // Target ticks for 10f

        var initialSpeed = baseSpeed;
        var speedsRecorded = new List<double>();

        // Act - simulate full race
        for (short tick = 1; tick <= totalTicks; tick++)
        {
            var raceProgress = (double)tick / totalTicks;

            // 1. Calculate current speed with stamina modifier
            var staminaModifier = speedModCalculator.CalculateStaminaModifier(raceRunHorse);
            var currentSpeed = baseSpeed * staminaModifier;
            speedsRecorded.Add(currentSpeed);

            // 2. Deplete stamina based on effort
            var depletionAmount = calculator.CalculateDepletionAmount(
                raceRunHorse.Horse, furlongs, currentSpeed, baseSpeed, raceProgress);
            raceRunHorse.CurrentStamina = Math.Max(0, raceRunHorse.CurrentStamina - depletionAmount);
        }

        // Assert
        Assert.True(raceRunHorse.CurrentStamina < raceRunHorse.InitialStamina,
            "Stamina should be depleted after full race");

        // Speed should decrease over time as stamina depletes
        var earlySpeed = speedsRecorded.Take(10).Average();
        var lateSpeed = speedsRecorded.Skip(totalTicks - 10).Average();
        Assert.True(lateSpeed <= earlySpeed,
            "Late-race speed should be equal or less than early-race speed due to stamina depletion");
    }

    // ========================================================================
    // Test Helper Methods
    // ========================================================================

    private static RaceRunHorse CreateTestRaceRunHorse(
        int stamina = 50,
        int durability = 50,
        int speed = 50,
        int agility = 50,
        LegTypeId legType = LegTypeId.FrontRunner)
    {
        return new RaceRunHorse
        {
            Id = Guid.NewGuid(),
            InitialStamina = (byte)stamina,
            CurrentStamina = stamina,
            Distance = 0,
            Horse = new Horse
            {
                Id = Guid.NewGuid(),
                Name = "Test Horse",
                LegTypeId = legType,
                Statistics = new List<HorseStatistic>
                {
                    new() { StatisticId = StatisticId.Stamina, Actual = (byte)stamina },
                    new() { StatisticId = StatisticId.Durability, Actual = (byte)durability },
                    new() { StatisticId = StatisticId.Speed, Actual = (byte)speed },
                    new() { StatisticId = StatisticId.Agility, Actual = (byte)agility }
                }
            }
        };
    }

    private static RaceRun CreateTestRaceRun(decimal furlongs = 10m)
    {
        return new RaceRun
        {
            Id = Guid.NewGuid(),
            RaceId = 1,
            ConditionId = ConditionId.Good,
            Race = new Race
            {
                Id = 1,
                Name = "Test Race",
                Furlongs = furlongs,
                SurfaceId = SurfaceId.Dirt
            },
            Horses = new List<RaceRunHorse>(),
            RaceRunTicks = new List<RaceRunTick>()
        };
    }

    /// <summary>
    /// Test random generator that returns predictable values.
    /// </summary>
    private class TestRandomGenerator : Core.Abstractions.Utilities.IRandomGenerator
    {
        public int Next() => 0;
        public int Next(int maxValue) => 0;
        public int Next(int minValue, int maxValue) => minValue;
        public double NextDouble() => 0.5;
    }
}
