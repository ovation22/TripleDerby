# Implementation Plan: Race Runs Grid Page

## Overview
This plan breaks down the Race Runs Grid feature into 4 concrete implementation phases. Each phase follows Test-Driven Development (TDD) with vertical slices that deliver end-to-end, testable functionality.

**Feature Spec**: [race-runs-grid.md](../features/race-runs-grid.md)

## Approach

### Key Observations
1. **Existing Infrastructure**: `PagedList<T>` already exists and is used by Horses/Races APIs
2. **API Pattern**: Current `GetRuns` endpoint returns `PagedResult<T>`, needs to return `PagedList<T>` for consistency
3. **Sorting**: Current implementation sorts in-memory; we'll extend this to support dynamic sorting
4. **No New Models**: Using existing `RaceRunSummary` as-is per requirements

### Implementation Strategy
- **Phase 1**: Add sorting to backend (Service + Controller + Tests)
- **Phase 2**: Extend API client with new method
- **Phase 3**: Build Blazor page with grid
- **Phase 4**: Add navigation from Races page

Each phase delivers working, testable functionality that builds on previous phases.

---

## Phase 1: Backend - Sortable Race Runs Endpoint

**Goal**: Extend the race runs endpoint to support dynamic sorting and return `PagedList<RaceRunSummary>`.

**Vertical Slice**: API consumers can request sorted, paginated race runs via query parameters.

**Estimated Complexity**: Medium
**Risks**: Mapping between `PagedResult` and `PagedList`; ensuring backward compatibility

### RED - Write Failing Tests

**File**: `TripleDerby.Tests.Unit/Controllers/RaceRunsControllerTests.cs`

- [ ] Test: `GetRuns_WithSortByWinnerName_ReturnsSortedAscending`
  - Given: Race with multiple runs
  - When: Call with sortBy=WinnerName, direction=Asc
  - Then: Returns runs sorted by winner name ascending

- [ ] Test: `GetRuns_WithSortByWinnerTime_ReturnsSortedDescending`
  - Given: Race with multiple runs with different times
  - When: Call with sortBy=WinnerTime, direction=Desc
  - Then: Returns runs sorted by time descending (slowest first)

- [ ] Test: `GetRuns_WithSortByFieldSize_ReturnsSortedAscending`
  - Given: Race with runs having different field sizes
  - When: Call with sortBy=FieldSize, direction=Asc
  - Then: Returns runs sorted by field size ascending

- [ ] Test: `GetRuns_WithoutSortParameters_ReturnsDefaultSort`
  - Given: Race with multiple runs
  - When: Call without sort parameters
  - Then: Returns runs in default order (most recent first, by ID descending)

- [ ] Test: `GetRuns_WithInvalidRaceId_ReturnsNotFound`
  - Given: Invalid race ID
  - When: Call GetRuns
  - Then: Returns 404 NotFound

**Why these tests**: Define the sorting behavior contract and ensure backward compatibility with default sorting.

### GREEN - Make Tests Pass

**File**: `TripleDerby.Core/Abstractions/Services/IRaceRunService.cs`
- [ ] Modify: Add `sortBy` and `direction` parameters to `GetRaceRuns` method signature
  ```csharp
  Task<PagedResult<RaceRunSummary>?> GetRaceRuns(
      byte raceId,
      int page,
      int pageSize,
      string? sortBy = null,
      string? direction = null,
      CancellationToken cancellationToken = default);
  ```

**File**: `TripleDerby.Core/Services/RaceRunService.cs`
- [ ] Implement: Dynamic sorting logic in `GetRaceRuns`
  - Use sortBy parameter to determine which property to sort by
  - Support: "WinnerName", "WinnerTime", "FieldSize", "ConditionName"
  - Default to descending by RaceRun.Id (most recent first)
  - Use direction parameter: "Asc" or "Desc"

**File**: `TripleDerby.Api/Controllers/RaceRunsController.cs`
- [ ] Modify: `GetRuns` endpoint to accept sort query parameters
  ```csharp
  public async Task<ActionResult<PagedList<RaceRunSummary>>> GetRuns(
      [FromRoute] byte raceId,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] string? sortBy = null,
      [FromQuery] string? direction = null)
  ```
