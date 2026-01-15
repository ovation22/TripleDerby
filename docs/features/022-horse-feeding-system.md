# Feature 022: Horse Feeding System

**Status**: Discovery Complete
**Created**: 2026-01-14

---

## Overview

Implement a comprehensive horse feeding system that allows players to improve their horses' happiness and stats between races through strategic feeding choices. Feeding complements the Training System (Feature 020) by providing a parallel care mechanic focused primarily on happiness restoration with secondary stat benefits. Players can feed once between races, choosing from 3 randomly offered options, with each horse having unique discoverable preferences that affect feeding effectiveness.

## Goals

1. **Happiness Management**: Primary mechanism for restoring happiness depleted by training
2. **Discovery Mechanic**: Each horse has hidden preferences (favorites/hated) revealed through feeding
3. **Strategic Choice**: 3 random daily options create meaningful decisions
4. **Complement Training**: Feeding focuses on happiness; training focuses on stats
5. **Build on Existing Foundation**: Leverage existing `Feeding` and `FeedingSession` entities
6. **Variety & Replayability**: 18 feed types across 6 categories with unique effects
7. **Horse Personality**: Preference system gives each horse distinct character

---

## Existing Foundation

### Current Data Model

**Feeding** (6 types already seeded - will expand to 18):
- Apple, Carrot, Oats, Sugar Cube, Hay, Peppermint

**FeedingSession** (partially implemented):
- Tracks which horse was fed
- Records feeding type
- Stores result (FeedResponse enum)

**FeedResponse** (already exists):
- Accepted, Rejected, Favorite, Meh, Hated

**Happiness** (already exists):
- Tracked as HorseStatistic (StatisticId.Happiness)
- Used in race speed and stamina calculations
- Range: 0-100

### What Needs Implementation

- **Stat effects**: Define how each feeding type affects stats and happiness
- **Category system**: Organize 18 feeds into 6 categories with base effects
- **Preference system**: Generate and discover horse preferences
- **Calculator service**: Compute feeding gains (similar to `TrainingCalculator`)
- **Service layer**: Execute feeding sessions, apply effects, discover preferences
- **Random selection**: Offer 3 random feeding options daily
- **Enhanced entities**: Category, stat modifiers, preference storage

---

## Requirements

### Functional Requirements

#### FR-1: Feeding Execution
- Players can select a feeding type **once between races**
- System offers **3 random feeding options** daily (cached until next day)
- Feeding is executed instantly
- Stat/happiness changes are applied immediately
- Feeding session is recorded in database
- Cannot feed again until after the next race

#### FR-2: Happiness & Stat Progression
- Each feeding type provides happiness gains (primary effect)
- Some feeding types provide small stat gains (secondary effect)
- **Stats CANNOT exceed DominantPotential** (genetic ceiling)
- Effects are modified by horse's preference for that feed
- Career phase affects preference probabilities

#### FR-3: Preference Discovery System
- Each horse has hidden preferences for each feed type
- Preferences are generated on **first feeding** using deterministic seeding
- Preference levels: Favorite (+50%), Liked (+25%), Neutral (0%), Disliked (-25%), Hated (-50%)
- Preferences are revealed when that feed is tried for the first time
- Once discovered, preferences are permanent and visible

#### FR-4: Category-Based Preference Weights
- Feed categories influence preference probability
- Treats more likely to be favorites, supplements more likely neutral
- LegType influences which categories are more likely to be favorites
- Career phase influences preference probabilities (young = more favorites, old = pickier)

#### FR-5: Random Feeding Selection
- Each day offers exactly 3 random options from the 18 types
- Selection is randomized daily and cached
- Options refresh the next day (not on page refresh)
- If a known favorite has been discovered, guarantee it appears when happiness < 50

#### FR-6: Feeding History
- All feeding sessions are logged
- Players can view feeding history per horse
- History shows date, feeding type, effects, discovered preference

### Non-Functional Requirements

