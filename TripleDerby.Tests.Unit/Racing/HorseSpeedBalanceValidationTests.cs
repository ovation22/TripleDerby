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
/// Balance validation tests for realistic horse speed calculations.
/// Validates that speed calculations produce balanced, realistic distributions.
/// </summary>
public class HorseSpeedBalanceValidationTests
{
    private readonly Mock<IRandomGenerator> _mockRandom;
    private readonly ISpeedModifierCalculator _speedCalculator;

    public HorseSpeedBalanceValidationTests()
    {
        _mockRandom = new Mock<IRandomGenerator>();
        _speedCalculator = new SpeedModifierCalculator(_mockRandom.Object);
    }

    // Helper methods
    private OvertakingManager CreateManager() =>
        new OvertakingManager(_mockRandom.Object, _speedCalculator);

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
    /// Calculates the natural speed of a horse (before traffic effects).
    /// </summary>
    private double CalculateNaturalSpeed(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
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

        baseSpeed *= _speedCalculator.CalculateStatModifiers(context);
        baseSpeed *= _speedCalculator.CalculateEnvironmentalModifiers(context);
        baseSpeed *= _speedCalculator.CalculatePhaseModifiers(context, raceRun);
        baseSpeed *= _speedCalculator.CalculateStaminaModifier(horse);

        return baseSpeed;
    }

    /// <summary>
    /// Test 1: Speed distribution across stat ranges should be reasonable
    /// </summary>
    [Fact]
    public void SpeedDistribution_AcrossStatRanges_IsReasonable()
    {
        // Arrange - create horses with speed stats from 0 to 100
        var manager = CreateManager();
        var speeds = new List<double>();

        for (byte speed = 0; speed <= 100; speed += 25) // Test 0, 25, 50, 75, 100
        {
            var horse = CreateRaceRunHorse(CreateHorse(speed: speed));
            var raceRun = CreateRaceRun(horse);

            var calculatedSpeed = CalculateNaturalSpeed(horse, raceRun, 50, 100);
            speeds.Add(calculatedSpeed);
        }

        // Assert - speeds should increase monotonically
        for (int i = 1; i < speeds.Count; i++)
        {
            Assert.True(speeds[i] > speeds[i - 1],
                $"Speed should increase with stat. Speed[{i * 25}]={speeds[i]}, Speed[{(i - 1) * 25}]={speeds[i - 1]}");
        }

        // Range should be reasonable (slowest to fastest within ~20% spread)
        var slowest = speeds[0];
        var fastest = speeds[speeds.Count - 1];
        var range = fastest / slowest;

        Assert.InRange(range, 1.15, 1.25); // 15-25% speed difference from 0 to 100 stat
    }

    /// <summary>
    /// Test 2: Traffic effects should differentiate horses appropriately
    /// </summary>
    [Fact]
    public void TrafficEffects_DifferentiateHorses_Appropriately()
    {
        // Arrange - create diverse field of 4 horses
        var manager = CreateManager();
        var horses = new List<RaceRunHorse>
        {
            CreateRaceRunHorse(CreateHorse(speed: 30, stamina: 80, legType: LegTypeId.StartDash), lane: 1, distance: 1.0m),
            CreateRaceRunHorse(CreateHorse(speed: 90, stamina: 40, legType: LegTypeId.StartDash), lane: 2, distance: 1.0m),
            CreateRaceRunHorse(CreateHorse(speed: 50, stamina: 90, legType: LegTypeId.LastSpurt), lane: 3, distance: 1.0m),
            CreateRaceRunHorse(CreateHorse(speed: 70, stamina: 50, legType: LegTypeId.StretchRunner), lane: 4, distance: 1.0m)
        };

        var raceRun = CreateRaceRun(horses.ToArray());
        short currentTick = 50;
        short totalTicks = 100;

        // Act - calculate speeds
        var speeds = new List<(int index, double speed)>();
        foreach (var horse in horses)
        {
            var speed = CalculateNaturalSpeed(horse, raceRun, currentTick, totalTicks);
            speeds.Add((horses.IndexOf(horse), speed));
        }

        // Assert - there should be meaningful speed differences
        var uniqueSpeeds = speeds.Select(s => Math.Round(s.speed, 6)).Distinct().Count();
        Assert.True(uniqueSpeeds >= 3, $"Should have at least 3 distinct speeds, got {uniqueSpeeds}");

        // Fastest and slowest should have clear separation
        var fastest = speeds.MaxBy(s => s.speed);
        var slowest = speeds.MinBy(s => s.speed);
        var speedRatio = fastest.speed / slowest.speed;

        Assert.True(speedRatio > 1.05,
            $"Fastest/slowest ratio should show clear differentiation. Ratio: {speedRatio:F3}");
    }

