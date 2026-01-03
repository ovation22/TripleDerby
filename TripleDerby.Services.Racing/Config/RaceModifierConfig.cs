using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Racing.Config;

/// <summary>
/// Configuration constants for race speed modifiers.
/// All modifier values are multiplicative and stack together to calculate final speed.
///
/// <para>
/// <strong>Modifier Pipeline:</strong>
/// Final Speed = Base Speed × Stat Modifiers × Environmental Modifiers × Phase Modifiers × Random Variance
/// </para>
///
/// <para>
/// <strong>Design Goals:</strong>
/// - No magic numbers: All values defined as constants or in dictionaries
/// - Easy to balance: Adjust values in config, no code changes needed
/// - Independent testing: Each modifier category testable in isolation
/// - Clear impact ranges: Each modifier documents its min/max effect
/// </para>
///
/// <para>
/// <strong>Reference:</strong> See race-modifiers-refactor.md feature spec for design rationale and balancing decisions.
/// </para>
/// </summary>
public static class RaceModifierConfig
{
    // ============================================================================
    // Base Speed Configuration
    // ============================================================================

    /// <summary>
    /// Target number of ticks for a standard 10-furlong race.
    /// Used to calculate base speed: baseSpeed = raceFurlongs / targetTicks
    /// </summary>
    public const double TargetTicksFor10Furlongs = 237.0;

    // ============================================================================
    // Race Simulation Configuration (consolidated from RaceService)
    // ============================================================================

    /// <summary>
    /// Average horse speed in miles per hour.
    /// Used as baseline for speed calculations.
    /// </summary>
    public const double BaseSpeedMph = 38.0;

    /// <summary>
    /// Conversion factor: 1 furlong = 1/8 mile.
    /// </summary>
    public const double MilesPerFurlong = 0.125;

    /// <summary>
    /// Conversion factor: 1 hour = 3600 seconds.
    /// </summary>
    public const double SecondsPerHour = 3600.0;

    /// <summary>
    /// Derived: furlongs per second at base speed.
    /// Calculated as: BaseSpeedMph × MilesPerFurlong / SecondsPerHour ≈ 0.001056
    /// </summary>
    public const double FurlongsPerSecond = BaseSpeedMph * MilesPerFurlong / SecondsPerHour;

    /// <summary>
    /// Simulation speed control: ticks per second.
    /// Higher value = faster simulation, shorter race duration.
    /// Value of 10.0 TPS = ~16 seconds for 10f race.
    /// </summary>
    public const double TicksPerSecond = 10.0;

    /// <summary>
    /// Average base speed in furlongs per tick.
    /// Calculated as: 10.0 / TargetTicksFor10Furlongs ≈ 0.0422
    /// </summary>
    public const double AverageBaseSpeed = 10.0 / TargetTicksFor10Furlongs;

    // ============================================================================
    // Stat Modifier Configuration
    // ============================================================================

    /// <summary>
    /// Speed stat modifier per point from neutral (50).
    /// Range: Speed 0 = 0.90x, Speed 50 = 1.0x, Speed 100 = 1.10x
    /// Total effect: ±10%
    /// </summary>
    public const double SpeedModifierPerPoint = 0.002;

    /// <summary>
    /// Agility stat modifier per point from neutral (50).
    /// Range: Agility 0 = 0.95x, Agility 50 = 1.0x, Agility 100 = 1.05x
    /// Total effect: ±5%
    /// </summary>
    public const double AgilityModifierPerPoint = 0.001;

    /// <summary>
    /// Divisor for happiness bonus calculation (above neutral).
    /// Smaller value = stronger bonus effect.
    /// Formula: modifier = log10(1 + excess) / HappinessSpeedBonusDivisor
    /// Current value (20) yields ~2.5% bonus at happiness=100.
    /// </summary>
    public const double HappinessSpeedBonusDivisor = 20.0;

    /// <summary>
    /// Divisor for happiness penalty calculation (below neutral).
    /// Smaller value = stronger penalty effect.
    /// Formula: modifier = log10(1 + deficit) / HappinessSpeedPenaltyDivisor
    /// Current value (15) yields ~3.4% penalty at happiness=0.
    /// Asymmetric design: penalty divisor < bonus divisor (unhappiness hurts more).
    /// Range: Happiness 0 = 0.9661x (-3.39%), Happiness 50 = 1.0x, Happiness 100 = 1.0255x (+2.55%)
    /// Total effect: ±3% (tertiary stat, weaker than Agility ±5%, stronger than Stamina at 10f)
    /// </summary>
    public const double HappinessSpeedPenaltyDivisor = 15.0;

