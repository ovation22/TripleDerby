# Implementation Plan: Breeding Kickoff UI

## Overview

This implementation plan breaks down the Breeding Kickoff UI feature into concrete, testable phases following TDD principles and vertical slice architecture. Each phase delivers working, end-to-end functionality that can be validated independently.

**Feature Specification**: [docs/features/016-breeding-kickoff-ui.md](../features/016-breeding-kickoff-ui.md)

**Branch**: `feature/016-breeding-kickoff-ui`

## Implementation Strategy

### Vertical Slice Approach

This feature will be implemented in vertical slices, where each phase delivers a complete user flow from UI through API to backend:

1. **Phase 1**: API client foundation - Submit breeding requests
2. **Phase 2**: API client polling - Get breeding status
3. **Phase 3**: UI submission flow - Select and submit breeding
4. **Phase 4**: UI polling flow - Track breeding status
5. **Phase 5**: UI success flow - Display foal results
6. **Phase 6**: UI error flow - Handle failures and retry

### TDD Approach

Each phase follows Red-Green-Refactor:
- **RED**: Define expected behavior through tests (manual testing for UI)
- **GREEN**: Implement minimum code to achieve behavior
- **REFACTOR**: Clean up, extract methods, improve structure

### Risk Mitigation

- **Early validation**: Phase 1-2 validate API integration before UI work
- **Proven patterns**: Mirror RaceRun.razor's successful polling implementation
- **Incremental delivery**: Each phase delivers testable functionality
- **Resource safety**: Early focus on proper cleanup (CancellationToken, Dispose)

---

## Phase 1: API Client - Submit Breeding Request

**Goal**: Implement API client method to submit breeding requests and get initial status

**Vertical Slice**: Submit breeding (sire + dam + owner) → Receive BreedingRequestStatusResult

**Estimated Complexity**: Simple

**Risks**: None - follows established pattern from RaceRunApiClient

### RED - Define Expected Behavior