#### NFR-1: Balance
- Feeding gains should be **primarily happiness** with **tiny stat gains**
- "Once between races" limit balances with training
- Training depletes happiness, feeding restores it
- Both can be done between races (independent limits)

#### NFR-2: Performance
- Feeding execution should be near-instant (<100ms)
- Preference calculation uses deterministic seeding (no pre-storage needed)
- Database writes are batched (horse stats + feeding session + preference if new)

#### NFR-3: Extensibility
- Feed types can be added via database seeding
- Categories and effects are configuration-driven
- Service layer supports future features (feeding schedules, special diets)

---

## Technical Design

### New Data Model

#### FeedingCategory (New Enum)

```csharp
public enum FeedingCategoryId : byte
{
    Treats = 1,
    Fruits = 2,
    Grains = 3,
    Proteins = 4,
    Supplements = 5,
    Premium = 6
}
```

#### Feeding (Existing - Expand Configuration)

```csharp
public class Feeding
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    // NEW: Category and effects
    public FeedingCategoryId CategoryId { get; set; }

    // Happiness effect (range - rolled randomly each feeding)
    public double HappinessMin { get; set; }      // e.g., 3.0
    public double HappinessMax { get; set; }      // e.g., 5.0

    // Stat effects (range - rolled randomly each feeding)
    public double StaminaMin { get; set; }        // e.g., 0.0
    public double StaminaMax { get; set; }        // e.g., 0.2
    public double DurabilityMin { get; set; }
    public double DurabilityMax { get; set; }
    public double SpeedMin { get; set; }
    public double SpeedMax { get; set; }
    public double AgilityMin { get; set; }
    public double AgilityMax { get; set; }
}
```

#### HorseFeedingPreference (New Entity)

```csharp
public class HorseFeedingPreference
{
    [Key]
    public Guid Id { get; set; }

    public Guid HorseId { get; set; }
    public virtual Horse Horse { get; set; } = null!;

    public byte FeedingId { get; set; }
    public virtual Feeding Feeding { get; set; } = null!;

    public FeedResponse Preference { get; set; }  // Favorite, Liked, Neutral, Disliked, Hated

    public DateTime DiscoveredDate { get; set; }
}
```

#### FeedingSession (Existing - Enhance Details)

```csharp
public class FeedingSession
{
    [Key]
    public Guid Id { get; set; }

    public byte FeedingId { get; set; }
    public virtual Feeding Feeding { get; set; } = null!;

    public Guid HorseId { get; set; }
    public virtual Horse Horse { get; set; } = null!;

    public FeedResponse Result { get; set; }

    // NEW: Detailed session data
    public DateTime SessionDate { get; set; }
    public short RaceStartsAtTime { get; set; }     // Career stage when fed

    // Actual effects from this session
    public double HappinessGain { get; set; }
    public double StaminaGain { get; set; }
    public double DurabilityGain { get; set; }
    public double SpeedGain { get; set; }
    public double AgilityGain { get; set; }

    public bool PreferenceDiscovered { get; set; }  // True if this was first time
}
```

#### Horse (Add Flag)

```csharp
// Add to Horse entity
public bool HasFedSinceLastRace { get; set; }
```

---

## Feed Type Definitions (18 Types)

### Category: Treats (4 types)
High happiness, no stats. Most likely to be favorites.

| ID | Name | Description | Happiness | Stats |
|----|------|-------------|-----------|-------|
| 1 | Sugar Cube | A sweet treat | 4.0-5.0 | None |
| 2 | Peppermint | Refreshing mint candy | 3.5-4.5 | None |
| 3 | Honey Cake | Sweet baked treat | 4.5-5.5 | None |
| 4 | Molasses Cookie | Rich, chewy cookie | 3.0-5.0 | None |

### Category: Fruits (4 types)
Good happiness, minor stamina/agility.

