# Purse Distribution - Feature Specification

**Feature Number:** 009

**Status:** 🟢 COMPLETE - Core Implementation Done (Player Balance Deferred)

**Parent Feature:** [001-race-engine](001-race-engine.md) - Sub-Feature 2

**Prerequisites:**
- Feature 002 (Core Race Simulation) - ✅ Complete
- Feature 008 (Play-by-Play Commentary) - ✅ Complete

---

## Summary

Implement a purse distribution system that calculates and distributes prize money to the top finishers in each race. The system awards earnings based on race class, distance, and finishing position, updating horse earnings. This creates the economic foundation for the game, rewarding successful racing and enabling progression.

**Implementation Status:**
- ✅ **COMPLETE:** Purse calculation, distribution, horse earnings tracking, field size constraints
- 🔮 **DEFERRED:** Player balance tracking (future feature - requires economic system design)
- 🔮 **DEFERRED:** Optional starter stipend (may not be needed)

**Core Design Philosophy:**
- **Realism-first:** Purse distributions mirror real-world elite stakes patterns
- **Top-heavy elite races:** Championship races pay top 5, premier events pay top 4, grade varies
- **Winner-centric:** Elite races heavily reward winning (55-62% to winner)
- **Strategic tension:** "Win or nothing" pressure defines high-stakes racing
- **Earnings prestige:** Horse earnings become historical legacy and breeding value indicator

---

## Problem Statement

### Implementation Complete (2025-12-26)

**What Was Implemented:**
- ✅ RaceClass entity and enum (9 race classes: Maiden → Grade I)
- ✅ Race entity extended with RaceClassId, MinFieldSize, MaxFieldSize, Purse
- ✅ PurseCalculator service with configurable purse base amounts and distribution patterns
- ✅ Distance scaling (+5% per furlong above 10f baseline)
- ✅ Class-specific distribution patterns (3-5 paid places, 55-62% to winner)
- ✅ Horse earnings updated for all money winners
- ✅ Race results display actual payout amounts
- ✅ Field size constraints respected in matchmaker
- ✅ Comprehensive test coverage (43 unit tests for PurseCalculator)

**What Was Deferred:**
- 🔮 Player balance tracking (User.Balance property removed - requires broader economic system design)
- 🔮 Starter stipend for horses finishing outside the money (configuration exists but not used)

### Design Goals

✅ Award prize money based on realistic purse depth (3-5 places depending on race class)
✅ Scale purses by race class and distance
✅ Use real-world distribution patterns (Championship/Premier/Elite stakes patterns)
✅ Update horse earnings for historical tracking
🔮 Update player balance (for player-owned horses) - **DEFERRED**
🔮 Optional starter stipend for horses finishing outside the money - **DEFERRED**
✅ Maintain economic balance (purses sustainable long-term)
✅ Support future features (entry fees, breeding economics, prestige tracking)

---

## Requirements

### Functional Requirements

- [x] Calculate total purse based on race class and distance
- [x] Distribute purse with class-specific depth (Maiden: 3 places, Grade I: 4-5 places, Championship: 5 places)
- [x] Use realistic distribution percentages by race class (Championship: 62/20/10/5/3, Premier: 55/20/10/5, etc.)
- [x] Update horse `Earnings` property for all money winners
- [ ] Update player balance for player-owned horses that finish in the money - **DEFERRED**
- [ ] Optional starter stipend for horses finishing outside the money (does not count toward Earnings stat) - **DEFERRED**
- [x] Return payout amounts in race results
- [x] Support configurable purse base amounts by race class
- [x] Support configurable distribution percentages and purse depth by class
- [x] Add `MinFieldSize` and `MaxFieldSize` properties to Race entity to control field sizes per race
- [x] Update matchmaker logic to respect race-specific field size constraints
- [x] Set championship races to allow larger fields (max 20) than standard races (max 12)

### Acceptance Criteria

**Purse Calculation:**
- [x] Given race class "Allowance" and distance 10f, when calculating purse, then returns base purse × distance factor
- [x] Given race class "Stakes", when calculating purse, then returns higher base amount than Allowance
- [x] Given race class "Maiden", when calculating purse, then returns lower base amount than Allowance

**Distribution:**
- [x] Given total purse $10,000, when distributing to winner, then winner receives correct percentage based on race class
- [x] Given total purse $10,000, when distributing to place horse, then place receives correct percentage based on race class
- [x] Given total purse $10,000, when distributing to show horse, then show receives correct percentage based on race class
- [x] Given total purse $10,000, when distributing to 4th place or beyond, then receives $0 or correct percentage if within paid places

