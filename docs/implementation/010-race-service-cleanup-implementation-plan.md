# Feature 010: RaceService Cleanup - Implementation Plan

**Feature Spec:** [010-race-service-cleanup.md](../features/010-race-service-cleanup.md)

**Status:** Ready for Implementation

**Created:** 2025-12-26

---

## Overview

This implementation plan breaks down Feature 010 (RaceService Cleanup and RaceResults API) into 6 concrete phases, following Test-Driven Development principles with vertical slices. Each phase delivers testable, incremental value while reducing technical debt and adding missing API functionality.

**Goals:**
- Reduce RaceService from 995 lines to ~400 lines
- Consolidate configuration into RaceModifierConfig
- Remove obsolete implementation phase comments
- Extract OvertakingManager and EventDetector subsystems
- Remove unused RaceRun.Purse database field
- Implement RESTful RaceRun API endpoints following Richardson Maturity Model Level 2

---

## Phase Structure

Each phase follows this pattern:
1. **RED**: Write/update tests that define expected behavior
2. **GREEN**: Implement changes to make tests pass
3. **REFACTOR**: Clean up and optimize
4. **VERIFY**: Run tests and validate deliverable

---

## Phase 1: Configuration Consolidation

**Goal:** Move duplicate simulation constants from RaceService to RaceModifierConfig

**Vertical Slice:** All race simulation code references centralized configuration

### RED - Write Failing Tests

No new tests needed - existing RaceServiceTests define expected behavior.

**Why:** Configuration consolidation is a pure refactoring. Existing tests ensure behavior preservation.

### GREEN - Make Tests Pass

**Task 1.1: Add simulation constants to RaceModifierConfig**

File: [TripleDerby.Core/Configuration/RaceModifierConfig.cs](../../TripleDerby.Core/Configuration/RaceModifierConfig.cs)

Add after line 37 (after `TargetTicksFor10Furlongs` constant):

```csharp
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
```

**Task 1.2: Remove constants from RaceService**

File: [TripleDerby.Core/Services/RaceService.cs](../../TripleDerby.Core/Services/RaceService.cs:22-37)

Remove lines 22-37 (all constant declarations).

**Task 1.3: Update all references in RaceService**

Search and replace in RaceService.cs:
- `AverageBaseSpeed` → `RaceModifierConfig.AverageBaseSpeed`
- `TargetTicksFor10Furlongs` → `RaceModifierConfig.TargetTicksFor10Furlongs`

**Expected locations:**
- Line ~130: `CalculateTotalTicks` method
- Line ~200+: Race simulation main loop

### REFACTOR - Clean Up

- Verify no duplicate constants remain
- Ensure consistent naming and documentation
- Add XML docs if missing

### Acceptance Criteria

- [ ] All 6 simulation constants moved to RaceModifierConfig
- [ ] Zero constants remain in RaceService (lines 22-37 deleted)
- [ ] All RaceServiceTests pass unchanged
- [ ] No performance degradation
- [ ] Code compiles without errors

**Deliverable:** RaceService references centralized configuration, no duplicate constants

**Estimated Complexity:** Simple

**Risks:** None - pure refactoring with test coverage

---

## Phase 2: Comment Cleanup

**Goal:** Remove obsolete implementation phase comments, preserve useful documentation

**Vertical Slice:** Codebase contains only relevant, helpful comments

### RED - Write Failing Tests

No tests needed - this is documentation cleanup.

**Why:** Comment removal doesn't affect runtime behavior.

### GREEN - Make Tests Pass

**Task 2.1: Remove obsolete feature/phase comments**

File: [TripleDerby.Core/Services/RaceService.cs](../../TripleDerby.Core/Services/RaceService.cs)

Remove these comment patterns:
- `// Feature 007 - Phase 1` (around line 376)
- `// Feature 007 - Phase 2` (around line 652)
- `// Phase 1: ...` (lines ~429, ~581)
- `// Phase 2: ...` (lines ~429, ~546, ~652)
- `// Feature 008:` (lines ~93, ~146, ~173, etc.)
- `// Feature 009:` (lines ~65, ~189)
- `// Feature 004:` (lines ~284, ~319)

File: [TripleDerby.Core/Configuration/RaceModifierConfig.cs](../../TripleDerby.Core/Configuration/RaceModifierConfig.cs)

Remove:
- `// Feature 005:` (line 158)
- `// Feature 007:` (lines 140, 237, 288)

**Keep these comments:**
- XML documentation (`/// <summary>`)
- Algorithm explanations (e.g., photo finish logic, quadratic stamina curves)
- Business rule clarifications (e.g., "Asymmetric: requires more clearance ahead than behind")

