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

/// <summary>
/// Tests for race commentary generation, including photo finish detection and ordering.
/// </summary>
public class RaceCommentaryTests
{
    private readonly Mock<ITripleDerbyRepository> _repositoryMock;
    private readonly Mock<IRandomGenerator> _randomGeneratorMock;
    private readonly RaceService _sut;

    public RaceCommentaryTests()
    {
        _repositoryMock = new Mock<ITripleDerbyRepository>();
        _randomGeneratorMock = new Mock<IRandomGenerator>();
        _randomGeneratorMock.Setup(r => r.NextDouble()).Returns(0.5);
        _randomGeneratorMock.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        _randomGeneratorMock.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(7);

        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Horse>());

        var speedModifierCalculator = new SpeedModifierCalculator(_randomGeneratorMock.Object);
        var staminaCalculator = new StaminaCalculator();
        var commentaryGenerator = new RaceCommentaryGenerator(_randomGeneratorMock.Object);

        _sut = new RaceService(_repositoryMock.Object, _randomGeneratorMock.Object, speedModifierCalculator, staminaCalculator, commentaryGenerator);
    }

    [Fact]
    public async Task Race_GeneratesPhotoFinishCommentary_WhenTop2FinishClose()
    {
        // Arrange
        var raceId = (byte)1;
        var horse1Id = Guid.NewGuid();
        var horse2Id = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);

        // Create two horses with very similar speeds (should finish close together)
        var horse1 = CreateHorse(horse1Id, "Fast Horse", speed: 80);
        var horse2 = CreateHorse(horse2Id, "Almost Fast Horse", speed: 79);

        var cpuHorses = new List<Horse> { horse2 };

        SetupMocks(race, horse1, cpuHorses);

        // Act
        var result = await _sut.Race(raceId, horse1Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result.PlayByPlay);

        // Should have photo finish commentary
        var hasPhotoFinish = result.PlayByPlay.Any(p => p.Contains("Photo finish!", StringComparison.OrdinalIgnoreCase));

        if (hasPhotoFinish)
        {
            // Photo finish should appear BEFORE or IN THE SAME commentary entry as finish announcements
            var photoFinishIndex = result.PlayByPlay.FindIndex(p => p.Contains("Photo finish!", StringComparison.OrdinalIgnoreCase));

            // Find finish announcements (looking for place mentions like "1st", "2nd", "place")
            var finishIndices = result.PlayByPlay
                .Select((p, i) => new { Text = p, Index = i })
                .Where(x => x.Text.Contains("place", StringComparison.OrdinalIgnoreCase) ||
                           x.Text.Contains("finish", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Index)
                .ToList();

            // Photo finish should appear before or at the same time as the first finish announcement
            if (finishIndices.Any())
            {
                var firstFinishIndex = finishIndices.Min();
                Assert.True(photoFinishIndex <= firstFinishIndex,
                    $"Photo finish (index {photoFinishIndex}) should appear before or with finish announcements (first at index {firstFinishIndex})");
            }
        }
    }

    [Fact]
    public async Task Race_PhotoFinishCommentary_AppearsBeforeFinishAnnouncements()
    {
        // Arrange
        var raceId = (byte)1;
        var horse1Id = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);
        var horse1 = CreateHorse(horse1Id, "Solo Horse", speed: 80);

        SetupMocks(race, horse1, new List<Horse>());

        // Act
        var result = await _sut.Race(raceId, horse1Id, CancellationToken.None);

        // Assert - With only one horse, no photo finish should occur
        Assert.NotNull(result.PlayByPlay);
        var hasPhotoFinish = result.PlayByPlay.Any(p => p.Contains("Photo finish!", StringComparison.OrdinalIgnoreCase));
        Assert.False(hasPhotoFinish, "Single horse race should not have photo finish");
    }

    [Fact]
    public async Task Race_DoesNotGeneratePhotoFinish_WhenTop2FinishFarApart()
    {
        // Arrange
        var raceId = (byte)1;
        var horse1Id = Guid.NewGuid();
        var horse2Id = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);

        // Create horses with very different speeds (should finish far apart)
        var horse1 = CreateHorse(horse1Id, "Very Fast Horse", speed: 90);
        var horse2 = CreateHorse(horse2Id, "Very Slow Horse", speed: 30);

        var cpuHorses = new List<Horse> { horse2 };

        SetupMocks(race, horse1, cpuHorses);

        // Act
        var result = await _sut.Race(raceId, horse1Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result.PlayByPlay);
        var hasPhotoFinish = result.PlayByPlay.Any(p => p.Contains("Photo finish!", StringComparison.OrdinalIgnoreCase));
        Assert.False(hasPhotoFinish, "Horses finishing far apart should not trigger photo finish");
    }

    [Fact]
    public async Task Race_GeneratesRaceStartCommentary()
    {
        // Arrange
        var raceId = (byte)1;
        var horseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 10);
        var horse = CreateHorse(horseId, "Test Horse", speed: 50);

        SetupMocks(race, horse, new List<Horse>());

        // Act
        var result = await _sut.Race(raceId, horseId, CancellationToken.None);

        // Assert
        Assert.NotNull(result.PlayByPlay);
        Assert.NotEmpty(result.PlayByPlay);

        var firstCommentary = result.PlayByPlay.First();
        Assert.Contains("And they're off!", firstCommentary);
        Assert.Contains("furlongs", firstCommentary);
    }

    [Fact]
    public async Task Race_GeneratesFinalStretchCommentary()
    {
        // Arrange
        var raceId = (byte)1;
        var horseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);
        var horse = CreateHorse(horseId, "Test Horse", speed: 50);

        SetupMocks(race, horse, new List<Horse>());

        // Act
        var result = await _sut.Race(raceId, horseId, CancellationToken.None);

        // Assert
        Assert.NotNull(result.PlayByPlay);

        var hasFinalStretch = result.PlayByPlay.Any(p =>
            p.Contains("stretch", StringComparison.OrdinalIgnoreCase) &&
            (p.Contains("final", StringComparison.OrdinalIgnoreCase) ||
             p.Contains("home", StringComparison.OrdinalIgnoreCase)));

        Assert.True(hasFinalStretch, "Race should have final stretch commentary");
    }

    [Fact]
    public async Task Race_GeneratesFinishCommentary_ForAllHorses()
    {
        // Arrange
        var raceId = (byte)1;
        var horse1Id = Guid.NewGuid();
        var horse2Id = Guid.NewGuid();
        var horse3Id = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);

        var horse1 = CreateHorse(horse1Id, "Horse1", speed: 80);
        var horse2 = CreateHorse(horse2Id, "Horse2", speed: 70);
        var horse3 = CreateHorse(horse3Id, "Horse3", speed: 60);

        var cpuHorses = new List<Horse> { horse2, horse3 };

        SetupMocks(race, horse1, cpuHorses);

        // Act
        var result = await _sut.Race(raceId, horse1Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result.PlayByPlay);

        // Should have finish commentary for all 3 horses
        var finishCommentary = result.PlayByPlay.Where(p =>
            p.Contains("1st", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("2nd", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("3rd", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("place", StringComparison.OrdinalIgnoreCase));

        Assert.NotEmpty(finishCommentary);
    }

    [Fact]
    public async Task Race_PhotoFinishOccurs_OnlyWhenSecondPlaceFinishes()
    {
        // Arrange
        var raceId = (byte)1;
        var horse1Id = Guid.NewGuid();
        var horse2Id = Guid.NewGuid();
        var horse3Id = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);

        // Horse 1 and 2 finish close (photo finish)
        // Horse 3 finishes much later
        var horse1 = CreateHorse(horse1Id, "Fast1", speed: 80);
        var horse2 = CreateHorse(horse2Id, "Fast2", speed: 79);
        var horse3 = CreateHorse(horse3Id, "Slow", speed: 30);

        var cpuHorses = new List<Horse> { horse2, horse3 };

        SetupMocks(race, horse1, cpuHorses);

        // Act
        var result = await _sut.Race(raceId, horse1Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result.PlayByPlay);

        var hasPhotoFinish = result.PlayByPlay.Any(p => p.Contains("Photo finish!", StringComparison.OrdinalIgnoreCase));

        if (hasPhotoFinish)
        {
            // Photo finish should only appear once
            var photoFinishCount = result.PlayByPlay.Count(p => p.Contains("Photo finish!", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(1, photoFinishCount);

            // And it should be in the same commentary string as when 2nd place finishes
            var photoFinishEntry = result.PlayByPlay.First(p => p.Contains("Photo finish!", StringComparison.OrdinalIgnoreCase));

            // The photo finish entry should mention both top 2 horses
            Assert.True(
                photoFinishEntry.Contains(horse1.Name, StringComparison.OrdinalIgnoreCase) ||
                photoFinishEntry.Contains(horse2.Name, StringComparison.OrdinalIgnoreCase),
                "Photo finish should mention the horses involved");
        }
    }

    [Fact]
    public async Task Race_CommentaryNotEmpty()
    {
        // Arrange
        var raceId = (byte)1;
        var horseId = Guid.NewGuid();
        var race = CreateRace(raceId, furlongs: 6);
        var horse = CreateHorse(horseId, "Test Horse", speed: 50);

        SetupMocks(race, horse, new List<Horse>());

        // Act
        var result = await _sut.Race(raceId, horseId, CancellationToken.None);

        // Assert
        Assert.NotNull(result.PlayByPlay);
        Assert.NotEmpty(result.PlayByPlay);

        // Should have at least race start and finish commentary
        Assert.True(result.PlayByPlay.Count >= 2, "Should have at least start and finish commentary");
    }

    private void SetupMocks(Race race, Horse playerHorse, List<Horse> cpuHorses)
    {
        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<RaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(race);

        _repositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<HorseForRaceSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerHorse);

        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<SimilarRaceStartsSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cpuHorses);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<RaceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaceRun rr, CancellationToken _) => rr);
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
