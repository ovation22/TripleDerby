using Moq;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Unit tests for Feature 007 - Overtaking and Lane Changes (Phase 1)
/// Tests cover:
/// - Cooldown calculations (agility-based)
/// - Overtaking threshold calculations (speed + phase)
/// - Lane clearance checks (asymmetric)
/// - Desired lane determination (RailRunner only in Phase 1)
/// </summary>
public class OvertakingLaneChangeTests
{
    // ============================================================================
    // Cooldown Calculation Tests
    // ============================================================================

    [Fact]
    public void Cooldown_WithAgility0_ShouldBe10Ticks()
    {
        // Arrange - Agility 0: 10 - (0 × 0.1) = 10 ticks
        var expectedCooldown = RaceModifierConfig.BaseLaneChangeCooldown -
                              (0 * RaceModifierConfig.AgilityCooldownReduction);

        // Assert
        Assert.Equal(10.0, expectedCooldown, precision: 5);
    }

    [Fact]
    public void Cooldown_WithAgility50_ShouldBe6Ticks()
    {
        // Arrange - Agility 50: 10 - (50 × 0.08) = 6 ticks (tuned in Phase 3 from 5)
        var expectedCooldown = RaceModifierConfig.BaseLaneChangeCooldown -
                              (50 * RaceModifierConfig.AgilityCooldownReduction);

        // Assert
        Assert.Equal(6.0, expectedCooldown, precision: 5);
    }

    [Fact]
    public void Cooldown_WithAgility100_ShouldBe2Ticks()
    {
        // Arrange - Agility 100: 10 - (100 × 0.08) = 2 ticks (tuned in Phase 3 from 0)
        var expectedCooldown = RaceModifierConfig.BaseLaneChangeCooldown -
                              (100 * RaceModifierConfig.AgilityCooldownReduction);

        // Assert
        Assert.Equal(2.0, expectedCooldown, precision: 5);
    }

    [Fact]
    public void Cooldown_WithAgility75_ShouldBe4Ticks()
    {
        // Arrange - Agility 75: 10 - (75 × 0.08) = 4 ticks (tuned in Phase 3 from 2.5)
        var expectedCooldown = RaceModifierConfig.BaseLaneChangeCooldown -
                              (75 * RaceModifierConfig.AgilityCooldownReduction);

        // Assert
        Assert.Equal(4.0, expectedCooldown, precision: 5);
    }

    // ============================================================================
    // Overtaking Threshold Calculation Tests
    // ============================================================================

    [Fact]
    public void OvertakingThreshold_WithSpeed50_EarlyRace_ShouldBe0Point275()
    {
        // Arrange - Speed 50, early race (tick 50/200 = 25%)
        // Threshold = 0.25 × (1.0 + 50×0.002) × 1.0 = 0.25 × 1.1 = 0.275
        var horse = CreateHorseWithStats(speed: 50);
        var raceRunHorse = CreateRaceRunHorse(horse);

        // Act - Early race phase
        var threshold = CalculateOvertakingThreshold(raceRunHorse, currentTick: 50, totalTicks: 200);

        // Assert
        Assert.Equal(0.275m, threshold, precision: 3);
    }

    [Fact]
    public void OvertakingThreshold_WithSpeed50_LateRace_ShouldBe0Point4125()
    {
        // Arrange - Speed 50, late race (tick 170/200 = 85% > 75%)
        // Threshold = 0.25 × (1.0 + 50×0.002) × 1.5 = 0.25 × 1.1 × 1.5 = 0.4125
        var horse = CreateHorseWithStats(speed: 50);
        var raceRunHorse = CreateRaceRunHorse(horse);

        // Act - Late race phase (triggers 1.5x multiplier)
        var threshold = CalculateOvertakingThreshold(raceRunHorse, currentTick: 170, totalTicks: 200);

        // Assert
        Assert.Equal(0.4125m, threshold, precision: 4);
    }

    [Fact]
    public void OvertakingThreshold_WithSpeed0_EarlyRace_ShouldBe0Point25()
    {
        // Arrange - Speed 0, early race
        // Threshold = 0.25 × (1.0 + 0×0.002) × 1.0 = 0.25
        var horse = CreateHorseWithStats(speed: 0);
        var raceRunHorse = CreateRaceRunHorse(horse);

        // Act
        var threshold = CalculateOvertakingThreshold(raceRunHorse, currentTick: 50, totalTicks: 200);

        // Assert - Base threshold with no speed bonus
        Assert.Equal(0.25m, threshold, precision: 3);
    }

