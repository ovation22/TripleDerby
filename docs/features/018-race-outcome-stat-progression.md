# Feature 018: Race Outcome Stat Progression System

**Status:** 📋 DISCOVERY COMPLETE - Ready for Implementation Planning

## Summary

Implements a comprehensive stat progression system where race outcomes directly affect horse development. Racing develops **Actual** stats toward their **DominantPotential** genetic ceilings, with race-type-specific focus, career phase modifiers, and performance-based bonuses. **Happiness** changes immediately based on race results and decays toward neutral over time, creating ongoing maintenance requirements. This feature completes the gameplay loop: breed genetic potential → race to develop it → retire at peak → breed superior offspring.

**Design Philosophy:** Horses are born with genetic potential (DominantPotential) but only 33-50% realized (Actual stats). Racing provides the life experience that helps them reach their genetic ceiling. Peak development occurs during prime career years (races 10-30), with diminishing returns afterward encouraging strategic retirement timing. Happiness represents morale and mental state—winning builds confidence, losing damages it, and both require ongoing care to maintain.

---

## Requirements

### Functional Requirements

#### Core Stat Development
- [ ] After each race, increase Actual stats toward DominantPotential ceiling
- [ ] Base growth rate: 2% of remaining gap per race
- [ ] Actual stats can NEVER exceed DominantPotential (genetic ceiling enforced)
- [ ] All four performance stats affected: Speed, Stamina, Agility, Durability
- [ ] Growth is immediate after race completion (no delayed/batched processing)

#### Career Phase System
- [ ] Young horses (0-9 races): 80% efficiency (still learning)
- [ ] Prime horses (10-29 races): 120% efficiency (peak development)
- [ ] Veteran horses (30-49 races): 60% efficiency (slowing development)
- [ ] Old horses (50+ races): 20% efficiency (minimal gains, retirement time)

#### Race-Type Stat Focus
- [ ] Sprint races (≤6 furlongs): Speed +50%, Agility +25%, others -25%
- [ ] Classic races (7-10 furlongs): All stats develop equally (100%)
- [ ] Distance races (11+ furlongs): Stamina +50%, Durability +25%, others -25%
- [ ] Focus multipliers stack with career phase and performance bonuses

#### Performance-Based Bonuses
- [ ] 1st place: +50% growth bonus (win accelerates development)
- [ ] 2nd place: +25% growth bonus
- [ ] 3rd place: +10% growth bonus
- [ ] Mid-pack (4th to 50th percentile): Normal growth (100%)
- [ ] Back of pack (bottom 50%): -25% growth penalty (still learn, just slower)

#### Happiness System
- [ ] Happiness changes immediately after race based on finish position:
  - 1st place: +8 Happiness
  - 2nd place: +4 Happiness
  - 3rd place: +2 Happiness
  - Mid-pack: 0 Happiness (neutral)
  - Back of pack: -3 Happiness (frustration)
- [ ] Exhaustion penalty: -5 Happiness if finished with <10% stamina remaining
- [ ] Happiness bounded to [0, 100] range (cannot overflow)
- [ ] Happiness decays toward neutral (50) over time at 10% per period
- [ ] Decay prevents permanent happiness/unhappiness states

### Acceptance Criteria

#### Stat Development
- [ ] Given young horse with Speed Actual=42, DominantPotential=85 after 1st race (mid-pack, classic), Actual increases by ~0.69 points → 43
- [ ] Given prime horse (15 races) with Speed Actual=55, Potential=85 after winning sprint race, Actual increases by ~1.08 points → 56
- [ ] Given veteran horse (40 races) with Speed Actual=70, Potential=85 after race, Actual increases by ~0.18 points → 71 (slow growth)
- [ ] Given horse with Speed Actual=84, Potential=85, after any race, Actual caps at 85 (cannot exceed genetic ceiling)
- [ ] Given horse racing 6f sprint vs 12f marathon, Speed develops faster in sprint, Stamina faster in marathon

#### Performance Impact
- [ ] Given two identical horses (Actual=50, Potential=80) over 10 races: winner finishes with Actual=59, loser with Actual=54 (5-point gap)
- [ ] Given horse finishing back of pack 5 races in row, stats still increase but at 75% rate (learning continues despite poor performance)

#### Happiness Dynamics
- [ ] Given horse with Happiness=50 winning race, Happiness becomes 58 (+8)
- [ ] Given horse with Happiness=50 finishing last in 10-horse field, Happiness becomes 47 (-3)
- [ ] Given horse finishing race with 5% stamina remaining, Happiness reduced by additional -5 (exhaustion trauma)
- [ ] Given horse with Happiness=80 after 1 week with no activity, Happiness decays toward 50 by ~3 points → 77
- [ ] Given horse with Happiness=20 after 1 week with no activity, Happiness recovers toward 50 by ~3 points → 23

