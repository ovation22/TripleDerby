using Moq;
using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Services;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Services;

public class RaceServiceTests
{
    private readonly Mock<ITripleDerbyRepository> _repositoryMock;
    private readonly Mock<IRandomGenerator> _randomGeneratorMock;
    private readonly RaceService _sut;

    public RaceServiceTests()
    {
        _repositoryMock = new Mock<ITripleDerbyRepository>();
        _randomGeneratorMock = new Mock<IRandomGenerator>();
        _randomGeneratorMock.Setup(r => r.NextDouble()).Returns(0.5);
        _randomGeneratorMock.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        // Default: no CPU horses available (can be overridden in individual tests)
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Horse>());

        // Feature 005: Phase 4 - DI Refactor
        var speedModifierCalculator = new SpeedModifierCalculator(_randomGeneratorMock.Object);
        var staminaCalculator = new StaminaCalculator();

        // Feature 008: Commentary generator
        var commentaryGenerator = new RaceCommentaryGenerator(_randomGeneratorMock.Object);

        // Feature 009: Purse calculator
        var purseCalculator = new PurseCalculator();

        // Feature 010: Overtaking and event detection
        var overtakingManager = new OvertakingManager(_randomGeneratorMock.Object);
        var eventDetector = new EventDetector();

        _sut = new RaceService(_repositoryMock.Object, _randomGeneratorMock.Object, speedModifierCalculator, staminaCalculator, commentaryGenerator, purseCalculator, overtakingManager, eventDetector);
    }

    [Fact]
    public async Task Race_AssignsSequentialLanesToHorses()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);
        var playerHorse = CreateHorse(playerHorseId, "Player Horse");

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        RaceRun? capturedRaceRun = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRaceRun);
        Assert.Single(capturedRaceRun.Horses);
        Assert.Equal(1, capturedRaceRun.Horses.First().Lane);
    }

    [Fact]
    public async Task Race_AssignsUniqueLanesToMultipleHorses()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);
        var playerHorse = CreateHorse(playerHorseId, "Player Horse");
        var cpuHorses = new List<Horse>
        {
            CreateHorse(Guid.NewGuid(), "CPU Horse 1"),
            CreateHorse(Guid.NewGuid(), "CPU Horse 2"),
            CreateHorse(Guid.NewGuid(), "CPU Horse 3"),
        };

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cpuHorses);

        RaceRun? capturedRaceRun = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRaceRun);
        var lanes = capturedRaceRun.Horses.Select(h => h.Lane).ToList();
        Assert.Equal(lanes.Count, lanes.Distinct().Count()); // All lanes unique
        Assert.All(lanes, lane => Assert.InRange(lane, 1, 12)); // Lanes in valid range
    }

    private static Race CreateRace(byte id, decimal furlongs)
    {
        return new Race
        {
            Id = id,
            Name = "Test Race",
            Furlongs = furlongs,
            SurfaceId = SurfaceId.Dirt,
            Surface = new Surface { Id = SurfaceId.Dirt, Name = "Dirt" },
            TrackId = TrackId.TripleSpires,
            Track = new Track { Id = TrackId.TripleSpires, Name = "Test Track" }
        };
    }

    [Fact]
    public async Task Race_DeterminesFinishPlacesBasedOnDistance()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);

        // Create a fast horse (high speed = finishes first)
        var fastHorse = CreateHorse(playerHorseId, "Fast Horse", speed: 80);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fastHorse);

        RaceRun? capturedRaceRun = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRaceRun);
        Assert.Single(capturedRaceRun.Horses);
        Assert.Equal(1, capturedRaceRun.Horses.First().Place); // Only horse should be 1st place
    }

    [Fact]
    public async Task Race_FetchesCpuHorsesWithSimilarRaceStarts()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);
        var playerHorse = CreateHorse(playerHorseId, "Player Horse", speed: 50);
        playerHorse.RaceStarts = 5;

        var cpuHorses = new List<Horse>
        {
            CreateHorse(Guid.NewGuid(), "CPU Horse 1", speed: 60),
            CreateHorse(Guid.NewGuid(), "CPU Horse 2", speed: 55),
            CreateHorse(Guid.NewGuid(), "CPU Horse 3", speed: 45),
        };

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cpuHorses);

        RaceRun? capturedRaceRun = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRaceRun);
        Assert.Equal(4, capturedRaceRun.Horses.Count); // 1 player + 3 CPU horses
        Assert.Contains(capturedRaceRun.Horses, h => h.Horse.Id == playerHorseId);
    }

    [Fact]
    public async Task Race_AssignsPlacesToAllHorsesInOrder()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);

        // Slow player horse
        var playerHorse = CreateHorse(playerHorseId, "Slow Horse", speed: 30);

        // Fast CPU horses
        var cpuHorses = new List<Horse>
        {
            CreateHorse(Guid.NewGuid(), "Fast Horse 1", speed: 90),
            CreateHorse(Guid.NewGuid(), "Fast Horse 2", speed: 80),
        };

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cpuHorses);

        RaceRun? capturedRaceRun = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRaceRun);
        var places = capturedRaceRun.Horses.Select(h => h.Place).OrderBy(p => p).ToList();
        Assert.Equal(new byte[] { 1, 2, 3 }, places); // All places assigned
    }

    [Fact]
    public async Task Race_RunsWithMinimumFieldWhenNoCpuHorsesAvailable()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);
        var playerHorse = CreateHorse(playerHorseId, "Solo Horse", speed: 50);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        // No CPU horses available
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Horse>());

        RaceRun? capturedRaceRun = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .Callback<RaceRun, CancellationToken>((rr, _) => capturedRaceRun = rr)
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert - should still run with just player's horse (minimum field of 1)
        Assert.NotNull(capturedRaceRun);
        Assert.Single(capturedRaceRun.Horses);
        Assert.Equal(1, capturedRaceRun.Horses.First().Place);
    }

    [Fact]
    public async Task Race_ReturnsResultWithRaceAndTrackNames()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRaceWithDetails(raceId, "Kentucky Derby", "Triple Spires", "Dirt");
        var playerHorse = CreateHorse(playerHorseId, "Thunder", speed: 50);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        var result = await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert
        Assert.Equal("Kentucky Derby", result.RaceName);
        Assert.Equal("Triple Spires", result.TrackName);
        Assert.Equal("Dirt", result.SurfaceName);
    }

    [Fact]
    public async Task Race_ReturnsHorseResultsWithActualData()
    {
        // Arrange
        var raceId = (byte)1;
        var playerHorseId = Guid.NewGuid();
        var race = CreateRaceWithDetails(raceId, "Test Race", "Test Track", "Turf");
        var playerHorse = CreateHorse(playerHorseId, "Lightning Bolt", speed: 50);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        // Act
        var result = await _sut.Race(raceId, playerHorseId, CancellationToken.None);

        // Assert
        Assert.Single(result.HorseResults);
        var horseResult = result.HorseResults.First();
        Assert.Equal(playerHorseId, horseResult.HorseId);
        Assert.Equal("Lightning Bolt", horseResult.HorseName);
        Assert.Equal(1, horseResult.Place);
    }

    [Fact]
    public async Task Race_FasterHorseFinishesInLessTime()
    {
        // Arrange - Phase 2 Integration Test
        var raceId = (byte)1;
        var fastHorseId = Guid.NewGuid();
        var slowHorseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);

        // Fast horse (Speed 80) vs Slow horse (Speed 40)
        var fastHorse = CreateHorse(fastHorseId, "Fast Horse", speed: 80);
        var slowHorse = CreateHorse(slowHorseId, "Slow Horse", speed: 40);

        // Run race for fast horse
        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);
        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fastHorse);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);

        var fastHorseResult = await _sut.Race(raceId, fastHorseId, CancellationToken.None);

        // Run race for slow horse (reset mocks)
        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(slowHorse);

        var slowHorseResult = await _sut.Race(raceId, slowHorseId, CancellationToken.None);

        // Assert - Fast horse should have lower finish time
        var fastTime = fastHorseResult.HorseResults.First().Time;
        var slowTime = slowHorseResult.HorseResults.First().Time;

        Assert.True(fastTime < slowTime,
            $"Fast horse (Speed 80) should finish in less time than slow horse (Speed 40). " +
            $"Fast: {fastTime:F2} ticks, Slow: {slowTime:F2} ticks");

        // Verify significant difference (should be noticeable)
        var timeDifference = slowTime - fastTime;
        Assert.True(timeDifference > 0.1,
            $"Time difference should be noticeable (>0.1 ticks). Actual: {timeDifference:F2}");
    }

    private static Race CreateRaceWithDetails(byte id, string raceName, string trackName, string surfaceName)
    {
        return new Race
        {
            Id = id,
            Name = raceName,
            Furlongs = 6,
            SurfaceId = SurfaceId.Dirt,
            Surface = new Surface { Id = SurfaceId.Dirt, Name = surfaceName },
            TrackId = TrackId.TripleSpires,
            Track = new Track { Id = TrackId.TripleSpires, Name = trackName }
        };
    }

    private static Horse CreateHorse(Guid id, string name, byte speed = 50)
    {
        return new Horse
        {
            Id = id,
            Name = name,
            LegTypeId = LegTypeId.FrontRunner,
            RaceStarts = 5,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = speed },
                new() { StatisticId = StatisticId.Agility, Actual = 50 },
                new() { StatisticId = StatisticId.Stamina, Actual = 50 },
                new() { StatisticId = StatisticId.Durability, Actual = 50 },
                new() { StatisticId = StatisticId.Happiness, Actual = 50 }
            }
        };
    }
}
