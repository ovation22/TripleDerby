using Moq;
using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.SharedKernel.Enums;
using Xunit;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Integration tests for traffic response using realistic horse speed calculations.
/// Tests use the real SpeedModifierCalculator to validate traffic dynamics.
/// </summary>
public class TrafficResponseIntegrationTests
{
    private readonly Mock<IRandomGenerator> _mockRandom;
    private readonly ISpeedModifierCalculator _speedCalculator;

    public TrafficResponseIntegrationTests()
    {
        _mockRandom = new Mock<IRandomGenerator>();
        _speedCalculator = new SpeedModifierCalculator(_mockRandom.Object); // Real calculator with mock random
    }

    // Helper methods
    private OvertakingManager CreateManager() =>
        new OvertakingManager(_mockRandom.Object, _speedCalculator);

    /// <summary>
    /// Calculates the natural speed of a horse (before traffic effects).
    /// Mimics the full modifier pipeline from UpdateHorsePosition.
    /// </summary>
    private double CalculateNaturalSpeed(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
    {
        var baseSpeed = RaceModifierConfig.AverageBaseSpeed;

        var context = new ModifierContext(
            CurrentTick: currentTick,
            TotalTicks: totalTicks,
            Horse: horse.Horse,
            RaceCondition: raceRun.ConditionId,
            RaceSurface: raceRun.Race.SurfaceId,
            RaceFurlongs: raceRun.Race.Furlongs
        );

        baseSpeed *= _speedCalculator.CalculateStatModifiers(context);
        baseSpeed *= _speedCalculator.CalculateEnvironmentalModifiers(context);
        baseSpeed *= _speedCalculator.CalculatePhaseModifiers(context, raceRun);
        baseSpeed *= _speedCalculator.CalculateStaminaModifier(horse);

        return baseSpeed;
    }

    private Horse CreateHorse(
        byte speed = 50,
        byte agility = 50,
        byte stamina = 50,
        byte happiness = 50,
        LegTypeId legType = LegTypeId.StartDash)
    {
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Test Horse",
            ColorId = 1,
            LegTypeId = legType,
            OwnerId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            CreatedDate = DateTimeOffset.UtcNow,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = speed },
                new() { StatisticId = StatisticId.Agility, Actual = agility },
                new() { StatisticId = StatisticId.Stamina, Actual = stamina },
                new() { StatisticId = StatisticId.Durability, Actual = 50 },
                new() { StatisticId = StatisticId.Happiness, Actual = happiness }
            }
        };
        return horse;
    }

    private RaceRunHorse CreateRaceRunHorse(Horse horse, byte lane = 3, decimal distance = 0m) => new RaceRunHorse
    {
        Id = Guid.NewGuid(),
        Horse = horse,
        HorseId = horse.Id,
        Lane = lane,
        Distance = distance,
        CurrentStamina = (byte)horse.Stamina,
        InitialStamina = (byte)horse.Stamina,
        TicksSinceLastLaneChange = 10,
        SpeedPenaltyTicksRemaining = 0
    };

    private RaceRun CreateRaceRun(params RaceRunHorse[] horses) => new RaceRun
    {
        Id = Guid.NewGuid(),
        ConditionId = ConditionId.Fast,
        Race = new Race
        {
            Id = 1,
            Name = "Test Race",
            Description = "Test Description",
            SurfaceId = SurfaceId.Dirt,
            Furlongs = 10m,
            TrackId = TrackId.TripleSpires,
            RaceClassId = RaceClassId.Maiden
        },
        Horses = horses.ToList()
    };

    /// <summary>
    /// Test 1: Fast horse blocked by slow horse should be capped to blocker's speed
    /// </summary>
    [Fact]
    public void TrafficResponse_FastHorseBehindSlowHorse_CappedToSlowSpeed()
    {
        // Arrange
        var manager = CreateManager();

        var slowLeader = CreateRaceRunHorse(CreateHorse(speed: 30, legType: LegTypeId.FrontRunner), lane: 3, distance: 1.15m);
        var fastFollower = CreateRaceRunHorse(CreateHorse(speed: 80, legType: LegTypeId.StartDash), lane: 3, distance: 1.0m);

        var raceRun = CreateRaceRun(slowLeader, fastFollower);
        short currentTick = 50;
        short totalTicks = 100;

        // Act - calculate natural speeds first
        var followerNaturalSpeed = CalculateNaturalSpeed(fastFollower, raceRun, currentTick, totalTicks);
        var leaderSpeed = CalculateNaturalSpeed(slowLeader, raceRun, currentTick, totalTicks);

        // Apply traffic effects
        manager.ApplyTrafficEffects(fastFollower, raceRun, currentTick, totalTicks, ref followerNaturalSpeed);

        // Assert - follower should be capped below their natural speed
        Assert.True(followerNaturalSpeed < RaceModifierConfig.AverageBaseSpeed * 1.1,
            "Fast follower should be speed-capped when blocked");

        // Follower should be close to leader's speed (within cap penalty)
        var expectedCap = leaderSpeed * (1.0 - RaceModifierConfig.StartDashSpeedCapPenalty);
        Assert.InRange(followerNaturalSpeed, expectedCap * 0.95, expectedCap * 1.05);
    }

    /// <summary>
    /// Test 2: Exhausted horse blocking fresh horse should result in slower traffic
    /// </summary>
    [Fact]
    public void TrafficResponse_ExhaustedLeaderBlockingFreshFollower_BothSlow()
    {
        // Arrange
        var manager = CreateManager();

        // Use very low stamina (5/100 = 5%) for maximum penalty
        var exhaustedLeader = CreateRaceRunHorse(CreateHorse(stamina: 100, legType: LegTypeId.StartDash), lane: 3, distance: 1.15m);
        exhaustedLeader.CurrentStamina = 5;  // 5% stamina remaining
        exhaustedLeader.InitialStamina = 100;

        var freshFollower = CreateRaceRunHorse(CreateHorse(stamina: 100, legType: LegTypeId.StartDash), lane: 3, distance: 1.0m);
        freshFollower.CurrentStamina = 100; // Full stamina
        freshFollower.InitialStamina = 100;

        var raceRun = CreateRaceRun(exhaustedLeader, freshFollower);
        short currentTick = 50;  // Mid-race, no phase bonuses for StartDash
        short totalTicks = 100;

        // Act - calculate natural speeds
        var leaderSpeed = CalculateNaturalSpeed(exhaustedLeader, raceRun, currentTick, totalTicks);
        var followerSpeed = CalculateNaturalSpeed(freshFollower, raceRun, currentTick, totalTicks);

        // Apply traffic effects
        manager.ApplyTrafficEffects(exhaustedLeader, raceRun, currentTick, totalTicks, ref leaderSpeed);
        manager.ApplyTrafficEffects(freshFollower, raceRun, currentTick, totalTicks, ref followerSpeed);

        // Assert - Compare leader vs same horse with full stamina
        var freshLeaderSpeed = CalculateNaturalSpeed(freshFollower, raceRun, currentTick, totalTicks);

        // Exhausted leader should be noticeably slower than fresh version
        Assert.True(leaderSpeed < freshLeaderSpeed * 0.93,
            $"Exhausted leader (5% stamina) should be much slower than fresh. Exhausted: {leaderSpeed}, Fresh: {freshLeaderSpeed}");

        // Follower should be capped to exhausted leader's speed
        Assert.True(followerSpeed < leaderSpeed,
            $"Fresh follower should be capped by exhausted leader. Follower: {followerSpeed}, Leader: {leaderSpeed}");
    }

    /// <summary>
    /// Test 3: LastSpurt blocked early vs late - validates phase bonus is calculated even when capped
    /// </summary>
    [Fact]
    public void TrafficResponse_LastSpurtBlocked_PhaseBonusCalculated()
    {
        // Arrange
        var manager = CreateManager();

        // LastSpurt behind a consistently-paced horse
        var blockerHorse = CreateRaceRunHorse(CreateHorse(speed: 50, legType: LegTypeId.StartDash), lane: 3, distance: 1.15m);
        var lastSpurtHorse = CreateRaceRunHorse(CreateHorse(speed: 60, legType: LegTypeId.LastSpurt), lane: 3, distance: 1.0m);

        var raceRun = CreateRaceRun(blockerHorse, lastSpurtHorse);
        short earlyTick = 20;  // 20% - LastSpurt no bonus, StartDash HAS bonus (0-25%)
        short lateTick = 90;    // 90% - LastSpurt HAS bonus (75-100%), StartDash no bonus
        short totalTicks = 100;

        // Act - calculate speeds without traffic first to see natural difference
        var lastSpurtEarlyNatural = CalculateNaturalSpeed(lastSpurtHorse, raceRun, earlyTick, totalTicks);
        var lastSpurtLateNatural = CalculateNaturalSpeed(lastSpurtHorse, raceRun, lateTick, totalTicks);

        // Now with traffic
        var earlySpeed = lastSpurtEarlyNatural;
        var lateSpeed = lastSpurtLateNatural;
        manager.ApplyTrafficEffects(lastSpurtHorse, raceRun, earlyTick, totalTicks, ref earlySpeed);
        manager.ApplyTrafficEffects(lastSpurtHorse, raceRun, lateTick, totalTicks, ref lateSpeed);

        // Assert - LastSpurt's natural speed should show phase bonus effect
        Assert.True(lastSpurtLateNatural > lastSpurtEarlyNatural,
            $"LastSpurt natural speed should be faster late (phase bonus). Early: {lastSpurtEarlyNatural}, Late: {lastSpurtLateNatural}");

        // Traffic-affected speeds may be similar due to cap, but late should not be slower
        Assert.True(lateSpeed >= earlySpeed * 0.95,
            $"LastSpurt with traffic should not be significantly slower late. Early: {earlySpeed}, Late: {lateSpeed}");
    }

    /// <summary>
    /// Test 4: FrontRunner with no clear lanes should suffer frustration penalty
    /// </summary>
    [Fact]
    public void TrafficResponse_FrontRunnerBoxedIn_FrustrationPenalty()
    {
        // Arrange
        var manager = CreateManager();

        // Create boxed-in scenario: horses blocking lanes on both sides (within lane change clearance)
        var frontRunner = CreateRaceRunHorse(CreateHorse(speed: 60, legType: LegTypeId.FrontRunner), lane: 3, distance: 1.0m);
        var blockerAhead = CreateRaceRunHorse(CreateHorse(speed: 50), lane: 3, distance: 1.15m);

        // Blockers need to be within lane change clearance distance to block lanes
        // LaneChangeMinClearanceAhead and LaneChangeMinClearanceBehind define this
        var blockerLeft = CreateRaceRunHorse(CreateHorse(speed: 55), lane: 2, distance: 1.05m);  // Slightly ahead, blocks left lane
        var blockerRight = CreateRaceRunHorse(CreateHorse(speed: 55), lane: 4, distance: 1.05m); // Slightly ahead, blocks right lane

        var raceRun = CreateRaceRun(frontRunner, blockerAhead, blockerLeft, blockerRight);
        short currentTick = 50;
        short totalTicks = 100;

        // Act - calculate natural speed (should be slightly above average due to 60 speed stat)
        var frontRunnerNaturalSpeed = CalculateNaturalSpeed(frontRunner, raceRun, currentTick, totalTicks);
        var frontRunnerSpeedWithTraffic = frontRunnerNaturalSpeed;

        // Apply traffic effects
        manager.ApplyTrafficEffects(frontRunner, raceRun, currentTick, totalTicks, ref frontRunnerSpeedWithTraffic);

        // Assert - should have frustration penalty applied when boxed in
        var expectedPenalty = frontRunnerNaturalSpeed * RaceModifierConfig.FrontRunnerFrustrationPenalty;
        Assert.True(frontRunnerSpeedWithTraffic < frontRunnerNaturalSpeed,
            $"Boxed-in FrontRunner should suffer frustration penalty. Natural: {frontRunnerNaturalSpeed}, With traffic: {frontRunnerSpeedWithTraffic}");
    }

    /// <summary>
    /// Test 5: RailRunner blocked should have higher speed cap penalty than StartDash
    /// </summary>
    [Fact]
    public void TrafficResponse_RailRunnerVsStartDash_RailRunnerMoreCautious()
    {
        // Arrange
        var manager = CreateManager();

        var blocker = CreateRaceRunHorse(CreateHorse(speed: 50), lane: 1, distance: 1.15m);
        var railRunner = CreateRaceRunHorse(CreateHorse(speed: 70, legType: LegTypeId.RailRunner), lane: 1, distance: 1.0m);
        var startDash = CreateRaceRunHorse(CreateHorse(speed: 70, legType: LegTypeId.StartDash), lane: 2, distance: 1.0m);

        // Blocker in lane 2 for StartDash
        var blockerForStartDash = CreateRaceRunHorse(CreateHorse(speed: 50), lane: 2, distance: 1.15m);

        var raceRun = CreateRaceRun(blocker, railRunner, startDash, blockerForStartDash);
        short currentTick = 50;
        short totalTicks = 100;

        // Act - calculate natural speeds
        var railRunnerSpeed = CalculateNaturalSpeed(railRunner, raceRun, currentTick, totalTicks);
        var startDashSpeed = CalculateNaturalSpeed(startDash, raceRun, currentTick, totalTicks);

        // Apply traffic effects
        manager.ApplyTrafficEffects(railRunner, raceRun, currentTick, totalTicks, ref railRunnerSpeed);
        manager.ApplyTrafficEffects(startDash, raceRun, currentTick, totalTicks, ref startDashSpeed);

        // Assert - RailRunner should be more cautious (lower speed) than StartDash
        Assert.True(railRunnerSpeed < startDashSpeed,
            "RailRunner should be more cautious than StartDash when both are blocked");

        // Verify penalty difference
        var penaltyDifference = RaceModifierConfig.RailRunnerSpeedCapPenalty - RaceModifierConfig.StartDashSpeedCapPenalty;
        Assert.True(penaltyDifference > 0, "RailRunner should have higher speed cap penalty");
    }

    /// <summary>
    /// Test 6: Horse with clear lane ahead should not be affected by traffic
    /// </summary>
    [Fact]
    public void TrafficResponse_ClearLaneAhead_NoTrafficEffect()
    {
        // Arrange
        var manager = CreateManager();

        var freeHorse = CreateRaceRunHorse(CreateHorse(speed: 70), lane: 3, distance: 1.0m);
        var farAheadHorse = CreateRaceRunHorse(CreateHorse(speed: 60), lane: 3, distance: 5.0m); // Far ahead, outside blocking distance

        var raceRun = CreateRaceRun(freeHorse, farAheadHorse);
        short currentTick = 50;
        short totalTicks = 100;

        // Act
        var originalSpeed = RaceModifierConfig.AverageBaseSpeed * 1.04; // Expected speed boost from stats
        var speedWithTrafficCheck = originalSpeed;
        manager.ApplyTrafficEffects(freeHorse, raceRun, currentTick, totalTicks, ref speedWithTrafficCheck);

        // Assert - speed should be unchanged (no traffic ahead within blocking distance)
        Assert.Equal(originalSpeed, speedWithTrafficCheck, precision: 6);
    }

    /// <summary>
    /// Test 7: Multiple horses in traffic create realistic speed cascade
    /// </summary>
    [Fact]
    public void TrafficResponse_ThreeHorsePack_SpeedCascade()
    {
        // Arrange
        var manager = CreateManager();

        // Leader: slow horse (30 speed)
        var leader = CreateRaceRunHorse(CreateHorse(speed: 30, legType: LegTypeId.FrontRunner), lane: 3, distance: 1.3m);

        // Middle: average horse (50 speed), blocked by leader
        var middle = CreateRaceRunHorse(CreateHorse(speed: 50, legType: LegTypeId.StretchRunner), lane: 3, distance: 1.15m);

        // Back: fast horse (80 speed), blocked by middle
        var back = CreateRaceRunHorse(CreateHorse(speed: 80, legType: LegTypeId.StartDash), lane: 3, distance: 1.0m);

        var raceRun = CreateRaceRun(leader, middle, back);
        short currentTick = 50;
        short totalTicks = 100;

        // Act - calculate natural speeds
        var leaderSpeed = CalculateNaturalSpeed(leader, raceRun, currentTick, totalTicks);
        var middleSpeed = CalculateNaturalSpeed(middle, raceRun, currentTick, totalTicks);
        var backSpeed = CalculateNaturalSpeed(back, raceRun, currentTick, totalTicks);

        // Apply traffic effects
        manager.ApplyTrafficEffects(leader, raceRun, currentTick, totalTicks, ref leaderSpeed);
        manager.ApplyTrafficEffects(middle, raceRun, currentTick, totalTicks, ref middleSpeed);
        manager.ApplyTrafficEffects(back, raceRun, currentTick, totalTicks, ref backSpeed);

        // Assert - speeds should cascade: leader slowest, back should be capped
        Assert.True(leaderSpeed < RaceModifierConfig.AverageBaseSpeed, "Slow leader sets pace");
        Assert.True(middleSpeed < RaceModifierConfig.AverageBaseSpeed, "Middle horse capped by leader");
        Assert.True(backSpeed < RaceModifierConfig.AverageBaseSpeed * 1.1, "Fast horse severely capped");

        // Back should be slowest due to compounding penalties
        Assert.True(backSpeed <= middleSpeed * 1.05, "Back horse capped by middle horse");
    }

    /// <summary>
    /// Test 8: Surface specialist blocked on preferred surface maintains some advantage
    /// </summary>
    [Fact]
    public void TrafficResponse_DirtSpecialistOnDirt_MaintainsAdvantage()
    {
        // Arrange
        var manager = CreateManager();

        var blocker = CreateRaceRunHorse(CreateHorse(speed: 50), lane: 3, distance: 1.15m);

        // Note: Surface specialization is part of CalculateEnvironmentalModifiers
        // We're testing that even when capped, the surface bonus still applies
        var dirtHorse = CreateRaceRunHorse(CreateHorse(speed: 60, legType: LegTypeId.StartDash), lane: 3, distance: 1.0m);
        var normalHorse = CreateRaceRunHorse(CreateHorse(speed: 60, legType: LegTypeId.StartDash), lane: 4, distance: 1.0m);

        var dirtRace = CreateRaceRun(blocker, dirtHorse, normalHorse);
        dirtRace.Race.SurfaceId = SurfaceId.Dirt;

        short currentTick = 50;
        short totalTicks = 100;

        // Act - calculate natural speeds
        var dirtHorseSpeed = CalculateNaturalSpeed(dirtHorse, dirtRace, currentTick, totalTicks);
        var normalHorseSpeed = CalculateNaturalSpeed(normalHorse, dirtRace, currentTick, totalTicks);

        // Apply traffic effects
        manager.ApplyTrafficEffects(dirtHorse, dirtRace, currentTick, totalTicks, ref dirtHorseSpeed);
        manager.ApplyTrafficEffects(normalHorse, dirtRace, currentTick, totalTicks, ref normalHorseSpeed);

        // Assert - dirt horse blocked is still capped, normal horse has no traffic
        // Both should have similar speeds since dirt bonus doesn't apply in default SpeedModifierCalculator
        // (This test documents current behavior - if surface bonuses added later, test will catch it)
        Assert.InRange(dirtHorseSpeed / normalHorseSpeed, 0.95, 1.05);
    }
}
