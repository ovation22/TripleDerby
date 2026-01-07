# Feature 020: Horse Training System

**Status**: Discovery (Revised)
**Created**: 2026-01-05
**Updated**: 2026-01-05

---

## Overview

Implement a comprehensive horse training system that allows players to improve their horses' stats between races through targeted training sessions. Training complements the race-based stat progression system (Feature 018) by giving players strategic control over stat development while managing Happiness. Players can train once between races, choosing from 3 randomly offered training options, making each session a unique strategic choice.

## Goals

1. **Strategic Depth**: Let players target specific stats to prepare for upcoming races
2. **Complement Racing**: Training fills gaps between races and shores up weaknesses
3. **Happiness Management**: Balance training intensity (Happiness cost) against development benefits
4. **Career Integration**: Training effectiveness varies by horse career stage
5. **Build on Existing Foundation**: Leverage existing `Training` and `TrainingSession` entities
6. **Natural Frequency Limit**: Training once between races creates meaningful choice
7. **Variety & Replayability**: Random 3-option selection ensures each session feels different

---

## Existing Foundation

### Current Data Model

**Training** (6 types already defined):
1. Sprint - Speed-focused
2. Endurance Run - Stamina-focused
3. Jump Training - Agility/Strength
4. Hill Climbing - Power/Stamina/Durability
5. Obstacle Course - Agility/Athleticism
6. Swimming - Low-impact conditioning

**TrainingSession** (partially implemented):
- Tracks which horse trained
- Records training type
- Stores result (currently just string)

**Happiness** (already exists):
- Tracked as HorseStatistic (StatisticId.Happiness)
- Already used in race speed and stamina calculations
- Range: 0-100

### What Needs Implementation

- **Stat effects**: Define how each training type affects stats
- **Happiness system**: Happiness depletion, recovery options, overwork risk
- **Calculator service**: Compute training gains (similar to `StatProgressionCalculator`)
- **Service layer**: Execute training sessions, apply effects, enforce once-between-races limit
- **Random selection**: Offer 3 random training options per session
- **Enhanced TrainingSession**: Timestamps, detailed results, stat changes
- **UI integration**: Training menu with 3 options, result display

---

## Requirements

### Functional Requirements

#### FR-1: Training Execution
- Players can select a training type **once between races**
- System offers **3 random training options** from the pool of 10 types
- Training is executed instantly (no mini-games in MVP)
- Stat changes are applied immediately
- Training session is recorded in database
- Cannot train again until after the next race

#### FR-2: Stat Progression
- Each training type provides targeted stat gains
- **Stats CANNOT exceed DominantPotential** (genetic ceiling)
- Attempting to train a maxed stat may have negative effects (overwork penalty)
- Training uses similar growth formula to racing (gap-based)
- Career phase affects training efficiency (young horses train better than in racing)

#### FR-3: Happiness Management
- Training depletes Happiness
- Different training types have different Happiness impacts
- Happiness must be above minimum threshold to train
- Training at low Happiness increases overwork risk
- Recovery options restore Happiness

#### FR-4: Random Training Selection
- Each training session offers exactly 3 random options from the 10 types
- Selection is randomized per session
- Rest/recovery options (Light Exercise, Rest Day) always available as one of the 3

#### FR-5: Training History
- All training sessions are logged
- Players can view training history per horse
- History shows date, training type, stat changes, Happiness impact

### Non-Functional Requirements

#### NFR-1: Balance
- Training gains should be **smaller** than racing gains (0.5-1.5 points vs 1-3 points)
- "Once between races" limit ensures training doesn't replace racing
- Happiness management creates strategic choice of training intensity
- Training complements racing, doesn't replace it
- Random 3-option selection adds variety and prevents "always optimal" strategies

#### NFR-2: Performance
- Training execution should be near-instant (<100ms)
- Stat calculations reuse existing `StatProgressionCalculator` patterns
- Database writes are batched (horse stats + training session)

#### NFR-3: Extensibility
- Training types can be added via database seeding
- Stat effects are configuration-driven (like `RaceModifierConfig`)
- Service layer supports future features (mini-games, training equipment, trainers)

---

## Technical Design

### Enhanced Data Model

