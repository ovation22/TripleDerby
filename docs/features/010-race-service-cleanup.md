# RaceService Cleanup and RaceResults API - Feature Specification

**Feature Number:** 010

**Status:** 🔵 **Planned**

**Created:** 2025-12-26

---

## Summary

Comprehensive cleanup and refactoring of RaceService to improve maintainability, remove technical debt, and add missing API functionality. This feature addresses code organization, removes obsolete configuration fields and comments, extracts subsystems into focused classes, and implements a proper RaceResults retrieval endpoint following REST principles.

---

## Motivation

### Current State

**RaceService has grown to 995 lines** containing:
- Race simulation engine (main loop, tick processing)
- Overtaking and lane change logic (Feature 007)
- Traffic response system
- Event detection for commentary (Feature 008)
- Speed/stamina calculations
- Result determination

**Technical Debt Identified:**
1. **Duplicate/Unused Configuration Fields**: RaceService contains constants that duplicate values in RaceModifierConfig or are no longer used
2. **Phase Comments**: Comments like "Feature 007 - Phase 1/2" reference implementation phases that are now complete
3. **Monolithic Service**: All race logic in one large file makes navigation and testing difficult
4. **Unused Database Field**: `RaceRun.Purse` field exists but is never populated (purse is stored in `Race.Purse`)
5. **Missing API Endpoint**: No way to retrieve completed race results via API (only `/run` endpoint exists)

### Problems

1. **Maintainability**: 1000-line service file is difficult to navigate and understand
2. **Testability**: Tightly coupled subsystems are hard to test in isolation
3. **Discoverability**: Important configuration values scattered between RaceService and RaceModifierConfig
4. **API Completeness**: Cannot retrieve historical race results, limiting frontend and integration capabilities
5. **Technical Debt**: Obsolete comments and unused fields create confusion

### Goals

1. ✅ **Reduce RaceService size** from 995 lines to ~400 lines by extracting subsystems
2. ✅ **Consolidate configuration** by moving duplicate constants to RaceModifierConfig
3. ✅ **Remove obsolete comments** that reference completed implementation phases
4. ✅ **Extract focused classes** for overtaking, traffic, and events
5. ✅ **Remove unused database field** (RaceRun.Purse)
6. ✅ **Implement RaceResults API** following Richardson Maturity Model Level 2
7. ✅ **Maintain existing functionality** - all current behavior preserved

---

## Requirements

### Functional Requirements

**FR1: Configuration Consolidation**
- Move `TargetTicksFor10Furlongs`, `BaseSpeedMph`, `MilesPerFurlong`, `SecondsPerHour`, `FurlongsPerSecond`, `TicksPerSecond`, `AverageBaseSpeed` from RaceService to RaceModifierConfig
- Remove duplicate constants from RaceService
- Update all references to use RaceModifierConfig

**FR2: Comment Cleanup**
- Remove all "Feature 007 - Phase 1/2" style comments from RaceService
- Remove "Feature 008" references (feature is complete)
- Remove "Feature 009" references (feature is complete)
- Retain useful documentation that explains logic, not implementation history

**FR3: Class Extraction (Conservative Approach)**

Extract 3 focused subsystems from RaceService:

1. **OvertakingManager** (~300 lines)
   - Handles all lane change logic
   - Traffic detection and response
   - Leg-type-specific lane strategies
   - Risky squeeze play mechanics
   - Methods:
     - `HandleOvertaking(horse, raceRun, tick, totalTicks)`
     - `AttemptLaneChange(...)`
     - `DetermineDesiredLane(...)`
     - `FindLeastCongestedLane(...)`
     - `FindBestOvertakingLane(...)`
     - `AttemptRiskySqueezePlay(...)`
     - `ApplyTrafficEffects(...)`
     - `FindHorseAheadInLane(...)`
     - `HasClearLaneAvailable(...)`
     - `IsLaneClear(...)`
     - `CalculateOvertakingThreshold(...)`
     - `CalculateHorseSpeed(...)`

2. **EventDetector** (~200 lines)
   - Detects race events for commentary generation
   - Tracks state changes between ticks
   - Manages event cooldowns
   - Methods:
     - `DetectEvents(...)`
     - `UpdatePreviousState(...)`