- [ ] Implement: Map `PagedResult<T>` to `PagedList<T>` in controller
  ```csharp
  var pagedResult = await raceRunService.GetRaceRuns(raceId, page, pageSize, sortBy, direction);
  if (pagedResult == null) return NotFound();

  var pagedList = new PagedList<RaceRunSummary>(
      pagedResult.Items,
      pagedResult.TotalCount,
      pagedResult.Page,
      pagedResult.PageSize);

  return Ok(pagedList);
  ```

**Implementation Notes**:
- Keep sorting in-memory for now (fetches all runs, sorts, then paginates)
- This is acceptable because race runs are scoped per-race (limited dataset)
- For future optimization, push sorting to database via Specification pattern

### REFACTOR - Clean Up

- [ ] Extract: Create helper method `ApplySorting(IEnumerable<RaceRunSummary> runs, string sortBy, string direction)` in RaceRunService
- [ ] Add: XML documentation to new parameters
- [ ] Consider: Creating constants for sortable field names to avoid magic strings

### Acceptance Criteria
- [ ] All new tests pass
- [ ] Existing RaceRunsController tests still pass (backward compatibility)
- [ ] Can call `/api/races/1/runs?page=1&pageSize=10&sortBy=WinnerTime&direction=Desc`
- [ ] Returns `PagedList<RaceRunSummary>` with sorted data
- [ ] Default behavior (no sort params) still works

**Deliverable**: API endpoint returns sorted race runs with query parameter support.

---

## Phase 2: API Client - FilterAsync Method

**Goal**: Add `FilterAsync` method to `RaceRunApiClient` that calls the enhanced endpoint.

**Vertical Slice**: Frontend can request sorted, paginated race runs using `PaginationRequest`.

**Estimated Complexity**: Simple
**Risks**: None - straightforward API client method

### RED - Write Failing Tests

**Note**: API clients are typically integration-tested or manually tested. For this phase, we'll verify manually via Swagger/Postman after implementation.

**Manual Test Plan**:
- [ ] Test: Call FilterAsync with default PaginationRequest
- [ ] Test: Call FilterAsync with sortBy=WinnerTime, direction=Desc
- [ ] Test: Call FilterAsync with invalid raceId, expect null return

### GREEN - Make Tests Pass

**File**: `TripleDerby.Web/ApiClients/Abstractions/IRaceRunApiClient.cs`
- [ ] Add: Method signature for FilterAsync
  ```csharp
  /// <summary>
  /// Gets a paginated, sortable list of race runs for a specific race.
  /// </summary>
  Task<PagedList<RaceRunSummary>?> FilterAsync(
      byte raceId,
      PaginationRequest request,
      CancellationToken cancellationToken = default);
  ```

**File**: `TripleDerby.Web/ApiClients/RaceRunApiClient.cs`
- [ ] Implement: FilterAsync method
  ```csharp
  public async Task<PagedList<RaceRunSummary>?> FilterAsync(
      byte raceId,
      PaginationRequest request,
      CancellationToken cancellationToken = default)
  {
      // Build query string: /api/races/{raceId}/runs?page=1&pageSize=10&sortBy=X&direction=Y
      var queryParams = new List<string>
      {
          $"page={request.Page}",
          $"pageSize={request.Size}"
      };

      if (!string.IsNullOrEmpty(request.SortBy))
          queryParams.Add($"sortBy={request.SortBy}");

      if (request.Direction != default)
          queryParams.Add($"direction={request.Direction}");

      var url = $"/api/races/{raceId}/runs?{string.Join("&", queryParams)}";
      var resp = await GetAsync<PagedList<RaceRunSummary>>(url, cancellationToken);

      if (resp.Success)
          return resp.Data;

      Logger.LogError("Unable to get race runs. RaceId: {RaceId}, Status: {Status} Error: {Error}",
          raceId, resp.StatusCode, resp.Error);
      return null;
  }
  ```

**Implementation Notes**:
- Reuse existing `BaseApiClient.GetAsync<T>` method
- Follow pattern from `HorseApiClient.FilterAsync`
- Build query string manually (simpler than using extension for race-scoped endpoint)