### REFACTOR - Clean Up

- Review all remaining comments for clarity
- Ensure XML docs are complete on public methods
- Remove any TODO comments that are now complete

### Acceptance Criteria

- [ ] Zero "Feature XXX - Phase Y" style comments remain
- [ ] All useful documentation preserved
- [ ] Code review confirms appropriate comment density
- [ ] All tests still pass

**Deliverable:** Clean, professional codebase without implementation history artifacts

**Estimated Complexity:** Simple

**Risks:** None - documentation only

---

## Phase 3: Class Extraction (Conservative Approach)

**Goal:** Extract OvertakingManager and EventDetector from RaceService

**Vertical Slice:** Race simulation delegates to focused subsystems via dependency injection

### RED - Write Failing Tests

No new tests - existing tests define expected behavior.

**Why:** Class extraction is refactoring. Tests verify behavior preservation.

### GREEN - Make Tests Pass

**Task 3.1: Create interfaces**

Create file: `TripleDerby.Core/Abstractions/Racing/IOvertakingManager.cs`

```csharp
namespace TripleDerby.Core.Abstractions.Racing;

/// <summary>
/// Manages overtaking detection, lane changes, and traffic response during race simulation.
/// </summary>
public interface IOvertakingManager
{
    /// <summary>
    /// Handles overtaking detection and lane change logic for a horse.
    /// Called once per tick per horse during race simulation.
    /// </summary>
    void HandleOvertaking(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks);

    /// <summary>
    /// Applies leg-type-specific traffic response effects when horse is blocked.
    /// Modifies speed based on traffic ahead and horse's personality.
    /// </summary>
    void ApplyTrafficEffects(RaceRunHorse horse, RaceRun raceRun, ref double currentSpeed);
}
```

Create file: `TripleDerby.Core/Abstractions/Racing/IEventDetector.cs`

```csharp
namespace TripleDerby.Core.Abstractions.Racing;

/// <summary>
/// Detects notable race events for commentary generation.
/// </summary>
public interface IEventDetector
{
    /// <summary>
    /// Detects notable events during a race tick for commentary generation.
    /// </summary>
    TickEvents DetectEvents(
        short tick,
        short totalTicks,
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        Guid? previousLeader,
        Dictionary<Guid, short> recentPositionChanges,
        Dictionary<Guid, short> recentLaneChanges);

    /// <summary>
    /// Updates the previous state tracking dictionaries for the next tick.
    /// </summary>
    void UpdatePreviousState(
        RaceRun raceRun,
        Dictionary<Guid, int> previousPositions,
        Dictionary<Guid, byte> previousLanes,
        ref Guid? previousLeader);
}
```

**Task 3.2: Implement OvertakingManager**

Create file: `TripleDerby.Core/Racing/OvertakingManager.cs`

Extract from RaceService.cs:
- `HandleOvertaking` method (lines ~587-620)
- `ApplyTrafficEffects` method (lines ~662-713)
- All helper methods:
  - `CalculateOvertakingThreshold`
  - `IsLaneClear`
  - `DetermineDesiredLane`
  - `FindLeastCongestedLane`
  - `FindBestOvertakingLane`
  - `AttemptLaneChange`
  - `AttemptRiskySqueezePlay`
  - `FindHorseAheadInLane`
  - `HasClearLaneAvailable`
  - `CalculateHorseSpeed`

Constructor injection: `IRandomGenerator`

**Task 3.3: Implement EventDetector**

Create file: `TripleDerby.Core/Racing/EventDetector.cs`

Extract from RaceService.cs:
- `DetectEvents` method (lines ~785-957)
- `UpdatePreviousState` method (lines ~966-994)

No dependencies needed (stateless).

**Task 3.4: Update RaceService**

File: [TripleDerby.Core/Services/RaceService.cs](../../TripleDerby.Core/Services/RaceService.cs:14-20)

