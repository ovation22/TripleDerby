using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;

namespace TripleDerby.Tests.Unit.Specifications;

public class SimilarRaceStartsSpecificationTests
{
    // Racers system user owns CPU horses
    private static readonly Guid RacersOwnerId = new("72115894-88CD-433E-9892-CAC22E335F1D");
    private static readonly Guid PlayerOwnerId = Guid.NewGuid();

    [Fact]
    public void Filters_Horses_Within_Tolerance_Of_Target_RaceStarts()
    {
        // Arrange
        var horses = new List<Horse>
        {
            CreateHorse(RacersOwnerId, raceStarts: 4), // Within tolerance (5 ± 2)
            CreateHorse(RacersOwnerId, raceStarts: 6), // Within tolerance
            CreateHorse(RacersOwnerId, raceStarts: 7), // Within tolerance
            CreateHorse(RacersOwnerId, raceStarts: 10), // Outside tolerance
            CreateHorse(RacersOwnerId, raceStarts: 0),  // Outside tolerance
        };

        var spec = new SimilarRaceStartsSpecification(
            targetRaceStarts: 5,
            tolerance: 2,
            limit: 11);

        // Act
        var result = ApplySpecification(horses, spec);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, h => h.RaceStarts == 10);
        Assert.DoesNotContain(result, h => h.RaceStarts == 0);
    }

    [Fact]
    public void Filters_Only_Racers_Owned_Horses()
    {
        // Arrange
        var horses = new List<Horse>
        {
            CreateHorse(RacersOwnerId, raceStarts: 5),  // Racers owned - should be included
            CreateHorse(PlayerOwnerId, raceStarts: 5), // Player owned - should be excluded
        };

        var spec = new SimilarRaceStartsSpecification(
            targetRaceStarts: 5,
            tolerance: 2,
            limit: 11);

        // Act
        var result = ApplySpecification(horses, spec);

        // Assert
        Assert.Single(result);
        Assert.Equal(RacersOwnerId, result.First().OwnerId);
    }

    [Fact]
    public void Excludes_Retired_Horses()
    {
        // Arrange
        var horses = new List<Horse>
        {
            CreateHorse(RacersOwnerId, raceStarts: 5, isRetired: false),
            CreateHorse(RacersOwnerId, raceStarts: 5, isRetired: true), // Retired - should be excluded
        };

        var spec = new SimilarRaceStartsSpecification(
            targetRaceStarts: 5,
            tolerance: 2,
            limit: 11);

        // Act
        var result = ApplySpecification(horses, spec);

        // Assert
        Assert.Single(result);
        Assert.False(result.First().IsRetired);
    }

    [Fact]
    public void Limits_Results_To_Specified_Count()
    {
        // Arrange
        var spec = new SimilarRaceStartsSpecification(
            targetRaceStarts: 5,
            tolerance: 2,
            limit: 7);

        // Assert
        Assert.Equal(7, spec.Take);
    }

    private static List<Horse> ApplySpecification(List<Horse> horses, SimilarRaceStartsSpecification spec)
    {
        IQueryable<Horse> query = horses.AsQueryable();
        foreach (var whereExpression in spec.WhereExpressions)
        {
            query = query.Where(whereExpression.Filter);
        }
        return query.ToList();
    }

    private static Horse CreateHorse(Guid ownerId, short raceStarts, bool isRetired = false)
    {
        var id = Guid.NewGuid();
        return new Horse
        {
            Id = id,
            Name = $"Horse-{id.ToString()[..8]}",
            OwnerId = ownerId,
            RaceStarts = raceStarts,
            IsRetired = isRetired,
            Statistics = new List<HorseStatistic>
            {
                new() { Speed = 50, Stamina = 50, Agility = 50, Durability = 50, Happiness = 50 }
            }
        };
    }
}