#### Training (Existing - Expand Configuration)

```csharp
public class Training
{
    public byte Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    // NEW: Training effects
    public double SpeedModifier { get; set; }     // Multiplier for Speed growth (0.0-2.0)
    public double StaminaModifier { get; set; }   // Multiplier for Stamina growth
    public double AgilityModifier { get; set; }   // Multiplier for Agility growth
    public double DurabilityModifier { get. set; } // Multiplier for Durability growth

    public double HappinessCost { get; set; }     // Happiness depletion (can be negative for recovery)
    public double OverworkRisk { get; set; }      // Overwork probability (0.0-1.0)
    public bool IsRecovery { get; set; }          // If true, always offered as recovery option
}
```

**Training Type Definitions** (10 types):

| ID | Name | Description | Speed | Stam | Agil | Dur | Happiness | Overwork Risk | Recovery? |
|----|------|-------------|-------|------|------|-----|-----------|---------------|-----------|
| 1 | Sprint Drills | Explosive speed work | 2.0 | 0.5 | 1.0 | 0.5 | -3.0 | 0.05 | No |
| 2 | Distance Gallops | Long conditioning | 0.5 | 2.0 | 0.5 | 1.0 | -3.0 | 0.03 | No |
| 3 | Agility Course | Pole weaving, turns | 1.0 | 0.5 | 2.0 | 0.5 | -2.5 | 0.02 | No |
| 4 | Strength Training | Weight/resistance work | 0.5 | 1.0 | 0.5 | 2.0 | -2.5 | 0.02 | No |
| 5 | Hill Climbing | Incline power work | 1.5 | 1.5 | 0.8 | 1.2 | -4.0 | 0.08 | No |
| 6 | Interval Training | Speed/stamina mix | 1.5 | 1.5 | 1.2 | 0.8 | -4.0 | 0.06 | No |
| 7 | Swimming | Low-impact conditioning | 0.3 | 1.5 | 0.5 | 1.5 | -1.0 | 0.01 | No |
| 8 | Gate Practice | Start drills | 1.5 | 0.3 | 1.2 | 0.3 | -2.0 | 0.03 | No |
| 9 | Light Exercise | Recovery workout | 0.2 | 0.2 | 0.2 | 0.5 | +3.0 | 0.00 | Yes |
| 10 | Rest Day | Complete rest | 0.0 | 0.0 | 0.0 | 0.0 | +8.0 | 0.00 | Yes |

**Note**: Recovery options (IsRecovery = true) are always included in the 3 random options when Happiness < 50.

#### TrainingSession (Existing - Enhance Details)

```csharp
public class TrainingSession
{
    public Guid Id { get; set; }
    public byte TrainingId { get; set; }
    public virtual Training Training { get; set; } = null!;

    public Guid HorseId { get; set; }
    public virtual Horse Horse { get; set; } = null!;

    // NEW: Detailed session data
    public DateTime SessionDate { get; set; }
    public short RaceStartsAtTime { get; set; }     // Career stage when trained

    // Stat changes from this session
    public double SpeedGain { get; set; }
    public double StaminaGain { get; set; }
    public double AgilityGain { get; set; }
    public double DurabilityGain { get; set; }
    public double HappinessChange { get; set; }      // Can be negative (cost) or positive (recovery)

    public bool OverworkOccurred { get; set; }

    [Obsolete("Use individual stat gain fields instead")]
    public string Result { get; set; } = null!;   // Keep for backward compatibility
}
```

---

## Service Layer

### ITrainingCalculator