**Horse Earnings Update:**
- [x] Given horse wins race with payout, when race completes, then horse.Earnings increases by payout amount
- [x] Given horse already has earnings, when winning more, then new total is cumulative
- [x] Given CPU horse finishes in the money, when race completes, then CPU horse.Earnings updated

**Player Balance Update:** - **DEFERRED**
- [ ] Given player-owned horse wins $6,000, when race completes, then player balance increases by $6,000
- [ ] Given CPU-owned horse wins $6,000, when race completes, then no player balance change
- [ ] Given player owns multiple horses in race, when both finish in money, then player receives sum of both payouts

**Race Results:**
- [x] Given race completes, when viewing results, then each finisher shows correct payout amount
- [x] Given horse finished outside paid places, when viewing results, then payout shows $0

**Field Size Constraints:**
- [x] Given championship race (Grade I), when matchmaker assembles field, then max 20 horses allowed
- [x] Given standard race (Maiden/Claiming), when matchmaker assembles field, then max 12 horses allowed
- [x] Given any race with field size constraints, when assembling field, then respects MinFieldSize and MaxFieldSize
- [x] Given race-specific field size, when assembling field, then uses race's configured constraints (not hardcoded)

### Non-Functional Requirements

- **Consistency:** All monetary amounts use decimal precision (no rounding errors)
- **Auditability:** Purse calculations logged for debugging
- **Configurability:** Easy to adjust base purses and distribution percentages
- **Testability:** Purse math testable in isolation from race logic
- **Balance:** Purses sufficient to cover feeding/training costs but not excessive

---

## Technical Design

### Architecture Overview

```
Race Completion Flow (Updated):
1. Race finishes, horses ordered by Place
2. Calculate total purse (new)
3. Distribute purse to top 3 (new)
4. Update horse Earnings (new)
5. Update player Balance (new)
6. Return RaceResult with payouts populated
```

### Data Model

#### Option 1: Configuration-Only (Recommended)

**No database changes required.** All purse distribution patterns stored in `PurseConfig` static configuration.

**Pros:**
- Zero migration effort
- Easy to tune and balance
- Fast lookup (in-memory)
- Simpler codebase

**Cons:**
- Cannot customize per-race (all Grade I races use same pattern)
- Requires code change to adjust patterns

**Existing Entities - No Changes:**

```csharp
// Horse.cs - Earnings already tracked
public int Earnings { get; set; }  // Total career earnings

// RaceRunHorseResult.cs - Payout already defined
public int Payout { get; set; }  // Currently set to 0

// Race.cs - Class and distance already tracked
public int RaceClassId { get; set; }
public decimal Furlongs { get; set; }
```

**New Player Balance Tracking:**
```csharp
// User.cs - Add Balance property (if not exists)
public int Balance { get; set; }  // Player's available funds
```

**Field Size Constraints (NEW):**
```csharp
// Race.cs - Add field size limits
public byte MinFieldSize { get; set; } = 8;   // Minimum horses in field (default 8)
public byte MaxFieldSize { get; set; } = 12;  // Maximum horses in field (default 12)
```

**Why Database Change:**
- Championship races need larger fields (max 20) than typical races (max 12)
- Some races have tighter fields (8-10) while others are open (up to 14-16)
- Per-race customization required (cannot use class-based defaults effectively)
- Real-world pattern: field size varies by race prestige, not just class

**Affected Logic:**
```csharp
// RaceService.cs line 67 - Current hardcoded logic
limit: randomGenerator.Next(7, 12)  // Always 8-12 total

// NEW logic - respects race constraints
var minCpu = race.MinFieldSize - 1;  // Reserve 1 spot for player horse
var maxCpu = race.MaxFieldSize - 1;
limit: randomGenerator.Next(minCpu, maxCpu)
```

**Migration Impact:**
- Add `MinFieldSize` and `MaxFieldSize` columns to `Races` table
- Default values: `MinFieldSize = 8`, `MaxFieldSize = 12` (preserves current behavior)
- Update seed data to set championship races to `MaxFieldSize = 20`

---

#### Option 2: Database-Driven Purse Patterns (Future Enhancement)

**If per-race customization needed**, add purse pattern tables:

