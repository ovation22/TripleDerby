# Play-by-Play Commentary System - Feature Specification

**Feature Number:** 008

**Status:** 🔵 PROPOSED - Ready for Implementation

**Parent Feature:** [001-race-engine](001-race-engine.md) - Sub-Feature 3

**Prerequisites:**
- Feature 002 (Core Race Simulation) - ✅ Complete
- Feature 007 (Overtaking & Lane Changes) - ✅ Complete (provides lane change events)

---

## Summary

Implement an **event-driven race commentary system** that generates engaging, varied narrative notes for key moments during races. Instead of storing `"TODO"` for every tick, the system will detect significant events (position changes, lane maneuvers, lead changes, finishes) and generate descriptive commentary with varied language using synonym pools and randomized sentence templates.

**Core Design Philosophy:**
- **Selective narration** - Only significant events, not every tick (~15-30 notes per race)
- **Varied language** - Synonym pools prevent repetitive text
- **Factual reporting** - What happened, not why (keep tactical analysis for future enhancement)
- **Lane change emphasis** - Always narrate lane changes (high strategic importance)
- **Performance-aware** - Event detection only, no heavy text generation every tick

---

## Requirements

### Functional Requirements

#### FR1: Event Detection System
- **FR1.1:** Detect race start (tick 1)
- **FR1.2:** Detect position changes (horse passes another horse)
- **FR1.3:** Detect lead changes (new horse takes 1st place)
- **FR1.4:** Detect lane changes (all types: clean, risky success, risky failure)
- **FR1.5:** Detect final stretch entry (last 25% of race)
- **FR1.6:** Detect horses crossing finish line (per-horse finish events)
- **FR1.7:** Detect photo finishes (top 2 within 0.5 ticks)
- **FR1.8:** Detect stamina-related slowdowns (optional enhancement)

#### FR2: Commentary Generation
- **FR2.1:** Generate race start note with field size and conditions
- **FR2.2:** Generate position change notes with horse names
- **FR2.3:** Generate lead change notes with dramatic language
- **FR2.4:** Generate lane change notes (always, per user requirement)
  - Clean lane change: Standard movement
  - Risky squeeze success: Dramatic threading
  - Risky squeeze failure: Blocked attempt
- **FR2.5:** Generate final stretch note when entering last 25%
- **FR2.6:** Generate finish notes as each horse crosses
- **FR2.7:** Generate photo finish note if applicable
- **FR2.8:** Use varied language via synonym pools and templates

#### FR3: Language Variation System
- **FR3.1:** Synonym pools for common terms:
  - Horse actions: surges, charges, accelerates, pushes, advances, drives
  - Positioning: ahead, past, by, around
  - Lane changes: moves, shifts, drifts, cuts, threads, squeezes
  - Leaders: takes command, assumes the lead, seizes control
- **FR3.2:** Randomized sentence structure templates
- **FR3.3:** Horse name integration in natural positions

#### FR4: Performance & Storage
- **FR4.1:** Only generate notes when events detected (not every tick)
- **FR4.2:** Store empty string `""` for ticks without events
- **FR4.3:** No database schema changes required (use existing Note field)
- **FR4.4:** Event detection adds <2% overhead to simulation

#### FR5: Integration with Existing Systems
- **FR5.1:** Integrate with lane change system (Feature 007)
- **FR5.2:** Access race state via `RaceRun` and `RaceRunHorse` entities
- **FR5.3:** Store notes in `RaceRunTick.Note` field
- **FR5.4:** Maintain existing race simulation flow

---

## Technical Design

### Architecture Overview

```
Race Simulation Loop (RaceService.cs):
├─ Tick loop executes
│  ├─ UpdateHorsePosition (existing)
│  ├─ HandleOvertaking (existing)
│  └─ [NEW] Track events in tick-local collection
└─ After all horses updated:
   ├─ [NEW] GenerateTickCommentary(tick, events, raceRun)
   ├─ Create RaceRunTick (existing)
   └─ [NEW] Set Note = generated commentary (or "")
```