    /// <summary>
    /// Test 3: Stamina depletion creates realistic speed degradation over race
    /// </summary>
    [Fact]
    public void StaminaDepletion_CreatesRealisticDegradation()
    {
        // Arrange - horse with average stats, simulate stamina depletion
        var manager = CreateManager();
        var horse = CreateRaceRunHorse(CreateHorse(stamina: 100));
        horse.InitialStamina = 100;
        var raceRun = CreateRaceRun(horse);

        var speedsOverTime = new List<(double staminaPercent, double speed)>();

        // Act - simulate progressive stamina depletion (test key points)
        foreach (var stamina in new byte[] { 100, 75, 50, 25, 0 })
        {
            horse.CurrentStamina = stamina;
            var speed = CalculateNaturalSpeed(horse, raceRun, 50, 100);
            var staminaPercent = stamina / 100.0;
            speedsOverTime.Add((staminaPercent, speed));
        }

        // Assert - speed should decrease as stamina depletes
        for (int i = 1; i < speedsOverTime.Count; i++)
        {
            Assert.True(speedsOverTime[i].speed <= speedsOverTime[i - 1].speed,
                $"Speed should not increase as stamina depletes. " +
                $"At {speedsOverTime[i].staminaPercent * 100}%: {speedsOverTime[i].speed}, " +
                $"At {speedsOverTime[i - 1].staminaPercent * 100}%: {speedsOverTime[i - 1].speed}");
        }

        // Maximum penalty should not be excessive (90-95% of full speed at 0% stamina)
        var fullSpeed = speedsOverTime[0].speed;
        var exhaustedSpeed = speedsOverTime[speedsOverTime.Count - 1].speed;
        var retentionPercent = exhaustedSpeed / fullSpeed;

        Assert.InRange(retentionPercent, 0.90, 0.95);
    }

    /// <summary>
    /// Test 4: Phase bonuses create tactical advantages at correct race phases
    /// </summary>
    [Fact]
    public void PhaseBonus_CreatesTacticalAdvantages()
    {
        // Arrange - test StartDash and LastSpurt (opposite phases)
        var manager = CreateManager();
        var startDash = CreateRaceRunHorse(CreateHorse(speed: 50, legType: LegTypeId.StartDash));
        var lastSpurt = CreateRaceRunHorse(CreateHorse(speed: 50, legType: LegTypeId.LastSpurt));
        var raceRun = CreateRaceRun(startDash, lastSpurt);

        // Act - measure speeds early and late in race
        var startDashEarly = CalculateNaturalSpeed(startDash, raceRun, 10, 100); // 10%
        var startDashLate = CalculateNaturalSpeed(startDash, raceRun, 90, 100);  // 90%

        var lastSpurtEarly = CalculateNaturalSpeed(lastSpurt, raceRun, 10, 100); // 10%
        var lastSpurtLate = CalculateNaturalSpeed(lastSpurt, raceRun, 90, 100);  // 90%

        // Assert - StartDash should be fastest early, LastSpurt fastest late
        Assert.True(startDashEarly > lastSpurtEarly,
            $"StartDash should be faster early. StartDash: {startDashEarly}, LastSpurt: {lastSpurtEarly}");

        Assert.True(lastSpurtLate > startDashLate,
            $"LastSpurt should be faster late. LastSpurt: {lastSpurtLate}, StartDash: {startDashLate}");

        // Phase bonus should create noticeable difference
        var startDashAdvantageEarly = (startDashEarly - lastSpurtEarly) / lastSpurtEarly;
        var lastSpurtAdvantageLate = (lastSpurtLate - startDashLate) / startDashLate;

        Assert.True(startDashAdvantageEarly > 0.03, // >3% faster
            $"StartDash early advantage: {startDashAdvantageEarly:P1}");
        Assert.True(lastSpurtAdvantageLate > 0.03, // >3% faster
            $"LastSpurt late advantage: {lastSpurtAdvantageLate:P1}");
    }

    /// <summary>
    /// Test 5: Performance test - speed calculation should be fast
    /// </summary>
    [Fact(Skip = "Performance test - run manually when needed")]
    public void SpeedCalculation_PerformanceIsAcceptable()
    {
        // Arrange - create typical race scenario
        var manager = CreateManager();
        var horses = new List<RaceRunHorse>();
        for (int i = 0; i < 12; i++) // 12-horse field
        {
            var horse = CreateRaceRunHorse(
                CreateHorse(
                    speed: (byte)(40 + i * 5),
                    stamina: (byte)(50 + i * 3),
                    legType: (LegTypeId)(i % 5 + 1)
                ),
                lane: (byte)(i % 8 + 1)
            );
            horses.Add(horse);
        }

        var raceRun = CreateRaceRun(horses.ToArray());
        const int iterations = 100; // Simulate 100 ticks worth of calculations

        // Act - measure performance
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int tick = 0; tick < iterations; tick++)
        {
            foreach (var horse in horses)
            {
                var speed = CalculateNaturalSpeed(horse, raceRun, (short)tick, 200);

                // Simulate applying traffic effects
                var speedWithTraffic = speed;
                manager.ApplyTrafficEffects(horse, raceRun, (short)tick, 200, ref speedWithTraffic);
            }
        }
        sw.Stop();

        // Assert - should complete in reasonable time
        // 12 horses * 100 ticks = 1,200 calculations
        // Should complete in < 200ms (conservative threshold)
        Assert.True(sw.ElapsedMilliseconds < 200,
            $"Performance test took {sw.ElapsedMilliseconds}ms, expected < 200ms");

        // Calculate operations per second for reporting
        var opsPerSecond = (horses.Count * iterations) / (sw.ElapsedMilliseconds / 1000.0);
        Assert.True(opsPerSecond > 5000, // Should handle at least 5k ops/sec
            $"Operations per second: {opsPerSecond:N0}, expected > 5,000");
    }
}