3. **RaceSimulationEngine** (manages flow, delegates to subsystems)
   - Coordinates race simulation main loop
   - Handles initialization, tick processing, result determination
   - Delegates to OvertakingManager and EventDetector
   - Remains inside RaceService (not extracted to separate file)

**RaceService after extraction** (~400 lines):
- Public API methods: `Get()`, `GetAll()`, `Race()`
- Private helpers: `InitializeHorses()`, `UpdateHorsePosition()`, `DetermineRaceResults()`, `CalculateTotalTicks()`, `GenerateRandomConditionId()`
- Main simulation loop (delegates to subsystems)

**FR4: Database Cleanup**
- Remove `Purse` field from `RaceRun` entity (unused, duplicate of `Race.Purse`)
- Drop and recreate database with updated schema (early development, no migration needed)

**FR5: RaceRun Results API Endpoint**

Implement `GET /api/raceruns/{raceRunId}` endpoint to retrieve a specific race run's detailed results.

**Richardson Maturity Model Level 2 Requirements:**
- Resource-oriented URI: `/api/raceruns/{raceRunId}` (not `/getRaceRunResults?id=...`)
- Proper HTTP verbs: GET for retrieval
- HTTP status codes: 200 (success), 404 (race run not found), 400 (bad request)
- Idempotent: repeated calls return same result
- Stateless: no server-side session state

**FR6: RaceRun Creation API Endpoint (Refactor Existing)**

Refactor the existing `POST /api/races/{raceId}/run?horseId={guid}` to follow Richardson Maturity Model with proper hierarchical resources:

**Current (not RESTful):**
```
POST /api/races/1/run?horseId=abc-123
```

**Proposed (RESTful - Nested Resources):**
```
POST /api/races/1/runs
Content-Type: application/json

{
  "horseId": "abc-123"
}

Response: 201 Created
Location: /api/races/1/runs/286705ec-ad9f-4a96-21c1-08de448d257c
Body: { ... race run result ... }
```

**Resource Hierarchy:**
- `GET /api/races` - List all races
- `GET /api/races/1` - Get specific race details
- `POST /api/races/1/runs` - Create new run for race #1
- `GET /api/races/1/runs` - List all runs for race #1 (paginated)
- `GET /api/races/1/runs/286705ec-...` - Get specific run details

**Benefits:**
- Properly expresses parent-child relationship (Race has many RaceRuns)
- raceId in URL path (not request body) makes relationship explicit
- RESTful hierarchy matches domain model
- Supports future pagination: `GET /api/races/1/runs?page=2&pageSize=20`
- Clear, discoverable API structure

**Response Format:**

```json
{
  "raceRunId": "286705ec-ad9f-4a96-21c1-08de448d257c",
  "raceId": 1,
  "raceName": "Triple Derby",
  "trackName": "Triple Spires",
  "surfaceName": "Dirt",
  "furlongs": 10.0,
  "conditionId": 1,
  "conditionName": "Fast",
  "fieldSize": 12,
  "purse": 1500000,
  "horseResults": [
    {
      "place": 1,
      "horseName": "Thunder Bolt",
      "time": 116.89,
      "payout": 930000,
      "finalLane": 3,
      "finalPosition": 10.0
    },
    {
      "place": 2,
      "horseName": "Lightning Strike",
      "time": 117.12,
      "payout": 300000,
      "finalLane": 2,
      "finalPosition": 9.95
    }
    // ... all horses in finish order
  ],
  "commentary": [
    "And they're off!",
    "Thunder Bolt surges to the front!",
    // ... all commentary lines
  ]
}
```

**FR6: Naming Convention**
- Keep existing Race vs RaceRun terminology
- Race = event definition/template (e.g., "Triple Derby at 10 furlongs")
- RaceRun = historical execution instance of a race

### Non-Functional Requirements

**NFR1: Performance**
- No performance degradation from refactoring
- API endpoint response time < 200ms for typical queries

**NFR2: Backward Compatibility**
- All existing API endpoints remain unchanged
- All existing behavior preserved
- Tests continue to pass without modification

**NFR3: Code Quality**
- Each extracted class has single responsibility
- Clear separation of concerns
- Comprehensive XML documentation on public methods

---

## Technical Design

### Phase 1: Configuration Consolidation

**Changes to RaceModifierConfig.cs:**

Add new section after existing configuration:

```csharp
// ============================================================================
// Race Simulation Configuration (formerly in RaceService)
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

// Note: TargetTicksFor10Furlongs already exists at line 37

/// <summary>
/// Average base speed in furlongs per tick.
/// Calculated as: 10.0 / TargetTicksFor10Furlongs ≈ 0.0422
/// </summary>
public const double AverageBaseSpeed = 10.0 / TargetTicksFor10Furlongs;
```

**Changes to RaceService.cs:**

Remove constants section (lines 22-37), replace with references to config:

```csharp
public class RaceService(
    ITripleDerbyRepository repository,
    IRandomGenerator randomGenerator,
    ISpeedModifierCalculator speedModifierCalculator,
    IStaminaCalculator staminaCalculator,
    IRaceCommentaryGenerator commentaryGenerator,
    IPurseCalculator purseCalculator,
    IOvertakingManager overtakingManager,  // NEW
    IEventDetector eventDetector) : IRaceService  // NEW
{
    // Configuration constants removed - now in RaceModifierConfig

    public Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default)
    // ... rest of service
```

Update all references:
- `AverageBaseSpeed` → `RaceModifierConfig.AverageBaseSpeed`
- `TargetTicksFor10Furlongs` → `RaceModifierConfig.TargetTicksFor10Furlongs`

### Phase 2: Comment Cleanup

**Remove these comment patterns:**
- `// Feature 007 - Phase 1` (line 376)
- `// Feature 007 - Phase 2` (line 652)
- `// Phase 1: ...` (lines 429, 581)
- `// Phase 2: ...` (lines 429, 546, 652)
- `// Feature 008:` (lines 93, 146, 173, etc.)
- `// Feature 009:` (lines 65, 189)
- `// Feature 004:` (lines 284, 319)
- `// Feature 005:` (line 158 in config)
- `// Feature 007:` (lines 140, 237, 288)

**Keep useful comments:**
- XML documentation on public methods
- Complex algorithm explanations (e.g., photo finish detection)
- Business logic clarifications (e.g., "Asymmetric: requires more clearance ahead than behind")

### Phase 3: Class Extraction

**File: TripleDerby.Core/Racing/OvertakingManager.cs**

```csharp
namespace TripleDerby.Core.Racing;

/// <summary>
/// Manages overtaking detection, lane changes, and traffic response during race simulation.
/// Handles leg-type-specific strategies and risky squeeze play mechanics.
/// </summary>
public class OvertakingManager : IOvertakingManager
{
    private readonly IRandomGenerator _randomGenerator;

    public OvertakingManager(IRandomGenerator randomGenerator)
    {
        _randomGenerator = randomGenerator;
    }

    /// <summary>
    /// Handles overtaking detection and lane change logic for a horse.
    /// Called once per tick per horse during race simulation.
    /// </summary>
    public void HandleOvertaking(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks)
    {
        // [Implementation from RaceService lines 587-620]
    }

    /// <summary>
    /// Applies leg-type-specific traffic response effects when horse is blocked.
    /// Modifies speed based on traffic ahead and horse's personality.
    /// </summary>
    public void ApplyTrafficEffects(RaceRunHorse horse, RaceRun raceRun, ref double currentSpeed)
    {
        // [Implementation from RaceService lines 662-713]
    }

    // Private helper methods:
    // - CalculateOvertakingThreshold
    // - IsLaneClear
    // - DetermineDesiredLane
    // - FindLeastCongestedLane
    // - FindBestOvertakingLane
    // - AttemptLaneChange
    // - AttemptRiskySqueezePlay
    // - FindHorseAheadInLane
    // - HasClearLaneAvailable
    // - CalculateHorseSpeed
}
```

**File: TripleDerby.Core/Racing/EventDetector.cs**

```csharp
namespace TripleDerby.Core.Racing;

/// <summary>
/// Detects notable race events for commentary generation.
/// Compares current race state with previous tick to identify changes.
/// </summary>
public class EventDetector : IEventDetector
{
    /// <summary>
    /// Detects notable events during a race tick for commentary generation.
    /// </summary>
    public TickEvents DetectEvents(
        short tick,
        short totalTicks,
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        Guid? previousLeader,
        Dictionary<Guid, short> recentPositionChanges,
        Dictionary<Guid, short> recentLaneChanges)
    {
        // [Implementation from RaceService lines 785-957]
    }

    /// <summary>
    /// Updates the previous state tracking dictionaries for the next tick.
    /// </summary>
    public void UpdatePreviousState(
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        ref Guid? previousLeader)
    {
        // [Implementation from RaceService lines 966-994]
    }
}
```