    [Fact]
    public void OvertakingThreshold_WithSpeed100_LateRace_ShouldBe0Point45()
    {
        // Arrange - Speed 100, late race (maximum threshold)
        // Threshold = 0.25 × (1.0 + 100×0.002) × 1.5 = 0.25 × 1.2 × 1.5 = 0.45
        var horse = CreateHorseWithStats(speed: 100);
        var raceRunHorse = CreateRaceRunHorse(horse);

        // Act - Late race with maximum speed
        var threshold = CalculateOvertakingThreshold(raceRunHorse, currentTick: 180, totalTicks: 200);

        // Assert - Maximum threshold
        Assert.Equal(0.45m, threshold, precision: 3);
    }

    [Fact]
    public void OvertakingThreshold_AtExact75Percent_ShouldUseEarlyMultiplier()
    {
        // Arrange - Exactly at 75% boundary (tick 150/200 = 0.75)
        // Logic: raceProgress > 0.75 means 76%+, so 75% exactly is NOT late race
        var horse = CreateHorseWithStats(speed: 50);
        var raceRunHorse = CreateRaceRunHorse(horse);

        // Act
        var threshold = CalculateOvertakingThreshold(raceRunHorse, currentTick: 150, totalTicks: 200);

        // Assert - Should use 1.0x multiplier, not 1.5x
        Assert.Equal(0.275m, threshold, precision: 3); // 0.25 × 1.1 × 1.0
    }

    [Fact]
    public void OvertakingThreshold_AtExact76Percent_ShouldUseLateMultiplier()
    {
        // Arrange - Just past 75% boundary (tick 152/200 = 0.76)
        var horse = CreateHorseWithStats(speed: 50);
        var raceRunHorse = CreateRaceRunHorse(horse);

        // Act
        var threshold = CalculateOvertakingThreshold(raceRunHorse, currentTick: 152, totalTicks: 200);

        // Assert - Should use 1.5x multiplier
        Assert.Equal(0.4125m, threshold, precision: 4); // 0.25 × 1.1 × 1.5
    }

    // ============================================================================
    // Lane Clearance Check Tests (Asymmetric)
    // ============================================================================

    [Fact]
    public void IsLaneClear_WithNobodyInTargetLane_ShouldReturnTrue()
    {
        // Arrange - Target lane is completely empty
        var horse1 = CreateHorseWithStats(speed: 50);
        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);

        var raceRun = CreateRaceRun(raceRunHorse1);