| ID | Name | Description | Happiness | Stats |
|----|------|-------------|-----------|-------|
| 5 | Apple | Crisp and refreshing | 2.0-3.0 | Stamina 0.1-0.2 |
| 6 | Carrot | Crunchy favorite | 2.0-3.0 | Agility 0.05-0.15 |
| 7 | Banana | Energy-rich fruit | 1.5-2.5 | Stamina 0.15-0.25 |
| 8 | Watermelon | Hydrating summer treat | 2.5-3.5 | Stamina 0.05-0.1 |

### Category: Grains (3 types)
Moderate happiness, stamina/durability focus.

| ID | Name | Description | Happiness | Stats |
|----|------|-------------|-----------|-------|
| 9 | Oats | Nutritious staple | 1.0-2.0 | Stamina 0.2-0.35, Durability 0.1-0.2 |
| 10 | Barley | Hearty grain | 1.0-2.0 | Stamina 0.15-0.3, Durability 0.15-0.25 |
| 11 | Bran Mash | Warm, digestible meal | 1.5-2.5 | Stamina 0.1-0.2, Durability 0.2-0.3 |

### Category: Proteins (2 types)
Moderate happiness, durability focus.

| ID | Name | Description | Happiness | Stats |
|----|------|-------------|-----------|-------|
| 12 | Flaxseed | Omega-rich supplement | 1.0-2.0 | Durability 0.3-0.45 |
| 13 | Soybean Meal | Protein-packed feed | 1.0-1.5 | Durability 0.35-0.5 |

### Category: Supplements (2 types)
Moderate happiness, balanced tiny boosts.

| ID | Name | Description | Happiness | Stats |
|----|------|-------------|-----------|-------|
| 14 | Electrolyte Mix | Hydration support | 1.0-2.0 | All stats 0.05-0.1 |
| 15 | Vitamin Pellets | Complete nutrition | 1.0-2.0 | All stats 0.05-0.15 |

### Category: Premium (3 types)
High happiness, varied stat bonuses.

| ID | Name | Description | Happiness | Stats |
|----|------|-------------|-----------|-------|
| 16 | Alfalfa Hay | Premium forage | 2.0-3.0 | Stamina 0.2-0.3, Durability 0.1-0.2 |
| 17 | Performance Blend | Race day nutrition | 2.0-3.5 | Speed 0.1-0.2, Stamina 0.1-0.2 |
| 18 | Champion's Treat | Elite reward | 3.0-4.0 | All stats 0.1-0.15 |

---

## Preference System

### Category Preference Weights

Each feed has a probability of being each preference level, weighted by category:

| Category | Favorite | Liked | Neutral | Disliked | Hated |
|----------|----------|-------|---------|----------|-------|
| **Treats** | 15% | 25% | 50% | 8% | 2% |
| **Fruits** | 10% | 20% | 55% | 12% | 3% |
| **Grains** | 5% | 15% | 65% | 12% | 3% |
| **Proteins** | 5% | 15% | 60% | 15% | 5% |
| **Supplements** | 3% | 10% | 75% | 10% | 2% |
| **Premium** | 12% | 22% | 50% | 12% | 4% |

### Career Phase Modifiers

| Career Phase | Races | Modifier |
|--------------|-------|----------|
| Young | 0-9 | +5% to Favorite/Liked probabilities |
| Prime | 10-29 | No modifier |
| Veteran | 30-49 | No modifier |
| Old | 50+ | +5% to Disliked/Hated probabilities |

### LegType Category Preferences

Each LegType has a preferred feed category with +10% favorite chance:

| LegType | Preferred Category | Rationale |
|---------|-------------------|-----------|
| StartDash | Treats | Explosive horses love quick energy |
| FrontRunner | Grains | Sustained energy for pace-setters |
| StretchRunner | Fruits | Balanced nutrition for mid-race ralliers |
| LastSpurt | Proteins | Power for closers |
| RailRunner | Supplements | Precision nutrition for tactical runners |

### Deterministic Preference Generation