### REFACTOR - Clean Up

- [ ] Consider: Extract query string building to helper method if reused
- [ ] Verify: Consistent error logging with other API client methods

### Acceptance Criteria
- [ ] Method compiles and follows interface contract
- [ ] Can call from Blazor page (will verify in Phase 3)
- [ ] Returns null on error, PagedList on success
- [ ] Query string correctly formatted

**Deliverable**: API client can fetch sorted race runs for use in Blazor page.

---

## Phase 3: Blazor Page - RaceRuns Grid

**Goal**: Create the `/races/{raceId}/runs` page with sortable, paginated grid.

**Vertical Slice**: Users can view a complete list of past race runs with sorting and pagination.

**Estimated Complexity**: Medium
**Risks**: FluentDataGrid configuration, time formatting, empty state handling

### RED - Write Failing Tests

**Note**: Blazor component testing is typically done via bUnit. For this project, we'll validate manually via browser testing.

**Manual Test Plan**:
- [ ] Test: Navigate to `/races/1/runs`, verify grid loads
- [ ] Test: Click "Winner" column header, verify sort changes
- [ ] Test: Click "Time" column header, verify numeric sort
- [ ] Test: Click pagination, verify page changes
- [ ] Test: Navigate to `/races/999/runs`, verify error message
- [ ] Test: Navigate to race with no runs, verify empty state

### GREEN - Make Tests Pass

**File**: `TripleDerby.Web/Components/Pages/RaceRuns.razor` (NEW)
- [ ] Create: New Blazor page with route `@page "/races/{RaceId:int}/runs"`
- [ ] Add: Render mode `@rendermode InteractiveServer`
- [ ] Add: Required using statements (match Horses.razor pattern)
- [ ] Inject: `IRaceRunApiClient`, `NavigationManager`
- [ ] Implement: `[Parameter] public int RaceId { get; set; }`

**Component Structure**:
```razor
@page "/races/{RaceId:int}/runs"
@rendermode InteractiveServer

@using TripleDerby.SharedKernel
@using TripleDerby.SharedKernel.Pagination
@using TripleDerby.Web.ApiClients.Abstractions
@using FUISort = Microsoft.FluentUI.AspNetCore.Components.SortDirection

@inject IRaceRunApiClient RaceRunApi
@inject NavigationManager Navigation

<PageTitle>Race Runs</PageTitle>

<h1>Race Runs - Race @RaceId</h1>

@if (_loading)
{
    <FluentProgressRing />
    <p>Loading race runs...</p>
}
else if (_error)
{
    <FluentMessageBar Title="Error" Intent="MessageIntent.Error">
        Unable to load race runs. The race may not exist or have no completed runs.
    </FluentMessageBar>
}
else
{
    <FluentDataGrid @ref="_dataGrid"
                    Items="@_raceRuns"
                    RefreshItems="RefreshItemsAsync"
                    TGridItem="RaceRunSummary"
                    Pagination="_pagination"
                    Loading="_loading">
        <PropertyColumn Property="@(r => r.WinnerName)"
                        Title="Winner"
                        Sortable="true"
                        InitialSortDirection="FUISort.Ascending" />
        <TemplateColumn Title="Time" Sortable="true" SortBy="@_sortByWinnerTime">
            @FormatTime(context.WinnerTime)
        </TemplateColumn>
        <PropertyColumn Property="@(r => r.FieldSize)"
                        Title="Field Size"
                        Sortable="true"
                        Align="Align.End" />
        <PropertyColumn Property="@(r => r.ConditionName)"
                        Title="Condition"
                        Sortable="true" />
        <TemplateColumn Title="Run ID" Align="Align.End">
            <div style="font-family: monospace; font-size: 0.85em;">
                @context.RaceRunId.ToString().Substring(0, 8)...
            </div>
        </TemplateColumn>
    </FluentDataGrid>

    <FluentPaginator State="@_pagination" />
}

@code {
    [Parameter]
    public int RaceId { get; set; }

    bool _loading = true;
    bool _error = false;
    IQueryable<RaceRunSummary> _raceRuns = Enumerable.Empty<RaceRunSummary>().AsQueryable();
    FluentDataGrid<RaceRunSummary>? _dataGrid;
    PaginationState _pagination = new() { ItemsPerPage = 10 };

    GridSort<RaceRunSummary> _sortByWinnerTime = GridSort<RaceRunSummary>
        .ByAscending(r => r.WinnerTime);

    protected override async Task OnParametersSetAsync()
    {
        // Reload data when RaceId parameter changes
        await base.OnParametersSetAsync();
    }

    private async Task RefreshItemsAsync(GridItemsProviderRequest<RaceRunSummary> req)
    {
        _loading = true;
        _error = false;

        var start = req.StartIndex;
        var count = req.Count ?? _pagination.ItemsPerPage;
        if (count <= 0) count = 10;

        var page = (start / count) + 1;

        // Extract sort parameters from grid request
        var sortBy = "Id"; // Default
        var sortDirection = SortDirection.Desc; // Most recent first

        var sortInfo = req.GetSortByProperties().FirstOrDefault();
        if (!string.IsNullOrEmpty(sortInfo.PropertyName))
        {
            sortBy = sortInfo.PropertyName;
            sortDirection = sortInfo.Direction == FUISort.Ascending
                ? SortDirection.Asc
                : SortDirection.Desc;
        }

        var request = new PaginationRequest
        {
            Page = page,
            Size = count,
            SortBy = sortBy,
            Direction = sortDirection
        };

        var response = await RaceRunApi.FilterAsync((byte)RaceId, request);

        if (response == null)
        {
            _error = true;
            _raceRuns = Enumerable.Empty<RaceRunSummary>().AsQueryable();
            await _pagination.SetTotalItemCountAsync(0);
        }
        else
        {
            _raceRuns = response.Data.AsQueryable();
            await _pagination.SetTotalItemCountAsync(response.Total);
        }

        _loading = false;
        await InvokeAsync(StateHasChanged);
    }

    private string FormatTime(double timeInSeconds)
    {
        // Match the format from RaceRunHorseResult.DisplayTime
        var timeSpan = TimeSpan.FromSeconds(timeInSeconds * 0.50633);
        return timeSpan.ToString(@"m\:ss\.ff");
    }
}
```

