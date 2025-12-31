# Feature 016: Breeding Kickoff UI with Polling and Status Tracking

## Feature Summary

Add breeding kickoff functionality to the Breeding.razor page, allowing users to submit breeding requests with owner selection and track the breeding process through polling with fallback/retry patterns. This feature mirrors the proven polling pattern from RaceRun.razor to provide real-time status updates and handle failures gracefully.

## Requirements

### Functional Requirements

1. **Breeding Submission**
   - User must select a dam (female horse) from the available dams
   - User must select a sire (male horse) from the available sires
   - User must specify an owner (required field) via autocomplete search
   - Submit breeding request with all three selections
   - Display loading state during submission

2. **Status Polling and Updates**
   - After submission, poll the breeding request status endpoint
   - Display current status to user (Pending, InProgress, Completed, Failed)
   - Poll at regular intervals (1 second) with maximum attempts (e.g., 10 attempts)
   - Show progress indicator while polling
   - Stop polling when status is Completed or Failed

3. **Success Handling**
   - When breeding completes successfully, display foal information
   - Fetch foal details using the FoalId from the status result
   - Show foal name, stats, and parentage
   - Provide option to breed another pair

4. **Error Handling**
   - Display error messages for submission failures
   - Show failure reason when breeding request fails
   - Display timeout message if polling exceeds maximum attempts
   - Provide retry/replay option for failed breeding requests
   - Handle validation errors (missing owner, invalid selections)

5. **User Experience**
   - Disable breed button while request is in progress
   - Clear previous results when starting new breeding
   - Maintain state consistency across polling lifecycle
   - Provide visual feedback for all states (loading, polling, success, error)

### Non-Functional Requirements

1. **Performance**
   - Poll status every 1000ms to balance responsiveness and server load
   - Maximum 10 polling attempts before timeout (10 seconds total)
   - Use CancellationTokenSource for proper cleanup on disposal

2. **Reliability**
   - Handle network failures gracefully
   - Support retry mechanism for failed breeding requests
   - Proper cleanup of polling resources on component disposal
   - Maintain consistency between UI state and server state

3. **Consistency**
   - Follow the same polling pattern as RaceRun.razor for consistency
   - Use similar error handling and state management patterns
   - Match existing UI/UX patterns in the application

## Technical Approach

### Architecture

The feature will follow the existing request/response pattern used in RaceRun:

1. **Request Submission**: `BreedingApiClient.SubmitBreedingAsync()` → Returns `BreedingRequestStatusResult`
2. **Status Polling**: `BreedingApiClient.GetRequestStatusAsync()` → Returns `BreedingRequestStatusResult`
3. **Result Retrieval**: `HorseApiClient.GetByIdAsync()` → Returns `HorseResult` (foal details)

### Integration Points

#### API Endpoints (To Be Created)

The following endpoints need to be added to `IBreedingApiClient` and `BreedingApiClient`:

```csharp
// IBreedingApiClient.cs
Task<BreedingRequestStatusResult?> SubmitBreedingAsync(
    Guid sireId,
    Guid damId,
    Guid ownerId,
    CancellationToken cancellationToken = default);

Task<BreedingRequestStatusResult?> GetRequestStatusAsync(
    Guid breedingRequestId,
    CancellationToken cancellationToken = default);
```

These will call the existing controller endpoints:
- POST `/api/breeding/requests` → Submit breeding request
- GET `/api/breeding/requests/{id}` → Get status

#### Data Flow

```
[Breeding.razor]
    ↓ Submit (sireId, damId, ownerId)
[BreedingApiClient.SubmitBreedingAsync]
    ↓ HTTP POST /api/breeding/requests
[BreedingController.CreateRequest]
    ↓ Creates BreedingRequest entity
[BreedingService.QueueBreedingAsync]
    ↓ Publishes BreedingRequested message
[Returns BreedingRequestStatusResult]
    ↓
[Breeding.razor polls every 1s]
    ↓ Poll (breedingRequestId)
[BreedingApiClient.GetRequestStatusAsync]
    ↓ HTTP GET /api/breeding/requests/{id}
[BreedingController.GetRequest]
    ↓ Queries BreedingRequest entity
[BreedingService.GetRequestStatus]
    ↓ Returns current status
[Returns BreedingRequestStatusResult]
    ↓
[Breeding.razor checks status]
    ├─ Completed → Fetch foal details
    ├─ Failed → Show error + retry option
    └─ Pending/InProgress → Continue polling
```