### Event Detection Strategy

**Event Collection Pattern:**
```csharp
// Collect events during tick processing
private class TickEvents
{
    public List<PositionChange> PositionChanges { get; set; } = new();
    public List<LaneChange> LaneChanges { get; set; } = new();
    public bool IsRaceStart { get; set; }
    public bool IsFinalStretch { get; set; }
    public List<HorseFinish> Finishes { get; set; } = new();
    public LeadChange? LeadChange { get; set; }
    public PhotoFinish? PhotoFinish { get; set; }
}

private record PositionChange(string HorseName, int OldPosition, int NewPosition);
private record LaneChange(string HorseName, int OldLane, int NewLane, LaneChangeType Type);
private record HorseFinish(string HorseName, int Place);
private record LeadChange(string NewLeader, string OldLeader);
private record PhotoFinish(string Horse1, string Horse2, double Margin);

private enum LaneChangeType { Clean, RiskySuccess, RiskyFailure }
```

### Commentary Generator Service

**New class: `RaceCommentaryGenerator.cs`**

```csharp
namespace TripleDerby.Core.Services;

public interface IRaceCommentaryGenerator
{
    string GenerateCommentary(TickEvents events, short tick, RaceRun raceRun);
}

public class RaceCommentaryGenerator : IRaceCommentaryGenerator
{
    private readonly IRandomGenerator _random;

    public RaceCommentaryGenerator(IRandomGenerator random)
    {
        _random = random;
    }

    public string GenerateCommentary(TickEvents events, short tick, RaceRun raceRun)
    {
        // Priority order (multiple events possible per tick)
        var notes = new List<string>();

        if (events.IsRaceStart)
            notes.Add(GenerateRaceStart(raceRun));

        if (events.LeadChange != null)
            notes.Add(GenerateLeadChange(events.LeadChange));

        foreach (var laneChange in events.LaneChanges)
            notes.Add(GenerateLaneChange(laneChange));

        foreach (var positionChange in events.PositionChanges)
            notes.Add(GeneratePositionChange(positionChange));

        if (events.IsFinalStretch)
            notes.Add(GenerateFinalStretch(raceRun));

        foreach (var finish in events.Finishes)
            notes.Add(GenerateFinish(finish));

        if (events.PhotoFinish != null)
            notes.Add(GeneratePhotoFinish(events.PhotoFinish));

        // Combine multiple events with "; " separator
        return string.Join("; ", notes);
    }

    // Individual generators with synonym pools...
}
```

### Synonym Pools & Templates

**Configuration class: `CommentaryConfig.cs`**

```csharp
namespace TripleDerby.Core.Configuration;

public static class CommentaryConfig
{
    // Action verbs for movement
    public static readonly string[] SurgeVerbs =
    {
        "surges", "charges", "accelerates", "pushes", "advances",
        "drives", "powers", "rockets", "bolts", "flies"
    };

    public static readonly string[] PassVerbs =
    {
        "passes", "overtakes", "moves past", "goes by",
        "slips past", "edges past", "gets around"
    };

    public static readonly string[] LaneChangeVerbs =
    {
        "moves", "shifts", "drifts", "cuts", "slides"
    };

    public static readonly string[] RiskySqueezeVerbs =
    {
        "threads through", "squeezes between", "darts through",
        "slips through traffic", "finds a seam", "navigates through"
    };

    // Position descriptors
    public static readonly string[] LeadPhrases =
    {
        "takes the lead", "assumes command", "seizes control",
        "moves to the front", "takes over", "grabs the lead"
    };

    // Finish descriptors
    public static readonly string[] FinishVerbs =
    {
        "crosses the line", "finishes", "completes the race",
        "hits the wire", "reaches the finish"
    };

    // Templates for each event type
    public static readonly string[] PositionChangeTemplates =
    {
        "{horse} {passVerb} {opponent} to move into {position}",
        "{horse} {surgeVerb} past {opponent}",
        "{horse} advances to {position}, passing {opponent}",
    };

    public static readonly string[] LaneChangeTemplates =
    {
        "{horse} {laneVerb} to lane {lane}",
        "{horse} {laneVerb} from lane {oldLane} to {newLane}",
        "Lane change: {horse} to {lane}",
    };

    public static readonly string[] RiskySqueezeTemplates =
    {
        "{horse} {squeezeVerb} into lane {lane}!",
        "Risky move! {horse} {squeezeVerb}",
        "{horse} makes a daring squeeze to lane {lane}",
    };

    // Event detection thresholds
    public const double PhotoFinishMargin = 0.5; // ticks
    public const decimal SignificantGap = 0.05m; // furlongs for position change
}
```