**Implementation Notes**:
- Follow exact pattern from `Horses.razor` and `Races.razor`
- Use `FluentDataGrid` with `RefreshItems` callback pattern
- Map grid sort properties to `PaginationRequest.SortBy`
- Handle null response as error state
- Format time consistently with existing `RaceRunHorseResult.DisplayTime`

### REFACTOR - Clean Up

- [ ] Extract: Time formatting to shared utility if used elsewhere
- [ ] Consider: Adding "Back to Races" navigation button
- [ ] Polish: Add race name to page title (would require fetching race details)

### Acceptance Criteria
- [ ] Page loads at `/races/1/runs`
- [ ] Grid displays race run data correctly
- [ ] Clicking column headers changes sort order
- [ ] Pagination works (next/previous)
- [ ] Loading spinner shows while fetching
- [ ] Error message shows for invalid race ID
- [ ] Empty state shows for race with no runs
- [ ] Time displays in mm:ss.ff format

**Deliverable**: Fully functional race runs grid page with sorting and pagination.

---

## Phase 4: Navigation - Add "View Runs" Button

**Goal**: Add navigation from Races page to RaceRuns page via action button.

**Vertical Slice**: Complete user flow from Races list to RaceRuns history view.

**Estimated Complexity**: Simple
**Risks**: None - straightforward button addition

### RED - Write Failing Tests

**Manual Test Plan**:
- [ ] Test: Visit `/races`, verify "View Runs" button appears in Actions column
- [ ] Test: Click "View Runs" for race ID 1, navigates to `/races/1/runs`
- [ ] Test: Back button returns to `/races` page

### GREEN - Make Tests Pass

**File**: `TripleDerby.Web/Components/Pages/Races.razor`
- [ ] Modify: Actions column to include "View Runs" button
  ```razor
  <TemplateColumn Title="Actions" Align="@Align.End">
      <FluentButton aria-label="Start race"
                    IconEnd="@(new Icons.Regular.Size16.Play())"
                    @onclick="@(() => NavigateToRaceRun(context))" />
      <FluentButton aria-label="View race runs"
                    IconEnd="@(new Icons.Regular.Size16.History())"
                    @onclick="@(() => NavigateToRaceRuns(context))" />
  </TemplateColumn>
  ```

