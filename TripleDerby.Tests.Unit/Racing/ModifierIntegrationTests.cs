using Moq;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.SharedKernel.Enums;
using Xunit.Abstractions;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Integration tests to verify modifiers are correctly calculated and applied.
/// </summary>
public class ModifierIntegrationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(0, 0.90)]   // Speed=0 should give 0.90x multiplier
    [InlineData(50, 1.00)]  // Speed=50 should give 1.00x multiplier (neutral)
    [InlineData(100, 1.10)] // Speed=100 should give 1.10x multiplier
    public void SpeedModifierCalculator_CalculateStatModifiers_VariesBySpeed(int speed, double expectedMultiplier)
    {
        // Arrange
        var mockRandom = new Mock<IRandomGenerator>();
        var calculator = new SpeedModifierCalculator(mockRandom.Object);

        var horse = new Horse
        {
            LegTypeId = LegTypeId.FrontRunner,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = (byte)speed },
                new() { StatisticId = StatisticId.Agility, Actual = 50 },
                new() { StatisticId = StatisticId.Stamina, Actual = 50 }
            }
        };

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 237,
            Horse: horse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10
        );

        // Act
        var result = calculator.CalculateStatModifiers(context);

        // Debug output
        output.WriteLine($"Speed={speed}, Expected={expectedMultiplier:F2}, Actual={result:F3}");

        // Assert
        Assert.Equal(expectedMultiplier, result, precision: 2);
    }

    [Fact]
    public void Verify_Modifier_Pipeline_Changes_Base_Speed()
    {
        // This test simulates what happens in UpdateHorsePosition
        var mockRandom = new Mock<IRandomGenerator>();
        mockRandom.Setup(r => r.NextDouble()).Returns(0.5); // Neutral variance

        var calculator = new SpeedModifierCalculator(mockRandom.Object);

        // Horse with high speed
        var fastHorse = new Horse
        {
            LegTypeId = LegTypeId.FrontRunner,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = 100 },
                new() { StatisticId = StatisticId.Agility, Actual = 50 },
                new() { StatisticId = StatisticId.Stamina, Actual = 50 }
            }
        };

        // Horse with low speed
        var slowHorse = new Horse
        {
            LegTypeId = LegTypeId.FrontRunner,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = 0 },
                new() { StatisticId = StatisticId.Agility, Actual = 50 },
                new() { StatisticId = StatisticId.Stamina, Actual = 50 }
            }
        };

        var context = new ModifierContext(
            CurrentTick: 100,
            TotalTicks: 237,
            Horse: fastHorse,
            RaceCondition: ConditionId.Good,
            RaceSurface: SurfaceId.Dirt,
            RaceFurlongs: 10
        );

        // Simulate RaceService logic
        const double AverageBaseSpeed = 10.0 / 237.0;
        var baseSpeed = AverageBaseSpeed;

        // Fast horse
        var fastStatModifier = calculator.CalculateStatModifiers(context);
        var fastEnvModifier = calculator.CalculateEnvironmentalModifiers(context);
        var fastPhaseModifier = calculator.CalculatePhaseModifiers(context);
        var fastRandomVariance = calculator.ApplyRandomVariance();

        var fastFinalSpeed = baseSpeed * fastStatModifier * fastEnvModifier * fastPhaseModifier * fastRandomVariance;

        // Slow horse
        var slowContext = context with { Horse = slowHorse };
        var slowStatModifier = calculator.CalculateStatModifiers(slowContext);
        var slowEnvModifier = calculator.CalculateEnvironmentalModifiers(slowContext);
        var slowPhaseModifier = calculator.CalculatePhaseModifiers(slowContext);
        var slowRandomVariance = calculator.ApplyRandomVariance();

        var slowFinalSpeed = baseSpeed * slowStatModifier * slowEnvModifier * slowPhaseModifier * slowRandomVariance;

        output.WriteLine("=== MODIFIER PIPELINE TEST ===");
        output.WriteLine($"Base Speed: {baseSpeed:F6} furlongs/tick");
        output.WriteLine("");
        output.WriteLine("FAST HORSE (Speed=100):");
        output.WriteLine($"  Stat Modifier: {fastStatModifier:F3}");
        output.WriteLine($"  Env Modifier: {fastEnvModifier:F3}");
        output.WriteLine($"  Phase Modifier: {fastPhaseModifier:F3}");
        output.WriteLine($"  Random Variance: {fastRandomVariance:F3}");
        output.WriteLine($"  Final Speed: {fastFinalSpeed:F6} furlongs/tick");
        output.WriteLine("");
        output.WriteLine("SLOW HORSE (Speed=0):");
        output.WriteLine($"  Stat Modifier: {slowStatModifier:F3}");
        output.WriteLine($"  Env Modifier: {slowEnvModifier:F3}");
        output.WriteLine($"  Phase Modifier: {slowPhaseModifier:F3}");
        output.WriteLine($"  Random Variance: {slowRandomVariance:F3}");
        output.WriteLine($"  Final Speed: {slowFinalSpeed:F6} furlongs/tick");
        output.WriteLine("");
        output.WriteLine($"Speed Difference: {((fastFinalSpeed - slowFinalSpeed) / slowFinalSpeed * 100):F1}%");

        // Assert that fast horse is faster than slow horse
        Assert.True(fastFinalSpeed > slowFinalSpeed, "Fast horse should have higher final speed than slow horse");

        // Assert reasonable difference (should be ~22% faster with neutral variance)
        var speedDifferencePercent = (fastFinalSpeed - slowFinalSpeed) / slowFinalSpeed * 100;
        Assert.InRange(speedDifferencePercent, 15, 30); // Expect ~22% difference
    }
}