### Event Detection Logic

**Integrated into RaceService.cs:**

```csharp
private TickEvents DetectEvents(
    short tick,
    short totalTicks,
    RaceRun raceRun,
    Dictionary<Guid, int> previousPositions,
    Dictionary<Guid, byte> previousLanes,
    Guid? previousLeader)
{
    var events = new TickEvents();

    // Race start
    if (tick == 1)
        events.IsRaceStart = true;

    // Final stretch (last 25%)
    var raceProgress = (double)tick / totalTicks;
    if (raceProgress >= 0.75 && (tick - 1) / (double)totalTicks < 0.75)
        events.IsFinalStretch = true;

    // Current positions
    var currentPositions = raceRun.Horses
        .OrderByDescending(h => h.Distance)
        .Select((h, index) => new { h.Horse.Id, h.Horse.Name, Position = index + 1 })
        .ToList();

    var currentLeader = currentPositions.FirstOrDefault()?.Id;

    // Lead change
    if (currentLeader != null && previousLeader != null && currentLeader != previousLeader)
    {
        var newLeaderName = currentPositions.First(p => p.Id == currentLeader).Name;
        var oldLeaderName = raceRun.Horses.First(h => h.Horse.Id == previousLeader).Horse.Name;
        events.LeadChange = new LeadChange(newLeaderName, oldLeaderName);
    }

    // Position changes
    foreach (var current in currentPositions)
    {
        if (previousPositions.TryGetValue(current.Id, out var oldPos))
        {
            if (current.Position < oldPos) // Improved position (lower is better)
            {
                events.PositionChanges.Add(new PositionChange(
                    current.Name,
                    oldPos,
                    current.Position));
            }
        }
    }

    // Lane changes (detected by comparing previous vs current lanes)
    foreach (var horse in raceRun.Horses)
    {
        if (previousLanes.TryGetValue(horse.Horse.Id, out var oldLane))
        {
            if (horse.Lane != oldLane)
            {
                // Determine type based on context
                var type = horse.SpeedPenaltyTicksRemaining > 0
                    ? LaneChangeType.RiskySuccess
                    : LaneChangeType.Clean;

                events.LaneChanges.Add(new LaneChange(
                    horse.Horse.Name,
                    oldLane,
                    horse.Lane,
                    type));
            }
        }
    }

    // Horses crossing finish line
    var finishedThisTick = raceRun.Horses
        .Where(h => h.Distance >= raceRun.Race.Furlongs && h.Time >= tick - 1 && h.Time < tick)
        .OrderBy(h => h.Time)
        .ToList();

    foreach (var horse in finishedThisTick)
    {
        events.Finishes.Add(new HorseFinish(horse.Horse.Name, horse.Place));
    }

    // Photo finish detection (after all horses finish)
    if (raceRun.Horses.All(h => h.Distance >= raceRun.Race.Furlongs))
    {
        var top2 = raceRun.Horses.OrderBy(h => h.Time).Take(2).ToList();
        if (top2.Count == 2)
        {
            var margin = top2[1].Time - top2[0].Time;
            if (margin <= CommentaryConfig.PhotoFinishMargin)
            {
                events.PhotoFinish = new PhotoFinish(
                    top2[0].Horse.Name,
                    top2[1].Horse.Name,
                    margin);
            }
        }
    }

    return events;
}
```

### Commentary Generation Examples