### State Management

The Breeding.razor component will maintain the following state variables:

```csharp
// Existing state (already present)
private Guid? _selectedDamId;
private Guid? _selectedSireId;
private UserResult? _selectedOwner;

// New state variables needed
private bool _isSubmitting;           // True during initial submission
private bool _isPolling;              // True while polling for status
private bool _breedingCompleted;      // True when breeding successfully completed
private Guid? _currentRequestId;      // Tracks current breeding request
private string _currentStatus = "";   // Display current status to user
private CancellationTokenSource? _pollCts;  // For cancelling polling

// Result state
private HorseResult? _foal;           // The resulting foal
private string? _errorMessage;        // Error messages
private bool _hideError = true;       // Controls error visibility
```

### Polling Implementation Pattern

Following the proven pattern from RaceRun.razor:

```csharp
private async Task StartPollingAsync()
{
    if (_currentRequestId == null)
        return;

    _pollCts?.Cancel();
    _pollCts?.Dispose();
    _pollCts = new CancellationTokenSource();

    _isPolling = true;
    var attempts = 0;
    var maxAttempts = 10;

    try
    {
        while (attempts < maxAttempts && !_pollCts.Token.IsCancellationRequested)
        {
            await Task.Delay(1000, _pollCts.Token);

            var status = await BreedingApi.GetRequestStatusAsync(_currentRequestId.Value);

            if (status == null)
            {
                _errorMessage = "Failed to get breeding status.";
                _hideError = false;
                break;
            }

            _currentStatus = status.Status.ToString();
            await InvokeAsync(StateHasChanged);

            if (status is { Status: BreedingRequestStatus.Completed, FoalId: not null })
            {
                _isPolling = false;
                await InvokeAsync(StateHasChanged);

                // Fetch foal details
                var foal = await HorseApi.GetByIdAsync(status.FoalId.Value);
                if (foal != null)
                {
                    _foal = foal;
                }
                break;
            }
            else if (status.Status == BreedingRequestStatus.Failed)
            {
                _errorMessage = $"Breeding failed: {status.FailureReason ?? "Unknown error"}";
                _hideError = false;
                break;
            }

            attempts++;
        }

        if (attempts >= maxAttempts)
        {
            _errorMessage = "Breeding processing timed out.";
            _hideError = false;
        }
    }
    catch (TaskCanceledException)
    {
        // Cancelled, ignore
    }
    catch (Exception ex)
    {
        _errorMessage = $"Error polling breeding status: {ex.Message}";
        _hideError = false;
    }
    finally
    {
        _isPolling = false;
        _breedingCompleted = true;
        await InvokeAsync(StateHasChanged);
    }
}
```

### UI Layout Changes

The Breeding.razor page will be updated with the following sections:

1. **Selection Section** (existing, minor updates)
   - Dam selector (existing)
   - Sire selector (existing)
   - Owner autocomplete (existing, but validation added)
   - Breed button (updated with new logic)

2. **Polling Status Section** (new)
   - Progress ring showing activity
   - Current status text (Pending, InProgress, etc.)
   - Only visible while `_isPolling` is true

3. **Error Section** (new)
   - FluentMessageBar with error messages
   - Retry button for failed requests
   - Only visible when `_errorMessage` is set

4. **Result Section** (new)
   - Display foal information when completed
   - Show foal name, stats, parentage
   - "Breed Another" button to reset state

5. **State-Based Visibility**
   - Hide selection section when breeding has started (`_hasBreedingStarted`)
   - Show polling section only when `_isPolling`
   - Show result section only when `_breedingCompleted` and `_foal` is set
   - Show error section when errors occur

### Key Implementation Files

#### Files to Modify

1. **TripleDerby.Web/Components/Pages/Breeding.razor**
   - Add new state variables
   - Implement `SubmitBreedingAsync()` method
   - Implement `StartPollingAsync()` method
   - Add UI sections for polling, errors, and results
   - Add `BreedAnotherPair()` method to reset state
   - Implement `IDisposable` for cleanup

2. **TripleDerby.Web/ApiClients/Abstractions/IBreedingApiClient.cs**
   - Add `SubmitBreedingAsync()` method signature
   - Add `GetRequestStatusAsync()` method signature