Update constructor to inject new dependencies:

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
```

Replace direct method calls with:
- `overtakingManager.HandleOvertaking(...)`
- `overtakingManager.ApplyTrafficEffects(...)`
- `eventDetector.DetectEvents(...)`
- `eventDetector.UpdatePreviousState(...)`

Delete extracted methods from RaceService.

**Task 3.5: Register services in DI**

File: [TripleDerby.Api/Program.cs](../../TripleDerby.Api/Program.cs:69-76)

Add after line 73 (after `IPurseCalculator`):

```csharp
builder.Services.AddScoped<IOvertakingManager, OvertakingManager>(); // Feature 010
builder.Services.AddScoped<IEventDetector, EventDetector>(); // Feature 010
```

### REFACTOR - Clean Up

- Verify single responsibility: each class has one clear purpose
- Ensure all public methods have XML documentation
- Check for any remaining duplicate code
- Confirm naming consistency

### Acceptance Criteria

- [ ] OvertakingManager.cs created (~300 lines)
- [ ] EventDetector.cs created (~200 lines)
- [ ] RaceService.cs reduced to ~400 lines (from 995)
- [ ] All interfaces created and registered in DI
- [ ] All RaceServiceTests pass unchanged
- [ ] All RaceBalanceValidationTests pass
- [ ] All LaneChangeBalanceValidationTests pass
- [ ] All RaceCommentaryTests pass
- [ ] Code coverage maintained

**Deliverable:** Modular, maintainable race simulation architecture

**Estimated Complexity:** Medium

**Risks:** Integration issues if method signatures change - mitigated by comprehensive test suite

---

## Phase 4: Database Cleanup

**Goal:** Remove unused RaceRun.Purse field

**Vertical Slice:** Database schema reflects actual usage (purse stored in Race, not RaceRun)

### RED - Write Failing Tests

No new tests - purse calculation tests already exist and use `Race.Purse`.

**Why:** Field is unused. Removal verified by tests continuing to pass.

### GREEN - Make Tests Pass

**Task 4.1: Remove Purse from entity**

File: [TripleDerby.Core/Entities/RaceRun.cs](../../TripleDerby.Core/Entities/RaceRun.cs:29)

Delete line 29:
```csharp
public int Purse { get; set; }
```

**Task 4.2: Search for references**

Run grep to find any code referencing `RaceRun.Purse`:

```bash
grep -r "RaceRun\.Purse" --include="*.cs"
grep -r "raceRun\.Purse" --include="*.cs"
```

If any found, update to use `raceRun.Race.Purse` instead.

**Task 4.3: Drop and recreate database**

Since we're in early development (no production data), drop and recreate:

```bash
# Stop the application
# Drop database via Aspire dashboard or SSMS
# Restart application - EnsureCreated() will recreate schema
```

Alternatively using EF Core:
```bash
dotnet ef database drop --project TripleDerby.Infrastructure --startup-project TripleDerby.Api
# Restart app - Program.cs calls db.Database.EnsureCreated()
```

### REFACTOR - Clean Up

- Verify no references to removed field
- Confirm database schema is correct
- Check that all tests use `Race.Purse` correctly

### Acceptance Criteria

- [ ] `RaceRun.Purse` property removed from entity
- [ ] No references to `RaceRun.Purse` in codebase
- [ ] Database dropped and recreated successfully
- [ ] All tests pass (particularly PurseCalculatorTests)
- [ ] Database schema verified via SSMS or Aspire dashboard

**Deliverable:** Clean database schema without unused fields

**Estimated Complexity:** Simple

**Risks:** Low - field is unused per feature spec analysis

---

## Phase 5: RaceRun Results API

**Goal:** Implement RESTful nested resource endpoints for race runs

**Vertical Slice:** Full CRUD API for RaceRun resources following REST principles

### RED - Write Failing Tests

**Task 5.1: Write integration tests**

Create file: `TripleDerby.Tests.Integration/Controllers/RacesControllerTests.cs`

```csharp
[Fact]
public async Task GetRun_ValidIds_Returns200WithDetails()
{
    // Arrange
    var client = _factory.CreateClient();
    // TODO: Create test race run in database

    // Act
    var response = await client.GetAsync("/api/races/1/runs/{testRunId}");

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<RaceRunDetailResult>();
    Assert.NotNull(result);
    Assert.NotEmpty(result.HorseResults);
    Assert.NotEmpty(result.Commentary);
}