**File: TripleDerby.Core/Abstractions/Racing/IOvertakingManager.cs**

```csharp
namespace TripleDerby.Core.Abstractions.Racing;

public interface IOvertakingManager
{
    void HandleOvertaking(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks);
    void ApplyTrafficEffects(RaceRunHorse horse, RaceRun raceRun, ref double currentSpeed);
}
```

**File: TripleDerby.Core/Abstractions/Racing/IEventDetector.cs**

```csharp
namespace TripleDerby.Core.Abstractions.Racing;

public interface IEventDetector
{
    TickEvents DetectEvents(
        short tick,
        short totalTicks,
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        Guid? previousLeader,
        Dictionary<Guid, short> recentPositionChanges,
        Dictionary<Guid, short> recentLaneChanges);

    void UpdatePreviousState(
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        ref Guid? previousLeader);
}
```

**Updated RaceService.cs:**

```csharp
public class RaceService(
    ITripleDerbyRepository repository,
    IRandomGenerator randomGenerator,
    ISpeedModifierCalculator speedModifierCalculator,
    IStaminaCalculator staminaCalculator,
    IRaceCommentaryGenerator commentaryGenerator,
    IPurseCalculator purseCalculator,
    IOvertakingManager overtakingManager,
    IEventDetector eventDetector) : IRaceService
{
    public Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default)
    {
        // Implementation (Phase 5)
    }

    public Task<IEnumerable<RacesResult>> GetAll(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<RaceRunResult> Race(byte raceId, Guid horseId, CancellationToken cancellationToken)
    {
        // [Existing implementation]
        // Replace direct calls with:
        // - overtakingManager.HandleOvertaking(...)
        // - overtakingManager.ApplyTrafficEffects(...)
        // - eventDetector.DetectEvents(...)
        // - eventDetector.UpdatePreviousState(...)
    }

    // Private helper methods remain:
    // - InitializeHorses
    // - UpdateHorsePosition
    // - DetermineRaceResults
    // - CalculateTotalTicks
    // - GenerateRandomConditionId
}
```

**Dependency Injection (Program.cs):**

```csharp
// Add new services
builder.Services.AddScoped<IOvertakingManager, OvertakingManager>();
builder.Services.AddScoped<IEventDetector, EventDetector>();
```

### Phase 4: Database Cleanup

**Update RaceRun.cs:**

```csharp
public class RaceRun
{
    [Key]
    public Guid Id { get; set; }

    public byte RaceId { get; set; }
    public virtual Race Race { get; set; } = null!;

    public ConditionId ConditionId { get; set; }

    public Guid? WinHorseId { get; set; }
    public virtual Horse WinHorse { get; set; } = null!;

    public Guid? PlaceHorseId { get; set; }
    public virtual Horse PlaceHorse { get; set; } = null!;

    public Guid? ShowHorseId { get; set; }
    public virtual Horse ShowHorse { get; set; } = null!;

    // REMOVED: public int Purse { get; set; }
    // Purse is stored in Race entity, not per RaceRun

    public virtual ICollection<RaceRunHorse> Horses { get; set; } = null!;
    public virtual ICollection<RaceRunTick> RaceRunTicks { get; set; } = null!;
}
```

**Drop and Recreate Database:**

Since we're in early development, simply drop and recreate the database with the updated schema:

```bash
# Stop the application
# Drop the database (via Aspire dashboard or SQL Server Management Studio)
# Or use EF Core:
dotnet ef database drop --project TripleDerby.Infrastructure --startup-project TripleDerby.Api
dotnet ef database update --project TripleDerby.Infrastructure --startup-project TripleDerby.Api
# Database will be recreated without RaceRun.Purse field
```

### Phase 5: RaceRun Results API Endpoint

**Update: RacesController.cs (add nested runs endpoints)**