#### Career Progression
- [ ] Given horse over 50-race career (mid-pack finishes), Actual stats reach 85-90% of DominantPotential ceiling
- [ ] Given horse in races 1-10 vs races 20-30, races 20-30 show 50% faster stat growth (prime development phase)
- [ ] Given horse after 50 races, stat growth drops to 20% efficiency (retirement signal)

### Non-Functional Requirements
- [ ] Performance: Stat updates add <5ms to race completion processing
- [ ] Data Integrity: All stat updates use single database transaction (prevent partial updates)
- [ ] Auditability: RaceRunHorse entity tracks which stats changed by how much (for debugging/analysis)
- [ ] Testability: All formulas unit testable with deterministic inputs
- [ ] Maintainability: Configuration constants externalized to RaceModifierConfig
- [ ] Future-proofing: Design reserves space for Training/Feeding enhancements

---

## Technical Analysis

### Affected Systems

**Core Entities:**
- `Horse` (Horse.cs) - Read/write Actual stats after races
- `HorseStatistic` (HorseStatistic.cs) - Update Actual values, respect DominantPotential ceiling
- `RaceRun` (RaceRun.cs) - No schema changes needed, already tracks completion
- `RaceRunHorse` (RaceRunHorse.cs) - Potential: add tracking fields for stat deltas (optional)

**Services:**
- `RaceExecutor` (Services.Racing/RaceExecutor.cs) - Add stat progression logic to FinalizeResults()
- `FeedingService` (Core/Services/FeedingService.cs) - Already has AffectHorseStatistic() helper (reuse pattern)
- `TrainingService` (Core/Services/TrainingService.cs) - Future: implement similar stat growth with higher rates

**Configuration:**
- `RaceModifierConfig` (Services.Racing/Racing/RaceModifierConfig.cs) - Add stat progression constants

**Repositories:**
- `ITripleDerbyRepository` - Use existing UpdateAsync(horse) for stat persistence

### Data Model Changes

**No schema migrations required.** All required fields already exist:
- `HorseStatistic.Actual` (current stat value) ✅
- `HorseStatistic.DominantPotential` (genetic ceiling) ✅
- `Horse.RaceStarts` (career phase tracking) ✅
- `Horse.Happiness` (property accessor) ✅

**Optional Enhancement (Future):**
```csharp
// Add to RaceRunHorse for analytics/debugging
public byte SpeedGained { get; set; }
public byte StaminaGained { get; set; }
public byte AgilityGained { get; set; }
public byte DurabilityGained { get; set; }
public int HappinessChange { get; set; }
```

### Integration Points

**RaceExecutor.FinalizeResults() (lines 315-342):**
```csharp
private void FinalizeResults(RaceRun raceRun)
{
    // Existing code: sort horses, assign places, update win/place/show counters

    // NEW: Apply stat progression after race
    foreach (var raceRunHorse in raceRun.Horses)
    {
        ApplyRaceOutcomeStatProgression(raceRunHorse, raceRun);
        ApplyHappinessChange(raceRunHorse, raceRun);
    }

    // Existing code: repository.UpdateAsync() already called in Race() method
}
```

**New Service Methods:**
```csharp
private void ApplyRaceOutcomeStatProgression(RaceRunHorse raceRunHorse, RaceRun raceRun);
private void ApplyHappinessChange(RaceRunHorse raceRunHorse, RaceRun raceRun);
private double CalculateAgeMultiplier(short raceStarts);
private double GetStatFocusMultiplier(decimal furlongs, StatisticId stat);
private double GetPerformanceBonus(byte place, int fieldSize);
private byte GrowStat(HorseStatistic stat, double totalMultiplier);
```

**Happiness Decay (New Scheduled Job - TBD):**
```csharp
// Could be:
// - Daily cron job
// - Triggered on page load (last login check)
// - Part of training/feeding flow
public async Task DecayHappinessForAllHorses()
{
    var horses = await repository.GetAllAsync<Horse>();
    foreach (var horse in horses)
    {
        ApplyHappinessDecay(horse);
    }
    await repository.SaveChangesAsync();
}
```

### Configuration Constants