[Fact]
public async Task GetRun_InvalidRunId_Returns404()
{
    // Act
    var response = await client.GetAsync("/api/races/1/runs/00000000-0000-0000-0000-000000000001");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public async Task CreateRun_ValidRequest_Returns201WithLocation()
{
    // Arrange
    var request = new CreateRaceRunRequest { HorseId = testHorseId };

    // Act
    var response = await client.PostAsJsonAsync("/api/races/1/runs", request);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(response.Headers.Location);
    Assert.Contains("/api/races/1/runs/", response.Headers.Location.ToString());
}

[Fact]
public async Task GetRuns_ValidRaceId_ReturnsPaginatedList()
{
    // Act
    var response = await client.GetAsync("/api/races/1/runs?page=1&pageSize=10");

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<PagedResult<RaceRunSummary>>();
    Assert.NotNull(result);
    Assert.True(result.TotalCount >= 0);
}
```

**Why these tests:** Define expected REST API behavior before implementation.

### GREEN - Make Tests Pass

**Task 5.2: Create result DTOs**

Create file: `TripleDerby.SharedKernel/Results/RaceRunResults.cs`

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

**Task 5.3: Create service interface**

Create file: `TripleDerby.Core/Abstractions/Services/IRaceRunService.cs`

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

**Task 5.4: Create specifications**

Create file: `TripleDerby.Core/Specifications/RaceRunSpecifications.cs`

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

**Task 5.5: Implement service**

Create file: `TripleDerby.Core/Services/RaceRunService.cs`

```csharp
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
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
        var raceRun = await _repository.FirstOrDefaultAsync(
            new RaceRunDetailSpecification(raceId, raceRunId),
            cancellationToken);

        if (raceRun == null)
            return null;

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
        var race = await _repository.GetByIdAsync<Race>(raceId, cancellationToken);
        if (race == null)
            return null;

        var spec = new RaceRunsByRaceSpecification(raceId);
        var allRuns = await _repository.ListAsync(spec, cancellationToken);
        var totalCount = allRuns.Count;

        var runs = allRuns
            .OrderByDescending(r => r.Id)
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

**Task 5.6: Update controller**

File: [TripleDerby.Api/Controllers/RacesController.cs](../../TripleDerby.Api/Controllers/RacesController.cs)

Replace entire file with nested resource implementation:

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
    [HttpPost("{raceId}/runs")]
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

        var result = await _raceService.Race(raceId, request.HorseId);

        return CreatedAtAction(
            nameof(GetRun),
            new { raceId = raceId, runId = result.RaceRunId },
            result);
    }

    /// <summary>
    /// Returns a paginated list of all runs for a specific race.
    /// </summary>
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
    [HttpGet("{raceId}/runs/{runId}")]
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
/// Request to create a new race run.
/// </summary>
public record CreateRaceRunRequest
{
    public Guid HorseId { get; init; }
}
```

**Task 5.7: Register service in DI**

File: [TripleDerby.Api/Program.cs](../../TripleDerby.Api/Program.cs:76)

Add after IRaceService registration:

```csharp
builder.Services.AddScoped<IRaceRunService, RaceRunService>(); // Feature 010
```

### REFACTOR - Clean Up

- Ensure all XML documentation is complete
- Verify HTTP status codes are appropriate
- Check pagination logic for edge cases
- Validate error messages are user-friendly

### Acceptance Criteria

- [ ] `GET /api/races` returns list of all races
- [ ] `GET /api/races/{id}` returns specific race details or 404
- [ ] `POST /api/races/{id}/runs` creates run, returns 201 with Location header
- [ ] `GET /api/races/{id}/runs` returns paginated list
- [ ] `GET /api/races/{id}/runs/{runId}` returns detailed results or 404
- [ ] All endpoints follow REST principles (resource URIs, proper verbs, status codes)
- [ ] Response time < 200ms for typical queries
- [ ] Integration tests pass
- [ ] Manual API testing confirms functionality

**Deliverable:** Complete RESTful API for race runs with nested resource hierarchy

**Estimated Complexity:** Medium

**Risks:** Database query performance on large result sets - mitigate with pagination and indexed queries

---

## Phase 6: Integration Testing

**Goal:** Verify all phases integrated correctly, no regressions

**Vertical Slice:** Full feature works end-to-end with all components

### RED - Write Failing Tests

Tests already written in previous phases.

### GREEN - Make Tests Pass

**Task 6.1: Run full test suite**

```bash
dotnet test
```

Expected results:
- All RaceServiceTests pass
- All PurseCalculatorTests pass (43 tests)
- All RaceBalanceValidationTests pass
- All LaneChangeBalanceValidationTests pass
- All RaceCommentaryTests pass
- All new integration tests pass

**Task 6.2: Manual API testing**

Use REST client (Postman, curl, or Swagger UI):

1. **GET /api/races** - verify list returned
2. **GET /api/races/1** - verify details returned
3. **POST /api/races/1/runs** - create run, verify 201 + Location header
4. **GET /api/races/1/runs** - verify paginated list
5. **GET /api/races/1/runs/{id}** - verify detailed results
6. **GET /api/races/1/runs/invalid-guid** - verify 404

**Task 6.3: Performance testing**

Measure response times:
- GET race runs list: should be < 100ms
- GET race run details: should be < 200ms

If slow, check:
- Database indexes on `RaceRun.RaceId`
- Specification includes are minimal
- No N+1 query issues

**Task 6.4: Code review**

- RaceService reduced to ~400 lines
- No duplicate configuration
- No obsolete comments
- Clean separation of concerns
- All public methods documented

### REFACTOR - Clean Up

- Remove any debug logging added during development
- Ensure consistent code style
- Verify all using statements are necessary
- Check for any remaining TODOs

### Acceptance Criteria

- [ ] All 43+ PurseCalculator tests pass
- [ ] All RaceService tests pass
- [ ] All balance validation tests pass
- [ ] All commentary tests pass
- [ ] All integration tests pass
- [ ] Manual API testing successful
- [ ] API response time < 200ms
- [ ] Code review confirms quality standards
- [ ] No console errors or warnings
- [ ] Database schema validated

**Deliverable:** Production-ready Feature 010 implementation

**Estimated Complexity:** Simple (verification only)

**Risks:** None - catch-all phase to ensure nothing missed

---

## Testing Strategy

### Existing Tests (Must Pass Unchanged)

**RaceServiceTests**
- Location: `TripleDerby.Tests.Unit/Services/RaceServiceTests.cs`
- Coverage: Race simulation logic, result determination, tick processing
- Expected: All tests pass after refactoring

**PurseCalculatorTests**
- Count: 43 tests
- Coverage: Purse distribution, payout calculation
- Expected: All pass (uses `Race.Purse`, not `RaceRun.Purse`)

**Balance Validation Tests**
- RaceBalanceValidationTests
- LaneChangeBalanceValidationTests
- Coverage: Game balance, fair competition
- Expected: All pass unchanged

**RaceCommentaryTests**
- Coverage: Event detection, commentary generation
- Expected: All pass (EventDetector extraction doesn't change behavior)

### New Tests (Phase 5)

**Integration Tests**
- `RacesControllerTests.cs`
- Coverage: REST API endpoints, HTTP status codes, response structure
- Tests: GET/POST for races and runs, pagination, error cases

**Performance Tests**
- Response time validation
- Database query efficiency
- No N+1 query issues

---

## Rollback Plan

If issues arise:

1. **Phase 1 (Config):** Revert RaceService.cs and RaceModifierConfig.cs
2. **Phase 2 (Comments):** Restore comments from git history
3. **Phase 3 (Extraction):** Remove DI registrations, restore monolithic RaceService
4. **Phase 4 (Database):** Drop and recreate with old schema (add Purse back)
5. **Phase 5 (API):** Remove new endpoints, restore `throw new NotImplementedException()`

All changes are version controlled. Each phase can be rolled back independently.

---

## Success Metrics

**Code Quality:**
- RaceService: 995 lines → ~400 lines (60% reduction)
- Zero duplicate configuration constants
- Zero obsolete comments
- Three focused classes with single responsibilities

**API Completeness:**
- 5 REST endpoints implemented
- Richardson Maturity Model Level 2 compliance
- Full CRUD for race runs
- Proper resource hierarchy

**Maintainability:**
- Clear separation of concerns
- Dependency injection for testability
- Comprehensive XML documentation
- Consistent code style

**Performance:**
- No degradation from refactoring
- API response < 200ms
- Efficient database queries

---

## Related Documentation

- [Feature Spec: 010-race-service-cleanup.md](../features/010-race-service-cleanup.md)
- [Feature 007: Overtaking & Lane Changes](../features/007-overtaking-lane-changes.md)
- [Feature 008: Play-by-Play Commentary](../features/008-play-by-play-commentary.md)
- [Feature 009: Purse Distribution](../features/009-purse-distribution.md)

---

## Implementation Notes

**Conservative Extraction Approach:**
- Only extract clearly bounded subsystems (overtaking, events)
- Keep core simulation loop in RaceService
- Minimize interface surface area
- Preserve all existing behavior

**REST API Design:**
- Nested resources: `/api/races/{id}/runs/{runId}`
- Proper HTTP verbs: GET for retrieval, POST for creation
- Proper status codes: 200 OK, 201 Created, 404 Not Found, 400 Bad Request
- Location header on 201 responses
- Pagination for list endpoints

**Database Strategy:**
- Early development = drop/recreate acceptable
- No migration needed (would add for production)
- EnsureCreated() in Program.cs handles schema

**Dependency Injection:**
- All services scoped (per-request lifetime)
- Constructor injection for testability
- Register in Program.cs for discoverability

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-26 | Claude Sonnet 4.5 | Initial implementation plan created based on feature spec |
