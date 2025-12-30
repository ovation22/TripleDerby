# Feature Specification: Race Runs Grid Page

## Overview
A new Blazor page that displays a paginated, sortable grid of past race runs for a specific race. This allows users to view the history of completed races and analyze past performance.

## Requirements

### Functional Requirements
1. Display all completed race runs for a specific race in a data grid
2. Support pagination (consistent with existing pages like Horses and Races)
3. Support column sorting for key fields
4. Navigate from the Races page to view runs for a specific race
5. Display key race run information using existing `RaceRunSummary` model:
   - Winner name
   - Winner time (formatted)
   - Field size
   - Track condition
   - Race run ID

### Non-Functional Requirements
1. Consistent UI/UX with existing pages (Horses, Races, etc.)
2. Use FluentUI Blazor components (FluentDataGrid, FluentPaginator)
3. Follow existing pagination patterns using `PaginationRequest`
4. Responsive and performant grid rendering
5. Loading states and error handling

## User Experience

### Navigation Flow
1. User visits the Races page (`/races`)
2. User clicks a new "View Runs" button in the Actions column of a race
3. User navigates to `/races/{raceId}/runs` showing race run history
4. User can sort and paginate through past runs

### Grid Display
The grid will show the following columns:

| Column | Sortable | Description |
|--------|----------|-------------|
| Winner | Yes | Name of the winning horse |
| Time | Yes | Winner's time (formatted as mm:ss.ff) |
| Field Size | Yes | Number of horses in the race |
| Condition | Yes | Track condition (Fast, Muddy, etc.) |
| Run ID | No | UUID of the race run (for reference) |

### Future Enhancements (Not in Scope)
- Filters by horse name, date range, or conditions
- Row actions to view detailed play-by-play
- Quick view modals showing full results
- Comparison features between multiple runs

## Technical Approach

### Architecture Components

#### 1. Frontend (Blazor Web Assembly)
**New File**: `TripleDerby.Web/Components/Pages/RaceRuns.razor`
- Route: `@page "/races/{RaceId:int}/runs"`
- Uses `FluentDataGrid<RaceRunSummary>` and `FluentPaginator`
- Follows the pattern established in Horses.razor and Races.razor
- Renders mode: `InteractiveServer`

#### 2. API Client Extension
**File to Modify**: `TripleDerby.Web/ApiClients/Abstractions/IRaceRunApiClient.cs`
- Add method: `Task<PaginationResponse<RaceRunSummary>?> FilterAsync(byte raceId, PaginationRequest request, CancellationToken cancellationToken = default)`

**File to Modify**: `TripleDerby.Web/ApiClients/RaceRunApiClient.cs`
- Implement the new `FilterAsync` method
- Call endpoint: `GET /api/races/{raceId}/runs` with pagination query params

#### 3. Backend API Controller
**File to Modify**: `TripleDerby.Api/Controllers/RaceRunsController.cs`
- Modify existing `GetRuns` endpoint to accept `PaginationRequest` parameters
- Map `PaginationRequest` to service layer calls
- Return `PaginationResponse<RaceRunSummary>`

**Current Implementation**:
```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<RaceRunSummary>>> GetRuns(
    [FromRoute] byte raceId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
```

**Proposed Changes**:
- Accept sort parameters (sortBy, direction)
- Return data in `PaginationResponse` format
- Support dynamic sorting via query string

#### 4. Service Layer
**File to Modify**: `TripleDerby.Core/Services/RaceRunService.cs`
- Modify `GetRaceRuns` method to accept sort parameters
- Apply dynamic sorting to query results
- Keep existing pagination logic

**File to Modify**: `TripleDerby.Core/Abstractions/Services/IRaceRunService.cs`
- Update method signature to include sort parameters

#### 5. Navigation Enhancement
**File to Modify**: `TripleDerby.Web/Components/Pages/Races.razor`
- Add new "View Runs" action button in the Actions column
- Button navigates to `/races/{raceId}/runs`

### Data Flow

```
User clicks "View Runs" on Races page
    ↓
Navigate to /races/{raceId}/runs
    ↓
RaceRuns.razor loads and calls RefreshItemsAsync
    ↓
Build PaginationRequest (page, size, sortBy, direction)
    ↓
RaceRunApiClient.FilterAsync(raceId, request)
    ↓
HTTP GET /api/races/{raceId}/runs?page=1&size=10&sortBy=WinnerTime&direction=Asc
    ↓
RaceRunsController.GetRuns parses parameters
    ↓
RaceRunService.GetRaceRuns(raceId, page, pageSize, sortBy, direction)
    ↓
Query database via RaceRunsByRaceSpecification
    ↓
Apply sorting and pagination
    ↓
Return PagedResult<RaceRunSummary>
    ↓
Map to PaginationResponse
    ↓
Return to client
    ↓
Display in FluentDataGrid
```

### Integration Points

1. **Existing API Endpoint**: `GET /api/races/{raceId}/runs`
   - Already exists in `RaceRunsController.cs:158`
   - Returns `PagedResult<RaceRunSummary>`
   - Needs enhancement for sorting support