**Add to RaceModifierConfig.cs:**
```csharp
// Stat Development Base Rates
public const double BaseStatGrowthRate = 0.02;  // 2% of gap per race

// Career Phase Multipliers
public const double YoungHorseMultiplier = 0.80;     // Races 0-9
public const double PrimeHorseMultiplier = 1.20;     // Races 10-29
public const double VeteranHorseMultiplier = 0.60;   // Races 30-49
public const double OldHorseMultiplier = 0.20;       // Races 50+

// Race-Type Focus Multipliers
public const double SprintSpeedMultiplier = 1.50;
public const double SprintAgilityMultiplier = 1.25;
public const double SprintOtherMultiplier = 0.75;

public const double DistanceStaminaMultiplier = 1.50;
public const double DistanceDurabilityMultiplier = 1.25;
public const double DistanceOtherMultiplier = 0.75;

// Performance Bonuses
public const double WinBonus = 1.50;
public const double PlaceBonus = 1.25;
public const double ShowBonus = 1.10;
public const double BackOfPackPenalty = 0.75;

// Happiness Changes
public const int WinHappinessBonus = 8;
public const int PlaceHappinessBonus = 4;
public const int ShowHappinessBonus = 2;
public const int BackOfPackHappinessPenalty = -3;
public const int ExhaustionHappinessPenalty = -5;
public const double ExhaustionStaminaThreshold = 0.10;  // <10% stamina

// Happiness Decay
public const double HappinessDecayRate = 0.10;  // 10% toward neutral per period
public const int HappinessNeutralPoint = 50;
```

### Risks & Challenges

**Balance Risks:**
- **Stat Inflation:** If growth rates too high, horses reach ceiling too fast (tested with 50-race simulation)
- **Grind Incentive:** If racing alone maxes stats, Training becomes pointless (mitigated by stronger Training gains)
- **Happiness Volatility:** Extreme happiness swings could frustrate players (decay system stabilizes)

**Technical Challenges:**
- **Transaction Safety:** Must update all stats atomically (use existing repository.UpdateAsync pattern)
- **Performance:** Updating 8-12 horses per race × stat calculations (minimal impact, simple math)
- **Testing Complexity:** Career progression requires multi-race simulation tests

**Design Challenges:**
- **Training/Feeding Balance:** Need to reserve design space for future enhancements
- **Decay Timing:** When/how to run happiness decay (daily job? login check? training trigger?)
- **UI Communication:** Players need to understand why their horse's stats changed

### Dependencies

**Existing Systems (Prerequisites):**
- ✅ Breeding system with genetic potential (DominantPotential) - COMPLETE
- ✅ Racing simulation with finish positions - COMPLETE
- ✅ FeedingService pattern for stat modification - EXISTS
- ✅ HorseStatistic entity with Actual/Potential fields - EXISTS