    /// <summary>
    /// Divisor for happiness stamina efficiency bonus (above neutral).
    /// Affects stamina DEPLETION RATE, not stamina pool size.
    /// Formula: efficiency = log10(1 + excess) / HappinessStaminaBonusDivisor
    /// Applied as: depletionRate = 1.0 - efficiency (INVERTED: lower = less depletion)
    /// Current value (25) yields ~6.82% less depletion at happiness=100.
    /// Example: Happiness 100 → 1.0 - log10(51)/25 = 0.9318 (~6.82% more efficient)
    /// </summary>
    public const double HappinessStaminaBonusDivisor = 25.0;

    /// <summary>
    /// Divisor for happiness stamina efficiency penalty (below neutral).
    /// Formula: efficiency = log10(1 + deficit) / HappinessStaminaPenaltyDivisor
    /// Applied as: depletionRate = 1.0 + efficiency (MORE depletion)
    /// Current value (20) yields ~8.54% more depletion at happiness=0.
    /// Asymmetric design: penalty divisor < bonus divisor (unhappiness hurts more).
    /// Example: Happiness 0 → 1.0 + log10(51)/20 = 1.0854 (~8.54% less efficient)
    /// </summary>
    public const double HappinessStaminaPenaltyDivisor = 20.0;

    // ============================================================================
    // Random Variance Configuration
    // ============================================================================

    /// <summary>
    /// Random variance range applied per tick.
    /// Value of 0.01 means ±1% random fluctuation per tick.
    /// </summary>
    public const double RandomVarianceRange = 0.01;

    // ============================================================================
    // Environmental Modifiers
    // ============================================================================

    /// <summary>
    /// Speed modifiers by track surface type.
    /// Dirt is neutral (1.00), Turf is faster (1.02), Artificial is consistent and slightly faster (1.01).
    /// Range: 1.00x to 1.02x (+0% to +2%)
    /// </summary>
    public static readonly IReadOnlyDictionary<SurfaceId, double> SurfaceModifiers =
        new Dictionary<SurfaceId, double>
        {
            { SurfaceId.Dirt, 1.00 },       // Neutral, most common surface
            { SurfaceId.Turf, 1.02 },       // Faster, grass surface
            { SurfaceId.Artificial, 1.01 }  // Consistent, synthetic surface
        };

    /// <summary>
    /// Speed modifiers by track condition.
    /// Fast/dry conditions provide speed bonuses, wet conditions provide penalties, extreme conditions have larger penalties.
    /// Range: 0.90x to 1.03x (-10% to +3%)
    /// </summary>
    public static readonly IReadOnlyDictionary<ConditionId, double> ConditionModifiers =
        new Dictionary<ConditionId, double>
        {
            // Dry/Fast conditions (positive modifiers)
            { ConditionId.Fast, 1.03 },      // Fastest condition
            { ConditionId.Firm, 1.02 },      // Fast and firm
            { ConditionId.Good, 1.00 },      // Neutral baseline

            // Wet conditions (slight penalties)
            { ConditionId.WetFast, 0.99 },   // Slightly wet but still fast
            { ConditionId.Soft, 0.98 },      // Softer surface
            { ConditionId.Yielding, 0.97 },  // Yielding surface
            { ConditionId.Muddy, 0.96 },     // Muddy conditions
            { ConditionId.Sloppy, 0.95 },    // Very wet and sloppy

            // Extreme conditions (larger penalties)
            { ConditionId.Heavy, 0.93 },     // Heavy, difficult conditions
            { ConditionId.Frozen, 0.92 },    // Frozen track
            { ConditionId.Slow, 0.90 }       // Slowest condition
        };

    // ============================================================================
    // Phase Modifiers
    // ============================================================================