```csharp
public interface ITrainingCalculator
{
    /// <summary>
    /// Calculates stat growth from training session.
    /// Similar to race-based growth but with training-specific modifiers.
    /// Returns 0 if stat is at or above DominantPotential.
    /// </summary>
    /// <param name="actualStat">Current stat value</param>
    /// <param name="dominantPotential">Genetic ceiling</param>
    /// <param name="trainingModifier">Training type multiplier (0.0-2.0)</param>
    /// <param name="careerMultiplier">Career phase efficiency</param>
    /// <param name="happinessModifier">Happiness-based effectiveness multiplier</param>
    /// <returns>Stat growth amount (0 if at ceiling)</returns>
    double CalculateTrainingGain(
        double actualStat,
        double dominantPotential,
        double trainingModifier,
        double careerMultiplier,
        double happinessModifier);

    /// <summary>
    /// Calculates career phase multiplier for training.
    /// Young horses train better than they race.
    /// </summary>
    /// <param name="raceStarts">Total number of races</param>
    /// <returns>Training efficiency multiplier</returns>
    double CalculateTrainingCareerMultiplier(short raceStarts);

    /// <summary>
    /// Calculates happiness impact from training (considering current happiness and overwork risk).
    /// </summary>
    /// <param name="baseHappinessCost">Training type's base happiness cost</param>
    /// <param name="currentHappiness">Horse's current happiness</param>
    /// <param name="overworkRisk">Training type's overwork probability</param>
    /// <returns>Tuple of (actualHappinessChange, overworkOccurred)</returns>
    (double happinessChange, bool overwork) CalculateHappinessImpact(
        double baseHappinessCost,
        double currentHappiness,
        double overworkRisk);

    /// <summary>
    /// Calculates happiness-based effectiveness multiplier for training.
    /// Similar to how happiness affects racing, but for training gains.
    /// </summary>
    /// <param name="happiness">Current happiness (0-100)</param>
    /// <returns>Effectiveness multiplier (0.5 to 1.0)</returns>
    double CalculateHappinessEffectivenessModifier(double happiness);

    /// <summary>
    /// Calculates LegType bonus for training effectiveness.
    /// Horses get +20% effectiveness when training matches their running style.
    /// </summary>
    /// <param name="legType">Horse's running style</param>
    /// <param name="trainingId">Training type ID</param>
    /// <returns>1.20 if training matches leg type, 1.0 otherwise</returns>
    double CalculateLegTypeBonus(LegTypeId legType, byte trainingId);
}
```

### ITrainingService

```csharp
public interface ITrainingService
{
    /// <summary>
    /// Executes a training session for a horse (once between races).
    /// </summary>
    /// <param name="horseId">Horse to train</param>
    /// <param name="trainingId">Training type</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Training session result with stat changes</returns>
    /// <exception cref="InvalidOperationException">If horse has already trained since last race</exception>
    Task<TrainingSessionResult> TrainAsync(
        Guid horseId,
        byte trainingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets 3 random training options for a horse.
    /// Ensures recovery options are included if happiness is low.
    /// </summary>
    /// <param name="horseId">Horse to get options for</param>
    /// <param name="cancellationToken"></param>
    /// <returns>3 randomly selected training types</returns>
    Task<List<Training>> GetAvailableTrainingOptionsAsync(
        Guid horseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training history for a horse.
    /// </summary>
    Task<List<TrainingSession>> GetTrainingHistoryAsync(
        Guid horseId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a horse can train (hasn't trained since last race, has minimum happiness).
    /// </summary>
    bool CanTrain(Horse horse);
}
```

### TrainingSessionResult (DTO)

```csharp
public record TrainingSessionResult
{
    public Guid SessionId { get; init; }
    public string TrainingName { get; init; } = null!;
    public bool Success { get; init; }
    public string Message { get; init; } = null!;

    // Stat changes
    public double SpeedGain { get; init; }
    public double StaminaGain { get; init; }
    public double AgilityGain { get; init; }
    public double DurabilityGain { get; init; }
    public double HappinessChange { get; init; }

    // Happiness impact
    public bool OverworkOccurred { get; init; }
    public double CurrentHappiness { get; init; }

    // Stats at ceiling (for UI feedback)
    public bool SpeedAtCeiling { get; init; }
    public bool StaminaAtCeiling { get; init; }
    public bool AgilityAtCeiling { get; init; }
    public bool DurabilityAtCeiling { get; init; }
}
```

---

## Training Gain Formula

Training uses a **modified version** of the race stat progression formula:

```
IF (Actual >= DominantPotential):
    TrainingGain = 0  // At ceiling, no growth

ELSE:
    TrainingGain = (DominantPotential - Actual) × BaseRate × CareerMultiplier × TrainingModifier × HappinessModifier × LegTypeBonus
```