```csharp
using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;

namespace TripleDerby.Api.Controllers;

/// <summary>
/// API endpoints for races and their runs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RacesController : ControllerBase
{
    private readonly IRaceService _raceService;
    private readonly IRaceRunService _raceRunService;

    public RacesController(IRaceService raceService, IRaceRunService raceRunService)
    {
        _raceService = raceService;
        _raceRunService = raceRunService;
    }

    /// <summary>
    /// Returns a list of all races.
    /// </summary>
    /// <returns>200 with list of races.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RacesResult>>> GetAll()
    {
        var races = await _raceService.GetAll();
        return Ok(races);
    }

    /// <summary>
    /// Returns details for a specific race.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <returns>200 with race details; 404 if not found.</returns>
    [HttpGet("{raceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceResult>> Get(byte raceId)
    {
        var race = await _raceService.Get(raceId);

        if (race == null)
            return NotFound($"Race with ID {raceId} not found.");

        return Ok(race);
    }

    /// <summary>
    /// Creates a new race run (simulates a race) for the specified race.
    /// </summary>
    /// <param name="raceId">Race identifier from URL path.</param>
    /// <param name="request">Race run creation request (horseId).</param>
    /// <returns>201 Created with race run results and Location header.</returns>
    /// <response code="201">Race run created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="404">Race or horse not found.</response>
    [HttpPost("{raceId}/runs")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceRunResult>> CreateRun(
        byte raceId,
        [FromBody] CreateRaceRunRequest request)
    {
        if (raceId == 0)
            return BadRequest("Race ID must be greater than 0.");

        if (request.HorseId == Guid.Empty)
            return BadRequest("Horse ID cannot be empty.");

        // Simulate the race (delegates to existing RaceService)
        var result = await _raceService.Race(raceId, request.HorseId);

        // Return 201 Created with Location header pointing to the new resource
        return CreatedAtAction(
            nameof(GetRun),
            new { raceId = raceId, runId = result.RaceRunId },
            result);
    }

    /// <summary>
    /// Returns a paginated list of all runs for a specific race.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>200 with paginated race runs.</returns>
    [HttpGet("{raceId}/runs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<RaceRunSummary>>> GetRuns(
        byte raceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1)
            return BadRequest("Page must be greater than 0.");

        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");

        var result = await _raceRunService.GetRaceRuns(raceId, page, pageSize);

        if (result == null)
            return NotFound($"Race with ID {raceId} not found.");

        return Ok(result);
    }

    /// <summary>
    /// Returns detailed results for a specific race run.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="runId">Race run identifier (GUID).</param>
    /// <returns>200 with race run details; 404 if not found.</returns>
    /// <response code="200">Returns the race run results.</response>
    /// <response code="404">Race run not found.</response>
    [HttpGet("{raceId}/runs/{runId}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaceRunDetailResult>> GetRun(byte raceId, Guid runId)
    {
        if (runId == Guid.Empty)
            return BadRequest("Race run ID cannot be empty.");

        var result = await _raceRunService.GetRaceRunDetails(raceId, runId);

        if (result == null)
            return NotFound($"Race run with ID {runId} not found for race {raceId}.");

        return Ok(result);
    }
}

/// <summary>
/// Request to create a new race run (horseId only, raceId in URL).
/// </summary>
public record CreateRaceRunRequest
{
    public Guid HorseId { get; init; }
}
```

**Update: IRaceRunService.cs**

```csharp
namespace TripleDerby.Core.Abstractions.Services;

public interface IRaceRunService
{
    /// <summary>
    /// Gets detailed results for a specific race run.
    /// </summary>
    Task<RaceRunDetailResult?> GetRaceRunDetails(byte raceId, Guid raceRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of race runs for a specific race.
    /// </summary>
    Task<PagedResult<RaceRunSummary>?> GetRaceRuns(byte raceId, int page, int pageSize, CancellationToken cancellationToken = default);
}
```

**Update: RaceRunService.cs**

