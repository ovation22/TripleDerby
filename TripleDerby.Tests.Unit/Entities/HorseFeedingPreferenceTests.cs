using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for HorseFeedingPreference entity (Feature 022 - Phase 3)
/// Stores discovered feeding preferences per horse per feed type
/// </summary>
public class HorseFeedingPreferenceTests
{
    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_SetBasicProperties_Succeeds()
    {
        // Arrange
        var preferenceId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var feedingId = (byte)5;

        // Act
        var preference = new HorseFeedingPreference
        {
            Id = preferenceId,
            HorseId = horseId,
            FeedingId = feedingId,
            Preference = FeedResponse.Favorite
        };

        // Assert
        Assert.Equal(preferenceId, preference.Id);
        Assert.Equal(horseId, preference.HorseId);
        Assert.Equal(feedingId, preference.FeedingId);
        Assert.Equal(FeedResponse.Favorite, preference.Preference);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_SetDiscoveredDate_Succeeds()
    {
        // Arrange
        var discoveredDate = DateTime.UtcNow;

        // Act
        var preference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = Guid.NewGuid(),
            FeedingId = 1,
            Preference = FeedResponse.Liked,
            DiscoveredDate = discoveredDate
        };

        // Assert
        Assert.Equal(discoveredDate, preference.DiscoveredDate);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_AllPreferenceLevels_CanBeSet()
    {
        // Arrange & Act
        var favorite = new HorseFeedingPreference { Preference = FeedResponse.Favorite };
        var liked = new HorseFeedingPreference { Preference = FeedResponse.Liked };
        var neutral = new HorseFeedingPreference { Preference = FeedResponse.Neutral };
        var disliked = new HorseFeedingPreference { Preference = FeedResponse.Disliked };
        var hated = new HorseFeedingPreference { Preference = FeedResponse.Hated };
        var rejected = new HorseFeedingPreference { Preference = FeedResponse.Rejected };

        // Assert
        Assert.Equal(FeedResponse.Favorite, favorite.Preference);
        Assert.Equal(FeedResponse.Liked, liked.Preference);
        Assert.Equal(FeedResponse.Neutral, neutral.Preference);
        Assert.Equal(FeedResponse.Disliked, disliked.Preference);
        Assert.Equal(FeedResponse.Hated, hated.Preference);
        Assert.Equal(FeedResponse.Rejected, rejected.Preference);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_UniqueHorseFeedingCombination_CanBeStored()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var feeding1Id = (byte)1;
        var feeding2Id = (byte)2;

        // Act - Same horse, different feeds
        var pref1 = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horseId,
            FeedingId = feeding1Id,
            Preference = FeedResponse.Favorite,
            DiscoveredDate = DateTime.UtcNow
        };

        var pref2 = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horseId,
            FeedingId = feeding2Id,
            Preference = FeedResponse.Hated,
            DiscoveredDate = DateTime.UtcNow
        };

        // Assert - Different preference IDs but same horse
        Assert.NotEqual(pref1.Id, pref2.Id);
        Assert.Equal(pref1.HorseId, pref2.HorseId);
        Assert.NotEqual(pref1.FeedingId, pref2.FeedingId);
        Assert.NotEqual(pref1.Preference, pref2.Preference);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_DifferentHorsesSameFeeding_CanHaveDifferentPreferences()
    {
        // Arrange
        var horse1Id = Guid.NewGuid();
        var horse2Id = Guid.NewGuid();
        var feedingId = (byte)3;

        // Act - Different horses, same feed
        var pref1 = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse1Id,
            FeedingId = feedingId,
            Preference = FeedResponse.Favorite,
            DiscoveredDate = DateTime.UtcNow
        };

        var pref2 = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse2Id,
            FeedingId = feedingId,
            Preference = FeedResponse.Hated,
            DiscoveredDate = DateTime.UtcNow
        };

        // Assert - Same feeding, different horses have different preferences
        Assert.Equal(pref1.FeedingId, pref2.FeedingId);
        Assert.NotEqual(pref1.HorseId, pref2.HorseId);
        Assert.NotEqual(pref1.Preference, pref2.Preference);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_NavigationProperties_CanBeSet()
    {
        // Arrange
        var horse = new Horse { Id = Guid.NewGuid(), Name = "Thunder Bolt" };
        var feeding = new Feeding { Id = 1, Name = "Sugar Cubes" };

        // Act
        var preference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            Horse = horse,
            FeedingId = feeding.Id,
            Feeding = feeding,
            Preference = FeedResponse.Favorite,
            DiscoveredDate = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(preference.Horse);
        Assert.Equal("Thunder Bolt", preference.Horse.Name);
        Assert.NotNull(preference.Feeding);
        Assert.Equal("Sugar Cubes", preference.Feeding.Name);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_DiscoveryScenario_FirstTimeFeeding()
    {
        // Arrange - Horse tries a new feed for the first time
        var horseId = Guid.NewGuid();
        var feedingId = (byte)7;
        var discoveryTime = DateTime.UtcNow;

        // Act - Preference is discovered and stored
        var preference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horseId,
            FeedingId = feedingId,
            Preference = FeedResponse.Liked,
            DiscoveredDate = discoveryTime
        };

        // Assert
        Assert.Equal(FeedResponse.Liked, preference.Preference);
        Assert.Equal(discoveryTime, preference.DiscoveredDate);
        Assert.Equal(horseId, preference.HorseId);
        Assert.Equal(feedingId, preference.FeedingId);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void HorseFeedingPreference_AllProperties_CanBeSet()
    {
        // Arrange
        var preferenceId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var feedingId = (byte)12;
        var discoveredDate = DateTime.UtcNow;

        // Act
        var preference = new HorseFeedingPreference
        {
            Id = preferenceId,
            HorseId = horseId,
            FeedingId = feedingId,
            Preference = FeedResponse.Disliked,
            DiscoveredDate = discoveredDate
        };

        // Assert
        Assert.Equal(preferenceId, preference.Id);
        Assert.Equal(horseId, preference.HorseId);
        Assert.Equal(feedingId, preference.FeedingId);
        Assert.Equal(FeedResponse.Disliked, preference.Preference);
        Assert.Equal(discoveredDate, preference.DiscoveredDate);
    }
}