    /// <summary>
    /// Phase-based modifiers for each leg type (running style).
    /// Defines when during the race each leg type gets a speed boost.
    /// StartDash and LastSpurt get highest bonus (1.04x), balanced strategies get moderate (1.03x).
    /// Note: RailRunner uses conditional lane/traffic bonus instead of phase timing.
    /// </summary>
    public static readonly IReadOnlyDictionary<LegTypeId, PhaseModifier> LegTypePhaseModifiers =
        new Dictionary<LegTypeId, PhaseModifier>
        {
            { LegTypeId.StartDash, new PhaseModifier(0.00, 0.25, 1.04) },      // First 25%, high risk/reward
            { LegTypeId.FrontRunner, new PhaseModifier(0.00, 0.20, 1.03) },    // First 20%, early speed
            { LegTypeId.StretchRunner, new PhaseModifier(0.60, 0.80, 1.03) },  // 60-80%, stretch run (adjusted for realism)
            { LegTypeId.LastSpurt, new PhaseModifier(0.75, 1.00, 1.04) }       // Final 25%, closing kick
            // RailRunner: Uses conditional lane/traffic bonus (see RailRunner configuration below)
        };

    // ============================================================================
    // Rail Runner Configuration
    // ============================================================================

    /// <summary>
    /// Rail runner speed bonus multiplier when positioned in lane 1 with clear path ahead.
    /// Bonus applied conditionally based on lane position and traffic, not race phase.
    /// Range: 1.03x (+3% speed) when conditions met, 1.0x (neutral) otherwise.
    /// </summary>
    public const double RailRunnerBonusMultiplier = 1.03;

    /// <summary>
    /// Minimum clear distance required ahead of rail runner to activate bonus (in furlongs).
    /// Checks for horses in same lane within this distance.
    /// Value of 0.5 furlongs balances realism (clear racing line) with playability.
    /// </summary>
    public const decimal RailRunnerClearPathDistance = 0.5m;

    // ============================================================================
    // Stamina Configuration
    // ============================================================================

    /// <summary>
    /// Base stamina depletion rates by race distance category.
    /// Values represent percentage of stamina pool depleted per 100 ticks.
    /// </summary>
    public static class StaminaDepletionRates
    {
        public const double Sprint = 0.08;      // ≤6f: Minimal stamina impact
        public const double Classic = 0.15;     // 7-10f: Moderate stamina impact
        public const double Long = 0.22;        // 11-12f: Significant stamina impact
        public const double Marathon = 0.30;    // 13f+: Severe stamina impact
    }

    /// <summary>
    /// Stamina stat modifier per point from neutral (50).
    /// Higher Stamina = bigger fuel tank = slower depletion.
    /// Range: Stamina 0 = 1.20x depletion, Stamina 100 = 0.80x depletion
    /// </summary>
    public const double StaminaDepletionModifierPerPoint = -0.004;

    /// <summary>
    /// Durability stat modifier per point from neutral (50).
    /// Higher Durability = fuel efficiency = slower depletion.
    /// Range: Durability 0 = 1.15x depletion, Durability 100 = 0.85x depletion
    /// </summary>
    public const double DurabilityDepletionModifierPerPoint = -0.003;

    /// <summary>
    /// Maximum speed penalty when stamina is fully depleted (0%).
    /// Value of 0.10 means exhausted horse runs at 90% speed.
    /// Uses quadratic curve for progressive penalty below 50% stamina.
    /// </summary>
    public const double MaxStaminaSpeedPenalty = 0.10;  // 10% max penalty (mild)

    // ============================================================================
    // Overtaking & Lane Change Configuration
    // ============================================================================

    /// <summary>
    /// Base threshold distance for detecting overtaking opportunities (in furlongs).
    /// Modified by speed factor and race phase multiplier.
    /// </summary>
    public const decimal OvertakingBaseThreshold = 0.25m;

    /// <summary>
    /// Speed stat influence on overtaking threshold per point.
    /// Higher speed = larger detection range.
    /// Range: Speed 0 = 1.0x threshold, Speed 100 = 1.2x threshold
    /// </summary>
    public const double OvertakingSpeedFactor = 0.002;

    /// <summary>
    /// Multiplier applied to overtaking threshold in final 25% of race.
    /// Creates more aggressive overtaking behavior late in races.
    /// </summary>
    public const double OvertakingLateRaceMultiplier = 1.5;

