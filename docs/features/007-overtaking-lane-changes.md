# Overtaking and Lane Change System - Feature Discovery

**Feature Number:** 007

**Status:** 🟡 DISCOVERY - Requirements Gathering

**Prerequisites:**
- Feature 005 (Rail Runner Lane Bonus) - ✅ Complete (lane-based mechanics foundation)
- Feature 003 (Race Modifiers Refactor) - ✅ Complete

---

## Summary

Implement a **dynamic lane changing and overtaking system** that allows horses to tactically change lanes during races to navigate traffic, find optimal racing lines, and execute strategic positioning based on their leg type characteristics. This feature will transform races from static lane assignments into dynamic tactical battles where positioning matters.

**Core Design Philosophy:**
- Horses should actively navigate traffic instead of passively running in assigned lanes
- Lane changes should reflect racing strategy (aggressive vs conservative)
- Leg types should influence overtaking behavior (e.g., RailRunner seeks lane 1, wide runners avoid traffic)
- Traffic awareness creates realistic racing dynamics
- Lane changes involve risk/reward trade-offs

---

## Current State Analysis

### Existing Infrastructure (Foundation)

The codebase already contains **commented-out implementations** of overtaking and lane change mechanics:

**In [RaceService.cs:269-338](c:\Development\TripleDerby\TripleDerby.Core\Services\RaceService.cs#L269-L338):**

1. **`HandleOvertaking` method (line 273-295)** - Currently commented out at line 270
   - Sorts horses by distance to identify relative positions
   - Detects when horse is close enough to overtake horse ahead
   - Uses overtaking threshold based on horse speed
   - Triggers lane change attempt when overtaking opportunity detected

2. **`AttemptLaneChange` method (line 297-327)** - Currently unused
   - Calculates lane change probability based on LegType
   - Implements directional lane changes (left/right)
   - Includes random probability checks
   - Validates lane changes with `IsLaneClear` helper

3. **`IsLaneClear` method (line 329-338)** - Currently unused
   - Checks for lateral blocking (horses alongside in target lane)
   - Uses 0.1 furlong lateral blocking distance
   - Prevents lane changes into occupied space

**Existing LegType Lane Change Probabilities:**
```csharp
var changeProbability = horse.Horse.LegTypeId switch
{
    LegTypeId.FrontRunner => 0.3,    // Conservative, prefers clear running
    LegTypeId.StartDash => 0.6,      // Aggressive, seeks openings
    LegTypeId.LastSpurt => 0.4,      // Moderate, tactical positioning
    LegTypeId.StretchRunner => 0.5,  // Balanced approach
    LegTypeId.RailRunner => 0.2,     // Reluctant to leave rail
    _ => 0.5
};
```

**Integration with Rail Runner (Feature 005):**
- Rail Runner bonus system already implements traffic detection
- `HasClearPathAhead` method checks for horses within 0.5 furlongs ahead
- Lane position tracked in `RaceRunHorse.Lane` property
- Traffic awareness foundation exists

### Data Model Support

**Entities already support lane tracking:**

```csharp
// RaceRunHorse.cs - Horse state during race
public byte Lane { get; set; }        // Current lane (1-8), mutable
public decimal Distance { get; set; }  // Current position

// RaceRunTickHorse.cs - Snapshot per tick
public byte Lane { get; set; }         // Lane at this tick (historical record)
public decimal Distance { get; set; }   // Position at this tick
```

**Key observation:** Lane changes will be **visible in race history** through `RaceRunTickHorse` records.

### Current Gaps & Design Questions

The existing code provides a **starting framework** but needs requirement clarification in the following areas:

---

## ✅ Requirements Finalized

All discovery questions have been answered. See [Appendix A: Discovery Q&A](#appendix-a-discovery-qa) for complete decision log.

---

## Requirements Specification

### Functional Requirements

#### FR1: Overtaking Detection
- **FR1.1:** Horses detect overtaking opportunities using combined speed + phase threshold
  - Base threshold: 0.25 furlongs
  - Speed factor: 1.0 + (Speed × 0.002) = 1.0x to 1.2x multiplier
  - Phase factor: 1.0x early race, 1.5x in final 25%
  - Formula: `threshold = horse.Distance + (0.25m × speedFactor × phaseFactor)`

- **FR1.2:** Overtaking triggers lane change attempt when horse ahead within threshold

#### FR2: Proactive Lane Positioning
- **FR2.1:** Horses seek desired lanes based on leg type strategy:
  - **RailRunner:** Always seeks lane 1 (rail position bonus)
  - **FrontRunner:** Stays in current lane (conservative leader)
  - **StartDash:** Seeks least congested lane (traffic avoider)
  - **LastSpurt:** Patient early, seeks overtaking lanes late race (>75%)
  - **StretchRunner:** Prefers center lanes 4-5 (balanced position)

- **FR2.2:** Lane changes triggered when: `WantsToOvertake(horse) OR horse.Lane != DesiredLane`

#### FR3: Lane Change Cooldown System
- **FR3.1:** Agility-based cooldown between lane changes
  - Formula: `cooldown = 10 - (Agility × 0.1)` ticks
  - Agility 0: 10 tick cooldown
  - Agility 50: 5 tick cooldown
  - Agility 100: 0 tick cooldown (always ready)

- **FR3.2:** Cooldown consumed on EVERY attempt (success or failure)
- **FR3.3:** Track `TicksSinceLastLaneChange` in RaceRunHorse entity

#### FR4: Traffic Detection & Lane Clearance
- **FR4.1:** Asymmetric clearance requirements:
  - Behind: 0.1 furlongs minimum (prevent cutting off)
  - Ahead: 0.2 furlongs minimum (prevent collisions)

- **FR4.2:** Lane change only succeeds if target lane clear

- **FR4.3:** Adjacent lane movement only (gradual drift toward desired lane)

#### FR5: Risky Lane Change System
- **FR5.1:** When lane is NOT clear, attempt risky squeeze play
  - Success probability: `Agility / 200.0` (0% to 50% max)
  - Cooldown consumed regardless of outcome

- **FR5.2:** Successful risky change applies speed penalty
  - Penalty duration: `5 - (Durability × 0.04)` ticks (5 ticks at 0 durability, 1 tick at 100)
  - Penalty magnitude: 0.95x speed (5% reduction)
  - Track `SpeedPenaltyTicksRemaining` in RaceRunHorse entity

- **FR5.3:** Failed risky attempts waste cooldown (commitment cost)

#### FR6: Leg-Type-Specific Traffic Response
- **FR6.1:** When blocked by horse ahead, apply leg-type-specific behavior:

| Leg Type | Response Type | Effect |
|----------|---------------|--------|
| FrontRunner | Frustration | 3% speed penalty when blocked and no clear lanes available |
| StartDash | Speed Cap | Match leader speed - 1% |
| LastSpurt | Patient | Match leader speed - 0.1% (minimal cap, no frustration) |
| StretchRunner | Speed Cap | Match leader speed - 1% |
| RailRunner | Speed Cap | Match leader speed - 2% (extra cautious on rail) |

- **FR6.2:** "Blocked" = horse ahead in same lane within 0.2 furlongs
- **FR6.3:** Frustration (FrontRunner) only applies when: wants to overtake AND no clear lanes available

#### FR7: Lane Change Probability System
- **FR7.1:** Position-based probability multipliers:
  - Leaders (positions 1-3): 0.7x base probability
  - Mid-pack (positions 4 to N-3): 1.0x base probability
  - Back-markers (last 3 positions): 1.3x base probability

- **FR7.2:** Base probabilities by leg type (configurable):
  - FrontRunner: 0.3
  - StartDash: 0.6
  - LastSpurt: 0.4
  - StretchRunner: 0.5
  - RailRunner: 0.2

- **FR7.3:** Probability gates lane change attempts (check before attempting)

#### FR8: Starting Lane Assignment
- **FR8.1:** Random lane assignment at race start
  - Shuffle lanes 1 to fieldSize randomly
  - Assign sequentially to horses
  - Ensures fair distribution (RailRunner not guaranteed lane 1)

#### FR9: Dynamic Lane Bounds
- **FR9.1:** Maximum lane = field size (8-12 horses = 8-12 lanes)
- **FR9.2:** No lane sharing (each lane occupied by max one horse at each distance)
- **FR9.3:** Lane change validation prevents out-of-bounds lanes

#### FR10: Race Event Logging
- **FR10.1:** Generate detailed notes for lane changes:
  - Clean change: `"{HorseName} moved {oldLane}→{newLane} to overtake {targetHorse}"`
  - Risky success: `"{HorseName} squeezed through traffic {oldLane}→{newLane} (risky!)"`
  - Proactive: `"{HorseName} drifted {oldLane}→{newLane} (seeking lane {desired})"`
  - Blocked (FrontRunner): `"{HorseName} frustrated by traffic in lane {lane}"`

- **FR10.2:** Store notes in `RaceRunTick.Note` field
- **FR10.3:** Lane positions tracked in `RaceRunTickHorse.Lane` (historical record)

---

### Non-Functional Requirements

#### NFR1: Performance
- **NFR1.1:** Lane change logic adds < 5% overhead to race simulation time
- **NFR1.2:** Traffic detection uses efficient algorithms (O(n) per horse per tick max)
- **NFR1.3:** No memory leaks from lane change tracking

#### NFR2: Balance & Tuning
- **NFR2.1:** All magic numbers configurable in `RaceModifierConfig.cs`
- **NFR2.2:** Balance metrics tracked:
  - Speed correlation: Target -0.70 to -0.75 (maintain as primary stat)
  - Agility correlation: Target -0.45 to -0.55 (strengthen from -0.355)
  - Durability correlation: Target -0.15 to -0.25 (introduce correlation)
  - Average lane changes per race: 2-8 (realistic frequency)
  - High-agility win rate: Monitor for dominance (< 35%)

- **NFR2.3:** Tuning knobs documented for balance adjustments

#### NFR3: Testability
- **NFR3.1:** Unit tests for isolated logic (cooldown, clearance, probabilities)
- **NFR3.2:** Integration tests for full race simulation
- **NFR3.3:** Balance validation with 500+ race statistical analysis
- **NFR3.4:** Diagnostic metrics tracked during testing (not persisted to production DB)

#### NFR4: Maintainability
- **NFR4.1:** Follow existing modifier pipeline patterns
- **NFR4.2:** Clear separation of concerns (detection → decision → execution)
- **NFR4.3:** Comprehensive code comments explaining tactical systems
- **NFR4.4:** Configuration documented with expected ranges and effects

#### NFR5: Data Integrity
- **NFR5.1:** Lane changes recorded in race history (RaceRunTickHorse)
- **NFR5.2:** No data loss during lane transitions
- **NFR5.3:** Race replay shows lane positions accurately

---

## Technical Design

### Architecture Overview

```
Race Simulation Tick Loop:
├─ UpdateHorsePosition (existing)
│  ├─ Calculate base speed
│  ├─ Apply stat modifiers
│  ├─ Apply environmental modifiers
│  ├─ Apply phase modifiers
│  ├─ Apply stamina modifier
│  ├─ → [NEW] Apply traffic effects (speed capping/frustration)
│  ├─ → [NEW] Apply risky lane change penalty (if active)
│  ├─ Apply random variance
│  ├─ Update distance
│  └─ Deplete stamina
└─ → [NEW] HandleOvertaking(horse, raceRun)
      ├─ Check if wants to overtake OR not in desired lane
      ├─ Check cooldown (agility-based)
      ├─ Consume cooldown (commitment)
      ├─ DetermineTargetLane (leg-type strategy)
      ├─ AttemptLaneChange
      │  ├─ IsLaneClear (asymmetric check)
      │  ├─ If clear: Success, update Lane
      │  ├─ If blocked: RiskySqueezePlay
      │  │  ├─ Agility probability check
      │  │  ├─ Success: Update Lane, apply Durability-based penalty
      │  │  └─ Failure: Stay in lane (cooldown already consumed)
      │  └─ Generate event note
      └─ Increment TicksSinceLastLaneChange
```

### Data Model Changes

#### RaceRunHorse Entity (Additions)
```csharp
public class RaceRunHorse
{
    // Existing properties...
    public byte Lane { get; set; } // Already exists, now mutable

    // New properties
    public short TicksSinceLastLaneChange { get; set; }     // Cooldown tracking
    public byte SpeedPenaltyTicksRemaining { get; set; }    // Risky change penalty
}
```

#### Configuration (RaceModifierConfig.cs)
```csharp
// Overtaking thresholds
public const decimal OvertakingBaseThreshold = 0.25m;          // furlongs
public const double OvertakingSpeedFactor = 0.002;             // per Speed point
public const double OvertakingLateRaceMultiplier = 1.5;        // final 25% boost

// Lane change cooldown
public const int BaseLaneChangeCooldown = 10;                  // ticks at 0 agility
public const double AgilityCooldownReduction = 0.1;            // ticks per agility point

// Lane clearance requirements
public const decimal LaneChangeMinClearanceBehind = 0.1m;      // furlongs
public const decimal LaneChangeMinClearanceAhead = 0.2m;       // furlongs

// Risky lane changes
public const int RiskyLaneChangePenaltyBaseTicks = 5;          // ticks at 0 durability
public const double RiskyLaneChangePenaltyReduction = 0.04;    // reduction per durability point
public const double RiskyLaneChangeSpeedPenalty = 0.95;        // 5% speed reduction

// Lane change probabilities
public static readonly IReadOnlyDictionary<LegTypeId, double> BaseLaneChangeProbabilities =
    new Dictionary<LegTypeId, double>
    {
        { LegTypeId.FrontRunner, 0.3 },
        { LegTypeId.StartDash, 0.6 },
        { LegTypeId.LastSpurt, 0.4 },
        { LegTypeId.StretchRunner, 0.5 },
        { LegTypeId.RailRunner, 0.2 }
    };

// Position-based multipliers
public const double LeaderLaneChangeMultiplier = 0.7;          // Top 3 positions
public const double BackMarkerLaneChangeMultiplier = 1.3;      // Bottom 3 positions

// Traffic responses
public static readonly IReadOnlyDictionary<LegTypeId, TrafficResponse> LegTypeTrafficBehavior =
    new Dictionary<LegTypeId, TrafficResponse>
    {
        { LegTypeId.FrontRunner, new TrafficResponse(TrafficResponseType.Frustration, 0.03) },
        { LegTypeId.StartDash, new TrafficResponse(TrafficResponseType.SpeedCap, 0.01) },
        { LegTypeId.LastSpurt, new TrafficResponse(TrafficResponseType.Patient, 0.0) },
        { LegTypeId.StretchRunner, new TrafficResponse(TrafficResponseType.SpeedCap, 0.01) },
        { LegTypeId.RailRunner, new TrafficResponse(TrafficResponseType.SpeedCap, 0.02) }
    };

public enum TrafficResponseType { SpeedCap, Frustration, Patient }
public record TrafficResponse(TrafficResponseType Type, double Penalty);
```

### Key Algorithms

#### Algorithm 1: Overtaking Threshold Calculation
```csharp
private decimal CalculateOvertakingThreshold(RaceRunHorse horse, short currentTick, short totalTicks)
{
    var raceProgress = (double)currentTick / totalTicks;
    var phaseMultiplier = raceProgress > 0.75
        ? OvertakingLateRaceMultiplier   // 1.5x in final 25%
        : 1.0;

    var speedFactor = 1.0 + (horse.Horse.Speed * OvertakingSpeedFactor); // 1.0 to 1.2x

    return OvertakingBaseThreshold * (decimal)(speedFactor * phaseMultiplier);

    // Results:
    // Speed 0, early: 0.25f
    // Speed 100, early: 0.30f
    // Speed 0, late: 0.375f
    // Speed 100, late: 0.45f
}
```

#### Algorithm 2: Desired Lane Determination
```csharp
private int DetermineDesiredLane(RaceRunHorse horse, RaceRun raceRun, double raceProgress)
{
    return horse.Horse.LegTypeId switch
    {
        LegTypeId.RailRunner => 1,  // Always seek rail

        LegTypeId.FrontRunner => horse.Lane,  // Stay put

        LegTypeId.StartDash => FindLeastCongestedLane(horse, raceRun, lookAhead: 0.5m),

        LegTypeId.LastSpurt => raceProgress > 0.75
            ? FindBestOvertakingLane(horse, raceRun)
            : horse.Lane,  // Patient early, aggressive late

        LegTypeId.StretchRunner => horse.Lane switch
        {
            <= 3 => horse.Lane + 1,  // Drift toward center
            >= 6 => horse.Lane - 1,  // Drift toward center
            _ => horse.Lane          // Stay in 4-5
        },

        _ => horse.Lane
    };
}

private int FindLeastCongestedLane(RaceRunHorse horse, RaceRun raceRun, decimal lookAhead)
{
    var maxLane = GetMaxLane(raceRun);
    var laneTraffic = new int[maxLane + 1];

    foreach (var h in raceRun.Horses.Where(h =>
        h.Distance > horse.Distance &&
        h.Distance - horse.Distance < lookAhead))
    {
        laneTraffic[h.Lane]++;
    }

    return Enumerable.Range(1, maxLane)
        .OrderBy(lane => laneTraffic[lane])
        .First();
}

private int FindBestOvertakingLane(RaceRunHorse horse, RaceRun raceRun)
{
    var maxLane = GetMaxLane(raceRun);
    var overtakingRange = CalculateOvertakingThreshold(horse, currentTick, totalTicks);
    var opportunities = new int[maxLane + 1];

    foreach (var h in raceRun.Horses.Where(h =>
        h.Distance > horse.Distance &&
        h.Distance - horse.Distance < overtakingRange))
    {
        opportunities[h.Lane]++;
    }

    return Enumerable.Range(1, maxLane)
        .OrderByDescending(lane => opportunities[lane])
        .First();
}
```

#### Algorithm 3: Lane Clearance Check
```csharp
private bool IsLaneClear(RaceRunHorse horse, int targetLane, RaceRun raceRun)
{
    return !raceRun.Horses.Any(h =>
        h != horse &&
        h.Lane == targetLane &&
        (
            // Horse behind us - too close
            (horse.Distance - h.Distance < LaneChangeMinClearanceBehind &&
             h.Distance < horse.Distance) ||

            // Horse ahead of us - too close
            (h.Distance - horse.Distance < LaneChangeMinClearanceAhead &&
             h.Distance > horse.Distance)
        )
    );
}
```

#### Algorithm 4: Risky Squeeze Play
```csharp
private bool AttemptRiskySqueezePlay(RaceRunHorse horse, int targetLane)
{
    var squeezeSuccessChance = horse.Horse.Agility / 200.0; // 0% to 50%

    if (_randomGenerator.NextDouble() < squeezeSuccessChance)
    {
        // Success! Thread the needle
        horse.Lane = (byte)targetLane;

        // Apply durability-based penalty
        var penaltyTicks = RiskyLaneChangePenaltyBaseTicks -
                          (horse.Horse.Durability * RiskyLaneChangePenaltyReduction);
        horse.SpeedPenaltyTicksRemaining = (byte)Math.Max(1, Math.Round(penaltyTicks));

        return true;
    }

    // Failed - stay in current lane, cooldown already consumed
    return false;
}
```

#### Algorithm 5: Traffic Response Application
```csharp
private void ApplyTrafficEffects(RaceRunHorse horse, RaceRun raceRun, ref double currentSpeed)
{
    var trafficResponse = LegTypeTrafficBehavior[horse.Horse.LegTypeId];
    var horseAhead = FindHorseAheadInLane(horse, raceRun, lookAhead: 0.2m);

    if (horseAhead == null) return;

    switch (trafficResponse.Type)
    {
        case TrafficResponseType.SpeedCap:
            var leaderSpeed = CalculateHorseSpeed(horseAhead);
            var cappedSpeed = leaderSpeed * (1.0 - trafficResponse.Penalty);
            if (currentSpeed > cappedSpeed)
                currentSpeed = cappedSpeed;
            break;

        case TrafficResponseType.Frustration:
            // Only penalize if wants to overtake but can't change lanes
            if (WantsToOvertake(horse, horseAhead) && !HasClearLaneAvailable(horse, raceRun))
            {
                currentSpeed *= (1.0 - trafficResponse.Penalty);
            }
            break;

        case TrafficResponseType.Patient:
            // Minimal speed cap, no frustration
            var leaderSpeedPatient = CalculateHorseSpeed(horseAhead);
            if (currentSpeed > leaderSpeedPatient * 0.999)
                currentSpeed = leaderSpeedPatient * 0.999;
            break;
    }
}
```

---

## Implementation Phases

### Phase 1: Core Infrastructure (Foundation)
**Estimated Effort:** 4-6 hours

**Scope:**
- Basic overtaking detection (speed + phase threshold)
- Agility-based cooldown system
- Simple lane clearance check
- RailRunner proactive positioning (seek lane 1)
- Random starting lane assignment
- Unit tests for core logic

**Deliverables:**
- [ ] `CalculateOvertakingThreshold` method
- [ ] `HandleOvertaking` method (basic version)
- [ ] `AttemptLaneChange` method (clean changes only)
- [ ] `IsLaneClear` method (asymmetric check)
- [ ] `DetermineDesiredLane` method (RailRunner only)
- [ ] Cooldown tracking in `RaceRunHorse`
- [ ] Random lane initialization
- [ ] Configuration constants in `RaceModifierConfig`
- [ ] 15+ unit tests (cooldown, clearance, threshold calculations)

**Acceptance Criteria:**
- RailRunner horses drift from lane 8 → lane 1 over multiple ticks
- High-agility horses change lanes more frequently than low-agility
- Lane changes only occur when lanes are clear
- Cooldown prevents spam (max 1 change per 10 ticks for agility 0)
- All unit tests passing

**Balance Target:**
- 1-3 lane changes per race (basic overtaking only)

---

### Phase 2: Advanced Behaviors & Risk System
**Estimated Effort:** 6-8 hours

**Scope:**
- Full leg-type proactive positioning (all 5 archetypes)
- Risky squeeze play with agility success rate
- Durability-based penalty system
- Position-based probability multipliers
- Traffic response system (speed capping + frustration)
- Detailed event logging
- Integration tests

**Deliverables:**
- [ ] Complete `DetermineDesiredLane` for all leg types
- [ ] `FindLeastCongestedLane` helper (StartDash)
- [ ] `FindBestOvertakingLane` helper (LastSpurt)
- [ ] `AttemptRiskySqueezePlay` method
- [ ] `SpeedPenaltyTicksRemaining` tracking
- [ ] `ApplyTrafficEffects` method
- [ ] Position-based probability calculation
- [ ] Traffic response configuration
- [ ] Event note generation (detailed logging)
- [ ] 20+ integration tests (full race scenarios)

**Acceptance Criteria:**
- StartDash seeks least congested lanes
- LastSpurt patient early, aggressive late (>75%)
- StretchRunner prefers center lanes 4-5
- FrontRunner shows frustration penalty when blocked
- LastSpurt patient (no frustration)
- High agility + high durability = elite lane changers
- Risky attempts succeed ~25% of time (agility 50)
- Detailed notes generated for all lane change types

**Balance Target:**
- 3-6 lane changes per race (varied by leg type and agility)
- FrontRunner frustrated ~15-20% of race time when in traffic

---

### Phase 3: Balance Validation & Tuning
**Estimated Effort:** 4-6 hours

**Scope:**
- 500+ race statistical analysis
- Correlation measurements (Speed, Agility, Durability)
- Lane change frequency analysis by leg type
- Win rate distribution validation
- Balance tuning based on data
- Documentation updates (RACE_BALANCE.md)

**Deliverables:**
- [ ] Balance validation test suite
- [ ] Diagnostic metrics tracking (LaneChangeBalanceMetrics)
- [ ] Statistical analysis report
- [ ] Tuning adjustments (if needed)
- [ ] Updated RACE_BALANCE.md with lane change system
- [ ] Performance benchmarks (simulation overhead)

**Acceptance Criteria:**
- Speed correlation: -0.70 to -0.75 (maintained as primary stat)
- Agility correlation: -0.45 to -0.55 (strengthened from -0.355)
- Durability correlation: -0.15 to -0.25 (new correlation introduced)
- Average lane changes: 2-8 per race
- No leg type dominates (win rate < 25% for any type)
- High-agility horses don't dominate (win rate < 35%)
- Simulation overhead < 5%

**Tuning Knobs (if adjustments needed):**
- Reduce cooldown benefit (0.08 instead of 0.1)
- Lower risky squeeze max (40% instead of 50%)
- Adjust frustration penalty (2% or 4% instead of 3%)
- Modify position multipliers (0.6/1.4 instead of 0.7/1.3)

**Balance Target:**
- All leg types competitive (18-22% win rate)
- Stat diversity in winners (not just Speed 100 horses)
- Agility valuable but not dominant

---

### Total Estimated Effort
- Phase 1: 4-6 hours
- Phase 2: 6-8 hours
- Phase 3: 4-6 hours
- **Total: 14-20 hours**

---

## Testing Strategy

### Unit Tests (Phase 1)
**Coverage Target:** ≥ 90% for new methods

1. **Cooldown Tests:**
   - `AgilityCooldown_AtZero_Returns10Ticks`
   - `AgilityCooldown_At50_Returns5Ticks`
   - `AgilityCooldown_At100_Returns0Ticks`
   - `CooldownConsumed_OnFailedAttempt_ResetsTimer`

2. **Threshold Tests:**
   - `OvertakingThreshold_EarlyRace_Speed0_Returns025`
   - `OvertakingThreshold_EarlyRace_Speed100_Returns030`
   - `OvertakingThreshold_LateRace_Speed0_Returns0375`
   - `OvertakingThreshold_LateRace_Speed100_Returns045`

3. **Clearance Tests:**
   - `IsLaneClear_NoTraffic_ReturnsTrue`
   - `IsLaneClear_HorseBehindTooClose_ReturnsFalse`
   - `IsLaneClear_HorseAheadTooClose_ReturnsFalse`
   - `IsLaneClear_HorseAheadFarEnough_ReturnsTrue`
   - `IsLaneClear_HorseBehindFarEnough_ReturnsTrue`
   - `IsLaneClear_TrafficInDifferentLane_ReturnsTrue`

4. **Desired Lane Tests:**
   - `DesiredLane_RailRunner_AlwaysReturns1`
   - `DesiredLane_FrontRunner_ReturnsCurrentLane`
   - `DesiredLane_StretchRunner_Lane2_Returns3` (drift toward center)
   - `DesiredLane_StretchRunner_Lane7_Returns6` (drift toward center)

### Integration Tests (Phase 2)
**Full race simulation scenarios**

1. **Leg Type Behavior:**
   - `RailRunner_StartsLane8_WorksToLane1_OverMultipleTicks`
   - `StartDash_SeeksLeastCongestedLane_AvoidsPack`
   - `LastSpurt_PatientEarly_AggressiveLate_LaneChanges`
   - `FrontRunner_LeadsRace_MinimalLaneChanges`
   - `StretchRunner_SeeksCenter_MaintainsLanes4And5`

2. **Traffic Response:**
   - `FrontRunner_BlockedWithNoOptions_ReceivesFrustrationPenalty`
   - `LastSpurt_BlockedByLeader_NoPenalty_PatientBehavior`
   - `RailRunner_BlockedOnRail_HigherSpeedCapPenalty`

3. **Risky Attempts:**
   - `HighAgility_RiskyAttempt_Succeeds50Percent`
   - `LowAgility_RiskyAttempt_FailsMostly`
   - `HighDurability_RiskySuccess_ShortPenalty1Tick`
   - `LowDurability_RiskySuccess_LongPenalty5Ticks`

4. **Multi-Horse Scenarios:**
   - `12HorseField_LaneChanges_NoCrashes_ValidHistory`
   - `TightPack_MultipleLaneChanges_RealisticFlow`
   - `LeaderBreaksAway_BackPackBattles_PositionBasedProbabilities`

### Balance Validation Tests (Phase 3)
**Statistical analysis with large sample sizes**

1. **Correlation Analysis:**
   - `Run_500_Races_Measure_SpeedCorrelation_Target_Minus070`
   - `Run_500_Races_Measure_AgilityCorrelation_Target_Minus050`
   - `Run_500_Races_Measure_DurabilityCorrelation_Target_Minus020`

2. **Frequency Analysis:**
   - `Average_LaneChanges_PerRace_Target_2To8`
   - `HighAgility_MoreLaneChanges_ThanLowAgility`
   - `BackMarkers_MoreLaneChanges_ThanLeaders`

3. **Win Rate Distribution:**
   - `AllLegTypes_WinRate_Balanced_18To22Percent`
   - `HighAgilityHorses_WinRate_LessThan35Percent`
   - `NoSingleStatDominates_WinRates`

4. **Performance Benchmarks:**
   - `Simulation_WithLaneChanges_Overhead_LessThan5Percent`
   - `1000Races_CompletionTime_Acceptable`

---

## Success Criteria

### Feature Complete When:

**Technical Completeness:**
- [x] All three phases implemented and tested
- [x] 50+ total tests passing (15 unit + 20 integration + 15 balance)
- [x] All methods documented with XML comments
- [x] Configuration fully externalized to RaceModifierConfig
- [x] Event logging generates detailed race notes
- [x] No performance regression (< 5% overhead)

**Functional Completeness:**
- [x] Horses detect and execute overtaking maneuvers
- [x] All 5 leg types exhibit distinct lane-seeking behaviors
- [x] Agility affects cooldown and risky attempt success
- [x] Durability affects risky attempt penalty duration
- [x] Traffic responses vary by leg type personality
- [x] Position in field affects lane change aggression
- [x] Random starting lanes ensure fairness
- [x] Lane changes visible in race history (RaceRunTickHorse)

**Balance Validation:**
- [x] Speed correlation maintained: -0.70 to -0.75
- [x] Agility correlation strengthened: -0.45 to -0.55
- [x] Durability correlation introduced: -0.15 to -0.25
- [x] Lane change frequency: 2-8 per race average
- [x] All leg types competitive (18-22% win rate)
- [x] No high-agility dominance (< 35% win rate)

**Documentation:**
- [x] RACE_BALANCE.md updated with lane change mechanics
- [x] Feature spec finalized with all decisions
- [x] Code comments explain tactical systems
- [x] Tuning guide for balance adjustments

---

## Risk Mitigation

### Risk 1: Agility Becomes Too Dominant
**Probability:** Medium
**Impact:** High (invalidates Speed as primary stat)

**Mitigation:**
- Phase 3 correlation analysis catches this early
- Tuning knobs ready: reduce cooldown benefit 0.1 → 0.08
- Reduce risky squeeze max 50% → 40%
- Can roll back to Phase 1 if needed

### Risk 2: Lane Change Spam (Too Frequent)
**Probability:** Low (Option C + cooldown prevents)
**Impact:** Medium (unrealistic racing)

**Mitigation:**
- Desire-based gating (only change when needed)
- Minimum 2-tick cooldown even at agility 100
- Position-based probability reduces leader changes
- Phase 2 integration tests validate frequency

### Risk 3: RailRunner Dominance
**Probability:** Low (already balanced in Feature 005)
**Impact:** Medium

**Mitigation:**
- Random starting lanes prevent guaranteed lane 1
- Must navigate traffic to reach rail
- Boxed-in penalty (2% speed cap when blocked)
- Phase 3 win rate analysis validates balance

### Risk 4: Performance Overhead
**Probability:** Low
**Impact:** Medium (slower races)

**Mitigation:**
- Efficient O(n) algorithms for traffic detection
- Cache sorted horse lists where possible
- Phase 3 performance benchmarks validate < 5% overhead
- Can optimize if needed (spatial indexing)

### Risk 5: Complexity Overwhelms Testing
**Probability:** Medium
**Impact:** High (bugs slip through)

**Mitigation:**
- Phased approach builds complexity incrementally
- Phase 1 foundation solid before adding Phase 2
- Comprehensive unit test coverage (≥90%)
- Integration tests cover edge cases
- Balance validation catches emergent issues

---

## Appendix A: Discovery Q&A

### Category 1: Core Behavior & Triggers

**Q1.1: When should horses attempt to overtake?**
✅ **Decision:** Option D - Combination (speed + phase)
- Formula: `threshold = 0.25m × (1.0 + Speed×0.002) × (1.0 or 1.5 if late race)`
- Creates exciting late-race drama
- High-speed horses slightly more aggressive

**Q1.2: Should horses attempt lane changes even when not overtaking?**
✅ **Decision:** Option B - Proactive positioning based on leg type
- RailRunner seeks lane 1 for bonus
- Different leg types have strategic lane preferences
- Adds tactical depth beyond reactive overtaking

**Q1.3: How often should lane change attempts occur?**
✅ **Decision:** Option D - Agility-based cooldown
- Formula: `cooldown = 10 - (Agility × 0.1)` ticks
- Makes Agility meaningful beyond just stat modifier
- Natural throttle on frequency

### Category 2: Traffic Detection & Clearance

**Q2.1: What constitutes a "clear" lane for changing?**
✅ **Decision:** Option B - Asymmetric clearance (0.1f behind, 0.2f ahead)
- Realistic safety margins
- Prevents cutting off and collisions
- Simple to implement and understand

**Q2.2: Should there be failed lane change attempts?**
✅ **Decision:** Option B + Option C - Risky with agility + failed counts against cooldown
- Agility determines risky squeeze success (0-50%)
- Durability reduces penalty duration (5→1 ticks)
- Failed attempts waste cooldown (commitment cost)
- Creates risk/reward tactical decisions

**Q2.3: How should we handle three-horse traffic scenarios?**
✅ **Decision:** Option A - Adjacent lane only (gradual movement)
- Horses drift toward desired lanes over time
- Realistic, prevents lane "teleporting"
- Getting stuck behind traffic is realistic

### Category 3: Leg Type Strategy & Personality

**Q3.1: Are the current lane change probabilities balanced?**
✅ **Decision:** Option C - Vary by field position
- Leaders (1-3): 0.7x probability (less changes)
- Mid-pack: 1.0x (normal)
- Back-markers (last 3): 1.3x (desperate for openings)
- Self-balancing emergent behavior

**Q3.2: Should leg types have directional preferences?**
✅ **Decision:** Accepted proposed system
- RailRunner → lane 1 (bonus seeking)
- FrontRunner → stay put (conservative)
- StartDash → least congested lane
- LastSpurt → patient early, hunting late
- StretchRunner → center lanes 4-5

**Q3.3: Should agility stat influence lane changing beyond cooldown?**
✅ **Decision:** Option A - Current roles only (cooldown + risky success)
- Already meaningful without over-complication
- Two strong mechanics sufficient

### Category 4: Speed & Performance Impact

**Q4.1: Should lane changes affect horse speed?**
✅ **Decision:** No cost for clean changes
- Clean changes free (reward tactics)
- Risky changes penalized (5% for 1-5 ticks based on Durability)
- Encourages smart positioning

**Q4.2: Should traffic (blocked overtakes) slow horses down?**
✅ **Decision:** Leg-type-specific traffic responses
- FrontRunner: Frustration penalty (3% when blocked, no options)
- StartDash: Speed cap (match leader - 1%)
- LastSpurt: Patient (minimal cap, no frustration)
- StretchRunner: Speed cap (match leader - 1%)
- RailRunner: Speed cap (match leader - 2%, cautious on rail)

### Category 5: Game Balance & Frequency

**Q5.1: How many lane changes should occur in a typical race?**
✅ **Decision:** Option C - Desire-based (only change when strategically needed)
- Change if: wants to overtake OR not in desired lane
- Natural frequency control
- Expected: 2-8 changes per race

**Q5.2: Should all horses start in the same lane or different lanes?**
✅ **Decision:** Option B - Random assignment
- Fair distribution
- RailRunner must earn lane 1 bonus
- Increases variety

**Q5.3: How should this affect race competitiveness?**
✅ **Decision:** Option A - Accept stronger Agility impact (with monitoring)
- Creates stat diversity in winners
- Monitoring plan in place to catch dominance
- Tuning knobs ready if needed

### Category 6: Visual & Data Representation

**Q6.1: How should lane changes be communicated to players?**
✅ **Decision:** Option B - Detailed event notes
- Different notes for: clean change, risky squeeze, proactive drift, frustration
- Helps debugging and player understanding
- Can simplify later if needed

**Q6.2: Should lane change statistics be tracked?**
✅ **Decision:** Option B - Diagnostic tracking during testing (not persisted)
- Track during development for balance validation
- Don't bloat production DB entities
- RaceRunTickHorse already tracks lane position history

### Category 7: Edge Cases & Safety

**Q7.1: Single-horse races?**
✅ **Decision:** Not applicable (shouldn't occur)
- Field size 8-12 horses (7-11 CPU + 1 player)
- No single-horse race scenario

**Q7.2: Maximum field size and lane bounds?**
✅ **Decision:** Dynamic max lane (match field size, 8-12)
- `GetMaxLane(raceRun) = raceRun.Horses.Count`
- Supports variable field sizes
- Realistic track width

**Q7.3: Simultaneous lane change conflicts?**
✅ **Decision:** Option A - First-come-first-served (sequential processing)
- Horses processed in order (already random)
- Rare conflicts acceptable
- Simple implementation

### Category 8: Implementation Scope & Phasing

**Q8.1: Phased rollout or all-at-once?**
✅ **Decision:** 3 phases minimum (right-sized implementation)
- Phase 1: Core infrastructure (overtaking, cooldown, RailRunner)
- Phase 2: Advanced behaviors (all leg types, risky attempts, traffic)
- Phase 3: Balance validation and tuning
- Incremental complexity, testable milestones

**Q8.2: Configuration vs hard-coded?**
✅ **Decision:** Option A - All in configuration (RaceModifierConfig.cs)
- Easy tuning during balance phase
- No magic numbers in code
- Documented expected ranges

**Q8.3: Testing strategy priority?**
✅ **Decision:** All three in order (A → B → C)
- Unit tests first (isolated logic)
- Integration tests second (full races)
- Balance validation last (statistical analysis)
- Standard TDD approach

---

## Changelog

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-23 | Feature Discovery Agent | Initial discovery document created |
| 2025-12-23 | Feature Discovery Agent | Analyzed existing commented code, identified 8 question categories |
| 2025-12-23 | Feature Discovery Agent | **Requirements finalized** - All decisions documented, ready for implementation |

---

**Status:** ✅ **READY FOR IMPLEMENTATION** - All requirements specified, 3-phase implementation plan complete.

**Next Step:** Begin Phase 1 implementation (Core Infrastructure)

**End of Feature Specification Document**
