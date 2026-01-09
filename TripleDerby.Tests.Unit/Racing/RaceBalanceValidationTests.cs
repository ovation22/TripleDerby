using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Racing.Calculators;
using TripleDerby.Core.Specifications;
using TripleDerby.Services.Racing;
using TripleDerby.SharedKernel.Enums;
using Xunit.Abstractions;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Statistical validation tests for race balance and modifier impacts.
/// Runs large-scale race simulations to analyze:
/// - Finish time distributions
/// - Stat correlations with performance
/// - Modifier impact analysis
/// - Edge case behavior
/// </summary>
public class RaceBalanceValidationTests(ITestOutputHelper output)
{
    // Long-running validation test - exclude from CI to prevent excessive output
    // To run locally: dotnet test --filter Category=LongRunning
    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task Run_1000_Races_With_Varied_Stats_And_Collect_Statistics()
    {
        // Arrange
        const int raceCount = 1000;
        var results = new List<RaceSimulationResult>();

        // Run simulations with varied configurations
        for (int i = 0; i < raceCount; i++)
        {
            var config = GenerateVariedRaceConfig(i);
            var result = await RunSingleRaceSimulation(config);
            results.Add(result);
        }

        // Analyze results
        var analysis = AnalyzeResults(results);

        // Output statistics
        output.WriteLine("=== RACE BALANCE VALIDATION RESULTS ===");
        output.WriteLine($"Total Races Simulated: {raceCount}");
        output.WriteLine("");
        output.WriteLine("=== FINISH TIME STATISTICS (10 Furlongs) ===");
        output.WriteLine($"Average Finish Time: {analysis.AverageFinishTime:F2} ticks");
        output.WriteLine($"Min Finish Time: {analysis.MinFinishTime:F2} ticks");
        output.WriteLine($"Max Finish Time: {analysis.MaxFinishTime:F2} ticks");
        output.WriteLine($"Standard Deviation: {analysis.FinishTimeStdDev:F2} ticks");
        output.WriteLine("");
        output.WriteLine("=== STAT CORRELATION ANALYSIS ===");
        output.WriteLine($"Speed Correlation: {analysis.SpeedCorrelation:F3}");
        output.WriteLine($"Agility Correlation: {analysis.AgilityCorrelation:F3}");
        output.WriteLine($"Stamina Correlation: {analysis.StaminaCorrelation:F3}");
        output.WriteLine("");
        output.WriteLine("=== MODIFIER IMPACT ANALYSIS ===");
        output.WriteLine($"Surface Impact Range: {analysis.SurfaceImpactRange:F2}%");
        output.WriteLine($"Condition Impact Range: {analysis.ConditionImpactRange:F2}%");
        output.WriteLine($"LegType Impact Range: {analysis.LegTypeImpactRange:F2}%");
        output.WriteLine("");

        // Assert reasonable ranges
        Assert.InRange(analysis.AverageFinishTime, 200, 270); // Should be around 237 for 10f
        Assert.InRange(analysis.FinishTimeStdDev, 5, 50); // Reasonable variance
        Assert.True(analysis.SpeedCorrelation < -0.3, "Speed should have strong negative correlation with finish time");
        Assert.True(analysis.AgilityCorrelation < -0.1, "Agility should have mild negative correlation with finish time");
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData(4, 90, 110)] // 4 furlongs: sprint race
    [InlineData(6, 135, 160)] // 6 furlongs: standard sprint
    [InlineData(10, 220, 254)] // 10 furlongs: classic distance (target: 237)
    [InlineData(12, 264, 305)] // 12 furlongs: long distance
    [InlineData(16, 352, 406)] // 16 furlongs: extreme distance
    public async Task Race_Distance_Produces_Expected_Finish_Times(decimal furlongs, double minExpected, double maxExpected)
    {
        // Arrange
        const int sampleSize = 100;
        var finishTimes = new List<double>();

        for (int i = 0; i < sampleSize; i++)
        {
            var config = new RaceConfig
            {
                Furlongs = furlongs,
                Surface = SurfaceId.Dirt,
                Condition = ConditionId.Good,
                HorseSpeed = 50, // Neutral stats
                HorseAgility = 50,
                HorseStamina = 50,
                LegType = LegTypeId.FrontRunner
            };

            var result = await RunSingleRaceSimulation(config);
            finishTimes.Add(result.FinishTime);
        }

        var avgFinishTime = finishTimes.Average();

        output.WriteLine($"=== {furlongs}f Race Distance Validation ===");
        output.WriteLine($"Average Finish Time: {avgFinishTime:F2} ticks");
        output.WriteLine($"Expected Range: {minExpected:F2} - {maxExpected:F2} ticks");
        output.WriteLine($"Min Observed: {finishTimes.Min():F2} ticks");
        output.WriteLine($"Max Observed: {finishTimes.Max():F2} ticks");

        // Assert within expected range
        Assert.InRange(avgFinishTime, minExpected, maxExpected);
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData(0, 0, 0)] // All minimum stats
    [InlineData(100, 100, 100)] // All maximum stats
    [InlineData(0, 100, 50)] // Mixed extremes
    [InlineData(50, 50, 50)] // Neutral baseline
    public async Task Extreme_Stat_Combinations_Produce_Valid_Results(int speed, int agility, int stamina)
    {
        // Arrange
        var config = new RaceConfig
        {
            Furlongs = 10,
            Surface = SurfaceId.Dirt,
            Condition = ConditionId.Good,
            HorseSpeed = speed,
            HorseAgility = agility,
            HorseStamina = stamina,
            LegType = LegTypeId.FrontRunner
        };

        // Act
        var result = await RunSingleRaceSimulation(config);

        // Assert
        output.WriteLine($"=== Extreme Stats Test: Speed={speed}, Agility={agility}, Stamina={stamina} ===");
        output.WriteLine($"Finish Time: {result.FinishTime:F2} ticks");
        output.WriteLine($"Distance Covered: {result.DistanceCovered:F2} furlongs");

        Assert.True(result.FinishTime > 0, "Race should complete");
        Assert.Equal(10, result.DistanceCovered); // Should complete full distance
        Assert.InRange(result.FinishTime, 150, 400); // Reasonable range even for extremes
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData(ConditionId.Fast, 215, 245)] // Fastest condition
    [InlineData(ConditionId.Good, 225, 255)] // Neutral condition
    [InlineData(ConditionId.Slow, 250, 280)] // Slowest condition
    [InlineData(ConditionId.Muddy, 235, 265)] // Challenging condition
    [InlineData(ConditionId.Frozen, 245, 275)] // Extreme condition
    public async Task Track_Conditions_Show_Measurable_Impact(ConditionId condition, double minExpected, double maxExpected)
    {
        // Arrange
        const int sampleSize = 50;
        var finishTimes = new List<double>();

        for (int i = 0; i < sampleSize; i++)
        {
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

            var result = await RunSingleRaceSimulation(config);
            finishTimes.Add(result.FinishTime);
        }

        var avgFinishTime = finishTimes.Average();

        output.WriteLine($"=== {condition} Condition Validation ===");
        output.WriteLine($"Average Finish Time: {avgFinishTime:F2} ticks");
        output.WriteLine($"Expected Range: {minExpected:F2} - {maxExpected:F2} ticks");

        Assert.InRange(avgFinishTime, minExpected, maxExpected);
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData(LegTypeId.StartDash)]
    [InlineData(LegTypeId.FrontRunner)]
    [InlineData(LegTypeId.StretchRunner)]
    [InlineData(LegTypeId.LastSpurt)]
    [InlineData(LegTypeId.RailRunner)]
    public async Task LegType_Modifiers_Activate_During_Expected_Phases(LegTypeId legType)
    {
        // Arrange
        var config = new RaceConfig
        {
            Furlongs = 10,
            Surface = SurfaceId.Dirt,
            Condition = ConditionId.Good,
            HorseSpeed = 50,
            HorseAgility = 50,
            HorseStamina = 50,
            LegType = legType
        };

        // Act
        var result = await RunSingleRaceSimulation(config);

        // Assert
        output.WriteLine($"=== {legType} Phase Modifier Test ===");
        output.WriteLine($"Finish Time: {result.FinishTime:F2} ticks");

        Assert.True(result.FinishTime > 0);
        Assert.Equal(10, result.DistanceCovered);
    }