**GenerateRaceStart:**
```csharp
private string GenerateRaceStart(RaceRun raceRun)
{
    var fieldSize = raceRun.Horses.Count;
    var condition = raceRun.ConditionId.ToString();
    var distance = raceRun.Race.Furlongs;

    return $"And they're off! {fieldSize} horses break from the gate for {distance} furlongs on a {condition} track.";
}
```

**GenerateLeadChange:**
```csharp
private string GenerateLeadChange(LeadChange leadChange)
{
    var leadPhrase = _random.PickRandom(CommentaryConfig.LeadPhrases);
    return $"{leadChange.NewLeader} {leadPhrase} from {leadChange.OldLeader}!";
}
```

**GenerateLaneChange:**
```csharp
private string GenerateLaneChange(LaneChange lc)
{
    return lc.Type switch
    {
        LaneChangeType.Clean =>
            $"{lc.HorseName} {_random.PickRandom(CommentaryConfig.LaneChangeVerbs)} to lane {lc.NewLane}",

        LaneChangeType.RiskySuccess =>
            $"{lc.HorseName} {_random.PickRandom(CommentaryConfig.RiskySqueezeVerbs)} into lane {lc.NewLane}!",

        LaneChangeType.RiskyFailure =>
            $"{lc.HorseName} blocked, unable to change lanes",

        _ => ""
    };
}
```

**GeneratePositionChange:**
```csharp
private string GeneratePositionChange(PositionChange pc)
{
    var ordinal = GetOrdinal(pc.NewPosition); // "1st", "2nd", "3rd", etc.
    var passVerb = _random.PickRandom(CommentaryConfig.PassVerbs);

    // Find who they passed (horse currently in position behind them)
    return $"{pc.HorseName} {passVerb} into {ordinal} place";
}
```

**GenerateFinish:**
```csharp
private string GenerateFinish(HorseFinish finish)
{
    var ordinal = GetOrdinal(finish.Place);
    var finishVerb = _random.PickRandom(CommentaryConfig.FinishVerbs);
    return $"{finish.HorseName} {finishVerb} in {ordinal} place";
}
```

**GeneratePhotoFinish:**
```csharp
private string GeneratePhotoFinish(PhotoFinish pf)
{
    return $"Photo finish! {pf.Horse1} edges {pf.Horse2} by {pf.Margin:F2} ticks!";
}
```

---

## Implementation Phases

### Phase 1: Core Infrastructure (2-3 hours)

**Scope:**
- Event detection framework
- Basic commentary generator
- Integration into RaceService tick loop
- Simple templates (no synonym variation yet)

**Tasks:**
1. Create `TickEvents` data structures
2. Create `IRaceCommentaryGenerator` interface
3. Create `RaceCommentaryGenerator` class (basic templates)
4. Add event detection to RaceService
5. Replace `Note = "TODO"` with commentary generation
6. Unit tests for event detection

**Deliverables:**
- [ ] TickEvents classes (PositionChange, LaneChange, etc.)
- [ ] RaceCommentaryGenerator service
- [ ] DetectEvents method in RaceService
- [ ] Integration into tick loop
- [ ] 10+ unit tests for event detection

**Acceptance Criteria:**
- Race start note generated
- Lead changes detected and narrated
- Position changes detected
- Lane changes detected
- Finish events narrated
- No performance regression (< 2% overhead)

---

### Phase 2: Language Variation System (2-3 hours)

**Scope:**
- Synonym pools configuration
- Randomized template selection
- Multiple sentence structures per event
- Natural language variety

**Tasks:**
1. Create `CommentaryConfig` with synonym pools
2. Add `IRandomGenerator.PickRandom<T>()` extension method
3. Expand templates with variations
4. Add ordinal number helper (1st, 2nd, 3rd...)
5. Integration tests with full races
6. Manual review of generated commentary

**Deliverables:**
- [ ] CommentaryConfig with 8+ synonym pools
- [ ] 3+ templates per event type
- [ ] Random selection logic
- [ ] Helper methods (ordinals, formatting)
- [ ] 5+ integration tests (full race commentary)