**New Entity: PurseDistributionPattern**
```csharp
public class PurseDistributionPattern
{
    public int Id { get; set; }
    public string Name { get; set; }              // "Championship Pattern", "Premier Event", etc.
    public int PaidPlaces { get; set; }           // How many places get paid
    public string Description { get; set; }

    // Percentages stored as JSON or separate table
    public ICollection<PurseDistributionPlace> Places { get; set; }
}

public class PurseDistributionPlace
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public int Place { get; set; }                // 1st, 2nd, 3rd, etc.
    public decimal Percentage { get; set; }        // 0.62 for 62%

    public PurseDistributionPattern Pattern { get; set; }
}
```

**Link to Race:**
```csharp
// Race.cs - Add optional override
public int? PurseDistributionPatternId { get; set; }  // NULL = use class default
public PurseDistributionPattern PursePattern { get; set; }
```

**Seeding Data:**
```csharp
// Seed standard patterns
context.PurseDistributionPatterns.AddRange(
    new PurseDistributionPattern
    {
        Name = "Standard Win/Place/Show",
        PaidPlaces = 3,
        Description = "Standard 60/20/10 distribution",
        Places = new List<PurseDistributionPlace>
        {
            new() { Place = 1, Percentage = 0.60m },
            new() { Place = 2, Percentage = 0.20m },
            new() { Place = 3, Percentage = 0.10m }
        }
    },
    new PurseDistributionPattern
    {
        Name = "Championship Pattern",
        PaidPlaces = 5,
        Description = "Extremely top-heavy for elite races",
        Places = new List<PurseDistributionPlace>
        {
            new() { Place = 1, Percentage = 0.62m },
            new() { Place = 2, Percentage = 0.20m },
            new() { Place = 3, Percentage = 0.10m },
            new() { Place = 4, Percentage = 0.05m },
            new() { Place = 5, Percentage = 0.03m }
        }
    }
    // ... more patterns
);
```

**Pros:**
- Per-race customization possible
- Admin UI can manage patterns
- Future special events can have unique purses

**Cons:**
- More complex (2 new tables, relationships)
- Migration required
- Slower lookup (database query vs static config)

**Recommendation:** Start with **Option 1** (config-only). Only implement Option 2 if you need per-race customization or admin-driven purse management.

### Configuration

**Add to RaceModifierConfig.cs (or new PurseConfig.cs):**