3. **TripleDerby.Web/ApiClients/BreedingApiClient.cs**
   - Implement `SubmitBreedingAsync()` HTTP POST call
   - Implement `GetRequestStatusAsync()` HTTP GET call

#### Files Referenced (No Changes)

- **TripleDerby.Api/Controllers/BreedingController.cs** - Already has needed endpoints
- **TripleDerby.Core/Services/BreedingService.cs** - Already has needed service methods
- **TripleDerby.SharedKernel/Dtos/BreedingRequestStatusResult.cs** - Already defined
- **TripleDerby.SharedKernel/BreedRequest.cs** - Already defined
- **TripleDerby.SharedKernel/Enums/BreedingRequestStatus.cs** - Already defined

## Implementation Plan

### Phase 1: API Client Enhancement
**Goal**: Add missing API client methods to communicate with breeding endpoints

**Tasks**:
1. Add method signatures to `IBreedingApiClient`:
   - `SubmitBreedingAsync(Guid sireId, Guid damId, Guid ownerId, CancellationToken)`
   - `GetRequestStatusAsync(Guid breedingRequestId, CancellationToken)`

2. Implement methods in `BreedingApiClient`:
   - `SubmitBreedingAsync()`: POST to `/api/breeding/requests` with `BreedRequest` body
   - `GetRequestStatusAsync()`: GET from `/api/breeding/requests/{id}`
   - Handle HTTP responses, deserialization, and error cases

**Testing**:
- Verify API calls return expected DTOs
- Test error handling for network failures
- Confirm cancellation token support

### Phase 2: Breeding.razor State and Submission
**Goal**: Add state management and breeding submission logic

**Tasks**:
1. Add new state variables to Breeding.razor @code section:
   - Submission and polling flags
   - Request tracking
   - Result and error state

2. Update existing `BreedAsync()` method to:
   - Validate owner selection (required)
   - Clear previous state
   - Set `_isSubmitting = true`
   - Call `BreedingApi.SubmitBreedingAsync()`
   - Store `_currentRequestId`
   - Initiate polling via `StartPollingAsync()`
   - Handle submission errors

3. Update Breed button:
   - Add owner validation to disabled condition
   - Show loading state during submission

**Testing**:
- Verify validation prevents submission without owner
- Confirm error handling for API failures
- Test state transitions

### Phase 3: Polling and Status Tracking
**Goal**: Implement polling mechanism with retry and timeout

**Tasks**:
1. Implement `StartPollingAsync()` method:
   - Initialize CancellationTokenSource
   - Poll every 1000ms for up to 10 attempts
   - Update `_currentStatus` on each poll
   - Handle Completed, Failed, and timeout cases
   - Trigger `StateHasChanged()` appropriately

2. Add cleanup in `Dispose()`:
   - Cancel and dispose `_pollCts`

3. Implement `IDisposable` interface

**Testing**:
- Verify polling starts and updates status
- Test timeout handling after 10 attempts
- Confirm cancellation on component disposal
- Test failure scenarios

### Phase 4: Result Display and Success Handling
**Goal**: Fetch and display foal information on successful breeding

**Tasks**:
1. Add logic to fetch foal when status is Completed:
   - Call `HorseApi.GetByIdAsync(status.FoalId.Value)`
   - Store in `_foal` state variable

2. Add result UI section:
   - Show foal name, color, stats
   - Display parentage information
   - Add "Breed Another Pair" button

3. Implement `BreedAnotherPair()` method:
   - Clear all state variables
   - Reset selections
   - Call `StateHasChanged()`

**Testing**:
- Verify foal details are fetched and displayed
- Test "Breed Another" functionality
- Confirm state reset works correctly

### Phase 5: Error Handling and Retry
**Goal**: Implement comprehensive error handling with retry mechanism

**Tasks**:
1. Add error UI section:
   - FluentMessageBar for error display
   - Show `_errorMessage` and `FailureReason`
   - Add retry button for failed requests

2. Implement retry/replay functionality:
   - Call existing replay endpoint if needed
   - Or reset state and allow resubmission

3. Add error handling for:
   - Submission failures
   - Polling failures
   - Timeout scenarios
   - Network errors

**Testing**:
- Test error message display
- Verify retry functionality
- Test various error scenarios

### Phase 6: UI Polish and Final Integration
**Goal**: Refine UI, add polish, and ensure consistent UX

**Tasks**:
1. Add polling status UI:
   - FluentProgressRing with status text
   - Conditional visibility based on `_isPolling`