**Acceptance Criteria:**
- Multiple races generate varied commentary
- No repetitive language in same race
- Natural sentence structures
- All synonym pools utilized
- Commentary reads naturally

---

### Phase 3: Advanced Events & Polish (2-3 hours)

**Scope:**
- Photo finish detection
- Final stretch commentary
- Multi-event tick handling
- Edge case handling
- Documentation

**Tasks:**
1. Implement photo finish detection
2. Implement final stretch entry note
3. Handle multiple events in one tick (combine with "; ")
4. Add configuration for verbosity (future: enable/disable event types)
5. Update RACE_BALANCE.md with commentary examples
6. Write feature documentation

**Deliverables:**
- [ ] Photo finish detection and note
- [ ] Final stretch note
- [ ] Multi-event combination logic
- [ ] Edge case handling (empty races, single horse, etc.)
- [ ] Updated documentation
- [ ] Feature spec finalized

**Acceptance Criteria:**
- Photo finishes detected correctly
- Final stretch note appears at 75% mark
- Multiple events combined naturally
- No crashes on edge cases
- Documentation complete
- Example race commentary included in docs

---

## Testing Strategy

### Unit Tests (Phase 1)

**Event Detection Tests:**
```csharp
[Fact]
public void DetectEvents_Tick1_IsRaceStart()
{
    // Arrange: Race at tick 1
    // Act: DetectEvents(tick: 1, ...)
    // Assert: events.IsRaceStart == true
}

[Fact]
public void DetectEvents_LeaderChanges_LeadChangeEvent()
{
    // Arrange: Horse A was leader, now Horse B leads
    // Act: DetectEvents with position change
    // Assert: events.LeadChange != null
}

[Fact]
public void DetectEvents_HorseCrossesFinish_FinishEvent()
{
    // Arrange: Horse distance goes from 9.9 to 10.1 furlongs (10f race)
    // Act: DetectEvents
    // Assert: events.Finishes.Count == 1
}

[Fact]
public void DetectEvents_LaneChanges_LaneChangeEvent()
{
    // Arrange: Horse lane changes from 3 to 4
    // Act: DetectEvents with previous lanes
    // Assert: events.LaneChanges.Count == 1
}
```

**Commentary Generation Tests:**
```csharp
[Fact]
public void GenerateRaceStart_ReturnsStartNote()
{
    // Arrange: RaceRun with 10 horses, Fast condition, 10f
    // Act: GenerateRaceStart(raceRun)
    // Assert: Contains "10 horses", "10 furlongs", "Fast"
}

[Fact]
public void GenerateLeadChange_UsesVariedPhrases()
{
    // Arrange: Run 10 times with same lead change
    // Act: GenerateLeadChange for each
    // Assert: At least 3 different phrasings used
}

[Fact]
public void GenerateLaneChange_RiskySuccess_DramaticLanguage()
{
    // Arrange: LaneChange with Type = RiskySuccess
    // Act: GenerateLaneChange
    // Assert: Contains dramatic verbs (threads, squeezes, etc.)
}
```

### Integration Tests (Phase 2)

**Full Race Commentary:**
```csharp
[Fact]
public async Task FullRace_GeneratesVariedCommentary()
{
    // Arrange: 10f race with 12 horses
    // Act: Run race simulation
    // Assert:
    //   - Race start note present
    //   - Multiple position/lane changes narrated
    //   - All horses have finish notes
    //   - No repetitive language
}

[Fact]
public async Task Race_With_LaneChanges_NarratesAll()
{
    // Arrange: Race with high-agility horses (frequent lane changes)
    // Act: Run race
    // Assert: All lane changes appear in commentary
}

[Fact]
public async Task PhotoFinish_DetectedAndNarrated()
{
    // Arrange: Create horses with very similar stats
    // Act: Run race (may need multiple attempts for photo)
    // Assert: Photo finish note generated when margin < 0.5 ticks
}
```

### Manual Testing (Phase 3)