- [ ] Add: Navigation method in `@code` section
  ```csharp
  private void NavigateToRaceRuns(RacesResult race)
  {
      Navigation.NavigateTo($"/races/{race.Id}/runs");
  }
  ```

**Implementation Notes**:
- Use History icon (or similar) for "View Runs" button
- Place button next to existing "Start Race" button
- Consider button tooltip for clarity

### REFACTOR - Clean Up

- [ ] Consider: Button styling/spacing between action buttons
- [ ] Verify: Icon choice is intuitive (History, List, or other)

### Acceptance Criteria
- [ ] "View Runs" button appears in Races grid
- [ ] Clicking button navigates to correct `/races/{id}/runs` URL
- [ ] Button is visually distinct from "Start Race" button
- [ ] Navigation works for all races in grid

**Deliverable**: Complete navigation flow from Races to RaceRuns page.

---

## Testing Strategy

### Unit Tests
- **Phase 1**: Controller tests for sorting logic
- **Phase 1**: Service tests for dynamic sorting

### Integration Tests
- **Phase 2**: Manual API testing via Swagger
- **Phase 3**: Manual browser testing of Blazor page

### Manual Testing Checklist
After all phases:
- [ ] Navigate from `/races` to `/races/{id}/runs` via button
- [ ] Sort by each column (Winner, Time, Field Size, Condition)
- [ ] Test pagination (page 1, 2, last, previous)
- [ ] Test with race that has 0 runs (empty state)
- [ ] Test with invalid race ID (error handling)
- [ ] Verify time formatting matches mm:ss.ff pattern
- [ ] Test on different screen sizes (responsive)
- [ ] Verify loading states appear appropriately

---

## Risk Mitigation

### Risk 1: PagedResult vs PagedList Mapping
**Mitigation**: Map in controller layer; keep service layer unchanged
**Validation**: Unit tests verify mapping correctness

### Risk 2: In-Memory Sorting Performance
**Mitigation**: Scoped to single race (limited dataset)
**Future**: Push sorting to database via Specification if needed

### Risk 3: Time Formatting Inconsistency
**Mitigation**: Reuse exact formula from `RaceRunHorseResult.DisplayTime`
**Validation**: Manual testing with known race results

### Risk 4: Empty State Handling
**Mitigation**: Explicitly handle null response from API
**Validation**: Test with race that has no completed runs

---

## Dependencies and Prerequisites

### Before Starting
- [x] Feature specification approved
- [x] Existing codebase explored
- [x] Test infrastructure available

### External Dependencies
- FluentUI Blazor components (already installed)
- No new NuGet packages required

### Internal Dependencies
- `PagedList<T>` (SharedKernel) - exists
- `PaginationRequest` (SharedKernel) - exists
- `RaceRunSummary` (SharedKernel) - exists
- `RaceRunsByRaceSpecification` (Core) - exists

---

## Implementation Order

1. **Phase 1** (Backend) - Foundation for all other phases
2. **Phase 2** (API Client) - Enables frontend development
3. **Phase 3** (Blazor Page) - Core user-facing feature
4. **Phase 4** (Navigation) - Completes user flow

**Estimated Total Time**: 3-4 hours for full implementation and testing

---

## Success Metrics

After implementation:
- [ ] All unit tests pass (Phase 1)
- [ ] API returns sorted data via query params (Phase 1)
- [ ] API client successfully calls enhanced endpoint (Phase 2)
- [ ] Blazor page renders grid with sorting/pagination (Phase 3)
- [ ] Navigation flow works end-to-end (Phase 4)
- [ ] No regressions in existing functionality
- [ ] Code follows existing patterns and conventions

---

## Notes

- This plan focuses on incremental delivery of value
- Each phase is independently testable
- Phases build on each other logically
- Following TDD principles throughout
- Consistent with existing codebase patterns
- Minimal changes to existing code (backward compatible)

**Ready to implement!** Start with Phase 1 tasks.