    /// <summary>
    /// Base cooldown between lane change attempts at 0 agility (in ticks).
    /// Reduced by agility: cooldown = BaseLaneChangeCooldown - (Agility × AgilityCooldownReduction)
    /// </summary>
    public const int BaseLaneChangeCooldown = 10;

    /// <summary>
    /// Reduction in cooldown per point of agility.
    /// Range: Agility 0 = 10 tick cooldown, Agility 100 = 2 tick cooldown
    /// Tuned: Reduced from 0.1 to 0.08 to decrease lane change frequency.
    /// </summary>
    public const double AgilityCooldownReduction = 0.08;

    /// <summary>
    /// Minimum clearance required behind horse when changing lanes (in furlongs).
    /// Prevents cutting off horses that are close behind.
    /// </summary>
    public const decimal LaneChangeMinClearanceBehind = 0.1m;

    /// <summary>
    /// Minimum clearance required ahead of horse when changing lanes (in furlongs).
    /// Prevents collisions with horses ahead in target lane.
    /// Asymmetric: requires more clearance ahead than behind for safety.
    /// </summary>
    public const decimal LaneChangeMinClearanceAhead = 0.2m;

    // ============================================================================
    // Risky Lane Change Configuration
    // ============================================================================

    /// <summary>
    /// Base penalty duration for successful risky lane changes at 0 durability (in ticks).
    /// Reduced by durability: penaltyTicks = RiskyLaneChangePenaltyBaseTicks - (Durability × RiskyLaneChangePenaltyReduction)
    /// </summary>
    public const int RiskyLaneChangePenaltyBaseTicks = 5;

    /// <summary>
    /// Reduction in penalty duration per point of durability.
    /// Range: Durability 0 = 5 tick penalty, Durability 100 = 1 tick penalty
    /// </summary>
    public const double RiskyLaneChangePenaltyReduction = 0.04;

    /// <summary>
    /// Speed multiplier applied during risky lane change penalty.
    /// Value of 0.95 means 5% speed reduction while penalty is active.
    /// </summary>
    public const double RiskyLaneChangeSpeedPenalty = 0.95;

    /// <summary>
    /// Divisor for calculating risky squeeze play success probability from agility.
    /// Formula: successChance = Agility / RiskySqueezeAgilityDivisor
    /// Value of 250.0 yields: Agility 0 = 0%, Agility 50 = 20%, Agility 100 = 40%
    /// Tuned: Increased from 200.0 to 250.0 to reduce risky attempt success rate.
    /// </summary>
    public const double RiskySqueezeAgilityDivisor = 250.0;

    // ============================================================================
    // Traffic Response Configuration
    // ============================================================================

    /// <summary>
    /// FrontRunner frustration penalty magnitude when blocked with no clear lanes.
    /// Applied as speed multiplier: speed *= (1.0 - FrontRunnerFrustrationPenalty)
    /// </summary>
    public const double FrontRunnerFrustrationPenalty = 0.03;  // 3% penalty

    /// <summary>
    /// StartDash speed cap penalty when blocked (follows leader minus this penalty).
    /// Applied as: cappedSpeed = leaderSpeed * (1.0 - StartDashSpeedCapPenalty)
    /// </summary>
    public const double StartDashSpeedCapPenalty = 0.01;  // 1% below leader

    /// <summary>
    /// LastSpurt speed cap penalty when blocked (minimal, patient behavior).
    /// Applied as: cappedSpeed = leaderSpeed * (1.0 - LastSpurtSpeedCapPenalty)
    /// </summary>
    public const double LastSpurtSpeedCapPenalty = 0.001;  // 0.1% below leader

    /// <summary>
    /// StretchRunner speed cap penalty when blocked.
    /// Applied as: cappedSpeed = leaderSpeed * (1.0 - StretchRunnerSpeedCapPenalty)
    /// </summary>
    public const double StretchRunnerSpeedCapPenalty = 0.01;  // 1% below leader

    /// <summary>
    /// RailRunner speed cap penalty when blocked on rail (extra cautious).
    /// Applied as: cappedSpeed = leaderSpeed * (1.0 - RailRunnerSpeedCapPenalty)
    /// </summary>
    public const double RailRunnerSpeedCapPenalty = 0.02;  // 2% below leader