2. **Existing Service**: `RaceRunService.GetRaceRuns`
   - Already fetches paginated race runs
   - Needs enhancement for dynamic sorting
   - Uses `RaceRunsByRaceSpecification`

3. **Existing Models**:
   - `RaceRunSummary` (SharedKernel) - used as-is
   - `PagedResult<T>` - needs mapping to `PaginationResponse<T>`
   - `PaginationRequest` - existing pagination infrastructure

4. **Existing UI Components**:
   - `FluentDataGrid` from Microsoft.FluentUI.AspNetCore.Components
   - `FluentPaginator` for pagination control
   - Pattern matches Horses.razor and Races.razor

## Implementation Plan

### Phase 1: Backend API Enhancement (Sortable Endpoint)
1. Update `IRaceRunService.GetRaceRuns` signature to accept sort parameters
2. Modify `RaceRunService.GetRaceRuns` to apply dynamic sorting
3. Update `RaceRunsController.GetRuns` to accept sort query parameters
4. Map `PagedResult<T>` to `PaginationResponse<T>` format
5. Test endpoint with Swagger/Postman

### Phase 2: API Client Extension
1. Add `FilterAsync` method to `IRaceRunApiClient` interface
2. Implement method in `RaceRunApiClient` to call enhanced endpoint
3. Build query string from `PaginationRequest` parameters

### Phase 3: Blazor Page Implementation
1. Create `RaceRuns.razor` with route `/races/{RaceId:int}/runs`
2. Implement data grid with columns for Winner, Time, Field Size, Condition
3. Add sorting support for all columns
4. Implement pagination using `FluentPaginator`
5. Add loading states and error handling
6. Format winner time using existing `DisplayTime` pattern

### Phase 4: Navigation Integration
1. Modify `Races.razor` to add "View Runs" button in Actions column
2. Wire up navigation to `/races/{raceId}/runs`

### Phase 5: Testing & Refinement
1. Test sorting on each column
2. Test pagination with various page sizes
3. Test with races that have no runs (empty state)
4. Test navigation flow from Races page
5. Verify consistent styling with other pages

## Success Criteria

1. ✓ User can navigate from Races page to RaceRuns page via "View Runs" button
2. ✓ Grid displays all race runs for the selected race with correct data
3. ✓ User can sort by Winner, Time, Field Size, and Condition
4. ✓ Pagination works correctly with configurable page sizes
5. ✓ Loading states display while fetching data
6. ✓ Empty state shows appropriate message when race has no runs
7. ✓ UI is consistent with existing Horses and Races pages
8. ✓ Winner time is formatted correctly (mm:ss.ff)

## Assumptions & Decisions

### Assumptions
1. The existing `RaceRunSummary` model has sufficient data for the grid
2. No filtering is needed initially (filters can be added later)
3. Users access this page per-race, not a global view of all race runs
4. The existing `PagedResult<T>` can be mapped to `PaginationResponse<T>`

### Decisions Made
1. **Route**: Use `/races/{raceId}/runs` for RESTful hierarchy
2. **API Pattern**: Extend endpoint to support `PaginationRequest` for consistency
3. **Data Model**: Use existing `RaceRunSummary` without modifications
4. **Navigation**: Add action button in Races grid (not in nav menu)
5. **Future Work**: Defer detailed view/modal to future enhancement

### Open Questions
None - all requirements clarified during discovery.

## Technical Considerations

### Performance
- Pagination limits database query size
- Sorting happens in-memory after fetch (small result sets expected)
- Consider adding database-level sorting if result sets grow large

### Scalability
- Current approach fetches all runs, then paginates in-memory
- For races with thousands of runs, consider query-level pagination
- `RaceRunsByRaceSpecification` already filters by race, limiting scope

### Security
- No authentication/authorization currently implemented
- Race ID validation happens in service layer
- Returns 404 if race not found

### Edge Cases
- Race with zero runs: Display empty state message
- Invalid race ID: Return 404 and show error message
- Network errors: Display error message with retry option

## Dependencies

### External Libraries
- Microsoft.FluentUI.AspNetCore.Components (already in use)
- No new dependencies required

### Internal Systems
- RaceRunService and RaceRunsController (existing)
- PaginationRequest/Response infrastructure (existing)
- FluentDataGrid patterns from Horses/Races pages (existing)

## Timeline
Implementation can be completed in a single development session following the phased approach above. Each phase builds on the previous one, allowing for incremental testing and validation.

## References
- Existing Patterns: [Horses.razor](../../TripleDerby.Web/Components/Pages/Horses.razor)
- Existing Patterns: [Races.razor](../../TripleDerby.Web/Components/Pages/Races.razor)
- API Controller: [RaceRunsController.cs](../../TripleDerby.Api/Controllers/RaceRunsController.cs)
- Service Layer: [RaceRunService.cs](../../TripleDerby.Core/Services/RaceRunService.cs)
- Data Model: [RaceRunSummary.cs](../../TripleDerby.SharedKernel/RaceRunSummary.cs)