**Readability Review:**
1. Run 10 races with varied field sizes
2. Read all commentary notes
3. Check for:
   - Natural language flow
   - Appropriate drama level
   - Factual accuracy
   - Variety in repeated events

**Performance Benchmark:**
1. Run 100 races without commentary (baseline)
2. Run 100 races with commentary (new)
3. Compare simulation times
4. Ensure overhead < 2%

---

## Configuration & Extensibility

### Configuration Options (Future Enhancement)

```csharp
public static class CommentaryConfig
{
    // Verbosity control
    public static bool NarratePositionChanges { get; set; } = true;
    public static bool NarrateLaneChanges { get; set; } = true;
    public static bool NarrateFinalStretch { get; set; } = true;

    // Thresholds
    public static int MinimumPositionGainToNarrate { get; set; } = 1; // Must gain at least X positions
    public static double PhotoFinishMargin { get; set; } = 0.5; // ticks
}
```

### Extension Points

**Future enhancements could add:**
1. **Tactical insights mode** - Include "why" explanations
   - "Thunderbolt shifts to lane 1 (seeking RailRunner bonus)"
   - "Lightning slows in the stretch (low stamina)"

2. **Stamina commentary**
   - "Moonbeam tiring in the final furlong"
   - "Stardust fading, stamina depleted"

3. **Traffic commentary**
   - "Comet boxed in by traffic"
   - "Eclipse frustrated, no clear path"

4. **Excitement scaling**
   - Close races use more dramatic language
   - Runaway wins use calmer descriptions

5. **Localization support**
   - Synonym pools per language
   - Template translation

---

## Example Race Commentary Output

### Sample 10-Furlong Race

**Tick 1:**
> "And they're off! 12 horses break from the gate for 10 furlongs on a Fast track."

**Tick 23:**
> "Thunderbolt surges to the front; Lightning Dash moves to lane 3"

**Tick 47:**
> "Moonbeam passes Stardust into 4th place"

**Tick 89:**
> "Comet threads through traffic into lane 2!"

**Tick 142:**
> "Eclipse takes the lead from Thunderbolt!"

**Tick 178:** (75% mark)
> "Into the final stretch! Eclipse leads with Thunderbolt in pursuit"

**Tick 201:**
> "Shooting Star darts through into lane 1"

**Tick 228:**
> "Thunderbolt powers past Eclipse to seize control!"

**Tick 237:**
> "Thunderbolt crosses the line in 1st place"

**Tick 238:**
> "Eclipse finishes in 2nd place; Photo finish! Thunderbolt edges Eclipse by 0.42 ticks!"

**Tick 239:**
> "Moonbeam hits the wire in 3rd place"

**Tick 240-245:**
> (Remaining finishes...)

---

## Success Criteria

### Functional Success
- [ ] All key events detected accurately
- [ ] Commentary generated for 15-30 events per typical race
- [ ] Language varies naturally (no repetition)
- [ ] Lane changes always narrated
- [ ] Photo finishes detected when margin < 0.5 ticks
- [ ] Multi-event ticks combined readably

### Quality Success
- [ ] Commentary reads naturally (manual review)
- [ ] Synonym pools provide 5+ variations per verb type
- [ ] Templates provide 3+ sentence structures per event
- [ ] No grammatical errors
- [ ] Factually accurate (horse names, positions, lanes correct)

### Performance Success
- [ ] Event detection adds < 2% overhead
- [ ] No string allocation waste
- [ ] No database performance impact

### Code Quality Success
- [ ] 20+ unit tests passing
- [ ] 5+ integration tests passing
- [ ] Clean separation: detection vs generation
- [ ] Configuration externalized
- [ ] Well-documented with XML comments

---

## Dependencies & Integration

### Integration with Feature 007 (Overtaking)

The lane change system already provides:
- Lane change detection (old lane vs new lane comparison)
- Risky squeeze success detection (SpeedPenaltyTicksRemaining > 0)
- Clean lane change detection (penalty = 0)

Commentary generator simply reads these state changes and narrates them.

### Integration with RaceService

**Minimal changes required:**