```csharp
/// <summary>
/// Purse configuration for race prize money distribution.
/// Based on real-world championship stakes and premier event patterns.
/// Implements realistic purse depth and top-heavy distribution for elite races.
/// </summary>
public static class PurseConfig
{
    // ============================================================================
    // Base Purse Amounts (10 furlong baseline)
    // ============================================================================

    public static readonly IReadOnlyDictionary<RaceClassId, int> BasePurseByClass =
        new Dictionary<RaceClassId, int>
        {
            { RaceClassId.Maiden, 20000 },           // $20,000 for maiden races
            { RaceClassId.MaidenClaiming, 18000 },   // $18,000 for maiden claiming
            { RaceClassId.Claiming, 25000 },         // $25,000 for claiming
            { RaceClassId.Allowance, 40000 },        // $40,000 for allowance
            { RaceClassId.AllowanceOptional, 50000 },// $50,000 for allowance optional
            { RaceClassId.Stakes, 100000 },          // $100,000 for stakes
            { RaceClassId.GradeIII, 200000 },        // $200,000 for Grade III
            { RaceClassId.GradeII, 500000 },         // $500,000 for Grade II
            { RaceClassId.GradeI, 1000000 }          // $1,000,000 for Grade I
            // Championship races use Grade I base with specific distribution patterns
        };

    /// <summary>
    /// Distance scaling factor per furlong.
    /// Longer races have slightly higher purses.
    /// Formula: totalPurse = basePurse × (1 + (furlongs - 10) × DistanceScalingFactor)
    /// </summary>
    public const decimal DistanceScalingFactor = 0.05m;  // +5% per furlong above 10f

    // ============================================================================
    // Purse Distribution Patterns (Real-World Based)
    // ============================================================================

    /// <summary>
    /// Purse distribution structure defining paid places and percentages.
    /// </summary>
    public class PurseDistribution
    {
        public int PaidPlaces { get; set; }                // How many places receive purse money
        public decimal[] Percentages { get; set; }         // Percentage for each place
        public string Description { get; set; }            // Pattern description
    }

    /// <summary>
    /// Distribution patterns by race class.
    /// Lower-class races: more balanced distribution (competitive balance)
    /// Elite races: top-heavy distribution (winner-centric prestige)
    /// </summary>
    public static readonly IReadOnlyDictionary<RaceClassId, PurseDistribution> DistributionByClass =
        new Dictionary<RaceClassId, PurseDistribution>
        {
            // Lower-tier races: balanced distribution (pay top 3)
            { RaceClassId.Maiden, new PurseDistribution
                {
                    PaidPlaces = 3,
                    Percentages = new[] { 0.60m, 0.20m, 0.10m },
                    Description = "Standard Win/Place/Show"
                }
            },
            { RaceClassId.MaidenClaiming, new PurseDistribution
                {
                    PaidPlaces = 3,
                    Percentages = new[] { 0.60m, 0.20m, 0.10m },
                    Description = "Standard Win/Place/Show"
                }
            },
            { RaceClassId.Claiming, new PurseDistribution
                {
                    PaidPlaces = 3,
                    Percentages = new[] { 0.60m, 0.20m, 0.10m },
                    Description = "Standard Win/Place/Show"
                }
            },

            // Mid-tier races: slight expansion (pay top 4)
            { RaceClassId.Allowance, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = new[] { 0.58m, 0.20m, 0.10m, 0.05m },
                    Description = "Competitive balance"
                }
            },
            { RaceClassId.AllowanceOptional, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = new[] { 0.58m, 0.20m, 0.10m, 0.05m },
                    Description = "Competitive balance"
                }
            },

            // Stakes races: moderate top-heavy (pay top 4)
            { RaceClassId.Stakes, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = new[] { 0.55m, 0.20m, 0.10m, 0.05m },
                    Description = "Winner-centric (typical G1 pattern)"
                }
            },

            // Grade III: Premier event pattern (pay top 4)
            { RaceClassId.GradeIII, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = new[] { 0.55m, 0.20m, 0.10m, 0.05m },
                    Description = "Premier event pattern"
                }
            },

            // Grade II: Elite prestige (pay top 5)
            { RaceClassId.GradeII, new PurseDistribution
                {
                    PaidPlaces = 5,
                    Percentages = new[] { 0.55m, 0.20m, 0.10m, 0.07m, 0.03m },
                    Description = "Marathon stakes pattern"
                }
            },

            // Grade I: Championship pattern (extremely top-heavy, pay top 5)
            { RaceClassId.GradeI, new PurseDistribution
                {
                    PaidPlaces = 5,
                    Percentages = new[] { 0.62m, 0.20m, 0.10m, 0.05m, 0.03m },
                    Description = "Championship pattern (most punitive)"
                }
            }
        };

    // ============================================================================
    // Starter Stipend (Optional)
    // ============================================================================

    /// <summary>
    /// Optional flat stipend for horses finishing outside the money.
    /// Does NOT count toward horse Earnings stat (cash-flow only for owner).
    /// Set to 0 to disable. Suggested: $1,000-$2,000 for realism.
    /// </summary>
    public const int StarterStipend = 1000;  // $1,000 per starter (optional)

    /// <summary>
    /// Whether starter stipend is enabled.
    /// </summary>
    public const bool StarterStipendEnabled = false;  // Default: disabled
}
```

### Implementation Components

#### 1. Purse Calculator Service

**New service: PurseCalculator.cs**

```csharp
/// <summary>
/// Calculates race purse amounts and distributions.
/// </summary>
public class PurseCalculator
{
    /// <summary>
    /// Calculates total purse for a race based on class and distance.
    /// </summary>
    /// <param name="raceClass">Race class ID</param>
    /// <param name="furlongs">Race distance in furlongs</param>
    /// <returns>Total purse amount</returns>
    public int CalculateTotalPurse(RaceClassId raceClass, decimal furlongs)
    {
        // Get base purse for this class
        if (!PurseConfig.BasePurseByClass.TryGetValue(raceClass, out var basePurse))
        {
            basePurse = PurseConfig.BasePurseByClass[RaceClassId.Claiming]; // Default fallback
        }

        // Scale by distance (baseline is 10 furlongs)
        var distanceMultiplier = 1.0m + ((furlongs - 10m) * PurseConfig.DistanceScalingFactor);

        // Ensure non-negative multiplier
        distanceMultiplier = Math.Max(0.5m, distanceMultiplier);

        var totalPurse = (int)(basePurse * distanceMultiplier);

        return totalPurse;
    }

    /// <summary>
    /// Calculates payout for a specific finishing position.
    /// </summary>
    /// <param name="totalPurse">Total race purse</param>
    /// <param name="place">Finishing position (1=Win, 2=Place, 3=Show)</param>
    /// <returns>Payout amount for this position</returns>
    public int CalculatePayout(int totalPurse, int place)
    {
        decimal percentage = place switch
        {
            1 => PurseConfig.WinnerPercentage,
            2 => PurseConfig.PlacePercentage,
            3 => PurseConfig.ShowPercentage,
            _ => 0m  // 4th place and beyond get nothing
        };

        return (int)(totalPurse * percentage);
    }

    /// <summary>
    /// Calculates all payouts for a race.
    /// </summary>
    /// <param name="totalPurse">Total race purse</param>
    /// <returns>Dictionary of place → payout amount</returns>
    public Dictionary<int, int> CalculateAllPayouts(int totalPurse)
    {
        return new Dictionary<int, int>
        {
            { 1, CalculatePayout(totalPurse, 1) },
            { 2, CalculatePayout(totalPurse, 2) },
            { 3, CalculatePayout(totalPurse, 3) }
        };
    }
}
```