    /// <summary>
    /// Distance threshold for detecting horse ahead as "blocking" (in furlongs).
    /// Used by traffic response system to identify when horse is close enough to apply effects.
    /// </summary>
    public const decimal TrafficBlockingDistance = 0.2m;

    // ============================================================================
    // Lane Finding Configuration
    // ============================================================================

    /// <summary>
    /// Look-ahead distance for StartDash to evaluate lane congestion (in furlongs).
    /// Checks for horses ahead within this distance to find least congested lane.
    /// </summary>
    public const decimal StartDashLookAheadDistance = 0.5m;

    // ============================================================================
    // Stat Progression Configuration (Feature 018)
    // ============================================================================

    /// <summary>
    /// Base stat growth rate per race as percentage of remaining gap to genetic ceiling.
    /// Formula: baseGain = (DominantPotential - Actual) × BaseStatGrowthRate
    /// Value of 0.02 means 2% of gap closed per race.
    /// </summary>
    public const double BaseStatGrowthRate = 0.02;

    // Career Phase Multipliers

    /// <summary>
    /// Development efficiency multiplier for young horses (0-9 races).
    /// Young horses are still learning, develop slower than prime career horses.
    /// Applied to base stat growth: finalGain = baseGain × YoungHorseMultiplier
    /// </summary>
    public const double YoungHorseMultiplier = 0.80;

    /// <summary>
    /// Development efficiency multiplier for prime career horses (10-29 races).
    /// Peak development phase - horses learn fastest during these years.
    /// Applied to base stat growth: finalGain = baseGain × PrimeHorseMultiplier
    /// </summary>
    public const double PrimeHorseMultiplier = 1.20;

    /// <summary>
    /// Development efficiency multiplier for veteran horses (30-49 races).
    /// Experienced horses still learn but growth slows down significantly.
    /// Applied to base stat growth: finalGain = baseGain × VeteranHorseMultiplier
    /// </summary>
    public const double VeteranHorseMultiplier = 0.60;

    /// <summary>
    /// Development efficiency multiplier for old horses (50+ races).
    /// Minimal development gains - signals retirement time.
    /// Applied to base stat growth: finalGain = baseGain × OldHorseMultiplier
    /// </summary>
    public const double OldHorseMultiplier = 0.20;

    // Career Phase Boundaries

    /// <summary>
    /// Race count when horse transitions from young to prime career phase.
    /// Prime phase (10-29 races) has highest development efficiency.
    /// </summary>
    public const short PrimeCareerStartRace = 10;

    /// <summary>
    /// Race count when horse transitions from prime to veteran career phase.
    /// Veteran phase (30-49 races) shows slowing development.
    /// </summary>
    public const short VeteranCareerStartRace = 30;

    /// <summary>
    /// Race count when horse transitions from veteran to old career phase.
    /// Old phase (50+ races) shows minimal development, retirement recommended.
    /// </summary>
    public const short OldCareerStartRace = 50;

    // Race-Type Focus Multipliers

    /// <summary>
    /// Distance threshold for sprint races (≤6 furlongs).
    /// Sprint races develop Speed and Agility stats faster.
    /// </summary>
    public const decimal SprintDistanceThreshold = 6m;

    /// <summary>
    /// Distance threshold for distance races (≥11 furlongs).
    /// Distance races develop Stamina and Durability stats faster.
    /// </summary>
    public const decimal DistanceRaceThreshold = 11m;

    /// <summary>
    /// Speed stat development multiplier for sprint races (≤6f).
    /// Sprint races favor Speed development.
    /// </summary>
    public const double SprintSpeedMultiplier = 1.50;

    /// <summary>
    /// Agility stat development multiplier for sprint races (≤6f).
    /// Sprint races favor Agility development (quick turns, acceleration).
    /// </summary>
    public const double SprintAgilityMultiplier = 1.25;

    /// <summary>
    /// Non-sprint-focused stat development multiplier for sprint races.
    /// Stamina and Durability develop slower in sprint races.
    /// </summary>
    public const double SprintOtherMultiplier = 0.75;

    /// <summary>
    /// Stamina stat development multiplier for distance races (≥11f).
    /// Distance races favor Stamina development.
    /// </summary>
    public const double DistanceStaminaMultiplier = 1.50;