2. Implement conditional rendering:
   - Hide selection section after breeding starts
   - Show appropriate sections based on state
   - Ensure smooth transitions

3. Apply styling consistent with RaceRun.razor

4. Add validation feedback:
   - Highlight missing owner
   - Show validation messages

**Testing**:
- Test all UI states and transitions
- Verify responsive design
- Ensure accessibility
- Cross-browser testing

### Phase 7: Testing and Documentation
**Goal**: Comprehensive testing and documentation

**Tasks**:
1. End-to-end testing:
   - Full breeding flow from selection to result
   - Error scenarios and recovery
   - Multiple breeding cycles
   - Component disposal and cleanup

2. Update documentation:
   - Add user guide for breeding feature
   - Document API client changes
   - Update component documentation

3. Performance testing:
   - Verify polling doesn't cause memory leaks
   - Test cancellation token cleanup
   - Monitor network requests

## Success Criteria

### Functional Success
- [ ] User can submit breeding request with dam, sire, and owner
- [ ] Owner selection is required and validated
- [ ] Polling shows real-time status updates
- [ ] Successful breeding displays foal information
- [ ] Failed breeding shows error with retry option
- [ ] Timeout after 10 polling attempts shows appropriate message
- [ ] "Breed Another" resets state for new breeding
- [ ] Component properly cleans up resources on disposal

### Technical Success
- [ ] Follows same polling pattern as RaceRun.razor
- [ ] Proper error handling for all failure scenarios
- [ ] CancellationTokenSource properly disposed
- [ ] No memory leaks from polling
- [ ] API client methods properly implemented
- [ ] State management is consistent and reliable
- [ ] UI updates reflect server state accurately

### UX Success
- [ ] Clear visual feedback for all states (loading, polling, success, error)
- [ ] Error messages are helpful and actionable
- [ ] Smooth transitions between states
- [ ] Consistent with existing UI patterns
- [ ] Responsive and accessible

## Open Questions

### Implementation Decisions

1. **Foal Display Details**
   - What specific foal information should be displayed?
   - Should we show full stats, or just summary?
   - Should we include a link to view full horse details?

2. **Retry Mechanism**
   - Should we use the existing replay endpoint (`/api/breeding/requests/{id}/replay`)?
   - Or should we allow the user to just resubmit with the same selections?
   - Should we auto-retry on certain failures?

3. **Polling Configuration**
   - Is 10 attempts (10 seconds) the right timeout?
   - Should polling interval be configurable?
   - Should we implement exponential backoff?

4. **Owner Selection**
   - Should we pre-select a default owner?
   - Should we remember the last selected owner?
   - Should we filter owners based on any criteria?

5. **Success Notification**
   - Should we show a success toast/notification?
   - Should we auto-navigate to the foal's detail page?
   - Should we update the dam/sire lists after successful breeding?

### Future Enhancements

1. **Real-time Updates**
   - Consider SignalR/WebSockets for push notifications instead of polling
   - Would eliminate polling overhead and improve responsiveness

2. **Breeding History**
   - Display recent breeding attempts on the page
   - Allow users to view past breeding results

3. **Advanced Filtering**
   - Filter dams/sires by stats, lineage, etc.
   - Show compatibility indicators

4. **Batch Breeding**
   - Allow queuing multiple breeding requests
   - Track multiple requests simultaneously

## Dependencies

- Existing `BreedingController` endpoints (already implemented)
- Existing `BreedingService` methods (already implemented)
- `HorseApiClient.GetByIdAsync()` (already implemented)
- FluentUI Blazor components (already in use)
- Existing user autocomplete pattern (already in use)

## Risk Assessment

### Low Risk
- API endpoints already exist and are tested
- Polling pattern is proven in RaceRun.razor
- Required DTOs and enums already defined

### Medium Risk
- State management complexity with multiple async operations
- Proper cleanup of polling resources
- Error handling coverage for all edge cases

### Mitigation Strategies
- Follow RaceRun.razor pattern closely (proven implementation)
- Implement comprehensive try-catch blocks
- Use CancellationToken properly throughout
- Add thorough testing for state transitions
- Code review focusing on resource cleanup

## Notes

- This feature mirrors the successful RaceRun.razor implementation
- The backend infrastructure (services, controllers, message processing) is already complete
- Focus is entirely on the frontend UI/UX layer
- Consider future enhancement to SignalR for real-time updates to eliminate polling