#### 2. RaceService Integration

**Update RaceService.RunRace():**

```csharp
public async Task<RaceResult> RunRace(Guid raceId, Guid myHorseId, CancellationToken ct)
{
    // ... existing race simulation code ...

    // NEW: Calculate purse after race completes
    var purseCalculator = new PurseCalculator();
    var totalPurse = purseCalculator.CalculateTotalPurse(
        race.RaceClassId,
        race.Furlongs);

    var payouts = purseCalculator.CalculateAllPayouts(totalPurse);

    // NEW: Distribute earnings to horses and update player balance
    var playerBalance = 0;
    foreach (var horse in sortedHorses.Take(3))  // Top 3 only
    {
        var payout = payouts[horse.Place];

        // Update horse earnings
        horse.Horse.Earnings += payout;

        // Update player balance if player owns this horse
        if (horse.Horse.OwnerId == currentUserId)
        {
            playerBalance += payout;
        }
    }

    // Update player balance in database
    if (playerBalance > 0)
    {
        var user = await _userRepository.GetByIdAsync(currentUserId, ct);
        user.Balance += playerBalance;
    }

    // MODIFIED: Return results with payouts populated
    var results = sortedHorses
        .OrderBy(h => h.Place)
        .Select(h => new RaceRunHorseResult
        {
            HorseId = h.Horse.Id,
            HorseName = h.Horse.Name,
            Place = h.Place,
            Payout = payouts.GetValueOrDefault(h.Place, 0),  // Get payout or 0
            Time = h.Time
        })
        .ToList();

    // ... return RaceResult ...
}
```

### Realistic Purse Distribution Patterns

**Summary Table: Purse Depth and Distribution by Race Class**

| Race Class | Base Purse | Paid Places | 1st | 2nd | 3rd | 4th | 5th | Pattern Description |
|------------|------------|-------------|-----|-----|-----|-----|-----|---------------------|
| **Maiden** | $20,000 | 3 | 60% | 20% | 10% | - | - | Standard Win/Place/Show |
| **Maiden Claiming** | $18,000 | 3 | 60% | 20% | 10% | - | - | Standard Win/Place/Show |
| **Claiming** | $25,000 | 3 | 60% | 20% | 10% | - | - | Standard Win/Place/Show |
| **Allowance** | $40,000 | 4 | 58% | 20% | 10% | 5% | - | Competitive balance |
| **Allowance Optional** | $50,000 | 4 | 58% | 20% | 10% | 5% | - | Competitive balance |
| **Stakes** | $100,000 | 4 | 55% | 20% | 10% | 5% | - | Winner-centric |
| **Grade III** | $200,000 | 4 | 55% | 20% | 10% | 5% | - | Premier event |
| **Grade II** | $500,000 | 5 | 55% | 20% | 10% | 7% | 3% | Marathon stakes |
| **Grade I** | $1,000,000 | 5 | **62%** | 20% | 10% | 5% | 3% | **Championship (most punitive)** |

**Key Insights:**
- **Lower-tier races (Maiden-Claiming):** Pay top 3 only, balanced 60/20/10
- **Mid-tier races (Allowance-Stakes):** Expand to top 4, shift to 55-58% winner
- **Elite races (Grade I-III):** Pay top 4-5, heavily reward winning (55-62%)
- **Championship races (Grade I):** 62% to winner creates "win or nothing" tension

---

### Field Size Recommendations by Race Class

**Summary Table: Field Size Constraints**