**Future Enhancements (Dependent Features):**
- ⏳ Training system implementation (should provide 3-5% gains vs racing's 2%)
- ⏳ Feeding system enhancement (maintain happiness, minor stat boosts)
- ⏳ Happiness decay scheduler (daily job or login-based)
- ⏳ Career stats UI (show stat progression over time)
- ⏳ Retirement recommendations (UI hint when gains slow down)

---

## Mathematical Design

### Core Stat Growth Formula

```csharp
/// <summary>
/// Calculates stat growth after a race.
/// Formula: BaseGain = (DominantPotential - Actual) × 0.02
///          FinalGain = BaseGain × AgeMultiplier × RaceTypeMultiplier × PerformanceBonus
/// </summary>
private byte GrowStat(
    HorseStatistic stat,
    short raceStarts,
    decimal raceFurlongs,
    byte finishPlace,
    int fieldSize)
{
    // 1. Calculate remaining gap to genetic ceiling
    double gap = stat.DominantPotential - stat.Actual;
    if (gap <= 0)
        return stat.Actual; // Already at ceiling, no growth

    // 2. Base growth: 2% of remaining gap
    double baseGain = gap * RaceModifierConfig.BaseStatGrowthRate;

    // 3. Apply career phase multiplier
    double ageMultiplier = CalculateAgeMultiplier(raceStarts);

    // 4. Apply race-type stat focus
    double focusMultiplier = GetStatFocusMultiplier(raceFurlongs, stat.StatisticId);

    // 5. Apply performance bonus
    double perfBonus = GetPerformanceBonus(finishPlace, fieldSize);

    // 6. Calculate final gain
    double totalMultiplier = ageMultiplier * focusMultiplier * perfBonus;
    double finalGain = baseGain * totalMultiplier;

    // 7. Apply gain and cap at genetic ceiling
    byte newActual = (byte)Math.Min(
        stat.Actual + Math.Round(finalGain),
        stat.DominantPotential
    );

    return newActual;
}
```

### Career Phase Multipliers

```csharp
/// <summary>
/// Returns development efficiency based on career stage.
/// Young horses are still learning, prime horses develop fastest,
/// veterans slow down, old horses barely improve.
/// </summary>
private double CalculateAgeMultiplier(short raceStarts)
{
    if (raceStarts < 10)
        return RaceModifierConfig.YoungHorseMultiplier;      // 0.80
    else if (raceStarts < 30)
        return RaceModifierConfig.PrimeHorseMultiplier;      // 1.20
    else if (raceStarts < 50)
        return RaceModifierConfig.VeteranHorseMultiplier;    // 0.60
    else
        return RaceModifierConfig.OldHorseMultiplier;        // 0.20
}
```

### Race-Type Stat Focus

```csharp
/// <summary>
/// Returns stat-specific multiplier based on race distance.
/// Sprints develop Speed/Agility, Distance races develop Stamina/Durability,
/// Classic races develop all stats equally.
/// </summary>
private double GetStatFocusMultiplier(decimal furlongs, StatisticId stat)
{
    // Sprint races (≤6 furlongs)
    if (furlongs <= 6)
    {
        return stat switch
        {
            StatisticId.Speed => RaceModifierConfig.SprintSpeedMultiplier,        // 1.50
            StatisticId.Agility => RaceModifierConfig.SprintAgilityMultiplier,    // 1.25
            _ => RaceModifierConfig.SprintOtherMultiplier                         // 0.75
        };
    }

    // Distance races (11+ furlongs)
    if (furlongs >= 11)
    {
        return stat switch
        {
            StatisticId.Stamina => RaceModifierConfig.DistanceStaminaMultiplier,     // 1.50
            StatisticId.Durability => RaceModifierConfig.DistanceDurabilityMultiplier, // 1.25
            _ => RaceModifierConfig.DistanceOtherMultiplier                          // 0.75
        };
    }

    // Classic races (7-10 furlongs) - balanced development
    return 1.00;
}
```

### Performance Bonus

```csharp
/// <summary>
/// Returns growth multiplier based on finish position.
/// Winners develop faster (positive reinforcement), losers still learn but slower.
/// </summary>
private double GetPerformanceBonus(byte place, int fieldSize)
{
    if (place == 1)
        return RaceModifierConfig.WinBonus;        // 1.50
    if (place == 2)
        return RaceModifierConfig.PlaceBonus;      // 1.25
    if (place == 3)
        return RaceModifierConfig.ShowBonus;       // 1.10

    // Mid-pack: top 50% of field gets normal development
    if (place <= fieldSize / 2)
        return 1.00;

    // Back of pack: bottom 50% gets reduced development (but still learn!)
    return RaceModifierConfig.BackOfPackPenalty;   // 0.75
}
```

### Happiness Change Formula

```csharp
/// <summary>
/// Calculates immediate happiness change after race.
/// Winning boosts morale, losing damages it, exhaustion compounds frustration.
/// </summary>
private int CalculateHappinessChange(
    byte finishPlace,
    int fieldSize,
    double currentStamina,
    double initialStamina)
{
    // Base change from finish position
    int baseChange = finishPlace switch
    {
        1 => RaceModifierConfig.WinHappinessBonus,        // +8
        2 => RaceModifierConfig.PlaceHappinessBonus,      // +4
        3 => RaceModifierConfig.ShowHappinessBonus,       // +2
        _ => finishPlace <= fieldSize / 2
            ? 0                                            // Mid-pack: neutral
            : RaceModifierConfig.BackOfPackHappinessPenalty // Back: -3
    };

    // Exhaustion penalty: finishing with <10% stamina is traumatic
    double staminaPercent = initialStamina > 0
        ? currentStamina / initialStamina
        : 1.0;

    if (staminaPercent < RaceModifierConfig.ExhaustionStaminaThreshold)
    {
        baseChange += RaceModifierConfig.ExhaustionHappinessPenalty; // -5
    }

    return baseChange;
}

/// <summary>
/// Applies happiness change and clamps to valid range [0, 100].
/// </summary>
private void ApplyHappinessChange(RaceRunHorse raceRunHorse, RaceRun raceRun)
{
    var happinessStat = raceRunHorse.Horse.Statistics
        .Single(s => s.StatisticId == StatisticId.Happiness);

    int change = CalculateHappinessChange(
        raceRunHorse.Place,
        raceRun.Horses.Count,
        (double)raceRunHorse.CurrentStamina,
        (double)raceRunHorse.InitialStamina
    );

    int newHappiness = Math.Clamp(
        happinessStat.Actual + change,
        0,
        100
    );

    happinessStat.Actual = (byte)newHappiness;
}
```

### Happiness Decay Formula

```csharp
/// <summary>
/// Decays happiness toward neutral point (50) over time.
/// Prevents permanent extreme happiness/unhappiness states.
/// Rate: 10% of distance to neutral per period.
/// </summary>
private void ApplyHappinessDecay(Horse horse)
{
    var happinessStat = horse.Statistics
        .Single(s => s.StatisticId == StatisticId.Happiness);

    int current = happinessStat.Actual;
    int neutral = RaceModifierConfig.HappinessNeutralPoint; // 50

    // Calculate 10% movement toward neutral
    int distance = neutral - current;
    int change = (int)Math.Round(distance * RaceModifierConfig.HappinessDecayRate);

    // Apply decay
    int newHappiness = Math.Clamp(current + change, 0, 100);
    happinessStat.Actual = (byte)newHappiness;
}
```

---

## Example Scenarios

### Scenario 1: Young Horse First 10 Races

**Horse Profile:**
- Speed: Actual=42, DominantPotential=85
- Stamina: Actual=45, DominantPotential=80
- Career: RaceStarts=0 (young phase)
- Initial Happiness: 50

**Race 1: 8f Classic, 5th/10 (mid-pack)**
```
Age multiplier: 0.80 (young)
Focus multiplier: 1.00 (classic, all stats equal)
Performance bonus: 1.00 (mid-pack)

Speed gain = (85-42) × 0.02 × 0.80 × 1.00 × 1.00 = 0.69 → 43
Stamina gain = (80-45) × 0.02 × 0.80 × 1.00 × 1.00 = 0.56 → 46
Happiness: 5th/10 = mid-pack = 0 change → 50
```

**Race 5: 6f Sprint, 1st/8 (WIN!)**
```
Age multiplier: 0.80 (still young)
Focus multiplier: 1.50 (sprint favors Speed)
Performance bonus: 1.50 (win)

Speed: Actual=46, gap=39
Speed gain = 39 × 0.02 × 0.80 × 1.50 × 1.50 = 1.40 → 48
Stamina gain = 35 × 0.02 × 0.80 × 0.75 × 1.50 = 0.32 → 49
Happiness: 1st place = +8 → 58
```

**Race 10: 12f Distance, 8th/10 (exhausted finish)**
```
Age multiplier: 1.20 (just entered prime)
Focus multiplier: 1.50 for Stamina, 0.75 for Speed
Performance bonus: 0.75 (back of pack)
Exhaustion: <10% stamina remaining

Speed: Actual=52, gap=33
Speed gain = 33 × 0.02 × 1.20 × 0.75 × 0.75 = 0.45 → 53
Stamina gain = 31 × 0.02 × 1.20 × 1.50 × 0.75 = 0.84 → 53
Happiness: -3 (back of pack) - 5 (exhaustion) = -8 → 50
```

### Scenario 2: Prime Career Horse (Race 20)

**Horse Profile:**
- Speed: Actual=63, DominantPotential=85
- RaceStarts=20 (prime phase)
- Happiness=65 (confident veteran)

**Race 21: 10f Classic, 2nd/12 (Place)**
```
Age multiplier: 1.20 (prime)
Focus multiplier: 1.00 (classic)
Performance bonus: 1.25 (place)

Speed gain = (85-63) × 0.02 × 1.20 × 1.00 × 1.25 = 0.66 → 64
Happiness: +4 (place) → 69
```

**After 10 more races (mix of wins/places in prime phase):**
- Speed progresses: 64 → 71 (~7 points over 10 races)
- Approaching 84% of genetic potential (71/85)
- Happiness stabilizes around 68-72 (winning keeps morale high)

### Scenario 3: Veteran Horse (Race 40)

**Horse Profile:**
- Speed: Actual=73, DominantPotential=85
- RaceStarts=40 (veteran phase)
- Happiness=55 (moderate)

**Race 41: 8f Classic, 3rd/10 (Show)**
```
Age multiplier: 0.60 (veteran)
Focus multiplier: 1.00 (classic)
Performance bonus: 1.10 (show)

Speed gain = (85-73) × 0.02 × 0.60 × 1.00 × 1.10 = 0.16 → 74
Happiness: +2 (show) → 57
```

**Insight:** Gains have slowed to ~0.1-0.2 points per race. Horse is 87% of potential (74/85). Ideal retirement window approaching—breeding value still high, but racing returns diminishing.

### Scenario 4: Happiness Decay Over Time

**Horse Profile:**
- Happiness: 85 (happy from recent wins)
- No racing activity for 3 weeks

**Week 1:**
```
Current: 85, Neutral: 50
Distance: 50 - 85 = -35
Decay: -35 × 0.10 = -3.5 → -4
New Happiness: 85 - 4 = 81
```

**Week 2:**
```
Current: 81, Distance: -31
Decay: -31 × 0.10 = -3.1 → -3
New Happiness: 81 - 3 = 78
```

**Week 3:**
```
Current: 78, Distance: -28
Decay: -28 × 0.10 = -2.8 → -3
New Happiness: 78 - 3 = 75
```

**Insight:** Happiness gradually drifts toward neutral. After 3 weeks without racing or feeding, happiness dropped from 85 → 75. Player needs to feed or race to maintain high morale.

---

## Implementation Phases

### Phase 1: Core Stat Progression (MVP)
**Goal:** Basic stat growth after races with career phase system

**Tasks:**
1. Add configuration constants to RaceModifierConfig
2. Implement CalculateAgeMultiplier() helper
3. Implement GetPerformanceBonus() helper
4. Implement GrowStat() method
5. Integrate into RaceExecutor.FinalizeResults()
6. Add unit tests for growth calculations
7. Add integration test for 50-race career simulation
8. Manual testing: verify stats grow and cap at DominantPotential

**Acceptance:** Horse stats increase after races, respect genetic ceiling, show career phase effects

### Phase 2: Race-Type Stat Focus
**Goal:** Different races develop different stats

**Tasks:**
1. Implement GetStatFocusMultiplier() helper
2. Apply focus multipliers in GrowStat()
3. Add unit tests: sprint develops Speed, distance develops Stamina
4. Add balance validation test: verify multipliers don't break progression
5. Manual testing: race same horse in 6f vs 12f, confirm stat differences

**Acceptance:** Sprint races develop Speed/Agility faster, distance races develop Stamina/Durability faster

### Phase 3: Happiness System
**Goal:** Race outcomes affect morale, happiness decays over time

**Tasks:**
1. Implement CalculateHappinessChange() helper
2. Implement ApplyHappinessChange() in FinalizeResults()
3. Add exhaustion detection logic (<10% stamina)
4. Add unit tests for happiness changes
5. Implement ApplyHappinessDecay() method (standalone, not integrated yet)
6. Add unit tests for decay formula
7. Manual testing: verify winning increases happiness, losing decreases it

**Acceptance:** Happiness changes after races, bounds to [0,100], decay formula works correctly

### Phase 4: Happiness Decay Scheduler (Future)
**Goal:** Automate happiness decay over real time

**Tasks:**
1. Design decay trigger mechanism (daily job? login check? training trigger?)
2. Implement scheduled job or trigger logic
3. Add decay timestamp tracking to Horse entity (LastHappinessDecay)
4. Apply decay on trigger
5. Add integration tests
6. UI indication of last decay date

**Acceptance:** Happiness automatically decays toward 50 over time without player action

### Phase 5: Analytics & UI (Future)
**Goal:** Show players their horse's progression history

**Tasks:**
1. Add RaceRunHorse stat delta fields (SpeedGained, etc.) - optional
2. Create career progression chart UI
3. Add "Retirement Recommended" indicator when age multiplier drops
4. Show genetic potential vs actual stats in horse details
5. Highlight recent stat changes in race results

**Acceptance:** Players can see stat growth history and understand progression

---

## Testing Strategy

### Unit Tests

**Stat Growth Calculations:**
- [ ] Given young horse (5 races), verify age multiplier = 0.80
- [ ] Given prime horse (15 races), verify age multiplier = 1.20
- [ ] Given veteran horse (35 races), verify age multiplier = 0.60
- [ ] Given old horse (55 races), verify age multiplier = 0.20
- [ ] Given 6f sprint, verify Speed multiplier = 1.50, Stamina = 0.75
- [ ] Given 12f distance, verify Stamina multiplier = 1.50, Speed = 0.75
- [ ] Given 1st place, verify performance bonus = 1.50
- [ ] Given 5th/10 place, verify performance bonus = 1.00
- [ ] Given 9th/10 place, verify performance bonus = 0.75
- [ ] Given stat at ceiling (Actual=85, Potential=85), verify no growth
- [ ] Given stat 1 point from ceiling, verify growth caps exactly at ceiling

**Happiness Changes:**
- [ ] Given 1st place finish, verify happiness +8
- [ ] Given 2nd place finish, verify happiness +4
- [ ] Given 8th/10 finish, verify happiness -3
- [ ] Given finish with 5% stamina, verify happiness -5 exhaustion penalty
- [ ] Given happiness=95 + win (+8), verify caps at 100
- [ ] Given happiness=5 + loss (-3), verify floors at 0

**Happiness Decay:**
- [ ] Given happiness=80, verify decays toward 50 by -3 per period
- [ ] Given happiness=20, verify recovers toward 50 by +3 per period
- [ ] Given happiness=50, verify no decay (already neutral)

### Integration Tests

**Career Simulation:**
- [ ] Simulate 50-race career with mid-pack finishes, verify final stats reach 85-90% of potential
- [ ] Simulate 30-race career with wins, verify faster progression than mid-pack
- [ ] Simulate 30-race career with losses, verify slower progression than mid-pack
- [ ] Verify stats never exceed DominantPotential across any scenario

**Multi-Stat Interaction:**
- [ ] Race horse in 10 sprints, verify Speed/Agility grow faster than Stamina/Durability
- [ ] Race horse in 10 distance races, verify Stamina/Durability grow faster than Speed
- [ ] Race horse in 10 classics, verify all stats grow equally

**Database Persistence:**
- [ ] Verify stat changes persist after race completion
- [ ] Verify RaceStarts counter increments correctly (used for age multiplier)
- [ ] Verify all 4 stats + Happiness update in single transaction

### Balance Validation Tests

**Progression Curves:**
- [ ] Run 1000-race simulation, verify stats don't inflate beyond balance thresholds
- [ ] Verify prime career (races 10-30) shows measurably faster growth than young (0-10)
- [ ] Verify old horses (50+ races) show minimal growth (retirement signal)

**Happiness Stability:**
- [ ] Simulate 100 races with mixed results, verify happiness stays in reasonable range (30-70)
- [ ] Verify feeding can offset negative happiness trends
- [ ] Verify decay prevents permanent extreme happiness states

### Manual Testing Checklist

- [ ] Create new foal (Actual=40-50), race 10 times, verify stats increase
- [ ] Race horse in sprints only, verify Speed develops faster than Stamina
- [ ] Race horse in distance only, verify Stamina develops faster than Speed
- [ ] Win 5 races in a row, verify happiness increases to ~90+
- [ ] Lose 5 races in a row, verify happiness decreases to ~20-30
- [ ] Check horse at race 50, verify stat growth has slowed significantly
- [ ] Verify stats cap at DominantPotential and never exceed

---

## Game Balance Considerations

### Stat Progression Balance

**Progression Rate Tuning:**
- Base 2% growth ensures 50-race career reaches ~85-90% of potential (not 100%, reserves room for Training)
- Prime career multiplier (1.20) creates meaningful "peak years" window
- Old horse multiplier (0.20) encourages retirement without hard-blocking continued racing

**Training/Feeding Balance:**
- Racing: 2% base growth (passive progression)
- Training (future): 3-5% growth per session (active, resource-intensive)
- Feeding (future): +1-2 flat points (maintenance, happiness focus)

**Why this works:**
- Racing alone cannot max out stats (reaches 85-90%, leaves gap)
- Training is most efficient for targeted stat development
- Feeding maintains happiness, prevents decay from ruining morale

### Happiness Balance

**Volatility Management:**
- Win bonus (+8) × 5 wins = +40 happiness (can't spam to 100 easily)
- Loss penalty (-3) × 10 losses = -30 happiness (can tank, but not catastrophic)
- Decay rate (10% per period) stabilizes extremes within 5-10 periods

**Strategic Depth:**
- Players must balance racing frequency with rest/feeding
- Overracing (grinding) risks happiness crash
- Underracing (hoarding) causes happiness decay
- Sweet spot: race regularly, feed periodically

### Retirement Incentives

**Natural Retirement Timing:**
- Race 30-40: Stat growth slows (60% efficiency), breeding value still high
- Race 50+: Minimal growth (20%), strong signal to retire
- Genetic ceiling: Cannot exceed DominantPotential, even with infinite races

**Why players will retire horses:**
- Stat growth becomes inefficient after race 50
- Breeding requires retired horses (assumption)
- Well-developed horses breed better foals (incentive to retire at peak)

---

## Open Questions

### Happiness Decay Timing
**Question:** When should happiness decay be applied?

**Options:**
1. **Daily cron job** - scheduled task runs every 24 hours
   - Pros: Predictable, real-time decay
   - Cons: Requires background job infrastructure
2. **On login** - decay applied when player logs in
   - Pros: Simple, no background jobs
   - Cons: Inactive players' horses frozen in time
3. **On training/feeding action** - decay applied when player interacts with horse
   - Pros: Integrated into existing systems
   - Cons: Horses that are never trained/fed never decay
4. **On race** - decay applied before each race
   - Pros: No new infrastructure needed
   - Cons: Horses that aren't raced never decay

**Recommendation:** Start with option #4 (on race) for MVP, migrate to option #1 (daily job) in Phase 4.

### Training System Integration
**Question:** How should Training interact with this stat progression system?

**Current Design Assumption:**
- Training provides **higher growth rates** (3-5% vs racing's 2%)
- Training allows **targeted stat development** (player chooses which stat)
- Training has **no race-count diminishing returns** (always effective)
- Training costs **resources/time** (balances higher efficiency)

**Needs User Confirmation:** Is this the intended Training design?

### Feeding System Enhancement
**Question:** Should Feeding provide any stat growth beyond Happiness?

**Current System:**
- Feeding only increases Happiness (+0 to +1 currently)

**Proposed Enhancement:**
- Feeding could add +1-2 flat points to random stat
- Still caps at DominantPotential
- Provides minor stat maintenance without breaking balance

**Needs User Confirmation:** Should Feeding affect stats beyond Happiness?

### Stat Delta Tracking
**Question:** Should we track how much each stat changed per race for analytics?

**Options:**
1. **No tracking** - stats update silently
2. **Session-only tracking** - show deltas in race results, don't persist
3. **Full tracking** - add SpeedGained, StaminaGained fields to RaceRunHorse entity

**Recommendation:** Start with option #1 (no tracking) for MVP, add option #3 (full tracking) in Phase 5 for analytics/UI.

---

## Success Criteria

### Feature Complete When:
- [ ] Horses' Actual stats increase after races toward DominantPotential ceiling
- [ ] Career phase system (young/prime/veteran/old) affects growth rates
- [ ] Race type (sprint/classic/distance) affects which stats develop faster
- [ ] Finish position affects growth rate (winners develop faster)
- [ ] Happiness changes immediately after races based on performance
- [ ] Happiness decays toward neutral (50) over time
- [ ] Stats never exceed DominantPotential genetic ceiling
- [ ] All tests passing (unit, integration, balance validation)
- [ ] 50-race career simulation reaches 85-90% of genetic potential
- [ ] Manual testing confirms expected progression patterns

### Definition of Done:
- [ ] Code implemented and reviewed
- [ ] Unit tests written and passing (90%+ coverage)
- [ ] Integration tests written and passing
- [ ] Balance validation tests confirm progression curves
- [ ] Configuration constants documented
- [ ] API documentation updated (if new endpoints added)
- [ ] Database migrations tested (if schema changes)
- [ ] Manual QA testing completed
- [ ] Feature specification updated with final implementation details

---

## Future Enhancements

### Post-MVP Features

**Training System Full Implementation:**
- Targeted stat training (player chooses which stat to develop)
- Higher growth rates (3-5% vs racing's 2%)
- Resource costs (money, time, items?)
- No career-phase diminishing returns (always effective)
- Mini-games or training types (speed drills, endurance runs, etc.)

**Feeding System Enhancements:**
- Multiple feeding types with different effects
- Happiness maintenance as primary purpose
- Minor stat boosts (+1-2 flat points)
- Feeding schedules and dietary preferences
- Special feeds with temporary buffs

**Happiness Decay Automation:**
- Daily scheduled job for decay application
- Last decay timestamp tracking
- UI indicators for happiness drift
- Decay notifications

**Career Analytics UI:**
- Stat progression charts over time
- Career milestone tracking
- Retirement recommendations
- Genetic potential vs actual visualization
- "Hall of Fame" for legendary horses

**Injury System:**
- Rare race injuries that temporarily reduce stats
- Requires rest/feeding to heal
- Adds risk to overracing
- Creates strategic decisions

**Aging/Retirement:**
- Hard age cap (70-80 races?) forcing retirement
- Stat degradation if racing beyond peak
- Retirement ceremony/bonuses
- Integration with breeding system

---

## References

**Related Features:**
- Feature 001: Race Engine - core racing simulation
- Feature 004: Stamina Depletion - stamina mechanics used for exhaustion detection
- Feature 006: Happiness Modifier - happiness affects race performance
- Feature 009: Purse Distribution - earnings tracking already implemented
- Breeding System (BreedingExecutor.cs) - genetic potential system

**Key Files:**
- `TripleDerby.Services.Racing/RaceExecutor.cs` - main integration point
- `TripleDerby.Core/Entities/Horse.cs` - stat properties
- `TripleDerby.Core/Entities/HorseStatistic.cs` - Actual/Potential fields
- `TripleDerby.Core/Services/FeedingService.cs` - pattern for stat modification
- `TripleDerby.Services.Breeding/BreedingExecutor.cs` - genetic system reference

**Documentation:**
- `docs/features/006-happiness-modifier.md` - happiness impact on racing
- `docs/features/004-stamina-depletion.md` - stamina mechanics
- Breeding genetics analysis (conversation context)

---

## Notes

**Design Decisions:**
- Immediate stat updates chosen over experience accumulation (user preference)
- Race-type specific development adds strategic depth (user confirmed)
- Diminishing returns after 30-50 races encourages retirement (user confirmed)
- Temporary happiness with decay creates maintenance gameplay (user confirmed)
- 2% base growth rate balances with future Training system (3-5% reserved)

**Balance Philosophy:**
- Racing provides baseline progression (2% growth)
- Training provides focused development (3-5% growth, future)
- Feeding provides maintenance (happiness + minor boosts, future)
- Genetic ceiling (DominantPotential) is absolute and immutable
- Career progression creates natural arc: learn → peak → decline → retire

**Implementation Priorities:**
1. **MVP:** Core stat progression with career phases (Phases 1-2)
2. **Full Feature:** Add happiness system (Phase 3)
3. **Future:** Happiness decay automation (Phase 4)
4. **Polish:** Analytics and UI (Phase 5)

---

**Last Updated:** 2026-01-02
**Feature Owner:** Game Design Team
**Target Release:** TBD (after feature approval)