**Compared to Racing**:
- **BaseRate**: `0.015` (75% of race rate 0.02) - training is slightly slower
- **CareerMultiplier**: **Different from racing** - Young horses train BETTER
  - Young (0-9 races): **1.20** (vs racing 0.80)
  - Prime (10-29): **1.40** (vs racing 1.20)
  - Veteran (30-49): **0.80** (vs racing 0.60)
  - Old (50+): **0.40** (vs racing 0.20)
- **TrainingModifier**: From training type (0.0-2.0) - allows targeted stat growth
- **HappinessModifier**: `1.0` at 100 happiness, scales down to `0.5` at 0 happiness
- **LegTypeBonus**: `1.20` if training matches horse's running style, `1.0` otherwise

### Career Multiplier Rationale

**Racing**: Experience matters more. Young horses lack race savvy, old horses decline.
**Training**: Learning capacity matters more. Young horses are eager students, old horses can still improve with practice (better than in races).

### Happiness Effectiveness Modifier Formula

```csharp
double CalculateHappinessEffectivenessModifier(double happiness)
{
    // Clamp to valid range
    happiness = Math.Clamp(happiness, 0, 100);

    // Linear scaling: 0% happiness = 0.50x effectiveness, 100% happiness = 1.00x effectiveness
    return 0.5 + (happiness / 100.0) * 0.5;
}
```

### Stat Ceiling Enforcement

```csharp
double CalculateTrainingGain(double actualStat, double dominantPotential, ...)
{
    // Strict ceiling enforcement
    if (actualStat >= dominantPotential)
    {
        return 0;  // No growth at ceiling
    }

    // Calculate gap-based growth
    var gap = dominantPotential - actualStat;
    var growth = gap * BaseRate * careerMultiplier * trainingModifier * happinessModifier;

    // Ensure we don't exceed ceiling
    if (actualStat + growth > dominantPotential)
    {
        growth = dominantPotential - actualStat;
    }

    return growth;
}
```

### LegType Bonus System

Horses naturally excel at training that matches their running style:

```csharp
double CalculateLegTypeBonus(LegTypeId legType, byte trainingId)
{
    if (TrainingConfig.LegTypePreferredTraining.TryGetValue(legType, out var preferredTrainingId))
    {
        if (preferredTrainingId == trainingId)
        {
            return TrainingConfig.LegTypeBonusMultiplier; // 1.20
        }
    }
    return 1.0; // No bonus
}
```

**Mappings**:
- **StartDash** → Sprint Drills (+20%): Explosive horses love speed work
- **FrontRunner** → Interval Training (+20%): Aggressive pacers thrive on intensity
- **StretchRunner** → Distance Gallops (+20%): Late-race horses benefit from endurance work
- **LastSpurt** → Hill Climbing (+20%): Closers develop power for final kicks
- **RailRunner** → Agility Course (+20%): Rail specialists excel at precise maneuvering

### Overwork System (Replaces Injury)

- **Overwork Chance**: Based on training type's `OverworkRisk` and current happiness
- **Formula**: `FinalRisk = BaseRisk × HappinessPenalty`
  - At 100 happiness: `HappinessPenalty = 1.0` (base risk)
  - At 50 happiness: `HappinessPenalty = 2.0` (double risk)
  - At 25 happiness: `HappinessPenalty = 4.0` (quadruple risk)
- **Overwork Effect**: If overwork occurs, happiness drops by additional 5 points, training gains reduced by 50%

---

## Configuration

### TrainingConfig (New)