```csharp
public FeedResponse GeneratePreference(Guid horseId, byte feedingId, FeedingCategoryId category,
                                        LegTypeId legType, short raceStarts)
{
    // Deterministic seed ensures same horse + feed = same preference
    var seed = HashCode.Combine(horseId, feedingId);
    var rng = new Random(seed);

    // Get base weights for this category
    var weights = FeedingConfig.CategoryPreferenceWeights[category];

    // Apply career phase modifier
    var careerModifier = GetCareerModifier(raceStarts);
    weights = ApplyCareerModifier(weights, careerModifier);

    // Apply LegType modifier
    if (FeedingConfig.LegTypePreferredCategory[legType] == category)
    {
        weights = ApplyLegTypeBonus(weights);  // +10% to favorite
    }

    // Roll preference
    return RollPreference(rng, weights);
}
```

### Preference Effectiveness Multipliers

| Preference | Multiplier | Description |
|------------|------------|-------------|
| Favorite | 1.50 | +50% to all effects, adds bonus stat if treat |
| Liked | 1.25 | +25% to all effects |
| Neutral | 1.00 | Base effects |
| Disliked | 0.75 | -25% to all effects |
| Hated | 0.50 | -50% to all effects, 20% chance of "upset stomach" |

### Favorite Bonus for Treats

When a horse eats a favorite treat (normally happiness-only), they also receive a small random stat bonus:

```csharp
if (preference == FeedResponse.Favorite && feeding.CategoryId == FeedingCategoryId.Treats)
{
    // Add small bonus stat (0.1-0.2 to random stat)
    var bonusStat = PickRandomStat(rng);
    ApplyBonusStat(horse, bonusStat, 0.1, 0.2);
}
```

### Hated Food Penalty

```csharp
if (preference == FeedResponse.Hated)
{
    // 20% chance of upset stomach
    if (rng.NextDouble() < 0.20)
    {
        // Additional happiness penalty
        happinessGain -= FeedingConfig.UpsetStomachPenalty;  // -2.0
    }
}
```

---

## Service Layer

### IFeedingCalculator

```csharp
public interface IFeedingCalculator
{
    /// <summary>
    /// Generates or retrieves a horse's preference for a specific feed.
    /// Uses deterministic seeding - same horse + feed = same preference.
    /// </summary>
    FeedResponse CalculatePreference(
        Guid horseId,
        byte feedingId,
        FeedingCategoryId category,
        LegTypeId legType,
        short raceStarts);

    /// <summary>
    /// Calculates happiness gain from feeding, applying preference modifier.
    /// </summary>
    double CalculateHappinessGain(
        double baseMin,
        double baseMax,
        FeedResponse preference,
        double currentHappiness);

    /// <summary>
    /// Calculates stat gain from feeding, applying preference modifier.
    /// Respects DominantPotential ceiling.
    /// </summary>
    double CalculateStatGain(
        double baseMin,
        double baseMax,
        double actualStat,
        double dominantPotential,
        FeedResponse preference);

    /// <summary>
    /// Calculates happiness-based effectiveness modifier.
    /// Low happiness reduces feeding effectiveness.
    /// </summary>
    double CalculateHappinessEffectivenessModifier(double happiness);

    /// <summary>
    /// Determines if upset stomach occurs for hated food (20% chance).
    /// </summary>
    bool RollUpsetStomach(Guid horseId, byte feedingId, DateTime feedingDate);
}
```

### IFeedingService (Enhanced)

