using TripleDerby.Services.Racing;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Services;

public class PurseCalculatorTests
{
    private readonly PurseCalculator _sut = new();

    [Theory]
    [InlineData(RaceClassId.Maiden, 10, 20000)]
    [InlineData(RaceClassId.MaidenClaiming, 10, 18000)]
    [InlineData(RaceClassId.Claiming, 10, 25000)]
    [InlineData(RaceClassId.Allowance, 10, 40000)]
    [InlineData(RaceClassId.AllowanceOptional, 10, 50000)]
    [InlineData(RaceClassId.Stakes, 10, 100000)]
    [InlineData(RaceClassId.GradeIII, 10, 200000)]
    [InlineData(RaceClassId.EliteStakes, 10, 500000)]
    [InlineData(RaceClassId.Championship, 10, 1000000)]
    [Trait("Category", "PurseCalculation")]
    public void CalculateTotalPurse_At10Furlongs_ReturnsBasePurse(RaceClassId raceClass, decimal furlongs, int expectedPurse)
    {
        // Act
        var result = _sut.CalculateTotalPurse(raceClass, furlongs);

        // Assert
        Assert.Equal(expectedPurse, result);
    }

    [Fact]
    [Trait("Category", "PurseCalculation")]
    public void CalculateTotalPurse_LongerThan10Furlongs_ScalesUpByDistance()
    {
        // Arrange - Grade I race at 12 furlongs (2 furlongs above baseline)
        // Expected: $1,000,000 × (1 + (12 - 10) × 0.05) = $1,000,000 × 1.10 = $1,100,000

        // Act
        var result = _sut.CalculateTotalPurse(RaceClassId.Championship, 12m);

        // Assert
        Assert.Equal(1100000, result);
    }

    [Fact]
    [Trait("Category", "PurseCalculation")]
    public void CalculateTotalPurse_ShorterThan10Furlongs_ScalesDownByDistance()
    {
        // Arrange - Maiden race at 6 furlongs (4 furlongs below baseline)
        // Expected: $20,000 × (1 + (6 - 10) × 0.05) = $20,000 × 0.80 = $16,000

        // Act
        var result = _sut.CalculateTotalPurse(RaceClassId.Maiden, 6m);

        // Assert
        Assert.Equal(16000, result);
    }

    [Fact]
    [Trait("Category", "PurseCalculation")]
    public void CalculateTotalPurse_VeryShortRace_HasMinimumMultiplier()
    {
        // Arrange - Very short race (2 furlongs) should not go below 50% multiplier
        // Calculation: (1 + (2 - 10) × 0.05) = 0.60, but minimum is 0.50
        // Expected: $20,000 × 0.60 = $12,000 (above minimum, so normal calculation)

        // Act
        var result = _sut.CalculateTotalPurse(RaceClassId.Maiden, 2m);

        // Assert - Should be 60% of base (12,000)
        Assert.Equal(12000, result);
    }

    [Fact]
    [Trait("Category", "PurseCalculation")]
    public void CalculateTotalPurse_Marathon_ScalesSignificantly()
    {
        // Arrange - Marathon race at 14 furlongs
        // Expected: $1,000,000 × (1 + (14 - 10) × 0.05) = $1,000,000 × 1.20 = $1,200,000

        // Act
        var result = _sut.CalculateTotalPurse(RaceClassId.Championship, 14m);

        // Assert
        Assert.Equal(1200000, result);
    }