```csharp
public static class TrainingConfig
{
    // Growth rates
    public const double BaseTrainingGrowthRate = 0.015;  // 75% of race rate

    // Happiness thresholds
    public const double MinimumHappinessToTrain = 15.0;     // Can't train below 15% happiness
    public const double HappinessWarningThreshold = 40.0;   // Warn player below 40%
    public const double OverworkHappinessPenalty = 5.0;      // Happiness lost if overworked
    public const double OverworkGainReduction = 0.50;       // Training gain multiplier if overworked

    // Career phase multipliers (training-specific, differs from racing)
    public const double YoungHorseTrainingMultiplier = 1.20;   // Young horses train BETTER (0-9 races)
    public const double PrimeHorseTrainingMultiplier = 1.40;   // Prime is best (10-29 races)
    public const double VeteranHorseTrainingMultiplier = 0.80; // Veterans train OK (30-49 races)
    public const double OldHorseTrainingMultiplier = 0.40;     // Old horses still learn (50+ races)

    // Random selection
    public const int TrainingOptionsOffered = 3;  // Always offer 3 options

    // LegType training bonuses
    public const double LegTypeBonusMultiplier = 1.20;  // +20% for matching training

    // LegType → Training mappings
    public static readonly Dictionary<LegTypeId, byte> LegTypePreferredTraining = new()
    {
        { LegTypeId.StartDash, 1 },      // Sprint Drills
        { LegTypeId.FrontRunner, 6 },    // Interval Training
        { LegTypeId.StretchRunner, 2 },  // Distance Gallops
        { LegTypeId.LastSpurt, 5 },      // Hill Climbing
        { LegTypeId.RailRunner, 3 }      // Agility Course
    };
}
```

---

## User Flow

### Training Flow (Happy Path)

1. **Player navigates to Training Menu** (between races)
2. **System generates 3 random training options**:
   - If Happiness >= 50: 3 random options from all 10 types
   - If Happiness < 50: 1 recovery option + 2 random regular options
3. **System displays**:
   - Current horse stats (Speed, Stamina, Agility, Durability, Happiness)
   - 3 available training types with effects and happiness costs
   - Which stats are at genetic ceiling (no growth possible)
   - Whether training is available (once between races)
4. **Player selects training type** (e.g., "Sprint Drills")
5. **System validates**:
   - Horse happiness >= minimum threshold (15%)
   - Horse has not trained since last race
   - Selected training is one of the 3 offered options
6. **System executes training**:
   - Calculate stat gains (with career phase, happiness modifiers)
   - Apply happiness cost
   - Check for overwork (RNG based on risk)
   - Apply stat changes (respecting DominantPotential ceilings)
   - Save TrainingSession record
   - Mark horse as "trained since last race"
7. **System displays results**:
   - "Sprint Drills Complete!"
   - Speed: 45.2 → 46.8 (+1.6)
   - Agility: 38.5 → 38.9 (+0.4)
   - Stamina: 52.0 → 52.2 (+0.2)
   - Happiness: 85 → 82 (-3)

### Error Cases

- **Already Trained**: "Your horse has already trained since the last race. Race again to unlock training."
- **Low Happiness**: "Your horse is too stressed to train safely. Happiness: 12%. Try Light Exercise or Rest Day."
- **Overwork Occurred**: "Training pushed your horse too hard! Overworked. Happiness: 45 → 35 (-10). Limited gains from this session."
- **Stat at Ceiling**: "Speed is at genetic maximum (65.0). No Speed gains possible. Consider training other stats."

---

## Integration Points

### With Feature 018 (Race Stat Progression)

- **Training uses DIFFERENT career phase system** (young horses train better than they race)
- **Training uses same genetic ceiling** (`DominantPotential`)
- **Training complements racing**: Smaller, targeted gains vs larger, random racing gains
- **Both systems track RaceStarts** for career phase
- **Both systems use Happiness** (training affects it, racing uses it)

### With Feeding System (Existing)

- Feeds already affect Happiness
- Training system uses current Happiness value
- Players must balance: Feed (boost Happiness) → Train (deplete Happiness) → Race

### With Economy System (Future Enhancement)

- Future versions may add training costs as an economic balance
- For now, "once between races" limit is the primary balance mechanism

### With Breeding System

- Trained stats (Actual) are visible to breeders
- High Actual stats make horses more valuable for breeding
- Training lets players "finish" a horse's development before breeding

---

## Success Criteria

### Must Have (MVP)

- [ ] 10 training types with distinct stat effects
- [ ] Training execution service (calculate gains, apply effects, save session)
- [ ] Happiness management (depletes with training, recovers with rest options)
- [ ] "Once between races" limit enforcement
- [ ] Random 3-option selection system
- [ ] Stat ceiling enforcement (no growth beyond DominantPotential)
- [ ] Training history (view past sessions)
- [ ] Basic UI (training menu with 3 options, results screen, training availability status)