```csharp
public interface IFeedingService
{
    /// <summary>
    /// Gets a single feeding by ID.
    /// </summary>
    Task<FeedingResult> Get(byte id);

    /// <summary>
    /// Gets all feeding types.
    /// </summary>
    Task<IEnumerable<FeedingsResult>> GetAll();

    /// <summary>
    /// Executes a feeding session for a horse (once between races).
    /// Discovers preference if first time.
    /// </summary>
    Task<FeedingSessionResult> Feed(byte feedingId, Guid horseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets 3 random feeding options for today.
    /// Cached daily. Guarantees favorite if happiness < 50 and one is discovered.
    /// </summary>
    Task<List<FeedingOption>> GetAvailableFeedingOptionsAsync(
        Guid horseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets feeding history for a horse.
    /// </summary>
    Task<List<FeedingSession>> GetFeedingHistoryAsync(
        Guid horseId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all discovered preferences for a horse.
    /// </summary>
    Task<List<HorseFeedingPreference>> GetDiscoveredPreferencesAsync(
        Guid horseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a horse can feed (hasn't fed since last race).
    /// </summary>
    bool CanFeed(Horse horse);
}
```

### FeedingOption (DTO)

```csharp
public record FeedingOption
{
    public byte FeedingId { get; init; }
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public FeedingCategoryId Category { get; init; }

    // Display ranges (for admin UI)
    public double HappinessMin { get; init; }
    public double HappinessMax { get; init; }
    public double StaminaMin { get; init; }
    public double StaminaMax { get; init; }
    public double DurabilityMin { get; init; }
    public double DurabilityMax { get; init; }
    public double SpeedMin { get; init; }
    public double SpeedMax { get; init; }
    public double AgilityMin { get; init; }
    public double AgilityMax { get; init; }

    // Preference info (null if not yet discovered)
    public FeedResponse? DiscoveredPreference { get; init; }
}
```

### FeedingSessionResult (Enhanced)

```csharp
public record FeedingSessionResult
{
    public Guid SessionId { get; init; }
    public string FeedingName { get; init; } = null!;
    public FeedResponse Result { get; init; }

    // Effects applied
    public double HappinessGain { get; init; }
    public double StaminaGain { get; init; }
    public double DurabilityGain { get; init; }
    public double SpeedGain { get; init; }
    public double AgilityGain { get; init; }

    // Current values after feeding
    public double CurrentHappiness { get; init; }

    // Discovery info
    public bool PreferenceDiscovered { get; init; }
    public string? DiscoveryMessage { get; init; }  // "Thunderbolt LOVES apples!"

    // Upset stomach (hated food penalty)
    public bool UpsetStomachOccurred { get; init; }
}
```

---

## Configuration

### FeedingConfig

```csharp
public static class FeedingConfig
{
    // Preference multipliers
    public const double FavoriteMultiplier = 1.50;
    public const double LikedMultiplier = 1.25;
    public const double NeutralMultiplier = 1.00;
    public const double DislikedMultiplier = 0.75;
    public const double HatedMultiplier = 0.50;

    // Negative mechanics
    public const double UpsetStomachChance = 0.20;      // 20% for hated foods
    public const double UpsetStomachPenalty = 2.0;      // Additional happiness loss

    // Happiness effectiveness (similar to training)
    public const double MinHappinessEffectiveness = 0.50;  // At 0 happiness
    public const double MaxHappinessEffectiveness = 1.00;  // At 100 happiness

    // Career phase modifiers
    public const double YoungHorseFavoriteBonus = 0.05;    // +5% favorite/liked chance
    public const double OldHorseDislikeBonus = 0.05;       // +5% disliked/hated chance

    // LegType category bonus
    public const double LegTypeFavoriteBonus = 0.10;       // +10% favorite chance for preferred category

    // Random selection
    public const int FeedingOptionsOffered = 3;
    public const double LowHappinessThreshold = 50.0;      // Guarantee favorite below this

    // Favorite treat bonus stat range
    public const double FavoriteTreatBonusMin = 0.1;
    public const double FavoriteTreatBonusMax = 0.2;

    // Category preference weights
    public static readonly Dictionary<FeedingCategoryId, PreferenceWeights> CategoryPreferenceWeights = new()
    {
        { FeedingCategoryId.Treats, new(0.15, 0.25, 0.50, 0.08, 0.02) },
        { FeedingCategoryId.Fruits, new(0.10, 0.20, 0.55, 0.12, 0.03) },
        { FeedingCategoryId.Grains, new(0.05, 0.15, 0.65, 0.12, 0.03) },
        { FeedingCategoryId.Proteins, new(0.05, 0.15, 0.60, 0.15, 0.05) },
        { FeedingCategoryId.Supplements, new(0.03, 0.10, 0.75, 0.10, 0.02) },
        { FeedingCategoryId.Premium, new(0.12, 0.22, 0.50, 0.12, 0.04) }
    };

    // LegType preferred categories
    public static readonly Dictionary<LegTypeId, FeedingCategoryId> LegTypePreferredCategory = new()
    {
        { LegTypeId.StartDash, FeedingCategoryId.Treats },
        { LegTypeId.FrontRunner, FeedingCategoryId.Grains },
        { LegTypeId.StretchRunner, FeedingCategoryId.Fruits },
        { LegTypeId.LastSpurt, FeedingCategoryId.Proteins },
        { LegTypeId.RailRunner, FeedingCategoryId.Supplements }
    };
}

public record PreferenceWeights(
    double Favorite,
    double Liked,
    double Neutral,
    double Disliked,
    double Hated);
```