```csharp
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Services;

public class RaceRunService : IRaceRunService
{
    private readonly ITripleDerbyRepository _repository;

    public RaceRunService(ITripleDerbyRepository repository)
    {
        _repository = repository;
    }

    public async Task<RaceRunDetailResult?> GetRaceRunDetails(
        byte raceId,
        Guid raceRunId,
        CancellationToken cancellationToken = default)
    {
        // Fetch race run with all related data
        var raceRun = await _repository.FirstOrDefaultAsync(
            new RaceRunDetailSpecification(raceId, raceRunId),
            cancellationToken);

        if (raceRun == null)
            return null;

        // Build response
        return new RaceRunDetailResult
        {
            RaceRunId = raceRun.Id,
            RaceId = raceRun.RaceId,
            RaceName = raceRun.Race.Name,
            TrackName = raceRun.Race.Track.Name,
            SurfaceName = raceRun.Race.Surface.Name,
            Furlongs = raceRun.Race.Furlongs,
            ConditionId = raceRun.ConditionId,
            ConditionName = raceRun.ConditionId.ToString(),
            FieldSize = raceRun.Horses.Count,
            Purse = raceRun.Race.Purse,
            HorseResults = raceRun.Horses
                .OrderBy(h => h.Place)
                .Select(h => new RaceRunHorseResult
                {
                    Place = h.Place,
                    HorseId = h.HorseId,
                    HorseName = h.Horse.Name,
                    Time = h.Time,
                    Payout = h.Payout,
                    FinalLane = h.Lane,
                    FinalPosition = h.Position
                })
                .ToList(),
            Commentary = raceRun.RaceRunTicks
                .Where(t => !string.IsNullOrWhiteSpace(t.Commentary))
                .OrderBy(t => t.Tick)
                .Select(t => t.Commentary)
                .ToList()
        };
    }

    public async Task<PagedResult<RaceRunSummary>?> GetRaceRuns(
        byte raceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Verify race exists
        var race = await _repository.GetByIdAsync<Race>(raceId, cancellationToken);
        if (race == null)
            return null;

        // Get total count of runs for this race
        var spec = new RaceRunsByRaceSpecification(raceId);
        var allRuns = await _repository.ListAsync(spec, cancellationToken);
        var totalCount = allRuns.Count;

        // Get paginated runs
        var runs = allRuns
            .OrderByDescending(r => r.Id) // Most recent first
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RaceRunSummary
            {
                RaceRunId = r.Id,
                ConditionId = r.ConditionId,
                ConditionName = r.ConditionId.ToString(),
                WinnerName = r.WinHorse?.Name ?? "Unknown",
                WinnerTime = r.Horses.FirstOrDefault(h => h.Place == 1)?.Time ?? 0.0,
                FieldSize = r.Horses.Count
            })
            .ToList();

        return new PagedResult<RaceRunSummary>
        {
            Items = runs,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
```

**Create: RaceRunResults.cs (all result types)**

```csharp
namespace TripleDerby.SharedKernel;

/// <summary>
/// Detailed results for a specific race run (full commentary and horse data).
/// </summary>
public record RaceRunDetailResult
{
    public Guid RaceRunId { get; init; }
    public byte RaceId { get; init; }
    public string RaceName { get; init; } = null!;
    public string TrackName { get; init; } = null!;
    public string SurfaceName { get; init; } = null!;
    public decimal Furlongs { get; init; }
    public ConditionId ConditionId { get; init; }
    public string ConditionName { get; init; } = null!;
    public int FieldSize { get; init; }
    public int Purse { get; init; }
    public List<RaceRunHorseResult> HorseResults { get; init; } = new();
    public List<string> Commentary { get; init; } = new();
}

/// <summary>
/// Summary of a race run (for list views, no commentary).
/// </summary>
public record RaceRunSummary
{
    public Guid RaceRunId { get; init; }
    public ConditionId ConditionId { get; init; }
    public string ConditionName { get; init; } = null!;
    public string WinnerName { get; init; } = null!;
    public double WinnerTime { get; init; }
    public int FieldSize { get; init; }
}

/// <summary>
/// Horse result details within a race run.
/// </summary>
public record RaceRunHorseResult
{
    public int Place { get; init; }
    public Guid HorseId { get; init; }
    public string HorseName { get; init; } = null!;
    public double Time { get; init; }
    public int Payout { get; init; }
    public byte FinalLane { get; init; }
    public double FinalPosition { get; init; }
}

/// <summary>
/// Generic paginated result wrapper.
/// </summary>
public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

**Update: RaceRunDetailSpecification.cs**

```csharp
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Specifications;

public class RaceRunDetailSpecification : Specification<RaceRun>
{
    public RaceRunDetailSpecification(byte raceId, Guid raceRunId)
        : base(rr => rr.RaceId == raceId && rr.Id == raceRunId)
    {
        AddInclude(rr => rr.Race);
        AddInclude($"{nameof(RaceRun.Race)}.{nameof(Race.Track)}");
        AddInclude($"{nameof(RaceRun.Race)}.{nameof(Race.Surface)}");
        AddInclude(rr => rr.Horses);
        AddInclude($"{nameof(RaceRun.Horses)}.{nameof(RaceRunHorse.Horse)}");
        AddInclude(rr => rr.RaceRunTicks);
    }
}

