# UI Resilience & Error Boundaries - Implementation Plan

## Overview

**Feature**: [Feature 026: UI Resilience & Error Boundaries](../features/026-ui-resilience-error-boundaries.md)
**Approach**: TDD with Vertical Slices + Component Extraction
**Total Phases**: 7

## Summary

This implementation adds comprehensive error handling and resilience to the TripleDerby admin web application. We'll implement Polly retry policies with circuit breakers, add ErrorBoundary components at multiple levels, extract reusable widgets to reduce duplication, and update all data-loading components to handle failures gracefully. The approach prioritizes infrastructure first (Polly), then shared components (error UI, extracted widgets), and finally applies patterns to existing pages.

**Key Decisions**:
- Use Microsoft.Extensions.Http.Resilience (official .NET resilience library based on Polly)
- Extract widgets before adding ErrorBoundary to reduce code duplication
- Start with dashboard widgets for early validation of error isolation
- No unit tests for Blazor components (integration tests would require bUnit, deferred to future)
- Integration tests for Polly policies using in-memory test server

---

## Phase 1: Polly Resilience Infrastructure

**Goal**: Add HTTP resilience policies (retry + circuit breaker) to all API clients

**Vertical Slice**: API calls automatically retry transient failures and circuit breaker prevents cascading failures

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Web/Resilience/PollyPoliciesTests.cs`

- [ ] Test: Given 5xx error response, When API call executes, Then retries 3 times with exponential backoff
- [ ] Test: Given 404 error response, When API call executes, Then fails immediately without retry
- [ ] Test: Given HttpRequestException, When API call executes, Then retries 3 times
- [ ] Test: Given 5 consecutive failures, When circuit breaker opens, Then subsequent calls fail fast for 30 seconds
- [ ] Test: Given circuit breaker half-open, When test request succeeds, Then circuit closes
- [ ] Test: Given TaskCanceledException, When API call executes, Then retries up to 3 times

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Web/Resilience/PollyPolicies.cs` - Static helper class with retry and circuit breaker policy builders
- `TripleDerby.Tests.Unit/Web/Resilience/PollyPoliciesTests.cs` - Test suite

**Files to Modify**:
- `TripleDerby.Web/TripleDerby.Web.csproj` - Add `Microsoft.Extensions.Http.Resilience` NuGet package
- `TripleDerby.Web/Program.cs` - Configure resilience policies for all HttpClient registrations

**Tasks**:
- [ ] Add `Microsoft.Extensions.Http.Resilience` NuGet package (version 9.0.0+)
- [ ] Create `PollyPolicies` class with `AddStandardResilienceHandler()` configuration
- [ ] Configure retry policy: max 3 attempts, exponential backoff (1s, 2s, 4s), retry on 5xx/timeouts/network errors
- [ ] Configure circuit breaker: open after 5 consecutive failures, 30-second break duration
- [ ] Update all 9 HttpClient registrations in `Program.cs` to use `.AddStandardResilienceHandler()`
- [ ] Add structured logging for retry attempts and circuit breaker state changes

### REFACTOR - Clean Up

- [ ] Extract common HttpClient configuration into extension method `AddApiClientWithResilience<TClient, TImplementation>()`
- [ ] Remove duplication in Program.cs HttpClient registrations

### Acceptance Criteria

- [ ] All tests pass
- [ ] Transient failures (5xx, network errors) retry automatically
- [ ] Client errors (4xx) fail immediately without retry
- [ ] Circuit breaker opens after 5 consecutive failures
- [ ] All retry attempts and circuit breaker state changes are logged
- [ ] No regressions in existing API functionality

**Deliverable**: All API clients have automatic retry and circuit breaker protection

---

## Phase 2: Reusable Error UI Components

**Goal**: Create shared error display components for consistent error UX

**Vertical Slice**: Components can display standardized error messages with retry functionality

### RED - Write Failing Tests

*Note: Blazor component tests would require bUnit. For now, we'll validate through manual testing and defer automated component tests to future work.*