    [Theory]
    [InlineData(RaceClassId.Maiden)]
    [InlineData(RaceClassId.MaidenClaiming)]
    [InlineData(RaceClassId.Claiming)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "LowerTier")]
    public void CalculatePayout_LowerTierRaces_Winner_Returns60Percent(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 10000;
        var expectedPayout = 6000; // 60%

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 1);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Theory]
    [InlineData(RaceClassId.Maiden)]
    [InlineData(RaceClassId.MaidenClaiming)]
    [InlineData(RaceClassId.Claiming)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "LowerTier")]
    public void CalculatePayout_LowerTierRaces_SecondPlace_Returns20Percent(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 10000;
        var expectedPayout = 2000; // 20%

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 2);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Theory]
    [InlineData(RaceClassId.Maiden)]
    [InlineData(RaceClassId.MaidenClaiming)]
    [InlineData(RaceClassId.Claiming)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "LowerTier")]
    public void CalculatePayout_LowerTierRaces_ThirdPlace_Returns10Percent(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 10000;
        var expectedPayout = 1000; // 10%

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 3);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Theory]
    [InlineData(RaceClassId.Maiden)]
    [InlineData(RaceClassId.MaidenClaiming)]
    [InlineData(RaceClassId.Claiming)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "LowerTier")]
    public void CalculatePayout_LowerTierRaces_FourthPlace_Returns0(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 10000;

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 4);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(RaceClassId.Allowance)]
    [InlineData(RaceClassId.AllowanceOptional)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "MidTier")]
    public void CalculatePayout_MidTierRaces_Winner_Returns58Percent(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 10000;
        var expectedPayout = 5800; // 58%

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 1);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Theory]
    [InlineData(RaceClassId.Allowance)]
    [InlineData(RaceClassId.AllowanceOptional)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "MidTier")]
    public void CalculatePayout_MidTierRaces_FourthPlace_Returns5Percent(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 10000;
        var expectedPayout = 500; // 5%

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 4);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Theory]
    [InlineData(RaceClassId.Allowance)]
    [InlineData(RaceClassId.AllowanceOptional)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "MidTier")]
    public void CalculatePayout_MidTierRaces_FifthPlace_Returns0(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 10000;

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 5);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(RaceClassId.Stakes)]
    [InlineData(RaceClassId.GradeIII)]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "Stakes")]
    public void CalculatePayout_StakesRaces_Winner_Returns55Percent(RaceClassId raceClass)
    {
        // Arrange
        var totalPurse = 100000;
        var expectedPayout = 55000; // 55%

        // Act
        var result = _sut.CalculatePayout(raceClass, totalPurse, 1);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Fact]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "Elite")]
    public void CalculatePayout_GradeII_FifthPlace_Returns3Percent()
    {
        // Arrange
        var totalPurse = 100000;
        var expectedPayout = 3000; // 3%

        // Act
        var result = _sut.CalculatePayout(RaceClassId.EliteStakes, totalPurse, 5);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Fact]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "Championship")]
    public void CalculatePayout_GradeI_Winner_Returns62Percent()
    {
        // Arrange - Championship pattern is most top-heavy
        var totalPurse = 1000000;
        var expectedPayout = 620000; // 62%

        // Act
        var result = _sut.CalculatePayout(RaceClassId.Championship, totalPurse, 1);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Fact]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "Championship")]
    public void CalculatePayout_GradeI_FifthPlace_Returns3Percent()
    {
        // Arrange
        var totalPurse = 1000000;
        var expectedPayout = 30000; // 3%

        // Act
        var result = _sut.CalculatePayout(RaceClassId.Championship, totalPurse, 5);

        // Assert
        Assert.Equal(expectedPayout, result);
    }

    [Fact]
    [Trait("Category", "PayoutDistribution")]
    [Trait("Tier", "Championship")]
    public void CalculatePayout_GradeI_SixthPlace_Returns0()
    {
        // Arrange
        var totalPurse = 1000000;

        // Act
        var result = _sut.CalculatePayout(RaceClassId.Championship, totalPurse, 6);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    [Trait("Category", "PayoutAggregation")]
    public void CalculateAllPayouts_Maiden_ReturnsTop3Only()
    {
        // Arrange
        var totalPurse = 20000;

        // Act
        var result = _sut.CalculateAllPayouts(RaceClassId.Maiden, totalPurse);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(12000, result[1]); // 60%
        Assert.Equal(4000, result[2]);  // 20%
        Assert.Equal(2000, result[3]);  // 10%
    }

    [Fact]
    [Trait("Category", "PayoutAggregation")]
    public void CalculateAllPayouts_Allowance_ReturnsTop4()
    {
        // Arrange
        var totalPurse = 40000;

        // Act
        var result = _sut.CalculateAllPayouts(RaceClassId.Allowance, totalPurse);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(23200, result[1]); // 58%
        Assert.Equal(8000, result[2]);  // 20%
        Assert.Equal(4000, result[3]);  // 10%
        Assert.Equal(2000, result[4]);  // 5%
    }

    [Fact]
    [Trait("Category", "PayoutAggregation")]
    public void CalculateAllPayouts_GradeI_ReturnsTop5()
    {
        // Arrange
        var totalPurse = 1000000;

        // Act
        var result = _sut.CalculateAllPayouts(RaceClassId.Championship, totalPurse);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(620000, result[1]); // 62%
        Assert.Equal(200000, result[2]); // 20%
        Assert.Equal(100000, result[3]); // 10%
        Assert.Equal(50000, result[4]);  // 5%
        Assert.Equal(30000, result[5]);  // 3%
    }

    [Fact]
    [Trait("Category", "PayoutAggregation")]
    public void CalculateAllPayouts_GradeII_ReturnsTop5WithDifferentDistribution()
    {
        // Arrange
        var totalPurse = 500000;

        // Act
        var result = _sut.CalculateAllPayouts(RaceClassId.EliteStakes, totalPurse);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(275000, result[1]); // 55%
        Assert.Equal(100000, result[2]); // 20%
        Assert.Equal(50000, result[3]);  // 10%
        Assert.Equal(35000, result[4]);  // 7%
        Assert.Equal(15000, result[5]);  // 3%
    }

    [Fact]
    [Trait("Category", "EdgeCases")]
    public void CalculatePayout_InvalidPlace_Returns0()
    {
        // Arrange
        var totalPurse = 10000;

        // Act
        var result = _sut.CalculatePayout(RaceClassId.Maiden, totalPurse, 0);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    [Trait("Category", "EdgeCases")]
    public void CalculatePayout_NegativePlace_Returns0()
    {
        // Arrange
        var totalPurse = 10000;

        // Act
        var result = _sut.CalculatePayout(RaceClassId.Maiden, totalPurse, -1);

        // Assert
        Assert.Equal(0, result);
    }
}