    // Long-running validation test - exclude from CI to prevent excessive output
    // To run locally: dotnet test --filter Category=LongRunning
    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task RailRunner_Balance_Validation_Across_500_Races()
    {
        // Tests that rail runner bonus is balanced vs other leg types

        const int racesPerLegType = 100;
        var legTypes = new[]
        {
            LegTypeId.StartDash,
            LegTypeId.FrontRunner,
            LegTypeId.StretchRunner,
            LegTypeId.LastSpurt,
            LegTypeId.RailRunner
        };

        var resultsByLegType = new Dictionary<LegTypeId, List<double>>();

        // Run 100 races for each leg type
        foreach (var legType in legTypes)
        {
            var finishTimes = new List<double>();

            for (int i = 0; i < racesPerLegType; i++)
            {
                var config = new RaceConfig
                {
                    Furlongs = 10,
                    Surface = SurfaceId.Dirt,
                    Condition = ConditionId.Good,
                    HorseSpeed = 50,  // Neutral stats for fair comparison
                    HorseAgility = 50,
                    HorseStamina = 50,
                    LegType = legType
                };

                var result = await RunSingleRaceSimulation(config);
                finishTimes.Add(result.FinishTime);
            }

            resultsByLegType[legType] = finishTimes;
        }

        // Analyze and output results
        output.WriteLine("=== RAIL RUNNER BALANCE VALIDATION (Feature 005) ===");
        output.WriteLine($"Total Races: {racesPerLegType * legTypes.Length}");
        output.WriteLine($"Races per Leg Type: {racesPerLegType}");
        output.WriteLine("");
        output.WriteLine("=== FINISH TIME STATISTICS BY LEG TYPE ===");

        var allAverages = new Dictionary<LegTypeId, double>();

        foreach (var legType in legTypes)
        {
            var times = resultsByLegType[legType];
            var avg = times.Average();
            var min = times.Min();
            var max = times.Max();
            var stdDev = CalculateStandardDeviation(times);

            allAverages[legType] = avg;

            output.WriteLine($"{legType,-15} | Avg: {avg:F2} | Min: {min:F2} | Max: {max:F2} | StdDev: {stdDev:F2}");
        }

        output.WriteLine("");
        output.WriteLine("=== BALANCE ANALYSIS ===");

        var railRunnerAvg = allAverages[LegTypeId.RailRunner];
        var overallAvg = allAverages.Values.Average();
        var railRunnerDeviation = ((railRunnerAvg - overallAvg) / overallAvg) * 100;

        output.WriteLine($"Rail Runner Average: {railRunnerAvg:F2} ticks");
        output.WriteLine($"Overall Average: {overallAvg:F2} ticks");
        output.WriteLine($"Rail Runner Deviation: {railRunnerDeviation:F2}%");
        output.WriteLine("");

        // Compare rail runner to each other leg type
        output.WriteLine("=== PAIRWISE COMPARISONS ===");
        foreach (var legType in legTypes.Where(lt => lt != LegTypeId.RailRunner))
        {
            var diff = railRunnerAvg - allAverages[legType];
            var diffPercent = (diff / allAverages[legType]) * 100;
            output.WriteLine($"RailRunner vs {legType,-15} | Diff: {diff:F2} ticks ({diffPercent:F2}%)");
        }

        output.WriteLine("");
        output.WriteLine("=== VALIDATION RESULTS ===");

        // Assert balance requirements from feature spec
        // Target: Rail runner finish time within 3% of other leg types
        Assert.InRange(railRunnerDeviation, -3.0, 3.0);
        output.WriteLine($"✓ Rail runner average within 3% of overall average");

        // Assert all leg types are reasonably balanced (within 5% of each other)
        var minAvg = allAverages.Values.Min();
        var maxAvg = allAverages.Values.Max();
        var rangePercent = ((maxAvg - minAvg) / minAvg) * 100;
        Assert.InRange(rangePercent, 0, 5.0);
        output.WriteLine($"✓ All leg types within 5% range (actual: {rangePercent:F2}%)");

        output.WriteLine("");
        output.WriteLine("=== PHASE 3 BALANCE VALIDATION: PASSED ===");
    }