### Should Have

- [ ] Overwork system (random overwork based on risk and happiness)
- [ ] Happiness warnings (alert player when happiness < 40%)
- [ ] Training recommendations (suggest training type for upcoming race)
- [ ] Visual indicators for stats at ceiling
- [ ] LegType training bonuses (horses get +20% effectiveness for training that matches their running style)

### Could Have (Future Enhancements)

- [ ] Mini-games for training sessions (interactive element)
- [ ] Training equipment (purchase items that boost effectiveness)
- [ ] Hired trainers (specialists that improve specific stats)
- [ ] Training plans (schedule multiple sessions in advance)
- [ ] Training analytics (graphs of stat progress over time)
- [ ] Feed integration (temporary buffs to training)
- [ ] Overwork penalties for attempting to train maxed stats

---

## Open Questions

1. **Training Tracking**: How to track "trained since last race"?
   - **Option A**: `Horse.LastTrainingDate` + `Horse.LastRaceDate` comparison
   - **Option B**: Boolean flag `HasTrainedSinceLastRace` reset after races
   - **Recommendation**: Option B (simpler, more explicit)

2. **Recovery Option Forcing**: Should recovery options ALWAYS appear when happiness < 50?
   - **Recommendation**: Yes, guarantee at least 1 recovery option when stressed

3. **Random Seed**: Should the 3 options be stable for a session (seed-based) or re-randomize each page refresh?
   - **Recommendation**: Stable until training is performed (prevent refresh-scumming)

4. **Maxed Stat Penalty**: What happens if player tries to train a stat that's at DominantPotential?
   - **Option A**: No penalty, just show "no growth" message
   - **Option B**: Happiness penalty for overworking a maxed stat
   - **Recommendation**: Option A for MVP, Option B for future enhancement

5. **Young Horse Training Multiplier**: Confirm that young horses should train BETTER (1.20x) vs racing (0.80x)?
   - **RESOLVED: Yes** (user confirmed)

6. **LegType Training Bonuses**: Should horses get bonuses for training that matches their running style?
   - **RESOLVED: Yes** - Add as "Should Have" feature
   - StartDash → Sprint Drills (+20%)
   - FrontRunner → Interval Training (+20%)
   - StretchRunner → Distance Gallops (+20%)
   - LastSpurt → Hill Climbing (+20%)
   - RailRunner → Agility Course (+20%)

---

## Architecture Guidance

### Lessons from Racing Service Retrospective

Based on lessons learned from Feature 018 and the racing service retrospective, the Training System should follow these architectural patterns:

#### 1. Service Layer Pattern
- **TrainingService** should be clean and focused (like RaceOrchestrator, not RaceExecutor)
- Delegate complex calculations to **TrainingCalculator** (like StatProgressionCalculator)
- Keep service methods under 50 lines each

#### 2. RabbitMQ Integration
- **Training session execution should publish to RabbitMQ**
- Pattern: `TrainHorseCommand` → RabbitMQ → `TrainingConsumer` → `TrainingService.ExecuteTrainingAsync()`
- Benefits:
  - Consistent with race execution pattern
  - Enables async processing
  - Supports future features (training queues, scheduled training)
  - Better error handling and retry logic

#### 3. Redis Caching for Training Options
- **Cache the 3 random training options** per horse session
- Pattern: Similar to `GetParentHorses` caching
- Cache key: `training:options:{horseId}:{sessionId}`
- TTL: Until training is performed or session expires (30 minutes)
- Benefits:
  - Prevents refresh-scumming (stable options)
  - Reduces database load
  - Consistent experience across page refreshes

#### 4. Clean Separation of Concerns
```
┌─────────────────────────────────────────────────────────────┐
│ API Layer: TrainingController                               │
│ - Validates requests                                         │
│ - Publishes TrainHorseCommand to RabbitMQ                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Message Queue: RabbitMQ                                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Consumer: TrainingConsumer                                   │
│ - Receives TrainHorseCommand                                 │
│ - Calls TrainingService                                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Application Layer: TrainingService                           │
│ - Orchestrates training workflow                             │
│ - Validates business rules                                   │
│ - Uses TrainingCalculator for computations                   │
│ - Saves TrainingSession to database                          │
│ - Clears Redis cache                                         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Domain Layer: TrainingCalculator                             │
│ - Pure calculation logic                                     │
│ - No dependencies on infrastructure                          │
│ - Fully unit testable                                        │
└─────────────────────────────────────────────────────────────┘
```