---

## User Flow

### Feeding Flow (Happy Path - New Feed)

1. **Player navigates to Feeding Menu** (between races)
2. **System generates 3 random feeding options** (cached daily)
3. **System displays**:
   - Current horse stats and happiness
   - 3 available feeds with effect ranges
   - Discovered preferences marked (if any)
   - Undiscovered feeds show "???" for preference
   - Whether feeding is available (once between races)
4. **Player selects feeding** (e.g., "Apple" - not yet tried)
5. **System validates**:
   - Horse has not fed since last race
   - Selected feeding is one of the 3 offered options
6. **System executes feeding**:
   - Generate preference (deterministic from horse+feed IDs)
   - Roll random values within feed's ranges
   - Apply preference multiplier
   - Apply happiness effectiveness modifier
   - Check for upset stomach if hated
   - Apply effects (respecting stat ceilings)
   - Save FeedingSession and HorseFeedingPreference
   - Mark `HasFedSinceLastRace = true`
7. **System displays results**:
   - "Thunderbolt LOVES apples!" (preference discovery)
   - Happiness: 72 -> 76 (+4)
   - Stamina: 45.2 -> 45.4 (+0.2)

### Feeding Flow (Known Favorite)

1. Player selects "Apple" (previously discovered as Favorite)
2. System shows: "Apple (Favorite +50%)"
3. Results show boosted effects from favorite multiplier

### Error Cases

- **Already Fed**: "Your horse has already eaten since the last race. Race again to unlock feeding."
- **Hated + Upset Stomach**: "Thunderbolt reluctantly ate the peppermint... and got an upset stomach! Happiness: 60 -> 57 (-3)"
- **Stat at Ceiling**: "Stamina is at genetic maximum. No Stamina gains from this feeding."

---

## Integration Points

### With Training System (Feature 020)

- **Independent limits**: Can train once AND feed once between races
- **Complementary purposes**: Training costs happiness, feeding restores it
- **Shared architecture**: Similar service patterns, calculators, session tracking
- **Both reset after race**: `HasTrainedSinceLastRace` and `HasFedSinceLastRace`

### With Race System

- **Reset flags**: After race completion, reset both `HasFedSinceLastRace` and `HasTrainedSinceLastRace`
- **Happiness impact**: Feeding affects happiness which affects race performance

### With Breeding System

- **Preference inheritance** (future): Could inherit parent preferences
- **Horse personality**: Preferences add character to horses

---

## Success Criteria

### Must Have (MVP)