1. Add previous state tracking:
```csharp
// Before tick loop
var previousPositions = new Dictionary<Guid, int>();
var previousLanes = new Dictionary<Guid, byte>();
Guid? previousLeader = null;

// Inside tick loop (after all horses updated)
var events = DetectEvents(tick, totalTicks, raceRun, previousPositions, previousLanes, previousLeader);
var commentary = _commentaryGenerator.GenerateCommentary(events, tick, raceRun);

var raceRunTick = new RaceRunTick
{
    Tick = tick,
    RaceRunTickHorses = new List<RaceRunTickHorse>(),
    Note = commentary  // CHANGED from "TODO"
};

// Update tracking dictionaries for next tick
UpdatePreviousState(raceRun, previousPositions, previousLanes, ref previousLeader);
```

2. Register service in DI:
```csharp
services.AddScoped<IRaceCommentaryGenerator, RaceCommentaryGenerator>();
```

3. Inject into RaceService constructor:
```csharp
public RaceService(
    ITripleDerbyRepository repository,
    IRandomGenerator randomGenerator,
    ISpeedModifierCalculator speedModifierCalculator,
    IStaminaCalculator staminaCalculator,
    IRaceCommentaryGenerator commentaryGenerator)  // NEW
```

---

## Files to Create/Modify

### New Files (Phase 1 & 2)

**Core Services:**
- `TripleDerby.Core/Services/RaceCommentaryGenerator.cs` (~200 lines)
- `TripleDerby.Core/Abstractions/Services/IRaceCommentaryGenerator.cs` (~15 lines)
- `TripleDerby.Core/Configuration/CommentaryConfig.cs` (~150 lines)
- `TripleDerby.Core/Services/CommentaryEvents.cs` (~50 lines - event data structures)

**Tests:**
- `TripleDerby.Tests.Unit/Services/RaceCommentaryGeneratorTests.cs` (~300 lines)
- `TripleDerby.Tests.Unit/Services/CommentaryEventDetectionTests.cs` (~250 lines)

### Modified Files

**Core Services:**
- `TripleDerby.Core/Services/RaceService.cs`
  - Add event detection logic (~80 lines)
  - Add previous state tracking (~30 lines)
  - Modify tick loop to call commentary generator (~10 lines)

**Dependency Injection:**
- `TripleDerby.AppHost/Program.cs` or DI registration file
  - Register IRaceCommentaryGenerator (~1 line)

**Extensions:**
- `TripleDerby.Core/Utilities/RandomGeneratorExtensions.cs` (new or modify)
  - Add `PickRandom<T>(this IRandomGenerator, T[] array)` extension

---

## Open Questions & Future Enhancements

### Deferred to Future
- [ ] **Tactical insights mode** - Explain WHY events happened (requires game design decision)
- [ ] **Stamina commentary** - Narrate fatigue effects
- [ ] **Traffic frustration** - Narrate blocked horses (FrontRunner frustration)
- [ ] **Excitement scaling** - Adjust drama based on race closeness
- [ ] **Localization** - Multi-language support
- [ ] **Configurable verbosity** - User/admin control over event types

### For Consideration
- Should we narrate horses fading due to stamina? (Currently: just the facts, no tactical insight)
- Should we limit note length? (Max characters per Note field?)
- Should we batch similar events? (e.g., "Three horses change lanes" vs listing each)
- Should there be a "highlight reel" mode with only top 5 most exciting events?

---

## Estimated Effort

- **Phase 1:** 2-3 hours (core infrastructure)
- **Phase 2:** 2-3 hours (language variation)
- **Phase 3:** 2-3 hours (advanced events & polish)
- **Total:** 6-9 hours

---

## Changelog

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-24 | Feature Discovery Skill | Initial specification created |
| 2025-12-24 | Feature Discovery Skill | User requirements gathered (selective, varied, always lanes, just facts) |

---

**Status:** 🔵 PROPOSED - Ready for Implementation

**Next Steps:** Approve specification and begin Phase 1 implementation

**End of Feature Specification**