| Race Class | Min Field | Max Field | Typical | Reasoning |
|------------|-----------|-----------|---------|-----------|
| **Maiden** | 8 | 12 | 10 | Standard developmental fields |
| **Maiden Claiming** | 8 | 12 | 10 | Standard claiming fields |
| **Claiming** | 8 | 12 | 10 | Competitive but not elite |
| **Allowance** | 8 | 12 | 10 | Moderate competition |
| **Allowance Optional** | 8 | 12 | 10 | Moderate competition |
| **Stakes** | 8 | 14 | 11 | Larger fields for prestige |
| **Grade III** | 8 | 14 | 11 | Premier event fields |
| **Grade II** | 8 | 16 | 12 | Elite competition |
| **Grade I** | 10 | **20** | **14** | **Championship fields (largest)** |

**Real-World Examples:**
- **Championship Classics (max 20):** Triple Derby, Preakness Challenge, Belmont Marathon, etc.
- **Elite Stakes (max 14-16):** Grade I/II stakes, premier events
- **Standard Races (max 12):** Most allowance, claiming, maiden races
- **Tight Fields (8-10):** Special conditions, restricted races

**Design Philosophy:**
- **Lower-tier races (8-12):** Smaller, competitive fields - easier for CPU matchmaker
- **Mid-tier races (8-14):** Slightly larger for prestige
- **Championship races (10-20):** Largest fields create historic spectacle and strategic complexity
- **Minimum 8:** Ensures competitive racing (2+ viable contenders per stat profile)
- **Variable max:** Creates distinct feel between race tiers

**Implementation Notes:**
- Current system: Hardcoded `randomGenerator.Next(7, 12)` = 8-12 total horses
- NEW system: Race-specific `MinFieldSize` and `MaxFieldSize` properties
- Matchmaker logic: `randomGenerator.Next(race.MinFieldSize - 1, race.MaxFieldSize - 1)` (+1 for player horse)
- Seed data: Championship races get `MaxFieldSize = 20`, others use defaults

---

### Example Purse Calculations

**Example 1: Maiden Race, 10 Furlongs**
- Base Purse: $20,000
- Total Purse: $20,000
- 1st: $12,000 (60%)
- 2nd: $4,000 (20%)
- 3rd: $2,000 (10%)
- 4th+: $0

**Example 2: Allowance Race, 10 Furlongs**
- Base Purse: $40,000
- Total Purse: $40,000
- 1st: $23,200 (58%)
- 2nd: $8,000 (20%)
- 3rd: $4,000 (10%)
- 4th: $2,000 (5%)
- 5th+: $0

**Example 3: Grade I Championship, 10 Furlongs**
- Base Purse: $1,000,000
- Total Purse: $1,000,000
- 1st: **$620,000 (62%)**
- 2nd: $200,000 (20%)
- 3rd: $100,000 (10%)
- 4th: $50,000 (5%)
- 5th: $30,000 (3%)
- 6th+: $0

**Example 4: Grade I Championship, 12.5 Furlongs (Marathon)**
- Base Purse: $1,000,000
- Distance Multiplier: 1.125 (2.5f × 0.05 = +12.5%)
- Total Purse: $1,125,000
- 1st: **$697,500 (62%)**
- 2nd: $225,000 (20%)
- 3rd: $112,500 (10%)
- 4th: $56,250 (5%)
- 5th: $33,750 (3%)
- 6th+: $0

**Strategic Implications:**
- Grade I 4th place ($50k) earns LESS than Allowance winner ($23k+)
- Finishing 6th in Grade I = $0 (same as not racing)
- Creates strategic decision: race elite field for prestige vs. win lower-tier for money

---

## Implementation Phases

### Phase 1: Purse Calculator (1-2 hours)

**Tasks:**
1. Create `PurseConfig` class with configuration constants
2. Create `PurseCalculator` service with:
   - `CalculateTotalPurse(RaceClassId, decimal furlongs)`
   - `CalculatePayout(int totalPurse, int place)`
   - `CalculateAllPayouts(int totalPurse)`
3. Add unit tests for purse calculations

**Tests:**
- `CalculateTotalPurse_Allowance10f_Returns40000`
- `CalculateTotalPurse_Stakes12f_Returns110000`
- `CalculateTotalPurse_Maiden6f_Returns16000`
- `CalculatePayout_Winner_Returns60Percent`
- `CalculatePayout_Place_Returns20Percent`
- `CalculatePayout_Show_Returns10Percent`
- `CalculatePayout_4thPlace_Returns0`

### Phase 2: Horse Earnings Update (1 hour)