Manual testing approach (Blazor client doesn't typically have unit tests):
- Expect `SubmitBreedingAsync()` to POST to `/api/breeding/requests`
- Expect `BreedRequest` body with `UserId`, `SireId`, `DamId`
- Expect return of `Resource<BreedingRequested>` unwrapped to status
- Expect proper error handling and logging

**Reference Pattern**: `RaceRunApiClient.SubmitRaceRunAsync()` (lines 15-26)

### GREEN - Implement API Client Method

#### Task 1.1: Add method signature to IBreedingApiClient
- File: `TripleDerby.Web/ApiClients/Abstractions/IBreedingApiClient.cs`
- Add:
  ```csharp
  Task<BreedingRequestStatusResult?> SubmitBreedingAsync(
      Guid sireId,
      Guid damId,
      Guid ownerId,
      CancellationToken cancellationToken = default);
  ```

#### Task 1.2: Implement SubmitBreedingAsync in BreedingApiClient
- File: `TripleDerby.Web/ApiClients/BreedingApiClient.cs`
- Create `BreedRequest` with provided IDs
- Need to add PostAsync with body support to BaseApiClient, OR use HttpClient directly
- POST to `/api/breeding/requests`
- Unwrap `Resource<BreedingRequested>` to get breeding request ID
- Create and return `BreedingRequestStatusResult` from response
- Add error logging following existing pattern

**Implementation Notes**:
- BaseApiClient only has `PostAsync<T>(url)` with null body
- Need to add `PostAsync<TRequest, TResponse>(url, body)` to BaseApiClient
- OR call `BreedingController.CreateRequest` which returns `Resource<BreedingRequested>`
- The BreedingRequested has RequestId which maps to BreedingRequest.Id
- We need to construct BreedingRequestStatusResult from BreedingRequested

**Alternative Approach**:
- Controller returns `Resource<BreedingRequested>` with RequestId
- Extract RequestId and make immediate call to `GetRequestStatusAsync` to get full status
- This provides consistency with polling flow

### REFACTOR - Clean Up

- Extract any duplicated error handling patterns
- Ensure consistent logging format with other API clients
- Consider adding PostAsync with body to BaseApiClient if needed elsewhere

### Acceptance Criteria

- [ ] `IBreedingApiClient.SubmitBreedingAsync()` method signature added
- [ ] `BreedingApiClient.SubmitBreedingAsync()` implemented
- [ ] Method POSTs correct `BreedRequest` body to `/api/breeding/requests`
- [ ] Method returns `BreedingRequestStatusResult` with request ID and initial status
- [ ] Errors are logged appropriately
- [ ] CancellationToken is respected
- [ ] Ready for manual testing with real API

### Deliverable

API client can submit breeding request and return initial status. Can be tested with breakpoints/logging.

---

## Phase 2: API Client - Get Breeding Status

**Goal**: Implement API client method to poll breeding request status

**Vertical Slice**: Poll by request ID → Receive current BreedingRequestStatusResult

**Estimated Complexity**: Simple

**Risks**: None - straightforward GET endpoint

### RED - Define Expected Behavior

Manual testing approach:
- Expect `GetRequestStatusAsync()` to GET from `/api/breeding/requests/{id}`
- Expect return of `Resource<BreedingRequestStatusResult>` unwrapped to status
- Expect proper error handling and logging

**Reference Pattern**: `RaceRunApiClient.GetRequestStatusAsync()` (lines 31-42)

### GREEN - Implement API Client Method

#### Task 2.1: Add method signature to IBreedingApiClient
- File: `TripleDerby.Web/ApiClients/Abstractions/IBreedingApiClient.cs`
- Add:
  ```csharp
  Task<BreedingRequestStatusResult?> GetRequestStatusAsync(
      Guid breedingRequestId,
      CancellationToken cancellationToken = default);
  ```

#### Task 2.2: Implement GetRequestStatusAsync in BreedingApiClient
- File: `TripleDerby.Web/ApiClients/BreedingApiClient.cs`
- GET from `/api/breeding/requests/{breedingRequestId}`
- Unwrap `Resource<BreedingRequestStatusResult>` to get status data
- Return `BreedingRequestStatusResult`
- Add error logging following existing pattern

**Implementation Notes**:
- Controller returns `Resource<BreedingRequestStatusResult>`
- Use `GetAsync<Resource<BreedingRequestStatusResult>>()` from BaseApiClient
- Extract `.Data.Data` to get the actual BreedingRequestStatusResult

### REFACTOR - Clean Up

- Ensure consistent error message format with SubmitBreedingAsync
- Consider extracting common Resource unwrapping logic if used frequently

### Acceptance Criteria

- [ ] `IBreedingApiClient.GetRequestStatusAsync()` method signature added
- [ ] `BreedingApiClient.GetRequestStatusAsync()` implemented
- [ ] Method GETs from correct endpoint
- [ ] Method returns `BreedingRequestStatusResult` with current status
- [ ] Errors are logged appropriately
- [ ] CancellationToken is respected
- [ ] Ready for manual testing with real API

### Deliverable

API client can poll breeding request status. Combined with Phase 1, full API integration is complete.

---

## Phase 3: UI - Breeding Submission Flow

**Goal**: Add state management and submission logic to Breeding.razor

**Vertical Slice**: User fills form → Clicks "Breed" → API call initiated → Loading state shown

**Estimated Complexity**: Medium

**Risks**:
- State management with existing selections
- Proper validation of owner field
- Error handling during submission

### RED - Define Expected Behavior

Manual testing checklist:
- User cannot submit without owner selected
- Clicking "Breed" disables button and shows loading state
- Submission calls `BreedingApi.SubmitBreedingAsync()` with correct IDs
- Errors display in error message bar
- Success triggers polling (to be implemented in Phase 4)

**Reference Pattern**: `RaceRun.razor` SubmitRaceRunAsync (lines 237-282)

### GREEN - Implement Submission Logic

#### Task 3.1: Add state variables to Breeding.razor
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add to @code section:
  ```csharp
  private bool _hasBreedingStarted;    // Tracks if breeding process has begun
  private bool _isSubmitting;          // True during API submission
  private bool _isPolling;             // True while polling for status
  private bool _breedingCompleted;     // True when process finishes
  private Guid? _currentRequestId;     // Current breeding request ID
  private string _currentStatus = "";  // Display text for status
  private HorseResult? _foal;          // Resulting foal
  private string? _errorMessage;       // Error messages
  private bool _hideError = true;      // Controls error visibility
  private CancellationTokenSource? _pollCts;  // For polling cancellation
  ```

#### Task 3.2: Update BreedAsync method with submission logic
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Replace placeholder implementation with:
  ```csharp
  private async Task BreedAsync()
  {
      // Validation
      if (_selectedDamId == null || _selectedSireId == null || _selectedOwner == null)
      {
          _errorMessage = "Please select a dam, sire, and owner.";
          _hideError = false;
          return;
      }

      // Clear previous state
      _foal = null;
      _errorMessage = null;
      _hideError = true;
      _breedingCompleted = false;

      _isSubmitting = true;
      _hasBreedingStarted = true;

      try
      {
          // Submit breeding request
          var result = await BreedingApi.SubmitBreedingAsync(
              _selectedSireId.Value,
              _selectedDamId.Value,
              _selectedOwner.Id);

          if (result == null)
          {
              _errorMessage = "Failed to submit breeding request.";
              _hideError = false;
              _hasBreedingStarted = false;
              return;
          }

          _currentRequestId = result.Id;
          _currentStatus = result.Status.ToString();

          // Start polling (implemented in Phase 4)
          // await StartPollingAsync();
      }
      catch (Exception ex)
      {
          _errorMessage = $"Error submitting breeding request: {ex.Message}";
          _hideError = false;
          _hasBreedingStarted = false;
      }
      finally
      {
          _isSubmitting = false;
          await InvokeAsync(StateHasChanged);
      }
  }
  ```

#### Task 3.3: Update Breed button with validation
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Update button disabled condition:
  ```razor
  <FluentButton Appearance="Appearance.Accent"
                Disabled="@(_selectedDamId == null || _selectedSireId == null || _selectedOwner == null || _isSubmitting || _hasBreedingStarted)"
                @onclick="BreedAsync">
      @(_isSubmitting ? "Submitting..." : "Breed selected pair")
  </FluentButton>
  ```

#### Task 3.4: Add error display section
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add after breed button:
  ```razor
  @if (!string.IsNullOrEmpty(_errorMessage))
  {
      <FluentMessageBar Style="margin-top: 2rem;"
                        Title="Error"
                        Intent="MessageIntent.Error"
                        @bind-Hidden="_hideError">
          @_errorMessage
      </FluentMessageBar>
  }
  ```

### REFACTOR - Clean Up

- Extract validation logic to separate method if needed
- Ensure consistent state transitions
- Consider adding validation message for specific missing fields

### Acceptance Criteria

- [ ] State variables added to Breeding.razor
- [ ] BreedAsync() validates owner is selected
- [ ] BreedAsync() clears previous state before submission
- [ ] BreedAsync() calls BreedingApi.SubmitBreedingAsync() with correct parameters
- [ ] Breed button is disabled when owner not selected
- [ ] Breed button shows "Submitting..." during submission
- [ ] Breed button is disabled after breeding starts
- [ ] Error messages display in FluentMessageBar
- [ ] Submission errors don't leave component in broken state

### Deliverable

User can submit breeding request. Validation prevents invalid submissions. Errors are displayed clearly. Ready to add polling in Phase 4.

---

## Phase 4: UI - Status Polling Flow

**Goal**: Implement polling mechanism to track breeding progress

**Vertical Slice**: After submission → Poll every 1s → Update status → Stop on completion/failure

**Estimated Complexity**: Medium

**Risks**:
- Proper cleanup of CancellationTokenSource
- Correct state transitions during polling
- Timeout handling

### RED - Define Expected Behavior

Manual testing checklist:
- After successful submission, polling starts automatically
- Progress ring shows while polling
- Status text updates each second (Pending → InProgress → Completed/Failed)
- Polling stops when status is Completed or Failed
- Polling stops after 10 attempts (timeout)
- Component disposal cancels polling

**Reference Pattern**: `RaceRun.razor` StartPollingAsync (lines 284-361)

### GREEN - Implement Polling Logic

#### Task 4.1: Implement StartPollingAsync method
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add method following RaceRun pattern:
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

                  // Fetch foal details (implemented in Phase 5)
                  // var foal = await HorseApi.GetByIdAsync(status.FoalId.Value);
                  // if (foal != null) _foal = foal;
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
          // Polling cancelled, ignore
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

#### Task 4.2: Call StartPollingAsync from BreedAsync
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Uncomment the polling call in BreedAsync:
  ```csharp
  // After storing _currentRequestId
  await StartPollingAsync();
  ```

#### Task 4.3: Implement IDisposable for cleanup
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add to top of file:
  ```razor
  @implements IDisposable
  ```
- Add Dispose method to @code:
  ```csharp
  public void Dispose()
  {
      _pollCts?.Cancel();
      _pollCts?.Dispose();
  }
  ```

#### Task 4.4: Add polling status UI
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add after error section:
  ```razor
  @if (_isPolling)
  {
      <div style="margin-top: 2rem;">
          <FluentProgressRing />
          <span style="margin-left: 1rem;">Status: @_currentStatus</span>
      </div>
  }
  ```

### REFACTOR - Clean Up

- Ensure all try-catch blocks are comprehensive
- Verify state transitions are correct
- Consider extracting magic numbers (1000ms, 10 attempts) to constants

### Acceptance Criteria

- [ ] StartPollingAsync() polls every 1000ms
- [ ] Status text updates on each poll
- [ ] Polling stops when status is Completed
- [ ] Polling stops when status is Failed
- [ ] Polling stops after 10 attempts (timeout)
- [ ] Timeout shows appropriate error message
- [ ] Failed status shows error with failure reason
- [ ] Progress ring displays while polling
- [ ] IDisposable implemented and cancels polling on disposal
- [ ] CancellationTokenSource is properly disposed
- [ ] TaskCanceledException is handled gracefully

### Deliverable

Breeding requests are tracked in real-time. User sees status updates. Polling handles all terminal states (Complete, Failed, Timeout). Resources are properly cleaned up.

---

## Phase 5: UI - Success Flow with Foal Display

**Goal**: Fetch and display foal information when breeding completes successfully

**Vertical Slice**: Breeding completes → Fetch foal → Display foal details → Allow "Breed Another"

**Estimated Complexity**: Simple

**Risks**: None - straightforward data display

### RED - Define Expected Behavior

Manual testing checklist:
- When breeding completes, foal details are fetched
- Foal name, color, and key stats are displayed
- Sire and Dam names are shown
- "Breed Another Pair" button resets the form
- Can perform multiple breeding cycles

**Reference Pattern**: `RaceRun.razor` result display and "Race Another Horse" (lines 102-110, 425-441)

### GREEN - Implement Success Flow

#### Task 5.1: Add foal fetching to StartPollingAsync
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Uncomment foal fetching in StartPollingAsync:
  ```csharp
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
  ```

#### Task 5.2: Add foal display UI section
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add after polling section:
  ```razor
  @if (_foal != null && _breedingCompleted)
  {
      <div style="margin-top: 2rem; padding: 1rem; border: 1px solid var(--neutral-stroke-rest); border-radius: 4px;">
          <h3>Breeding Successful!</h3>
          <div style="margin-top: 1rem;">
              <div><strong>Foal Name:</strong> @_foal.Name</div>
              <div><strong>Color:</strong> @_foal.Color</div>
              <div style="margin-top: 0.5rem;"><strong>Parents:</strong></div>
              <div style="margin-left: 1rem;">
                  <div>Sire: @(_foal.Sire ?? "Unknown")</div>
                  <div>Dam: @(_foal.Dam ?? "Unknown")</div>
              </div>
              <div style="margin-top: 0.5rem;"><strong>Stats:</strong></div>
              <div style="margin-left: 1rem;">
                  <div>Starts: @_foal.RaceStarts</div>
                  <div>Wins: @_foal.RaceWins</div>
                  <div>Place: @_foal.RacePlace</div>
                  <div>Show: @_foal.RaceShow</div>
                  <div>Earnings: $@_foal.Earnings</div>
              </div>
          </div>

          <div style="margin-top: 1rem;">
              <FluentButton Appearance="Appearance.Accent"
                            @onclick="BreedAnotherPair">
                  Breed Another Pair
              </FluentButton>
          </div>
      </div>
  }
  ```

#### Task 5.3: Implement BreedAnotherPair method
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add to @code section:
  ```csharp
  private void BreedAnotherPair()
  {
      // Clear all state
      _foal = null;
      _selectedOwner = null;
      _hasBreedingStarted = false;
      _breedingCompleted = false;
      _isPolling = false;
      _isSubmitting = false;
      _currentRequestId = null;
      _currentStatus = "";
      _errorMessage = null;
      _hideError = true;

      // Reset to first dam/sire (keep selections visible)
      // User can change selections for next breeding

      StateHasChanged();
  }
  ```

### REFACTOR - Clean Up

- Consider improving foal display styling to match app theme
- Could extract foal display to a reusable component for future use
- Verify all state is properly reset in BreedAnotherPair

### Acceptance Criteria

- [ ] Foal details are fetched when breeding completes
- [ ] Foal name, color, and stats are displayed
- [ ] Parent names (Sire and Dam) are shown
- [ ] Success display only shows when foal is available
- [ ] "Breed Another Pair" button is visible
- [ ] Clicking "Breed Another Pair" resets all state
- [ ] Can perform multiple breeding cycles without refresh
- [ ] Selections remain available for next breeding

### Deliverable

Successful breeding displays foal information. User can easily start another breeding. Complete happy-path flow works end-to-end.

---

## Phase 6: UI - Error Handling and Polish

**Goal**: Add retry mechanism and refine error handling

**Vertical Slice**: Breeding fails → Error displayed → User can retry → Or start new breeding

**Estimated Complexity**: Medium

**Risks**:
- Deciding on retry mechanism (replay vs. resubmit)
- Ensuring all error states are covered

### RED - Define Expected Behavior

Manual testing checklist:
- Failed breeding shows clear error message with failure reason
- User can retry failed breeding
- Timeout errors provide helpful guidance
- Network errors are handled gracefully
- Missing selection errors are clear
- All states have appropriate visual feedback

**Reference Pattern**: `RaceRun.razor` error handling (lines 76-84, 230-282)

### GREEN - Implement Error Handling

#### Task 6.1: Add retry button to error section
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Update error section:
  ```razor
  @if (!string.IsNullOrEmpty(_errorMessage))
  {
      <FluentMessageBar Style="margin-top: 2rem;"
                        Title="Error"
                        Intent="MessageIntent.Error"
                        @bind-Hidden="_hideError">
          @_errorMessage

          @if (_currentRequestId.HasValue && _breedingCompleted)
          {
              <div style="margin-top: 0.5rem;">
                  <FluentButton Appearance="Appearance.Stealth"
                                @onclick="RetryBreedingAsync">
                      Try Again
                  </FluentButton>
                  <FluentButton Appearance="Appearance.Stealth"
                                @onclick="BreedAnotherPair">
                      Start New Breeding
                  </FluentButton>
              </div>
          }
      </FluentMessageBar>
  }
  ```

#### Task 6.2: Implement RetryBreedingAsync method
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add to @code section:
  ```csharp
  private async Task RetryBreedingAsync()
  {
      if (_currentRequestId == null)
          return;

      // Clear error state
      _errorMessage = null;
      _hideError = true;
      _breedingCompleted = false;

      // Restart polling for the same request
      await StartPollingAsync();
  }
  ```

**Implementation Notes**:
- We're using a simple retry that just resumes polling
- This assumes the backend will reprocess the failed request
- Alternative: Call the `/api/breeding/requests/{id}/replay` endpoint
- Current approach is simpler and sufficient for UI-level retries

#### Task 6.3: Hide selection section during breeding
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Wrap the selection FluentGrid with conditional:
  ```razor
  @if (!_hasBreedingStarted)
  {
      <FluentGrid>
          <!-- Existing dam/sire/owner selectors -->
      </FluentGrid>
  }
  ```

#### Task 6.4: Add validation feedback for owner
- File: `TripleDerby.Web/Components/Pages/Breeding.razor`
- Add helper text below owner selector:
  ```razor
  <div style="margin-top:1rem;">
      <label><strong>Owner</strong> <span style="color: var(--error);">*</span></label>

      <FluentAutocomplete TOption="UserResult"
                          Width="320px"
                          Placeholder="Search users by username"
                          OptionText="@(u => $"{u.Username} ({u.Email})")"
                          Multiple="false"
                          OnOptionsSearch="OnOptionsSearchAsync"
                          @bind-SelectedOption="_selectedOwner" />

      @if (_selectedOwner == null)
      {
          <div style="color: var(--neutral-foreground-hint); font-size: 0.875rem; margin-top: 0.25rem;">
              Owner selection is required
          </div>
      }
  </div>
  ```

### REFACTOR - Clean Up

- Review all error messages for clarity and consistency
- Ensure error state doesn't block user from recovering
- Consider extracting error display to component if reused
- Add comments explaining retry strategy

### Acceptance Criteria

- [ ] Failed breeding shows error with failure reason
- [ ] Retry button appears for failed breeding
- [ ] "Start New Breeding" button appears for failed breeding
- [ ] Retry resumes polling for the same request
- [ ] "Start New Breeding" fully resets state
- [ ] Selection section hides during breeding
- [ ] Owner field shows required indicator (asterisk)
- [ ] Owner field shows hint when empty
- [ ] All error states have recovery options
- [ ] User never gets stuck in an error state

### Deliverable

Complete error handling with retry capability. All user flows have clear recovery paths. UI provides helpful feedback in all states.

---

## Summary of Phases

| Phase | Goal | Complexity | Deliverable |
|-------|------|-----------|-------------|
| 1 | API Client Submit | Simple | Can submit breeding via API |
| 2 | API Client Status | Simple | Can poll breeding status via API |
| 3 | UI Submission | Medium | User can submit breeding from UI |
| 4 | UI Polling | Medium | UI tracks breeding progress in real-time |
| 5 | UI Success | Simple | Foal details displayed on success |
| 6 | UI Errors | Medium | Comprehensive error handling with retry |

## Testing Strategy

### Manual Testing Checklist

After each phase, test:
- [ ] Happy path works as expected
- [ ] Error cases are handled gracefully
- [ ] State transitions are smooth
- [ ] No console errors
- [ ] Resources are cleaned up

### End-to-End Testing

After all phases complete:
- [ ] Submit breeding with valid selections → Success flow
- [ ] Submit breeding with invalid selections → Validation prevents
- [ ] Submit breeding that fails backend validation → Error displayed
- [ ] Submit breeding that times out → Timeout message shown
- [ ] Submit breeding, close browser → No hanging resources
- [ ] Complete breeding, "Breed Another" → Can submit new breeding
- [ ] Failed breeding, "Retry" → Resumes polling
- [ ] Failed breeding, "Start New" → Fresh form

### Performance Testing

- [ ] Polling doesn't cause memory leaks (check in browser dev tools)
- [ ] CancellationTokenSource is disposed properly
- [ ] No excessive re-renders during polling
- [ ] Network requests are reasonable (1 per second during polling)

## Dependencies

### Required for Implementation
- [x] BreedingController.CreateRequest endpoint exists
- [x] BreedingController.GetRequest endpoint exists
- [x] BreedingService.QueueBreedingAsync implemented
- [x] BreedingService.GetRequestStatus implemented
- [x] HorseApiClient.GetByIdAsync implemented
- [x] UserApiClient for owner autocomplete exists
- [x] FluentUI Blazor components available

### No Additional Dependencies Required
- Backend infrastructure is complete
- All DTOs and enums exist
- Message processing is implemented
- No new NuGet packages needed

## Risk Assessment

### Low Risk (Phases 1-2)
- API client implementation follows proven pattern
- Endpoints are tested and working
- Simple GET/POST operations

### Medium Risk (Phases 3-6)
- State management complexity
- CancellationToken handling
- Error recovery flows

### Mitigation
- Follow RaceRun.razor pattern exactly
- Test each phase thoroughly before proceeding
- Focus on cleanup and disposal early
- Add extensive error handling

## Post-Implementation

### Verification
1. Manual test all user flows
2. Check browser console for errors
3. Verify no memory leaks during polling
4. Test on different browsers
5. Validate accessibility

### Documentation
- Update user guide with breeding feature instructions
- Document API client changes in code comments
- Add inline comments explaining polling logic

### Future Enhancements
- Consider SignalR for real-time updates (eliminates polling)
- Add breeding history view
- Show breeding costs/fees
- Add foal naming during breeding
- Batch breeding capabilities

## Notes

- This implementation mirrors RaceRun.razor's proven patterns
- Backend is fully complete - focus is UI only
- Each phase delivers working, testable functionality
- Phases build incrementally - no big-bang integration
- Resource cleanup is a primary concern throughout