Manual Test Cases:
- Error widget displays error message
- Error widget shows retry button when OnRetry callback provided
- Error widget hides retry button when OnRetry is null
- Error message is accessible (ARIA labels present)

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Web/Components/Shared/ErrorWidget.razor` - Reusable error display with retry button
- `TripleDerby.Web/Components/Shared/ErrorWidget.razor.css` - Scoped styles for error widget

**Tasks**:
- [ ] Create `ErrorWidget.razor` with parameters: `ErrorMessage`, `OnRetry` (EventCallback), `ShowDetails` (bool)
- [ ] Add FluentMessageBar with Intent.Error for error display
- [ ] Add FluentButton for retry action (conditional on OnRetry != null)
- [ ] Add ARIA labels for accessibility
- [ ] Create scoped CSS for consistent styling
- [ ] Add optional details section for developer-facing error info (visible only in Development environment)

### REFACTOR - Clean Up

- [ ] Extract common styles to CSS variables
- [ ] Ensure consistent spacing with FluentUI design tokens

### Acceptance Criteria

- [ ] ErrorWidget displays error messages consistently
- [ ] Retry button appears only when callback provided
- [ ] Component is accessible (screen reader friendly)
- [ ] Styling matches FluentUI design system
- [ ] Component can be reused across pages and widgets

**Deliverable**: Reusable ErrorWidget component ready for use in ErrorBoundary fallbacks

---

## Phase 3: Widget Extraction - Horse Card Component

**Goal**: Extract duplicated horse card UI into reusable component

**Vertical Slice**: Horse cards display consistently across breeding and future horse-selection pages

### RED - Write Failing Tests

Manual Test Cases:
- Horse card displays horse name and earnings
- Horse card flips to show stats on back
- Card accepts custom height/width
- Card displays gender icon for male/female horses

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Web/Components/Widgets/HorseFlipCard.razor` - Reusable horse card component

**Tasks**:
- [ ] Create `HorseFlipCard.razor` with parameters: `Horse` (HorseResult), `Height`, `Width`, `MinimalStyle`
- [ ] Extract flip card markup from Breeding.razor (lines 40-63, 86-121)
- [ ] Make component flexible for both dam and sire display
- [ ] Add gender icon display (male/female)
- [ ] Support all existing stats fields (Starts, Wins, Place, Show, Sire, Dam, Earnings)

**Files to Modify**:
- `TripleDerby.Web/Components/Pages/Breeding.razor` - Replace duplicated card markup with `<HorseFlipCard />` component

### REFACTOR - Clean Up

- [ ] Remove 80+ lines of duplicated markup from Breeding.razor
- [ ] Ensure consistent styling between old and new implementation

### Acceptance Criteria

- [ ] HorseFlipCard displays all horse details correctly
- [ ] Breeding page uses HorseFlipCard for both Dams and Sires
- [ ] No visual regression in breeding page
- [ ] Component can be reused in future horse-selection scenarios

**Deliverable**: Reusable HorseFlipCard component, Breeding.razor simplified

---

## Phase 4: Widget Extraction - Race Details Header

**Goal**: Extract race details header into reusable component

**Vertical Slice**: Race metadata displays consistently across race-related pages

### RED - Write Failing Tests

Manual Test Cases:
- Race details header displays track, distance, surface, class, purse
- Header formatting is consistent
- Component handles null race data gracefully

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Web/Components/Widgets/RaceDetailsHeader.razor` - Reusable race details header

**Tasks**:
- [ ] Create `RaceDetailsHeader.razor` with parameter: `Race` (RaceResult or RacesResult)
- [ ] Extract header markup from RaceRuns.razor (lines 29-38)
- [ ] Display: Track, Distance (Furlongs), Surface, Class, Purse (formatted as currency)
- [ ] Add null handling for missing race data
- [ ] Style with FluentUI design tokens for consistent spacing

**Files to Modify**:
- `TripleDerby.Web/Components/Pages/RaceRuns.razor` - Replace header markup with `<RaceDetailsHeader />` component

### REFACTOR - Clean Up

- [ ] Remove duplicated race detail markup
- [ ] Ensure accessibility (proper heading hierarchy, ARIA labels)

### Acceptance Criteria

- [ ] RaceDetailsHeader displays all race metadata correctly
- [ ] RaceRuns page uses RaceDetailsHeader component
- [ ] No visual regression
- [ ] Component handles null/missing data gracefully

**Deliverable**: Reusable RaceDetailsHeader component, RaceRuns.razor simplified

---

## Phase 5: Dashboard Widgets - Add Error Handling & ErrorBoundary

**Goal**: Add error handling to dashboard widgets with ErrorBoundary isolation

**Vertical Slice**: Dashboard widgets fail independently and allow retry, demonstrating error isolation

### RED - Write Failing Tests

Manual Test Cases:
- When Stats API returns error, widget displays error state
- When widget fails, other widgets on page continue functioning
- Retry button re-attempts API call
- Loading state displays during API call
- Error state displays after failed API call

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Web/Components/Widgets/HorseGenderStats.razor` - Add error handling and retry
- `TripleDerby.Web/Components/Widgets/HorseColorStats.razor` - Add error handling and retry
- `TripleDerby.Web/Components/Widgets/HorseLegTypeStats.razor` - Add error handling and retry
- `TripleDerby.Web/Components/Pages/Home.razor` - Wrap each widget in ErrorBoundary

