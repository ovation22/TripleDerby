using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Specifications;

public class RaceRunDetailSpecificationTests
{
    [Fact]
    public void Projects_RaceRun_To_RaceRunResult()
    {
        // Arrange
        var raceId = (byte)1;
        var raceRunId = Guid.NewGuid();
        var horseId = Guid.NewGuid();

        var track = new Track { Id = TrackId.TripleSpires, Name = "Churchill Downs" };
        var surface = new Surface { Id = SurfaceId.Dirt, Name = "Dirt" };
        var race = new Race
        {
            Id = raceId,
            Name = "Kentucky Derby",
            TrackId = TrackId.TripleSpires,
            Track = track,
            SurfaceId = SurfaceId.Dirt,
            Surface = surface,
            Furlongs = 10.0m
        };

        var horse = new Horse
        {
            Id = horseId,
            Name = "Secretariat"
        };

        var raceRun = new RaceRun
        {
            Id = raceRunId,
            RaceId = raceId,
            Race = race,
            ConditionId = ConditionId.Fast,
            Purse = 100000,
            Horses = new List<RaceRunHorse>
            {
                new()
                {
                    HorseId = horseId,
                    Horse = horse,
                    Place = 1,
                    Time = 119.4,
                    Payout = 5000,
                    Lane = 3,
                    Distance = 1320.0m
                }
            },
            RaceRunTicks = new List<RaceRunTick>
            {
                new() { Tick = 1, Note = "And they're off!" },
                new() { Tick = 2, Note = "" }, // Should be filtered out
                new() { Tick = 3, Note = "Secretariat takes the lead!" }
            }
        };

        var raceRuns = new List<RaceRun> { raceRun };
        var spec = new RaceRunDetailSpecification(raceId, raceRunId);

        // Act
        var result = ApplySpecification(raceRuns, spec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(raceRunId, result.RaceRunId);
        Assert.Equal(raceId, result.RaceId);
        Assert.Equal("Kentucky Derby", result.RaceName);
        Assert.Equal(TrackId.TripleSpires, result.TrackId);
        Assert.Equal("Churchill Downs", result.TrackName);
        Assert.Equal(SurfaceId.Dirt, result.SurfaceId);
        Assert.Equal("Dirt", result.SurfaceName);
        Assert.Equal(10.0m, result.Furlongs);
        Assert.Equal(ConditionId.Fast, result.ConditionId);
        Assert.Equal("Fast", result.ConditionName);
    }

    [Fact]
    public void Projects_Horse_Results_With_Correct_Data()
    {
        // Arrange
        var raceId = (byte)1;
        var raceRunId = Guid.NewGuid();
        var horse1Id = Guid.NewGuid();
        var horse2Id = Guid.NewGuid();

        var race = CreateMinimalRace(raceId);
        var horse1 = new Horse { Id = horse1Id, Name = "Winner" };
        var horse2 = new Horse { Id = horse2Id, Name = "Runner Up" };

        var raceRun = new RaceRun
        {
            Id = raceRunId,
            RaceId = raceId,
            Race = race,
            ConditionId = ConditionId.Fast,
            Purse = 50000,
            Horses = new List<RaceRunHorse>
            {
                new()
                {
                    HorseId = horse1Id,
                    Horse = horse1,
                    Place = 1,
                    Time = 120.5,
                    Payout = 3000,
                    Lane = 2,
                    Distance = 1320.0m
                },
                new()
                {
                    HorseId = horse2Id,
                    Horse = horse2,
                    Place = 2,
                    Time = 121.0,
                    Payout = 1500,
                    Lane = 4,
                    Distance = 1318.5m
                }
            },
            RaceRunTicks = new List<RaceRunTick>()
        };

        var raceRuns = new List<RaceRun> { raceRun };
        var spec = new RaceRunDetailSpecification(raceId, raceRunId);

        // Act
        var result = ApplySpecification(raceRuns, spec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.HorseResults.Count);

        var winnerResult = result.HorseResults[0];
        Assert.Equal((byte)1, winnerResult.Place);
        Assert.Equal(horse1Id, winnerResult.HorseId);
        Assert.Equal("Winner", winnerResult.HorseName);
        Assert.Equal(120.5, winnerResult.Time);
        Assert.Equal(3000, winnerResult.Payout);

        var runnerUpResult = result.HorseResults[1];
        Assert.Equal((byte)2, runnerUpResult.Place);
        Assert.Equal(horse2Id, runnerUpResult.HorseId);
        Assert.Equal("Runner Up", runnerUpResult.HorseName);
        Assert.Equal(121.0, runnerUpResult.Time);
        Assert.Equal(1500, runnerUpResult.Payout);
    }

    [Fact]
    public void Orders_Horse_Results_By_Place()
    {
        // Arrange
        var raceId = (byte)1;
        var raceRunId = Guid.NewGuid();

        var race = CreateMinimalRace(raceId);
        var raceRun = new RaceRun
        {
            Id = raceRunId,
            RaceId = raceId,
            Race = race,
            ConditionId = ConditionId.Fast,
            Purse = 50000,
            Horses = new List<RaceRunHorse>
            {
                new() { Horse = new Horse { Name = "Third" }, Place = 3, Payout = 500 },
                new() { Horse = new Horse { Name = "First" }, Place = 1, Payout = 2000 },
                new() { Horse = new Horse { Name = "Second" }, Place = 2, Payout = 1000 }
            },
            RaceRunTicks = new List<RaceRunTick>()
        };

        var raceRuns = new List<RaceRun> { raceRun };
        var spec = new RaceRunDetailSpecification(raceId, raceRunId);

        // Act
        var result = ApplySpecification(raceRuns, spec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("First", result.HorseResults[0].HorseName);
        Assert.Equal("Second", result.HorseResults[1].HorseName);
        Assert.Equal("Third", result.HorseResults[2].HorseName);
    }

    [Fact]
    public void Filters_PlayByPlay_To_Non_Empty_Notes_Ordered_By_Tick()
    {
        // Arrange
        var raceId = (byte)1;
        var raceRunId = Guid.NewGuid();

        var race = CreateMinimalRace(raceId);
        var raceRun = new RaceRun
        {
            Id = raceRunId,
            RaceId = raceId,
            Race = race,
            ConditionId = ConditionId.Fast,
            Purse = 50000,
            Horses = new List<RaceRunHorse>(),
            RaceRunTicks = new List<RaceRunTick>
            {
                new() { Tick = 3, Note = "Final stretch!" },
                new() { Tick = 1, Note = "They're off!" },
                new() { Tick = 2, Note = "" }, // Empty - should be filtered
                new() { Tick = 4, Note = null! }, // Null - should be filtered
                new() { Tick = 5, Note = "   " }, // Whitespace - should be filtered
                new() { Tick = 6, Note = "Photo finish!" }
            }
        };

        var raceRuns = new List<RaceRun> { raceRun };
        var spec = new RaceRunDetailSpecification(raceId, raceRunId);

        // Act
        var result = ApplySpecification(raceRuns, spec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.PlayByPlay.Count);
        Assert.Equal("They're off!", result.PlayByPlay[0]);
        Assert.Equal("Final stretch!", result.PlayByPlay[1]);
        Assert.Equal("Photo finish!", result.PlayByPlay[2]);
    }

    [Fact]
    public void Filters_By_RaceId_And_RaceRunId()
    {
        // Arrange
        var targetRaceId = (byte)1;
        var targetRaceRunId = Guid.NewGuid();
        var otherRaceRunId = Guid.NewGuid();

        var race1 = CreateMinimalRace(1);
        var race2 = CreateMinimalRace(2);

        var raceRuns = new List<RaceRun>
        {
            new() { Id = targetRaceRunId, RaceId = 1, Race = race1, Horses = new List<RaceRunHorse>(), RaceRunTicks = new List<RaceRunTick>() },
            new() { Id = otherRaceRunId, RaceId = 1, Race = race1, Horses = new List<RaceRunHorse>(), RaceRunTicks = new List<RaceRunTick>() },
            new() { Id = Guid.NewGuid(), RaceId = 2, Race = race2, Horses = new List<RaceRunHorse>(), RaceRunTicks = new List<RaceRunTick>() }
        };

        var spec = new RaceRunDetailSpecification(targetRaceId, targetRaceRunId);

        // Act
        var filteredRuns = raceRuns.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        Assert.Single(filteredRuns);
        Assert.Equal(targetRaceRunId, filteredRuns[0].Id);
        Assert.Equal(targetRaceId, filteredRuns[0].RaceId);
    }

    private static RaceRunResult? ApplySpecification(List<RaceRun> raceRuns, RaceRunDetailSpecification spec)
    {
        // Apply where clause
        IQueryable<RaceRun> query = raceRuns.AsQueryable();
        foreach (var whereExpression in spec.WhereExpressions.ToList())
        {
            query = query.Where(whereExpression.Filter);
        }

        // Apply projection
        if (spec.Selector != null)
        {
            return query.Select(spec.Selector).FirstOrDefault();
        }

        return null;
    }

    private static Race CreateMinimalRace(byte raceId)
    {
        return new Race
        {
            Id = raceId,
            Name = "Test Race",
            Track = new Track { Id = TrackId.TripleSpires, Name = "Test Track" },
            Surface = new Surface { Id = SurfaceId.Dirt, Name = "Dirt" },
            Furlongs = 8.0m
        };
    }
}
