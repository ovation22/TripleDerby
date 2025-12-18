using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;

namespace TripleDerby.Tests.Unit.Specifications;

public class SimilarRaceStartsSpecificationTests
{
    [Fact]
    public void Filters_Horses_Within_Tolerance_Of_Target_RaceStarts()
    {
        // Arrange
        var playerHorseId = Guid.NewGuid();
        var horses = new List<Horse>
        {
            CreateHorse(playerHorseId, raceStarts: 5),  // Player's horse - should be excluded
            CreateHorse(Guid.NewGuid(), raceStarts: 4), // Within tolerance (5 ± 2)
            CreateHorse(Guid.NewGuid(), raceStarts: 6), // Within tolerance
            CreateHorse(Guid.NewGuid(), raceStarts: 7), // Within tolerance
            CreateHorse(Guid.NewGuid(), raceStarts: 10), // Outside tolerance
            CreateHorse(Guid.NewGuid(), raceStarts: 0),  // Outside tolerance
        };

        var spec = new SimilarRaceStartsSpecification(
            targetRaceStarts: 5,
            excludeHorseId: playerHorseId,
            tolerance: 2,
            limit: 11);

        // Act
        var result = ApplySpecification(horses, spec);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, h => h.Id == playerHorseId);
        Assert.DoesNotContain(result, h => h.RaceStarts == 10);
        Assert.DoesNotContain(result, h => h.RaceStarts == 0);
    }

    [Fact]
    public void Excludes_Retired_Horses()
    {
        // Arrange
        var playerHorseId = Guid.NewGuid();
        var horses = new List<Horse>
        {
            CreateHorse(Guid.NewGuid(), raceStarts: 5, isRetired: false),
            CreateHorse(Guid.NewGuid(), raceStarts: 5, isRetired: true), // Retired - should be excluded
        };

        var spec = new SimilarRaceStartsSpecification(
            targetRaceStarts: 5,
            excludeHorseId: playerHorseId,
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
        var playerHorseId = Guid.NewGuid();
        var horses = Enumerable.Range(1, 20)
            .Select(i => CreateHorse(Guid.NewGuid(), raceStarts: 5))
            .ToList();

        var spec = new SimilarRaceStartsSpecification(
            targetRaceStarts: 5,
            excludeHorseId: playerHorseId,
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

    private static Horse CreateHorse(Guid id, short raceStarts, bool isRetired = false)
    {
        return new Horse
        {
            Id = id,
            Name = $"Horse-{id.ToString()[..8]}",
            RaceStarts = raceStarts,
            IsRetired = isRetired,
            Statistics = new List<HorseStatistic>
            {
                new() { Speed = 50, Stamina = 50, Agility = 50, Durability = 50, Happiness = 50 }
            }
        };
    }
}