        // Act - Check if lane 2 is clear (nobody there)
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert
        Assert.True(isClear);
    }

    [Fact]
    public void IsLaneClear_WithHorseFarAhead_ShouldReturnTrue()
    {
        // Arrange - Horse 1 at 5.0f checking lane 2, Horse 2 at 5.5f in lane 2
        // Distance ahead: 5.5 - 5.0 = 0.5f (>= 0.2f threshold)
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorse2 = CreateRaceRunHorse(horse2, lane: 2, distance: 5.5m);

        var raceRun = CreateRaceRun(raceRunHorse1, raceRunHorse2);

        // Act
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert - Far enough ahead, lane is clear
        Assert.True(isClear);
    }

    [Fact]
    public void IsLaneClear_WithHorseTooCloseAhead_ShouldReturnFalse()
    {
        // Arrange - Horse 1 at 5.0f checking lane 2, Horse 2 at 5.15f in lane 2
        // Distance ahead: 5.15 - 5.0 = 0.15f (< 0.2f threshold)
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorse2 = CreateRaceRunHorse(horse2, lane: 2, distance: 5.15m);

        var raceRun = CreateRaceRun(raceRunHorse1, raceRunHorse2);

        // Act
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert - Too close ahead, lane blocked
        Assert.False(isClear);
    }

    [Fact]
    public void IsLaneClear_WithHorseFarBehind_ShouldReturnTrue()
    {
        // Arrange - Horse 1 at 5.0f checking lane 2, Horse 2 at 4.85f in lane 2
        // Distance behind: 5.0 - 4.85 = 0.15f (>= 0.1f threshold)
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorse2 = CreateRaceRunHorse(horse2, lane: 2, distance: 4.85m);

        var raceRun = CreateRaceRun(raceRunHorse1, raceRunHorse2);

        // Act
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert - Far enough behind, lane is clear
        Assert.True(isClear);
    }

    [Fact]
    public void IsLaneClear_WithHorseTooCloseBehind_ShouldReturnFalse()
    {
        // Arrange - Horse 1 at 5.0f checking lane 2, Horse 2 at 4.95f in lane 2
        // Distance behind: 5.0 - 4.95 = 0.05f (< 0.1f threshold)
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorse2 = CreateRaceRunHorse(horse2, lane: 2, distance: 4.95m);

        var raceRun = CreateRaceRun(raceRunHorse1, raceRunHorse2);

        // Act
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert - Too close behind (would cut off), lane blocked
        Assert.False(isClear);
    }

    [Fact]
    public void IsLaneClear_AsymmetricThreshold_AheadRequiresMoreSpace()
    {
        // Arrange - Demonstrate asymmetry: 0.15f ahead blocks, 0.15f behind is OK
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var horse3 = CreateHorseWithStats(speed: 50);

        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorseAhead = CreateRaceRunHorse(horse2, lane: 2, distance: 5.15m);
        var raceRunHorseBehind = CreateRaceRunHorse(horse3, lane: 3, distance: 4.85m);

        var raceRunAhead = CreateRaceRun(raceRunHorse1, raceRunHorseAhead);
        var raceRunBehind = CreateRaceRun(raceRunHorse1, raceRunHorseBehind);

        // Act
        var isClearAhead = IsLaneClear(raceRunHorse1, targetLane: 2, raceRunAhead);
        var isClearBehind = IsLaneClear(raceRunHorse1, targetLane: 3, raceRunBehind);

        // Assert - 0.15f ahead blocks (< 0.2f), but 0.15f behind is OK (>= 0.1f)
        Assert.False(isClearAhead);
        Assert.True(isClearBehind);
    }

    [Fact]
    public void IsLaneClear_ExactlyAtAheadThreshold_ShouldReturnTrue()
    {
        // Arrange - Exactly 0.2f ahead (boundary test)
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorse2 = CreateRaceRunHorse(horse2, lane: 2, distance: 5.2m);

        var raceRun = CreateRaceRun(raceRunHorse1, raceRunHorse2);

        // Act
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert - Exactly at threshold is clear (< is exclusive)
        Assert.True(isClear);
    }

    [Fact]
    public void IsLaneClear_ExactlyAtBehindThreshold_ShouldReturnTrue()
    {
        // Arrange - Exactly 0.1f behind (boundary test)
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorse2 = CreateRaceRunHorse(horse2, lane: 2, distance: 4.9m);

        var raceRun = CreateRaceRun(raceRunHorse1, raceRunHorse2);

        // Act
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert - Exactly at threshold is clear (< is exclusive)
        Assert.True(isClear);
    }

    [Fact]
    public void IsLaneClear_MultipleHorsesInTargetLane_ShouldCheckAll()
    {
        // Arrange - Lane 2 has multiple horses, one is too close
        var horse1 = CreateHorseWithStats(speed: 50);
        var horse2 = CreateHorseWithStats(speed: 50);
        var horse3 = CreateHorseWithStats(speed: 50);

        var raceRunHorse1 = CreateRaceRunHorse(horse1, lane: 1, distance: 5.0m);
        var raceRunHorse2 = CreateRaceRunHorse(horse2, lane: 2, distance: 5.6m);  // Far ahead - OK
        var raceRunHorse3 = CreateRaceRunHorse(horse3, lane: 2, distance: 5.15m); // Too close - blocks

        var raceRun = CreateRaceRun(raceRunHorse1, raceRunHorse2, raceRunHorse3);

        // Act
        var isClear = IsLaneClear(raceRunHorse1, targetLane: 2, raceRun);

        // Assert - Blocked by the closer horse
        Assert.False(isClear);
    }

    // ============================================================================
    // Desired Lane Determination Tests (Phase 1: RailRunner Only)
    // ============================================================================

    [Fact]
    public void DetermineDesiredLane_RailRunner_ShouldReturnLane1()
    {
        // Arrange - RailRunner always wants lane 1
        var horse = CreateHorseWithStats(speed: 50, legType: LegTypeId.RailRunner);
        var raceRunHorse = CreateRaceRunHorse(horse, lane: 5); // Currently in lane 5

        // Act
        var desiredLane = DetermineDesiredLane(raceRunHorse);

        // Assert
        Assert.Equal(1, desiredLane);
    }

    [Fact]
    public void DetermineDesiredLane_FrontRunner_ShouldStayInCurrentLane()
    {
        // Arrange - Phase 1: FrontRunner stays in current lane
        var horse = CreateHorseWithStats(speed: 50, legType: LegTypeId.FrontRunner);
        var raceRunHorse = CreateRaceRunHorse(horse, lane: 3);

        // Act
        var desiredLane = DetermineDesiredLane(raceRunHorse);

        // Assert - Phase 1 behavior: stay in current lane
        Assert.Equal(3, desiredLane);
    }

    [Fact]
    public void DetermineDesiredLane_StartDash_ShouldStayInCurrentLane()
    {
        // Arrange - Phase 1: StartDash stays in current lane
        var horse = CreateHorseWithStats(speed: 50, legType: LegTypeId.StartDash);
        var raceRunHorse = CreateRaceRunHorse(horse, lane: 7);

        // Act
        var desiredLane = DetermineDesiredLane(raceRunHorse);

        // Assert - Phase 1 behavior: stay in current lane
        Assert.Equal(7, desiredLane);
    }

    [Fact]
    public void DetermineDesiredLane_LastSpurt_ShouldStayInCurrentLane()
    {
        // Arrange - Phase 1: LastSpurt stays in current lane
        var horse = CreateHorseWithStats(speed: 50, legType: LegTypeId.LastSpurt);
        var raceRunHorse = CreateRaceRunHorse(horse, lane: 2);

        // Act
        var desiredLane = DetermineDesiredLane(raceRunHorse);

        // Assert - Phase 1 behavior: stay in current lane
        Assert.Equal(2, desiredLane);
    }

    [Fact]
    public void DetermineDesiredLane_StretchRunner_ShouldStayInCurrentLane()
    {
        // Arrange - Phase 1: StretchRunner stays in current lane
        var horse = CreateHorseWithStats(speed: 50, legType: LegTypeId.StretchRunner);
        var raceRunHorse = CreateRaceRunHorse(horse, lane: 4);

        // Act
        var desiredLane = DetermineDesiredLane(raceRunHorse);

        // Assert - Phase 1 behavior: stay in current lane
        Assert.Equal(4, desiredLane);
    }

    // ============================================================================
    // Configuration Constant Validation Tests
    // ============================================================================

    [Fact]
    public void Config_OvertakingBaseThreshold_ShouldBe0Point25()
    {
        // Arrange & Assert - Validate configuration constant
        Assert.Equal(0.25m, RaceModifierConfig.OvertakingBaseThreshold);
    }

    [Fact]
    public void Config_OvertakingSpeedFactor_ShouldBe0Point002()
    {
        // Arrange & Assert
        Assert.Equal(0.002, RaceModifierConfig.OvertakingSpeedFactor);
    }

    [Fact]
    public void Config_OvertakingLateRaceMultiplier_ShouldBe1Point5()
    {
        // Arrange & Assert
        Assert.Equal(1.5, RaceModifierConfig.OvertakingLateRaceMultiplier);
    }

    [Fact]
    public void Config_BaseLaneChangeCooldown_ShouldBe10()
    {
        // Arrange & Assert
        Assert.Equal(10, RaceModifierConfig.BaseLaneChangeCooldown);
    }

    [Fact]
    public void Config_AgilityCooldownReduction_ShouldBe0Point08()
    {
        // Arrange & Assert (tuned in Phase 3 from 0.1 to 0.08)
        Assert.Equal(0.08, RaceModifierConfig.AgilityCooldownReduction);
    }

    [Fact]
    public void Config_LaneChangeMinClearanceBehind_ShouldBe0Point1()
    {
        // Arrange & Assert
        Assert.Equal(0.1m, RaceModifierConfig.LaneChangeMinClearanceBehind);
    }

    [Fact]
    public void Config_LaneChangeMinClearanceAhead_ShouldBe0Point2()
    {
        // Arrange & Assert
        Assert.Equal(0.2m, RaceModifierConfig.LaneChangeMinClearanceAhead);
    }

    // ============================================================================
    // Helper Methods (Mimic actual implementation logic for unit testing)
    // ============================================================================

    /// <summary>
    /// Mimics RaceService.CalculateOvertakingThreshold for unit testing
    /// </summary>
    private static decimal CalculateOvertakingThreshold(RaceRunHorse horse, short currentTick, short totalTicks)
    {
        var raceProgress = (double)currentTick / totalTicks;

        var phaseMultiplier = raceProgress > 0.75
            ? RaceModifierConfig.OvertakingLateRaceMultiplier
            : 1.0;

        var speedFactor = 1.0 + (horse.Horse.Speed * RaceModifierConfig.OvertakingSpeedFactor);

        return RaceModifierConfig.OvertakingBaseThreshold * (decimal)(speedFactor * phaseMultiplier);
    }

    /// <summary>
    /// Mimics RaceService.IsLaneClear for unit testing
    /// </summary>
    private static bool IsLaneClear(RaceRunHorse horse, int targetLane, RaceRun raceRun)
    {
        return !raceRun.Horses.Any(h =>
            h != horse &&
            h.Lane == targetLane &&
            (
                // Horse behind us - prevent cutting off
                (horse.Distance - h.Distance < RaceModifierConfig.LaneChangeMinClearanceBehind &&
                 h.Distance < horse.Distance) ||

                // Horse ahead of us - prevent collisions
                (h.Distance - horse.Distance < RaceModifierConfig.LaneChangeMinClearanceAhead &&
                 h.Distance > horse.Distance)
            )
        );
    }

    /// <summary>
    /// Mimics RaceService.DetermineDesiredLane for unit testing (Phase 1)
    /// </summary>
    private static int DetermineDesiredLane(RaceRunHorse horse)
    {
        return horse.Horse.LegTypeId switch
        {
            LegTypeId.RailRunner => 1,  // Always seek the rail
            _ => horse.Lane              // Phase 1: All others stay in current lane
        };
    }

    // ============================================================================
    // Test Data Creation Helpers
    // ============================================================================

    private static Horse CreateHorseWithStats(byte speed, byte agility = 50, LegTypeId legType = LegTypeId.FrontRunner)
    {
        return new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Test Horse",
            LegTypeId = legType,
            Statistics = new List<HorseStatistic>
            {
                new()
                {
                    StatisticId = StatisticId.Speed,
                    Actual = speed
                },
                new()
                {
                    StatisticId = StatisticId.Agility,
                    Actual = agility
                },
                new()
                {
                    StatisticId = StatisticId.Stamina,
                    Actual = 50
                },
                new()
                {
                    StatisticId = StatisticId.Durability,
                    Actual = 50
                },
                new()
                {
                    StatisticId = StatisticId.Happiness,
                    Actual = 50
                }
            }
        };
    }

    private static RaceRunHorse CreateRaceRunHorse(Horse horse, byte lane = 1, decimal distance = 0m)
    {
        return new RaceRunHorse
        {
            Id = Guid.NewGuid(),
            Horse = horse,
            HorseId = horse.Id,
            Lane = lane,
            Distance = distance,
            InitialStamina = horse.Stamina,
            CurrentStamina = horse.Stamina,
            TicksSinceLastLaneChange = 10, // Start with cooldown elapsed
            SpeedPenaltyTicksRemaining = 0
        };
    }

    private static RaceRun CreateRaceRun(params RaceRunHorse[] horses)
    {
        var race = new Race
        {
            Id = 1,
            Furlongs = 10m,
            SurfaceId = SurfaceId.Dirt
        };

        var raceRun = new RaceRun
        {
            Id = Guid.NewGuid(),
            RaceId = race.Id,
            Race = race,
            ConditionId = ConditionId.Good,
            Horses = new List<RaceRunHorse>(horses)
        };

        return raceRun;
    }
}