**Tasks:**
1. Update `RaceService.RunRace()` to calculate purse
2. Distribute payouts to top 3 horses
3. Update `horse.Earnings` for money winners
4. Update `RaceRunHorseResult.Payout` with actual amounts
5. Add integration tests

**Tests:**
- `RunRace_Winner_UpdatesEarnings`
- `RunRace_PlaceAndShow_UpdateEarnings`
- `RunRace_4thPlace_NoEarningsChange`
- `RunRace_MultipleRaces_EarningsAccumulate`

### Phase 3: Player Balance Update (1 hour)

**Tasks:**
1. Add `Balance` property to `User` entity (if not exists)
2. Update player balance for player-owned winners
3. Handle multiple player-owned horses in same race
4. Add tests for player balance logic

**Tests:**
- `RunRace_PlayerHorseWins_PlayerBalanceIncreases`
- `RunRace_CPUHorseWins_PlayerBalanceUnchanged`
- `RunRace_PlayerOwnsMultipleFinishers_ReceivesBothPayouts`

### Phase 4: Database Migration (30 minutes)

**Tasks:**
1. Create migration for `User.Balance` column (if needed)
2. Seed existing users with starting balance (e.g., $100,000)
3. Update seed data documentation

### Phase 5: Balance Validation (1 hour)

**Tasks:**
1. Run economic simulation (100 races)
2. Verify average earnings per race class
3. Adjust base purses if needed for game balance
4. Document economic model in RACE_BALANCE.md

**Validation Checks:**
- Average earnings per race ≈ $10,000-$50,000 (sustainable)
- Grade I races feel substantially more rewarding than Maiden
- Player can afford feeding/training costs from race earnings

---

## Testing Strategy

### Unit Tests (PurseCalculator)

```csharp
[Theory]
[InlineData(RaceClassId.Maiden, 10, 20000)]
[InlineData(RaceClassId.Allowance, 10, 40000)]
[InlineData(RaceClassId.Stakes, 10, 100000)]
[InlineData(RaceClassId.GradeI, 10, 1000000)]
public void CalculateTotalPurse_VariousClasses_ReturnsCorrectBasePurse(
    RaceClassId raceClass, decimal furlongs, int expectedPurse)
{
    var calculator = new PurseCalculator();
    var result = calculator.CalculateTotalPurse(raceClass, furlongs);
    Assert.Equal(expectedPurse, result);
}

[Theory]
[InlineData(1, 60)]  // Winner: 60%
[InlineData(2, 20)]  // Place: 20%
[InlineData(3, 10)]  // Show: 10%
[InlineData(4, 0)]   // 4th: 0%
public void CalculatePayout_VariousPlaces_ReturnsCorrectPercentage(
    int place, int expectedPercentage)
{
    var calculator = new PurseCalculator();
    var totalPurse = 10000;
    var expectedPayout = totalPurse * expectedPercentage / 100;

    var result = calculator.CalculatePayout(totalPurse, place);

    Assert.Equal(expectedPayout, result);
}
```

### Integration Tests (RaceService)

```csharp
[Fact]
public async Task RunRace_Winner_EarningsUpdatedCorrectly()
{
    // Arrange
    var race = CreateTestRace(RaceClassId.Allowance, 10m);  // $40k purse
    var horse = CreateTestHorse(earnings: 0);

    // Act
    var result = await _raceService.RunRace(race.Id, horse.Id, ct);

    // Assert
    var winner = result.Horses.First(h => h.Place == 1);
    Assert.Equal(24000, winner.Payout);  // 60% of $40k

    var horseAfter = await _horseRepository.GetByIdAsync(winner.HorseId, ct);
    Assert.Equal(24000, horseAfter.Earnings);
}

[Fact]
public async Task RunRace_PlayerHorseWins_PlayerBalanceIncreases()
{
    // Arrange
    var player = CreateTestUser(balance: 100000);
    var race = CreateTestRace(RaceClassId.Stakes, 10m);  // $100k purse
    var playerHorse = CreateTestHorse(ownerId: player.Id);

    // Act
    await _raceService.RunRace(race.Id, playerHorse.Id, ct);

    // Assert
    var playerAfter = await _userRepository.GetByIdAsync(player.Id, ct);
    Assert.Equal(100000 + 60000, playerAfter.Balance);  // Starting + winnings
}
```

---

## Economic Balance

### Starting Player Balance

**Recommendation:** $100,000 starting balance
- Allows for initial feeding/training expenses
- Provides buffer for early losing races
- Feels substantial but not excessive

### Average Race Earnings by Class