#### 5. Redis Cache Pattern for Training Options

```csharp
// In TrainingService.GetAvailableTrainingOptionsAsync()
public async Task<List<Training>> GetAvailableTrainingOptionsAsync(
    Guid horseId,
    CancellationToken cancellationToken = default)
{
    // Generate session ID (could be passed from UI or generated here)
    var sessionId = Guid.NewGuid();
    var cacheKey = $"training:options:{horseId}:{sessionId}";

    // Try to get from cache first
    var cachedOptions = await _cache.GetAsync<List<Training>>(cacheKey, cancellationToken);
    if (cachedOptions != null)
    {
        return cachedOptions;
    }

    // Cache miss: generate new random options
    var horse = await _horseRepository.GetByIdAsync(horseId, cancellationToken);
    var allTrainings = await _trainingRepository.GetAllAsync(cancellationToken);

    var options = GenerateRandomOptions(horse, allTrainings);

    // Cache for 30 minutes
    await _cache.SetAsync(cacheKey, options, TimeSpan.FromMinutes(30), cancellationToken);

    return options;
}
```

#### 6. Command Pattern for Training Execution

```csharp
public record TrainHorseCommand
{
    public Guid HorseId { get; init; }
    public byte TrainingId { get; init; }
    public Guid SessionId { get; init; }  // Links to cached options
    public Guid UserId { get; init; }
}

// Controller publishes command
[HttpPost("train")]
public async Task<IActionResult> TrainHorse([FromBody] TrainHorseRequest request)
{
    var command = new TrainHorseCommand
    {
        HorseId = request.HorseId,
        TrainingId = request.TrainingId,
        SessionId = request.SessionId,
        UserId = User.GetId()
    };

    await _messageBus.PublishAsync(command);

    return Accepted(); // 202 Accepted - processing asynchronously
}
```

#### 7. Error Handling & Domain Events (Future)

Following the retrospective recommendations, consider:
- **Domain Events**: `TrainingCompletedEvent`, `OverworkOccurredEvent`
- **Event Handlers**: Clean separation of concerns (stat updates, achievements, notifications)
- **Not required for MVP** but plan for it in Phase 2+

---

## Implementation Phases

### Phase 1: Data Model & Configuration
1. Expand Training entity with stat modifiers, happiness costs, overwork risks
2. Enhance TrainingSession with detailed fields
3. Add `Horse.HasTrainedSinceLastRace` boolean flag
4. Create TrainingConfig class
5. Update ModelBuilderExtensions with 10 training types (expand existing 6)
6. Create database migration

### Phase 2: Calculator & Service Layer
1. Implement TrainingCalculator (stat growth, happiness impact, career multipliers)
2. Implement random 3-option selection logic
3. Implement TrainingService (execute training, validate, save)
4. Unit tests for TrainingCalculator (various scenarios, ceiling enforcement)
5. Unit tests for TrainingService (happy path, errors, random selection)

### Phase 3: Race Integration (Reset Training Flag)
1. Update RaceService to reset `HasTrainedSinceLastRace` after race completion
2. Integration tests for training → race → training flow

### Phase 4: API & UI
1. Create TrainingController (API endpoints)
2. Add training menu UI (show 3 random options, display effects)
3. Add training results UI (display stat changes)
4. Add training history UI (view past sessions)
5. Integration tests (end-to-end training flow)

### Phase 5: Polish & Balance
1. Tune training happiness depletion rates
2. Tune overwork probabilities
3. Add happiness warnings and training recommendations
4. Add visual indicators for stats at ceiling
5. Add training analytics/history graphs
6. Performance optimization (if needed)