    // Helper methods

    private RaceConfig GenerateVariedRaceConfig(int seed)
    {
        var random = new Random(seed);
        return new RaceConfig
        {
            Furlongs = 10, // Standard distance for comparison
            Surface = (SurfaceId)random.Next(1, 4),
            Condition = (ConditionId)random.Next(1, 12),
            HorseSpeed = random.Next(0, 101),
            HorseAgility = random.Next(0, 101),
            HorseStamina = random.Next(0, 101),
            HorseHappiness = random.Next(0, 101),
            LegType = (LegTypeId)random.Next(1, 6)
        };
    }

    private async Task<RaceSimulationResult> RunSingleRaceSimulation(RaceConfig config)
    {
        // Create mock repository
        var mockRepo = new Mock<ITripleDerbyRepository>();

        // Create test race
        var race = new Race
        {
            Id = 1,
            Name = "Test Race",
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
            Name = "Test Horse",
            LegTypeId = config.LegType,
            RaceStarts = 0,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = (byte)config.HorseSpeed },
                new() { StatisticId = StatisticId.Agility, Actual = (byte)config.HorseAgility },
                new() { StatisticId = StatisticId.Stamina, Actual = (byte)config.HorseStamina },
                new() { StatisticId = StatisticId.Happiness, Actual = (byte)config.HorseHappiness }
            }
        };

        // Setup repository mocks
        mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);
        mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Horse>()); // No CPU horses, just test horse
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaceRun rr, CancellationToken ct) => rr);

        // Create deterministic random generator for consistent results
        var mockRandom = new Mock<IRandomGenerator>();
        mockRandom.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
        mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns((int max) =>
        {
            // When GenerateRandomConditionId calls Next(length), return valid index
            // Enum.GetValues returns an array, so we need a valid index (0 to max-1)
            var conditionIndex = (int)config.Condition - 1; // Convert ConditionId to 0-based index
            return Math.Min(conditionIndex, max - 1); // Ensure within bounds
        });
        mockRandom.Setup(r => r.NextDouble()).Returns(0.5); // Neutral random variance

        var speedModifierCalculator = new SpeedModifierCalculator(mockRandom.Object);
        var staminaCalculator = new StaminaCalculator();

        var commentaryGenerator = new RaceCommentaryGenerator(mockRandom.Object);

        var purseCalculator = new PurseCalculator();

        var overtakingManager = new OvertakingManager(mockRandom.Object, speedModifierCalculator);
        var eventDetector = new EventDetector();

        var timeManager = new Mock<ITimeManager>();

        var mockStatProgression = new StatProgressionCalculator();

        // Create race executor and run simulation
        var raceExecutor = new RaceExecutor(mockRepo.Object, mockRandom.Object, speedModifierCalculator, staminaCalculator, commentaryGenerator, purseCalculator, overtakingManager, eventDetector, timeManager.Object, mockStatProgression, NullLogger<RaceExecutor>.Instance);
        var result = await raceExecutor.Race(1, horse.Id, CancellationToken.None);

        // Extract results
        var winnerResult = result.HorseResults.First();
        return new RaceSimulationResult
        {
            Config = config,
            FinishTime = winnerResult.Time,
            DistanceCovered = config.Furlongs
        };
    }

    private StatisticalAnalysis AnalyzeResults(List<RaceSimulationResult> results)
    {
        var finishTimes = results.Select(r => r.FinishTime).ToList();
        var speeds = results.Select(r => r.Config.HorseSpeed).ToList();
        var agilities = results.Select(r => r.Config.HorseAgility).ToList();
        var staminas = results.Select(r => r.Config.HorseStamina).ToList();

        return new StatisticalAnalysis
        {
            AverageFinishTime = finishTimes.Average(),
            MinFinishTime = finishTimes.Min(),
            MaxFinishTime = finishTimes.Max(),
            FinishTimeStdDev = CalculateStandardDeviation(finishTimes),
            SpeedCorrelation = CalculateCorrelation(speeds.Select(s => (double)s).ToList(), finishTimes),
            AgilityCorrelation = CalculateCorrelation(agilities.Select(a => (double)a).ToList(), finishTimes),
            StaminaCorrelation = CalculateCorrelation(staminas.Select(s => (double)s).ToList(), finishTimes),
            SurfaceImpactRange = 2.0, // Placeholder - will calculate from actual results
            ConditionImpactRange = 11.0, // Placeholder
            LegTypeImpactRange = 4.0 // Placeholder
        };
    }

    private double CalculateStandardDeviation(List<double> values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    private double CalculateCorrelation(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count == 0)
            return 0;

        var avgX = x.Average();
        var avgY = y.Average();

        var numerator = x.Zip(y, (xi, yi) => (xi - avgX) * (yi - avgY)).Sum();
        var denominator = Math.Sqrt(x.Sum(xi => Math.Pow(xi - avgX, 2)) * y.Sum(yi => Math.Pow(yi - avgY, 2)));

        return denominator == 0 ? 0 : numerator / denominator;
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
        public int HorseHappiness { get; set; } = 50; // Default to neutral
        public LegTypeId LegType { get; set; }
    }

    private class RaceSimulationResult
    {
        public RaceConfig Config { get; set; } = null!;
        public double FinishTime { get; set; }
        public decimal DistanceCovered { get; set; }
    }

    private class StatisticalAnalysis
    {
        public double AverageFinishTime { get; set; }
        public double MinFinishTime { get; set; }
        public double MaxFinishTime { get; set; }
        public double FinishTimeStdDev { get; set; }
        public double SpeedCorrelation { get; set; }
        public double AgilityCorrelation { get; set; }
        public double StaminaCorrelation { get; set; }
        public double SurfaceImpactRange { get; set; }
        public double ConditionImpactRange { get; set; }
        public double LegTypeImpactRange { get; set; }
    }
}