    /// <summary>
    /// Durability stat development multiplier for distance races (≥11f).
    /// Distance races favor Durability development (endurance, wear resistance).
    /// </summary>
    public const double DistanceDurabilityMultiplier = 1.25;

    /// <summary>
    /// Non-distance-focused stat development multiplier for distance races.
    /// Speed and Agility develop slower in distance races.
    /// </summary>
    public const double DistanceOtherMultiplier = 0.75;

    /// <summary>
    /// Classic race (7-10f) stat development multiplier.
    /// All stats develop equally in classic races.
    /// </summary>
    public const double ClassicRaceMultiplier = 1.00;

    // Performance Bonuses

    /// <summary>
    /// Stat growth bonus multiplier for winning (1st place).
    /// Winners develop 50% faster than baseline.
    /// Applied to base growth: finalGain = baseGain × WinBonus
    /// </summary>
    public const double WinBonus = 1.50;

    /// <summary>
    /// Stat growth bonus multiplier for placing (2nd place).
    /// Place finishers develop 25% faster than baseline.
    /// Applied to base growth: finalGain = baseGain × PlaceBonus
    /// </summary>
    public const double PlaceBonus = 1.25;

    /// <summary>
    /// Stat growth bonus multiplier for showing (3rd place).
    /// Show finishers develop 10% faster than baseline.
    /// Applied to base growth: finalGain = baseGain × ShowBonus
    /// </summary>
    public const double ShowBonus = 1.10;

    /// <summary>
    /// Stat growth multiplier for mid-pack finishers (4th to 50th percentile).
    /// Mid-pack horses develop at baseline rate (neutral).
    /// </summary>
    public const double MidPackMultiplier = 1.00;

    /// <summary>
    /// Stat growth penalty multiplier for back of pack finishers (bottom 50%).
    /// Back of pack horses still learn but 25% slower than baseline.
    /// Applied to base growth: finalGain = baseGain × BackOfPackPenalty
    /// </summary>
    public const double BackOfPackPenalty = 0.75;

    // Happiness Changes

    /// <summary>
    /// Happiness increase for winning a race (1st place).
    /// Winning significantly boosts horse morale.
    /// </summary>
    public const int WinHappinessBonus = 8;

    /// <summary>
    /// Happiness increase for placing in a race (2nd place).
    /// Good performance moderately boosts morale.
    /// </summary>
    public const int PlaceHappinessBonus = 4;

    /// <summary>
    /// Happiness increase for showing in a race (3rd place).
    /// Solid performance provides small morale boost.
    /// </summary>
    public const int ShowHappinessBonus = 2;

    /// <summary>
    /// Happiness change for mid-pack finishers (4th to 50th percentile).
    /// Mid-pack finishes are neutral - no happiness change.
    /// </summary>
    public const int MidPackHappinessChange = 0;

    /// <summary>
    /// Happiness decrease for back of pack finishers (bottom 50%).
    /// Poor performance damages morale.
    /// </summary>
    public const int BackOfPackHappinessPenalty = -3;

    /// <summary>
    /// Happiness penalty for finishing race in exhausted state (<10% stamina).
    /// Physical trauma from exhaustion compounds frustration.
    /// Applied in addition to finish-position happiness change.
    /// </summary>
    public const int ExhaustionHappinessPenalty = -5;

    /// <summary>
    /// Stamina percentage threshold for exhaustion detection.
    /// Finishing with less than 10% stamina remaining triggers exhaustion penalty.
    /// </summary>
    public const double ExhaustionStaminaThreshold = 0.10;

    // Happiness Decay (Future - not implemented in Phase 6)

    /// <summary>
    /// Happiness decay rate toward neutral point per time period.
    /// Value of 0.10 means 10% of distance to neutral (50) decays per period.
    /// Prevents permanent extreme happiness/unhappiness states.
    /// Note: Decay scheduler not implemented yet - reserved for future feature.
    /// </summary>
    public const double HappinessDecayRate = 0.10;

    /// <summary>
    /// Neutral happiness point that happiness decays toward over time.
    /// Happiness naturally drifts back to 50 (neutral) without maintenance.
    /// </summary>
    public const int HappinessNeutralPoint = 50;
}