public class RaceRunsByRaceSpecification : Specification<RaceRun>
{
    public RaceRunsByRaceSpecification(byte raceId)
        : base(rr => rr.RaceId == raceId)
    {
        AddInclude(rr => rr.Horses);
        AddInclude($"{nameof(RaceRun.Horses)}.{nameof(RaceRunHorse.Horse)}");
        AddInclude(rr => rr.WinHorse);
    }
}
```

**Register Service in Program.cs:**

```csharp
builder.Services.AddScoped<IRaceRunService, RaceRunService>();
```

---

## Implementation Plan

### Phase 1: Configuration Consolidation (1 hour)

**Tasks:**
1. Add simulation configuration constants to RaceModifierConfig.cs
2. Remove constants from RaceService.cs
3. Update all references to use RaceModifierConfig
4. Run tests to verify no behavior changes

**Verification:**
- All RaceServiceTests pass
- No performance degradation

### Phase 2: Comment Cleanup (30 minutes)

**Tasks:**
1. Remove "Feature XXX - Phase X" comments from RaceService.cs
2. Remove feature number references from completed features
3. Retain useful documentation comments
4. Update XML documentation where needed

**Verification:**
- Code review confirms only obsolete comments removed
- Important logic explanations preserved

### Phase 3: Class Extraction (3 hours)

**Tasks:**
1. Create IOvertakingManager and IEventDetector interfaces
2. Create OvertakingManager class with extracted methods
3. Create EventDetector class with extracted methods
4. Update RaceService to use injected dependencies
5. Register new services in DI container (Program.cs)
6. Run all tests

**Verification:**
- All RaceServiceTests pass unchanged
- All RaceBalanceValidationTests pass
- All LaneChangeBalanceValidationTests pass
- RaceCommentaryTests pass
- Code coverage maintained

### Phase 4: Database Cleanup (15 minutes)

**Tasks:**
1. Update RaceRun.cs entity (remove Purse property)
2. Drop and recreate database with updated schema
3. Verify no code references RaceRun.Purse

**Verification:**
- Database recreated successfully without RaceRun.Purse
- All tests pass
- Database schema correct

### Phase 5: RaceRun Results API Endpoint (2 hours)

**Tasks:**
1. Create RaceRunDetailResult record classes
2. Create IRaceRunService interface
3. Implement RaceRunService
4. Create RaceRunDetailSpecification
5. Create RaceRunsController
6. Register service in DI
7. Write unit tests for new endpoint
8. Test API manually

**Verification:**
- GET /api/raceruns/{guid} returns 200 with full race run details
- GET /api/raceruns/{invalid-guid} returns 404
- GET /api/raceruns/00000000-0000-0000-0000-000000000000 returns 400
- Response includes all horse results, commentary, purse, etc.
- Tests pass

### Phase 6: Integration Testing (1 hour)

**Tasks:**
1. Run full test suite
2. Manual API testing
3. Performance testing (verify < 200ms response)
4. Code review

**Verification:**
- All 43+ PurseCalculator tests pass
- All RaceService tests pass
- All balance validation tests pass
- API responds in < 200ms for typical queries

---

## Testing Strategy

### Unit Tests

**Existing Tests (must continue to pass):**
- RaceServiceTests (all scenarios)
- PurseCalculatorTests (43 tests)
- RaceBalanceValidationTests
- LaneChangeBalanceValidationTests
- RaceCommentaryTests

**New Tests:**

```csharp
[Fact]
public async Task Get_ValidRaceId_ReturnsRaceResultWithStatistics()
{
    // Arrange
    var raceId = 1;

    // Act
    var result = await _sut.Get(raceId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(raceId, result.RaceId);
    Assert.True(result.TotalRuns >= 0);
    Assert.NotEmpty(result.RecentRuns);
}

[Fact]
public async Task Get_RaceWithNoRuns_ReturnsZeroStatistics()
{
    // Arrange
    var raceId = 99; // Race exists but never run

    // Act
    var result = await _sut.Get(raceId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(0, result.TotalRuns);
    Assert.Equal(0.0, result.FastestTime);
    Assert.Empty(result.RecentRuns);
}

[Fact]
public async Task Get_InvalidRaceId_ReturnsNull()
{
    // Arrange
    var raceId = 255; // Doesn't exist

    // Act
    var result = await _sut.Get(raceId);

    // Assert
    Assert.Null(result);
}
```

### Integration Tests

**API Endpoint Tests:**

```csharp
[Fact]
public async Task GetRace_ValidId_Returns200WithStatistics()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/races/1");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<RaceResult>(content);

    Assert.NotNull(result);
    Assert.Equal(1, result.RaceId);
    Assert.Contains("Derby", result.RaceName);
}

[Fact]
public async Task GetRace_InvalidId_Returns404()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/races/255");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

### Performance Tests

```csharp
[Fact]
public async Task GetRace_ResponseTime_UnderThreshold()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();

    // Act
    var result = await _sut.Get(1);

    // Assert
    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 200,
        $"Response took {stopwatch.ElapsedMilliseconds}ms (threshold: 200ms)");
}
```

---

## Acceptance Criteria

### Configuration Consolidation
- [x] All simulation constants moved to RaceModifierConfig
- [x] No duplicate constants remain in RaceService
- [x] All tests pass unchanged

### Comment Cleanup
- [x] All "Phase X" comments removed
- [x] All "Feature XXX" references removed from completed features
- [x] Useful documentation preserved

### Class Extraction
- [x] OvertakingManager extracted with 11 methods
- [x] EventDetector extracted with 2 methods
- [x] Interfaces created and registered in DI
- [x] RaceService reduced to ~400 lines
- [x] All existing tests pass unchanged

### Database Cleanup
- [x] RaceRun.Purse field removed from entity
- [x] Database dropped and recreated with updated schema
- [x] No references to RaceRun.Purse remain in codebase

### RaceRun API (Nested Resources)
- [x] GET /api/races endpoint returns list of all races
- [x] GET /api/races/{raceId} endpoint returns specific race details
- [x] POST /api/races/{raceId}/runs endpoint creates new race run
  - [x] raceId in URL path (not request body)
  - [x] Returns 201 Created with Location header: `/api/races/{raceId}/runs/{runId}`
  - [x] Request body contains only horseId (JSON)
- [x] GET /api/races/{raceId}/runs endpoint returns paginated list of runs
  - [x] Supports query parameters: page, pageSize
  - [x] Returns PagedResult with pagination metadata
  - [x] Most recent runs first
- [x] GET /api/races/{raceId}/runs/{runId} endpoint returns detailed race run
  - [x] Returns 200 with full race run details (horses, commentary, purse)
  - [x] Returns 404 for non-existent race or race run
  - [x] Validates raceId matches the race run's parent race
- [x] Response time < 200ms
- [x] Follows Richardson Maturity Model Level 2 (hierarchical resource URIs, HTTP verbs, status codes, Location header)
- [x] Old POST /api/races/{id}/run endpoint deprecated (optional: keep for backwards compatibility)

### Naming Convention
- [x] Race vs RaceRun terminology maintained throughout
- [x] No renaming required

---

## Rollback Plan

If issues arise during deployment:

1. **Configuration Consolidation**: Revert RaceService.cs and RaceModifierConfig.cs to previous version
2. **Class Extraction**: Remove new DI registrations, restore RaceService to monolithic version
3. **Database Schema**: Drop and recreate database from previous backup
4. **API Endpoint**: Remove/comment out new endpoint, restore `throw new NotImplementedException()`

All changes are backward-compatible. Database can be recreated from scratch if needed.

---

## Future Enhancements

**Deferred for Later:**

1. **RaceRun.RunDate Field**: Add timestamp tracking for when race was run
   - Requires migration to add `RunDate` column
   - Update RaceService to populate field
   - Enable time-based sorting of RecentRuns

2. **Pagination for RecentRuns**: Add skip/take parameters for large result sets

3. **Additional Statistics**:
   - Average field size
   - Most common winner
   - Track record comparisons

4. **Caching**: Cache RaceResults for performance (Redis or in-memory)

5. **HATEOAS (Richardson Level 3)**: Add hypermedia links to related resources

---

## Related Features

- **Feature 007**: Overtaking & Lane Changes (logic being extracted)
- **Feature 008**: Play-by-Play Commentary (event detection being extracted)
- **Feature 009**: Purse Distribution (cleanup of unused fields)

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-26 | Claude Sonnet 4.5 | Initial specification created based on codebase analysis and user requirements |