**Tasks for each widget**:
- [ ] Add `string? errorMessage` field
- [ ] Add `[Inject] ILogger<WidgetName> Logger` for logging
- [ ] Update `LoadAsync()` to check `ApiResponse.Success` and set `errorMessage` on failure
- [ ] Add try/catch in `LoadAsync()` to catch exceptions
- [ ] Add error state UI: display `errorMessage` with retry button
- [ ] Wire retry button to call `LoadAsync()` again
- [ ] Log errors with structured data (component name, API endpoint, status code)

**Tasks for Home.razor**:
- [ ] Wrap each `<HorseGenderStats />` in `<ErrorBoundary>` with `<ErrorContent>` using `<ErrorWidget />`
- [ ] Wrap each `<HorseColorStats />` in `<ErrorBoundary>` with `<ErrorContent>` using `<ErrorWidget />`
- [ ] Wrap each `<HorseLegTypeStats />` in `<ErrorBoundary>` with `<ErrorContent>` using `<ErrorWidget />`
- [ ] Add `@ref` to each widget for ErrorBoundary `Recover()` callback

### REFACTOR - Clean Up

- [ ] Extract common error handling pattern into base widget class or helper
- [ ] Ensure consistent error messages across widgets

### Acceptance Criteria

- [ ] When one widget fails, others continue working
- [ ] Failed widget displays error message with retry button
- [ ] Retry button successfully reloads widget data
- [ ] All errors are logged with context
- [ ] Loading states work correctly during retry
- [ ] ErrorBoundary catches unhandled widget exceptions

**Deliverable**: Dashboard widgets have isolated error handling, Home page demonstrates error isolation

---

## Phase 6: Data Grid Pages - Add Error Handling & ErrorBoundary

**Goal**: Add error handling to data grid pages (Horses, Races, RaceRuns)

**Vertical Slice**: Grid pages handle API failures gracefully and allow retry

### RED - Write Failing Tests

Manual Test Cases:
- When grid API call fails, error message displays in grid area
- Retry button reloads grid data
- Grid remains functional after error (pagination, sorting still work)
- Error state clears when retry succeeds

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Web/Components/Pages/Horses.razor` - Add error handling to `RefreshItemsAsync()`
- `TripleDerby.Web/Components/Pages/Races.razor` - Add error handling to `RefreshItemsAsync()`
- `TripleDerby.Web/Components/Pages/RaceRuns.razor` - Add error handling to `RefreshItemsAsync()` and `LoadRaceDetailsAsync()`

**Tasks for each grid page**:
- [ ] Add `string? errorMessage` field
- [ ] Add `bool hasError` field
- [ ] Update `RefreshItemsAsync()` to check `response == null` or `response.Total == 0` for error state
- [ ] Set `errorMessage` and `hasError = true` on failure
- [ ] Add error state UI above grid: FluentMessageBar with retry button
- [ ] Wire retry button to call `dataGrid.RefreshDataAsync()` or reload method
- [ ] Clear error state on successful load
- [ ] Wrap page content in `<ErrorBoundary>` at top level

### REFACTOR - Clean Up

- [ ] Extract grid error handling pattern if duplication exists
- [ ] Ensure consistent error messaging

### Acceptance Criteria

- [ ] Failed grid loads display error message with retry button
- [ ] Retry button successfully reloads grid data
- [ ] Grid pagination and sorting remain functional after error
- [ ] ErrorBoundary catches unhandled page-level exceptions
- [ ] All grid pages handle errors consistently

**Deliverable**: Data grid pages handle API failures gracefully with retry capability

---

## Phase 7: Complex Pages - Improve Error Handling (Breeding, TrainHorse)

**Goal**: Improve error handling in complex interactive pages with polling logic

**Vertical Slice**: Long-running operations (training, breeding) handle timeouts and failures with clear user feedback

### RED - Write Failing Tests

Manual Test Cases:
- When breeding submission fails, error message displays with retry option
- When polling times out, user sees timeout message
- When breeding API unavailable, clear error message displays
- Training polling handles API failures gracefully
- Failed operations allow user to retry or start new operation

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Web/Components/Pages/Breeding.razor` - Improve error handling in submission and polling
- `TripleDerby.Web/Components/Pages/TrainHorse.razor` - Improve error handling in training selection and polling