---

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Training makes racing obsolete | High | Medium | Balance: Training gains 75% of race gains, limited to once between races |
| Players grind happiness to zero | Medium | High | Minimum happiness threshold (15%), exponential effectiveness drop |
| Overwork system too punishing | Medium | Medium | Overwork reduces effectiveness, doesn't block training entirely |
| Random 3-option feels unfair | Medium | Medium | Guarantee recovery options when happiness is low |
| Training types feel samey | Medium | Low | Clear visual feedback, distinct stat patterns, varied happiness costs |
| Performance issues with history | Low | Low | Limit history queries to 20 sessions, add index on HorseId |
| Players forget they already trained | Low | Medium | Clear UI indicator showing training availability status |
| Stat ceiling confusion | Medium | Low | Clear UI messaging when stat is at ceiling, explain no growth possible |

---

## Future Enhancements

1. **Mini-Games**: Interactive training sessions (timing challenges, pattern matching, etc.)
2. **Training Equipment**: Purchase items (weights, hurdles, etc.) that boost specific stats
3. **Elite Trainers**: Hire specialist trainers (Sprint Coach, Endurance Guru) for bonuses
4. **Training Plans**: Schedule 5 sessions in advance with auto-execution
5. **Seasonal Effects**: Winter training less effective, summer training more risky
6. **Training Facilities**: Upgrade player's training grounds for permanent bonuses
7. **Multi-Horse Training**: Train multiple horses simultaneously (bulk operations)
8. **Overwork Penalties**: Happiness penalty for repeatedly training maxed stats
9. **Training Competitions**: Compete with friends for best training gains in a week

---

## Appendix: Example Training Session

**Scenario**: Player trains a prime-age horse (15 races) with 85% happiness

**Horse Stats**:
- Speed: 45.2 / 65.0 (Actual / DominantPotential)
- Stamina: 52.0 / 70.0
- Agility: 38.5 / 60.0
- Durability: 50.3 / 72.0
- Happiness: 85.0
- LegType: StartDash

**System offers 3 random options**:
1. Sprint Drills (Speed: 2.0, Happiness: -3.0, Risk: 0.05) ⭐ **Matches LegType!**
2. Swimming (Stamina: 1.5, Happiness: -1.0, Risk: 0.01)
3. Agility Course (Agility: 2.0, Happiness: -2.5, Risk: 0.02)

**Player selects**: Sprint Drills

**Calculations**:
```
CareerMultiplier = 1.40 (Prime, 15 races - TRAINING multiplier, not racing)
HappinessModifier = 0.5 + (85 / 100) * 0.5 = 0.925
LegTypeBonus = 1.20 (StartDash + Sprint Drills = MATCH!)

SpeedGain = (65.0 - 45.2) × 0.015 × 1.40 × 2.0 × 0.925 × 1.20
          = 19.8 × 0.015 × 1.40 × 2.0 × 0.925 × 1.20
          = 0.918

StaminaGain = (70.0 - 52.0) × 0.015 × 1.40 × 0.5 × 0.925 × 1.20
            = 18.0 × 0.015 × 1.40 × 0.5 × 0.925 × 1.20
            = 0.210

AgilityGain = (60.0 - 38.5) × 0.015 × 1.40 × 1.0 × 0.925 × 1.20
            = 21.5 × 0.015 × 1.40 × 1.0 × 0.925 × 1.20
            = 0.500

DurabilityGain = (72.0 - 50.3) × 0.015 × 1.40 × 0.5 × 0.925 × 1.20
               = 21.7 × 0.015 × 1.40 × 0.5 × 0.925 × 1.20
               = 0.252

OverworkCheck = Random(0.0-1.0) < 0.05? → No overwork
HappinessChange = -3.0
```

**Results**:
- Speed: 45.2 → 46.1 (+0.9) 🏃 **Boosted by LegType match!**
- Stamina: 52.0 → 52.2 (+0.2)
- Agility: 38.5 → 39.0 (+0.5)
- Durability: 50.3 → 50.5 (+0.2)
- Happiness: 85.0 → 82.0 (-3.0)

**Player sees**: "Sprint Drills Complete! ⭐ Your StartDash excels at this training! Speed +0.9, Agility +0.5, Stamina +0.2, Durability +0.2. Happiness 82%."

---

**Feature 020 Status**: Ready for review and implementation planning.