- [ ] 18 feed types across 6 categories with effect ranges
- [ ] Feeding execution service (calculate effects, apply, save session)
- [ ] Preference discovery system (deterministic generation on first feeding)
- [ ] Preference effectiveness multipliers (Favorite/Liked/Neutral/Disliked/Hated)
- [ ] "Once between races" limit enforcement (`HasFedSinceLastRace`)
- [ ] Random 3-option daily selection (cached)
- [ ] Stat ceiling enforcement
- [ ] Feeding history (view past sessions)
- [ ] Discovered preferences storage and display

### Should Have

- [ ] Category-weighted preference probabilities
- [ ] Career phase modifiers (young = more favorites)
- [ ] LegType preferred category bonus
- [ ] Upset stomach mechanic for hated foods
- [ ] Low happiness effectiveness reduction
- [ ] Favorite treat bonus stat
- [ ] Guarantee favorite in options when happiness < 50

### Could Have (Future Enhancements)

- [ ] Preference shifting over time (neutral -> liked with repeated feeding)
- [ ] Feeding schedules (plan multiple feedings)
- [ ] Special diets (combinations of feeds)
- [ ] Seasonal feeds (limited availability)
- [ ] Feed quality tiers (basic/premium versions)
- [ ] Breeding preference inheritance

---

## Implementation Phases

### Phase 1: Data Model & Configuration
1. Create `FeedingCategoryId` enum
2. Expand `Feeding` entity with category, effect ranges
3. Create `HorseFeedingPreference` entity
4. Enhance `FeedingSession` with detailed fields
5. Add `Horse.HasFedSinceLastRace` boolean flag
6. Create `FeedingConfig` class
7. Update ModelBuilderExtensions with 18 feed types
8. Create database migration

### Phase 2: Calculator & Preference System
1. Implement `FeedingCalculator` (preference generation, effect calculation)
2. Implement deterministic seeding for preferences
3. Implement preference weight system (category, career, LegType)
4. Unit tests for calculator (preference generation, effect calculation)

### Phase 3: Service Layer
1. Enhance `FeedingService` with new methods
2. Implement daily 3-option selection with caching
3. Implement preference discovery flow
4. Implement feeding execution with all modifiers
5. Unit tests for service (happy path, errors, discovery)

### Phase 4: Race Integration
1. Update race completion to reset `HasFedSinceLastRace`
2. Integration tests for feeding -> race -> feeding flow

### Phase 5: API & Admin UI
1. Update `FeedingsController` with new endpoints
2. Add feeding options display (with effect ranges)
3. Add preference indicators (discovered vs unknown)
4. Add feeding results display
5. Add feeding history view

### Phase 6: Polish
1. Tune preference probabilities
2. Tune effect ranges for balance
3. Add upset stomach mechanic
4. Add favorite guarantee when happiness < 50
5. Performance optimization (if needed)

---

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Feeding makes training less valuable | Medium | Low | Feeding is primarily happiness; training is stats |
| Preference system feels unfair | Medium | Medium | Weighted probabilities ensure most feeds are neutral/liked |
| Daily cache feels restrictive | Low | Medium | Clear UI messaging, options refresh next day |
| Too many hated foods frustrating | Medium | Low | Hated is rare (2-5%), most feeds are neutral or better |
| Deterministic seeding discovered | Low | Low | Seed combines horse ID + feed ID, not guessable |

---

## Comparison: Training vs Feeding

| Aspect | Training | Feeding |
|--------|----------|---------|
| **Primary Effect** | Stats | Happiness |
| **Secondary Effect** | Happiness cost | Small stat gains |
| **Frequency** | Once between races | Once between races |
| **Selection** | 3 random options | 3 random daily options |
| **Randomness** | Career/LegType bonuses | Preference discovery |
| **Negative Mechanic** | Overwork risk | Hated food upset stomach |
| **Career Phase** | Affects stat gains | Affects preference probabilities |
| **LegType** | +20% for matching training | +10% favorite chance for category |
| **Ceiling** | DominantPotential | DominantPotential |

---

**Feature 022 Status**: Discovery complete, ready for implementation planning.
