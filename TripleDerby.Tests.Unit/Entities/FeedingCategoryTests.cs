using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for the FeedingCategory lookup entity.
/// </summary>
public class FeedingCategoryTests
{
    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingCategory_HasIdNameDescription_Properties()
    {
        // Arrange & Act
        var category = new FeedingCategory
        {
            Id = FeedingCategoryId.Treats,
            Name = "Treats",
            Description = "High happiness, no stats."
        };

        // Assert
        Assert.Equal(FeedingCategoryId.Treats, category.Id);
        Assert.Equal("Treats", category.Name);
        Assert.Equal("High happiness, no stats.", category.Description);
    }

    [Theory]
    [Trait("Category", "Entity")]
    [InlineData(FeedingCategoryId.Treats, "Treats")]
    [InlineData(FeedingCategoryId.Fruits, "Fruits")]
    [InlineData(FeedingCategoryId.Grains, "Grains")]
    [InlineData(FeedingCategoryId.Proteins, "Proteins")]
    [InlineData(FeedingCategoryId.Supplements, "Supplements")]
    [InlineData(FeedingCategoryId.Premium, "Premium")]
    public void FeedingCategory_CanBeCreatedForAllCategories(FeedingCategoryId id, string expectedName)
    {
        // Arrange & Act
        var category = new FeedingCategory
        {
            Id = id,
            Name = expectedName,
            Description = "Test description"
        };

        // Assert
        Assert.Equal(id, category.Id);
        Assert.Equal(expectedName, category.Name);
    }
}