**Tasks for Breeding.razor**:
- [ ] Wrap page content in `<ErrorBoundary>`
- [ ] Improve error messages in `BreedAsync()` catch block (distinguish network vs API errors)
- [ ] Add timeout message when polling exceeds max attempts
- [ ] Ensure retry button works after all failure scenarios
- [ ] Log all errors with structured data

**Tasks for TrainHorse.razor**:
- [ ] Wrap page content in `<ErrorBoundary>`
- [ ] Add error handling to `LoadHorseAndOptions()` (handle null horse, null training options)
- [ ] Improve polling timeout handling in `PollTrainingStatus()`
- [ ] Add retry capability for failed training selection
- [ ] Display clear error messages for different failure modes (horse not found, training unavailable, polling timeout)
- [ ] Log all errors with context

### REFACTOR - Clean Up

- [ ] Extract common polling error handling pattern
- [ ] Ensure consistent error messaging across long-running operations

### Acceptance Criteria

- [ ] Breeding page handles all failure modes with clear messages
- [ ] Training page handles all failure modes with clear messages
- [ ] Polling timeouts display helpful user guidance
- [ ] Retry buttons restore functionality after failures
- [ ] All errors are logged with context
- [ ] ErrorBoundary catches unhandled exceptions on both pages

**Deliverable**: Complex pages have robust error handling for all failure scenarios

---

## Files Summary

### New Files
- `TripleDerby.Web/Resilience/PollyPolicies.cs` - Resilience policy configuration
- `TripleDerby.Web/Components/Shared/ErrorWidget.razor` - Reusable error UI
- `TripleDerby.Web/Components/Shared/ErrorWidget.razor.css` - Error widget styles
- `TripleDerby.Web/Components/Widgets/HorseFlipCard.razor` - Extracted horse card component
- `TripleDerby.Web/Components/Widgets/RaceDetailsHeader.razor` - Extracted race details header
- `TripleDerby.Tests.Unit/Web/Resilience/PollyPoliciesTests.cs` - Polly integration tests

### Modified Files
- `TripleDerby.Web/TripleDerby.Web.csproj` - Add resilience NuGet package
- `TripleDerby.Web/Program.cs` - Configure resilience for HttpClients
- `TripleDerby.Web/Components/Pages/Home.razor` - Add ErrorBoundary to widgets
- `TripleDerby.Web/Components/Widgets/HorseGenderStats.razor` - Add error handling
- `TripleDerby.Web/Components/Widgets/HorseColorStats.razor` - Add error handling
- `TripleDerby.Web/Components/Widgets/HorseLegTypeStats.razor` - Add error handling
- `TripleDerby.Web/Components/Pages/Horses.razor` - Add error handling and ErrorBoundary
- `TripleDerby.Web/Components/Pages/Races.razor` - Add error handling and ErrorBoundary
- `TripleDerby.Web/Components/Pages/RaceRuns.razor` - Add error handling and ErrorBoundary
- `TripleDerby.Web/Components/Pages/Breeding.razor` - Use HorseFlipCard, improve error handling
- `TripleDerby.Web/Components/Pages/TrainHorse.razor` - Improve error handling and ErrorBoundary

---

