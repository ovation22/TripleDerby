using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Services;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel.Enums;
using Xunit.Abstractions;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Diagnostic tests to understand current race balance and identify issues.
/// Runs controlled races with known configurations to analyze actual vs expected behavior.
/// </summary>
public class RaceBalanceDiagnosticTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task Diagnostic_Baseline_10F_Race_With_Neutral_Stats()
    {
        // Arrange - Perfect baseline: neutral stats, neutral conditions
        var config = new RaceConfig
        {
            Furlongs = 10,
            Surface = SurfaceId.Dirt,
            Condition = ConditionId.Good,
            HorseSpeed = 50,
            HorseAgility = 50,
            HorseStamina = 50,
            LegType = LegTypeId.FrontRunner
        };

        // Act
        var result = await RunDiagnosticRace(config);

        // Output detailed diagnostics
        output.WriteLine("=== DIAGNOSTIC: Baseline 10F Race ===");
        output.WriteLine($"Configuration:");
        output.WriteLine($"  Distance: {config.Furlongs}f");
        output.WriteLine($"  Surface: {config.Surface} (expected modifier: 1.00)");
        output.WriteLine($"  Condition: {config.Condition} (expected modifier: 1.00)");
        output.WriteLine($"  Horse Stats: Speed={config.HorseSpeed}, Agility={config.HorseAgility}, Stamina={config.HorseStamina}");
        output.WriteLine($"  LegType: {config.LegType}");
        output.WriteLine("");
        output.WriteLine("Results:");
        output.WriteLine($"  Finish Time: {result.FinishTime:F2} ticks");
        output.WriteLine($"  Target Time: ~237 ticks");
        output.WriteLine($"  Difference: {result.FinishTime - 237:F2} ticks ({((result.FinishTime - 237) / 237 * 100):F1}%)");
        output.WriteLine("");
        output.WriteLine("Analysis:");
        output.WriteLine($"  Base Speed: {RaceService_GetAverageBaseSpeed():F6} furlongs/tick");
        output.WriteLine($"  Expected Total Ticks: {Math.Ceiling(10.0 / RaceService_GetAverageBaseSpeed()):F0}");
        output.WriteLine($"  With all neutral modifiers (1.0), should take ~237 ticks");

        // This test is diagnostic only - no assertions
        Assert.True(result.FinishTime > 0, "Race should complete");
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData(0, "Expected: ~270 ticks (0.90 speed multiplier)")]
    [InlineData(25, "Expected: ~250 ticks (0.95 speed multiplier)")]
    [InlineData(50, "Expected: ~237 ticks (1.00 speed multiplier - baseline)")]
    [InlineData(75, "Expected: ~224 ticks (1.05 speed multiplier)")]
    [InlineData(100, "Expected: ~215 ticks (1.10 speed multiplier)")]
    public async Task Diagnostic_Speed_Stat_Impact(int speed, string expectedNote)
    {
        // Arrange
        var config = new RaceConfig
        {
            Furlongs = 10,
            Surface = SurfaceId.Dirt,
            Condition = ConditionId.Good,
            HorseSpeed = speed,
            HorseAgility = 50,  // Neutral
            HorseStamina = 50,  // Neutral
            LegType = LegTypeId.FrontRunner
        };

        // Act
        var result = await RunDiagnosticRace(config);

        // Output
        output.WriteLine($"=== DIAGNOSTIC: Speed={speed} Impact ===");
        output.WriteLine($"Config Horse Speed: {config.HorseSpeed}");
        output.WriteLine($"Finish Time: {result.FinishTime:F2} ticks");
        output.WriteLine($"{expectedNote}");
        output.WriteLine($"Speed Multiplier: {1.0 + ((speed - 50) * 0.002):F3}");
        output.WriteLine($"Actual Horse Speed in Result: {result.HorseSpeed}");

        Assert.True(result.FinishTime > 0);
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData(ConditionId.Fast, 1.03, "Expected: ~230 ticks")]
    [InlineData(ConditionId.Good, 1.00, "Expected: ~237 ticks (baseline)")]
    [InlineData(ConditionId.Slow, 0.90, "Expected: ~263 ticks")]
    public async Task Diagnostic_Condition_Impact(ConditionId condition, double expectedModifier, string expectedNote)
    {
        // Arrange
        var config = new RaceConfig
        {
            Furlongs = 10,
            Surface = SurfaceId.Dirt,
            Condition = condition,
            HorseSpeed = 50,
            HorseAgility = 50,
            HorseStamina = 50,
            LegType = LegTypeId.FrontRunner
        };

        // Act
        var result = await RunDiagnosticRace(config);

        // Output
        output.WriteLine($"=== DIAGNOSTIC: {condition} Condition ===");
        output.WriteLine($"Finish Time: {result.FinishTime:F2} ticks");
        output.WriteLine($"Expected Modifier: {expectedModifier:F2}");
        output.WriteLine($"{expectedNote}");

        Assert.True(result.FinishTime > 0);
    }

    // Helper to access private constant from RaceService
    private static double RaceService_GetAverageBaseSpeed()
    {
        // From RaceService: const double AverageBaseSpeed = 10.0 / 237.0
        return 10.0 / 237.0; // ≈ 0.0422
    }

    private async Task<RaceDiagnosticResult> RunDiagnosticRace(RaceConfig config)
    {
        // Create mock repository
        var mockRepo = new Mock<ITripleDerbyRepository>();

        // Create test race
        var race = new Race
        {
            Id = 1,
            Name = "Diagnostic Race",
            Furlongs = config.Furlongs,
            SurfaceId = config.Surface,
            TrackId = (TrackId)1,
            Track = new Track { Id = (TrackId)1, Name = "Test Track" },
            Surface = new Surface { Id = config.Surface, Name = config.Surface.ToString() }
        };

        // Create test horse with specified stats
        // IMPORTANT: Horse stats are stored in Statistics collection, must initialize properly
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Diagnostic Horse",
            LegTypeId = config.LegType,
            RaceStarts = 0,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = (byte)config.HorseSpeed },
                new() { StatisticId = StatisticId.Agility, Actual = (byte)config.HorseAgility },
                new() { StatisticId = StatisticId.Stamina, Actual = (byte)config.HorseStamina }
            }
        };

        // Setup repository mocks
        mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);
        mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Horse>()); // No CPU horses
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRun, CancellationToken>((rr, ct) =>
            {
                // Force the condition we want for testing
                rr.ConditionId = config.Condition;
            })
            .ReturnsAsync((RaceRun rr, CancellationToken ct) => rr);

        // Create random generator with no variance for consistent diagnostics
        var mockRandom = new Mock<IRandomGenerator>();
        mockRandom.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
        mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        mockRandom.Setup(r => r.NextDouble()).Returns(0.5); // Exactly neutral (0% variance)

        // Feature 005: Phase 4 - DI Refactor
        var speedModifierCalculator = new SpeedModifierCalculator(mockRandom.Object);
        var staminaCalculator = new StaminaCalculator();

        // Feature 008: Commentary generator
        var commentaryGenerator = new RaceCommentaryGenerator(mockRandom.Object);

        // Create race service and run simulation
        var raceService = new RaceService(mockRepo.Object, mockRandom.Object, speedModifierCalculator, staminaCalculator, commentaryGenerator);

        // We need to set the condition BEFORE the race runs
        // The issue is that RaceService.Race() calls GenerateRandomConditionId()
        // Let's override the mock to return our desired condition
        mockRandom.Setup(r => r.Next(It.Is<int>(i => i > 10))).Returns((int)config.Condition - 1);

        var result = await raceService.Race(1, horse.Id, CancellationToken.None);

        // Extract results
        var winnerResult = result.HorseResults.First();

        // Find the actual horse to verify stats
        var actualHorse = await mockRepo.Object.FirstOrDefaultAsync(
            new HorseForRaceSpecification(winnerResult.HorseId),
            CancellationToken.None);

        return new RaceDiagnosticResult
        {
            Config = config,
            FinishTime = winnerResult.Time,
            DistanceCovered = config.Furlongs,
            HorseSpeed = actualHorse?.Speed ?? 0
        };
    }

    // Data structures
    private class RaceConfig
    {
        public decimal Furlongs { get; set; }
        public SurfaceId Surface { get; set; }
        public ConditionId Condition { get; set; }
        public int HorseSpeed { get; set; }
        public int HorseAgility { get; set; }
        public int HorseStamina { get; set; }
        public LegTypeId LegType { get; set; }
    }

    private class RaceDiagnosticResult
    {
        public RaceConfig Config { get; set; } = null!;
        public double FinishTime { get; set; }
        public decimal DistanceCovered { get; set; }
        public byte HorseSpeed { get; set; }
    }
}