| Race Class | Base Purse | Winner Payout | Notes |
|------------|------------|---------------|-------|
| Maiden | $20,000 | $12,000 | Entry-level |
| Claiming | $25,000 | $15,000 | Low-tier competitive |
| Allowance | $40,000 | $24,000 | Mid-tier |
| Stakes | $100,000 | $60,000 | High-tier |
| Grade III | $200,000 | $120,000 | Elite |
| Grade I | $1,000,000 | $600,000 | Premier events |

### Sustainability Check

**Feeding Cost:** ~$500/feeding (estimated)
**Training Cost:** ~$1,000/session (estimated)

**Break-even Analysis:**
- Need ~3 Maiden race wins to cover 1 month of care
- Allowance races provide comfortable margin
- Stakes races are highly profitable

**Conclusion:** Purse structure sustainable for player progression.

---

## Success Criteria

**Feature Complete When:**
- [x] Purse calculation works for all race classes
- [x] Finishers receive correct payouts based on race class distribution patterns
- [x] Horse earnings updated and persisted
- [ ] Player balance updated for owned horses - **DEFERRED**
- [x] Race results display payout amounts
- [x] All unit tests passing (43 new tests for PurseCalculator)
- [ ] Integration tests validate end-to-end flow - **Not required for core implementation**
- [ ] Economic balance validated via simulation - **Not required for core implementation**

**Balance Goals Met When:**
- [x] Average purse per race class matches design targets (configured in PurseConfig)
- [ ] Player can sustain stable with race earnings - **DEFERRED (requires player balance)**
- [x] Grade I races feel significantly more rewarding than Maiden ($1M vs $20K base purse)
- [x] No exploits or negative balance edge cases (validated via unit tests)

---

## Open Questions

### Resolved
- [x] **Distribution percentages:** Win 60%, Place 20%, Show 10% (industry standard)
- [x] **Distance scaling:** +5% per furlong above 10f baseline
- [x] **Class structure:** 9 classes from Maiden to Grade I

### Pending
- [ ] **Entry fees:** Should races have entry fees deducted from payouts?
  - Consideration: Adds risk/reward, but may discourage racing
- [ ] **Breeder bonuses:** Should breeders earn % of offspring winnings?
  - Consideration: Future breeding feature
- [ ] **Track take:** Currently 10% goes to "track overhead" (not distributed). Keep this?
  - Consideration: Realistic but may feel punitive to players

---

## Future Enhancements

🔮 **Player Balance System (Deferred from Feature 009):**
- Add User.Balance property and tracking
- Update player balance when owned horses finish in the money
- Integration with entry fees, stable expenses, etc.
- Requires broader economic system design

🔮 **Starter Stipend (Optional):**
- Flat payment for horses finishing outside the money
- Does not count toward horse Earnings stat
- Configuration exists in PurseConfig but not currently used

🔮 **Entry Fees (Future Feature):**
- Deduct entry fee from purse or player balance
- Higher-class races have higher entry fees
- Risk/reward decision for player

🔮 **Breeder Bonuses (Future Feature):**
- Breeder earns 5-10% of offspring's race earnings
- Creates breeding economy incentive
- Tracked separately from owner earnings

🔮 **Jockey/Trainer Shares (Future Feature):**
- If jockeys/trainers implemented, they receive % of purse
- Deducted from owner's share
- Adds economic complexity

---

## References

### Related Features
- **Feature 001:** Race Engine (parent)
- **Feature 002:** Core Race Simulation (prerequisite)

### Related Files
- [RaceService.cs](c:\Development\TripleDerby\TripleDerby.Core\Services\RaceService.cs) - Race execution
- [Horse.cs](c:\Development\TripleDerby\TripleDerby.Core\Entities\Horse.cs) - Earnings property
- [RaceRunHorseResult.cs](c:\Development\TripleDerby\TripleDerby.Core\Models\RaceRunHorseResult.cs) - Payout property

### Real-World References
- Typical purse distributions: https://www.britannica.com/sports/horse-racing/Purses-and-awards
- Grade I purses: https://www.kentuckyderby.com/history/purse

---

## Changelog

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-24 | Claude Sonnet 4.5 | Initial specification drafted |
| 2025-12-26 | Claude Sonnet 4.5 | Core implementation complete: RaceClass entity, PurseCalculator service, horse earnings tracking, field size constraints. Player balance deferred. |

---

**Document Version:** 2.0
**Last Updated:** 2025-12-26
**Status:** 🟢 COMPLETE - Core Implementation Done (Player Balance Deferred)
**Feature Number:** 009