## Milestones

| Milestone | After Phase | What's Working |
|-----------|-------------|----------------|
| Resilience Infrastructure | Phase 1 | API calls retry automatically, circuit breaker prevents cascading failures |
| Reusable Components | Phase 2-4 | Standardized error UI, extracted horse cards and race headers reduce duplication |
| Dashboard Resilience | Phase 5 | Dashboard widgets fail independently, demonstrate error isolation |
| Grid Resilience | Phase 6 | Data grids handle failures gracefully with retry |
| Feature Complete | Phase 7 | All pages have robust error handling, long-running operations handle timeouts |

---

## Risks

| Risk | Mitigation | Phase |
|------|------------|-------|
| Polly configuration incorrect | Write comprehensive integration tests, validate with simulated failures | 1 |
| ErrorBoundary not catching exceptions | Test with intentionally thrown exceptions, review Blazor lifecycle | 5-7 |
| Retry logic causes performance issues | Use exponential backoff, monitor retry metrics, add circuit breaker | 1 |
| Extracted widgets break existing pages | Manual testing after extraction, visual regression checks | 3-4 |
| Error messages confuse users | User testing with non-technical users, iterate on messaging | 2, 5-7 |
| Logging overhead | Use structured logging efficiently, avoid logging sensitive data | 1, 5-7 |

---

## Phase Progression Protocol

**CRITICAL**: After each phase, STOP and follow this protocol:

1. **Implement** the phase tasks
2. **Run tests** to verify (unit tests for Phase 1, manual testing for other phases)
3. **Report results** to user with:
   - Summary of what was implemented
   - Test results (pass/fail counts)
   - Files created/modified
   - Any issues encountered
4. **STOP and WAIT** for user review
5. **Ask**: "Would you like me to commit these changes?"
6. **Wait for approval** before committing
7. **Wait for approval** before starting next phase

**Do NOT proceed to the next phase without explicit user approval.**

---

## Success Criteria

- [ ] All phases implemented
- [ ] Phase 1 tests passing (Polly integration tests)
- [ ] Manual testing passes for all error scenarios
- [ ] Dashboard widgets demonstrate error isolation
- [ ] Data grids handle failures gracefully
- [ ] Complex pages handle all failure modes
- [ ] No regressions in existing functionality
- [ ] Error messages are user-friendly and actionable
- [ ] All errors logged with structured context
- [ ] Circuit breaker prevents cascading failures
- [ ] Retry logic works as expected (3 attempts, exponential backoff)

---

## Testing Strategy

### Automated Tests (Phase 1 Only)
- **Polly Integration Tests**: Test retry logic, circuit breaker behavior, error handling
- **Location**: `TripleDerby.Tests.Unit/Web/Resilience/`
- **Framework**: xUnit, Moq
- **Coverage Goal**: 100% for PollyPolicies class

### Manual Testing (Phases 2-7)
- **Error Scenarios**: Kill API, return 5xx errors, timeout requests
- **Isolation Testing**: Break one widget, verify others work
- **Retry Testing**: Click retry buttons, verify data loads
- **Accessibility Testing**: Screen reader testing for error messages
- **Visual Testing**: Ensure no regressions after widget extraction

### Future Enhancement
- Add bUnit for Blazor component testing
- Add Playwright/Selenium for E2E testing
- Add automated accessibility testing

---

## Implementation Notes

### Why No Component Unit Tests?
Blazor component testing requires bUnit, which adds significant complexity. Given the UI-focused nature of this feature and the existing manual QA process, we'll validate through manual testing initially and defer automated component tests to future work.

### Polly vs Microsoft.Extensions.Http.Resilience
We're using `Microsoft.Extensions.Http.Resilience` (the official .NET library) instead of raw Polly because:
1. Better integration with .NET dependency injection
2. Pre-configured standard resilience patterns
3. Built-in logging and telemetry
4. Maintained by Microsoft as part of .NET ecosystem

### ErrorBoundary Strategy
- **Application level**: Routes.razor (catches catastrophic failures)
- **Component level**: Each widget/section (isolates failures)
- **Page level**: Optional (used for complex pages like Breeding, TrainHorse)

This multi-level approach ensures maximum isolation while providing fallbacks at each level.
