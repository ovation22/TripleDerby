# Implementation Plan: Message Request Management & Monitoring

**Feature:** [029-message-request-management.md](../features/029-message-request-management.md)

## Overview

This implementation plan breaks down the Message Request Management feature into six testable vertical slices using Test-Driven Development (TDD). Each phase delivers working functionality and builds incrementally toward the complete feature.

## Implementation Strategy

### Vertical Slice Approach

Each phase delivers end-to-end functionality:
1. **Phase 1:** Foundation - Complete replay API endpoints (Feeding, Training batch)
2. **Phase 2:** Schema consistency - Move FeedingRequest to `fed` schema
3. **Phase 3:** Aggregation layer - Unified MessageService for cross-service queries
4. **Phase 4:** Dashboard widget - Real-time health monitoring on Home page
5. **Phase 5:** Management page - Full admin UI for viewing/replaying requests
6. **Phase 6:** Polish - Error handling, accessibility, performance

### TDD Pattern

Each phase follows:
- **RED** - Write failing tests first
- **GREEN** - Implement minimum code to pass tests
- **REFACTOR** - Clean up, extract patterns, remove duplication

### Task Sizing

- Individual tasks: 30-90 minutes
- Phases: 2-4 hours
- Total estimated time: 12-16 hours

## Dependencies

### Existing Code Patterns

- **Replay pattern:** BreedingService.ReplayBreedingRequest (lines [TripleDerby.Core/Services/BreedingService.cs:104-179](c:\Development\TripleDerby\TripleDerby.Core\Services\BreedingService.cs#L104-L179))
- **Batch replay pattern:** BreedingService.ReplayAllNonComplete (lines [TripleDerby.Core/Services/BreedingService.cs:182-253](c:\Development\TripleDerby\TripleDerby.Core\Services\BreedingService.cs#L182-L253))
- **Controller endpoints:** BreedingController, RaceRunsController with HATEOAS links
- **Test pattern:** RaceRequestProcessorTests (Moq, xUnit, Arrange-Act-Assert)
- **Widget pattern:** HorseGenderStats with skeleton loading, error states

### Required Packages

All dependencies already installed:
- Ardalis.Specification (repository patterns)
- Microsoft.FluentUI.AspNetCore.Components (UI components)
- Moq, xUnit (testing)

---

## Phase 1: Complete Replay Endpoints for Feeding & Training

**Goal:** Add missing replay functionality to achieve parity across all four services

**Vertical Slice:** Feeding and Training services can replay individual failed requests and batch-replay all non-complete requests via API endpoints

**Duration:** 2-3 hours

### RED - Write Failing Tests

#### Test 1.1: FeedingService.ReplayFeedingRequest
- [ ] **File:** `TripleDerby.Tests.Unit/Services/FeedingServiceTests.cs`
- [ ] Test: `ReplayFeedingRequest_ValidPendingRequest_RepublishesMessage`
  - Arrange: Mock FeedingRequest (Pending), mock publisher
  - Act: Call ReplayFeedingRequest
  - Assert: Publisher.PublishAsync called once, returns true
- [ ] Test: `ReplayFeedingRequest_FailedRequest_ResetsStatusAndRepublishes`
  - Arrange: Mock FeedingRequest (Failed), mock repository, mock publisher
  - Act: Call ReplayFeedingRequest
  - Assert: Status set to Pending, FailureReason nulled, message published
- [ ] Test: `ReplayFeedingRequest_CompletedRequest_ReturnsFalse`
  - Arrange: Mock FeedingRequest (Completed)
  - Act: Call ReplayFeedingRequest
  - Assert: Returns false, publisher NOT called
- [ ] Test: `ReplayFeedingRequest_NotFound_ReturnsFalse`
  - Arrange: Repository returns null
  - Act: Call ReplayFeedingRequest
  - Assert: Returns false

#### Test 1.2: FeedingService.ReplayAllNonComplete
- [ ] **File:** `TripleDerby.Tests.Unit/Services/FeedingServiceTests.cs`
- [ ] Test: `ReplayAllNonComplete_MultipleRequests_PublishesInParallel`
  - Arrange: Mock repository returns 5 non-complete requests
  - Act: Call ReplayAllNonComplete(maxDegreeOfParallelism: 2)
  - Assert: Returns 5, publisher called 5 times
- [ ] Test: `ReplayAllNonComplete_NoRequests_ReturnsZero`
  - Arrange: Repository returns empty list
  - Act: Call ReplayAllNonComplete
  - Assert: Returns 0

#### Test 1.3: TrainingService.ReplayAllNonComplete
- [ ] **File:** `TripleDerby.Tests.Unit/Services/TrainingServiceTests.cs`
- [ ] Test: `ReplayAllNonComplete_MultipleRequests_PublishesInParallel`
  - Same pattern as Feeding
- [ ] Test: `ReplayAllNonComplete_NoRequests_ReturnsZero`

#### Test 1.4: FeedingsController Replay Endpoints
- [ ] **File:** `TripleDerby.Tests.Unit/Controllers/FeedingsControllerTests.cs` (create if needed)
- [ ] Test: `ReplayRequest_ValidId_Returns202Accepted`
  - Arrange: Mock service returns true
  - Act: POST /api/feedings/requests/{id}/replay
  - Assert: 202 Accepted
- [ ] Test: `ReplayRequest_NotFound_Returns404`
  - Arrange: Mock service returns false
  - Act: POST /api/feedings/requests/{id}/replay
  - Assert: 404 NotFound
- [ ] Test: `ReplayAll_ValidRequest_Returns202WithCount`
  - Arrange: Mock service returns 5
  - Act: POST /api/feedings/requests/replay-all
  - Assert: 202 Accepted with body {published: 5}

#### Test 1.5: TrainingsController Batch Endpoint
- [ ] **File:** `TripleDerby.Tests.Unit/Controllers/TrainingsControllerTests.cs` (create if needed)
- [ ] Test: `ReplayAll_ValidRequest_Returns202WithCount`

### GREEN - Make Tests Pass

#### Implementation 1.1: FeedingService Methods
- [ ] **File:** `TripleDerby.Core/Abstractions/Services/IFeedingService.cs`
  - Add method signature: `Task<bool> ReplayFeedingRequest(Guid id, CancellationToken cancellationToken = default)`
  - Add method signature: `Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default)`

- [ ] **File:** `TripleDerby.Core/Services/FeedingService.cs`
  - Implement `ReplayFeedingRequest` (copy pattern from [BreedingService.cs:104-179](c:\Development\TripleDerby\TripleDerby.Core\Services\BreedingService.cs#L104-L179))
    - Validate ID
    - Fetch FeedingRequest entity
    - Return false if null or Completed
    - If Failed, reset status to Pending, clear FailureReason
    - Publish FeedingRequested message
    - Handle exceptions (restore Failed status on error)
    - Return true on success

  - Implement `ReplayAllNonComplete` (copy pattern from [BreedingService.cs:182-253](c:\Development\TripleDerby\TripleDerby.Core\Services\BreedingService.cs#L182-L253))
    - Fetch all non-complete requests (Status != Completed)
    - Use SemaphoreSlim for parallelism control
    - For each request: reset to Pending if Failed, publish message
    - Return count of successfully published messages

#### Implementation 1.2: TrainingService Batch Method
- [ ] **File:** `TripleDerby.Core/Abstractions/Services/ITrainingService.cs`
  - Add method signature: `Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default)`

- [ ] **File:** `TripleDerby.Core/Services/TrainingService.cs`
  - Implement `ReplayAllNonComplete` (same pattern as FeedingService)

#### Implementation 1.3: FeedingsController Endpoints
- [ ] **File:** `TripleDerby.Api/Controllers/FeedingsController.cs`
  - Add endpoint: `[HttpPost("requests/{id:guid}/replay")]`
    ```csharp
    public async Task<IActionResult> ReplayRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        var published = await feedingService.ReplayFeedingRequest(id, cancellationToken);
        if (!published)
            return NotFound();
        return Accepted();
    }
    ```

  - Add endpoint: `[HttpPost("requests/replay-all")]`
    ```csharp
    public async Task<ActionResult> ReplayAll(
        [FromQuery] int maxDegreeOfParallelism = 10,
        CancellationToken cancellationToken = default)
    {
        var published = await feedingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken);
        return Accepted(new { published });
    }
    ```

#### Implementation 1.4: TrainingsController Batch Endpoint
- [ ] **File:** `TripleDerby.Api/Controllers/TrainingsController.cs`
  - Add endpoint: `[HttpPost("requests/replay-all")]` (same pattern as Feedings)

### REFACTOR - Clean Up

- [ ] Review logging consistency across all replay methods
- [ ] Extract common replay logic if duplication exists (consider base class/helper)
- [ ] Ensure all replay methods use ITimeManager for timestamp updates
- [ ] Verify cancellation token propagation in all async calls

### Acceptance Criteria - Phase 1

- [ ] All unit tests pass (12+ new tests)
- [ ] FeedingService has ReplayFeedingRequest and ReplayAllNonComplete methods
- [ ] TrainingService has ReplayAllNonComplete method
- [ ] FeedingsController has both replay endpoints
- [ ] TrainingsController has batch replay endpoint
- [ ] Manual smoke test: POST to `/api/feedings/requests/{id}/replay` returns 202
- [ ] Manual smoke test: POST to `/api/trainings/requests/replay-all` returns 202 with count

**Deliverable:** All four services have complete replay functionality with consistent API patterns

**STOP HERE** - Wait for user review and approval before proceeding to Phase 2

---

## Phase 2: Move FeedingRequest to `fed` Schema

**Goal:** Achieve schema consistency across all request entities

**Vertical Slice:** FeedingRequest stored in `fed` schema with proper indexes, matching the pattern of Breeding/Racing/Training

**Duration:** 30-60 minutes

### RED - Write Failing Tests

#### Test 2.1: Schema Configuration
- [ ] **File:** `TripleDerby.Tests.Unit/Infrastructure/Data/FeedingRequestConfigurationTests.cs` (create)
- [ ] Test: `Configure_SetsCorrectSchemaAndTable`
  - Arrange: Create in-memory DbContext with FeedingRequest
  - Act: Query model metadata
  - Assert: Schema is "fed", Table is "FeedingRequests"
- [ ] Test: `Configure_CreatesStatusIndex`
  - Assert: Index on Status column exists
- [ ] Test: `Configure_CreatesCreatedDateIndex`
  - Assert: Index on CreatedDate column exists
- [ ] Test: `Configure_CreatesHorseIdIndex`
  - Assert: Index on HorseId column exists

### GREEN - Make Tests Pass

#### Implementation 2.1: Update EF Configuration
- [ ] **File:** `TripleDerby.Infrastructure/Data/Configurations/FeedingRequestConfiguration.cs`
  - Replace content with:
    ```csharp
    public void Configure(EntityTypeBuilder<FeedingRequest> builder)
    {
        builder.ToTable("FeedingRequests", schema: "fed");

        builder.Property(fr => fr.Status)
            .HasConversion<byte>()
            .HasDefaultValue(FeedingRequestStatus.Pending)
            .IsRequired();

        builder.Property(fr => fr.FailureReason)
            .HasMaxLength(1024);

        // Indexes for common queries
        builder.HasIndex(fr => fr.Status);
        builder.HasIndex(fr => fr.CreatedDate);
        builder.HasIndex(fr => fr.HorseId);
    }
    ```

#### Implementation 2.2: Generate Migration
- [ ] **Command:** Run EF migration generation
  ```bash
  cd TripleDerby.Infrastructure
  dotnet ef migrations add MoveFeedingRequestToFedSchema --startup-project ../TripleDerby.Api
  ```

- [ ] **Review:** Check generated migration SQL
  - Verify schema creation: `CREATE SCHEMA IF NOT EXISTS fed`
  - Verify table move: `ALTER TABLE dbo.FeedingRequests SET SCHEMA fed` (or equivalent)
  - Verify indexes: `CREATE INDEX IX_FeedingRequests_Status...`

#### Implementation 2.3: Apply Migration
- [ ] **Command:** Drop and recreate database (per user instruction)
  ```bash
  cd TripleDerby.Api
  dotnet ef database drop --force
  dotnet ef database update
  ```

- [ ] **Verify:** Query database to confirm schema
  ```sql
  SELECT table_schema, table_name
  FROM information_schema.tables
  WHERE table_name = 'FeedingRequests';
  -- Should return schema: fed
  ```

### REFACTOR - Clean Up

- [ ] Remove any old migration files if they conflict
- [ ] Update any hardcoded schema references in queries (unlikely, using EF)
- [ ] Verify all FeedingRequest queries still work

### Acceptance Criteria - Phase 2

- [ ] EF configuration test passes
- [ ] Migration generated successfully
- [ ] Database recreated with FeedingRequests in `fed` schema
- [ ] All existing FeedingService methods still work
- [ ] Indexes exist on Status, CreatedDate, HorseId columns
- [ ] Query performance baseline established (optional: use EXPLAIN ANALYZE)

**Deliverable:** FeedingRequest entity in `fed` schema with proper indexing

**STOP HERE** - Wait for user review and approval before proceeding to Phase 3

---

## Phase 3: MessageService & Controller (Aggregation Layer)

**Goal:** Create unified API for querying and replaying requests across all services

**Vertical Slice:** Single `/api/messages` endpoint returns aggregated summary of all request types, supports filtering and replay delegation

**Duration:** 3-4 hours

### RED - Write Failing Tests

#### Test 3.1: MessageService Summary Logic
- [ ] **File:** `TripleDerby.Tests.Unit/Services/MessageServiceTests.cs` (create)
- [ ] Test: `GetSummaryAsync_ReturnsCorrectCounts`
  - Arrange: Mock repositories return various counts per service and status
    - BreedingRequest repo: 5 Pending, 2 Failed, 10 Completed
    - FeedingRequest repo: 3 Pending, 1 Failed, 8 Completed
    - RaceRequest repo: 7 Pending, 0 Failed, 15 Completed
    - TrainingRequest repo: 2 Pending, 3 Failed, 6 Completed
  - Act: Call GetSummaryAsync
  - Assert:
    - Breeding: {Pending:5, Failed:2, Completed:10}
    - Feeding: {Pending:3, Failed:1, Completed:8}
    - Racing: {Pending:7, Failed:0, Completed:15}
    - Training: {Pending:2, Failed:3, Completed:6}
    - TotalPending: 17
    - TotalFailed: 6

#### Test 3.2: MessageService GetAll with Filters
- [ ] **File:** `TripleDerby.Tests.Unit/Services/MessageServiceTests.cs`
- [ ] Test: `GetAllRequestsAsync_NoFilters_ReturnsAllServices`
  - Arrange: Mock repositories return 2 requests each (total 8)
  - Act: Call GetAllRequestsAsync with pagination (page:1, size:50)
  - Assert: Returns 8 MessageRequestSummary records
- [ ] Test: `GetAllRequestsAsync_StatusFilter_ReturnsFiltered`
  - Arrange: Mock repositories return mix of statuses
  - Act: Call GetAllRequestsAsync with statusFilter: Failed
  - Assert: Only Failed requests returned
- [ ] Test: `GetAllRequestsAsync_ServiceTypeFilter_ReturnsFiltered`
  - Arrange: Mock repositories
  - Act: Call GetAllRequestsAsync with serviceTypeFilter: Breeding
  - Assert: Only Breeding requests returned

#### Test 3.3: MessagesController Endpoints
- [ ] **File:** `TripleDerby.Tests.Unit/Controllers/MessagesControllerTests.cs` (create)
- [ ] Test: `GetSummary_ReturnsOk200WithSummary`
  - Arrange: Mock service returns summary
  - Act: GET /api/messages/summary
  - Assert: 200 OK, body contains summary
- [ ] Test: `GetAll_ReturnsOk200WithPagedList`
  - Arrange: Mock service returns paged list
  - Act: GET /api/messages?page=1&size=50
  - Assert: 200 OK, PagedList structure
- [ ] Test: `ReplayRequest_ValidBreedingRequest_Returns202`
  - Arrange: Mock BreedingService.ReplayBreedingRequest returns true
  - Act: POST /api/messages/Breeding/{id}/replay
  - Assert: 202 Accepted
- [ ] Test: `ReplayRequest_InvalidServiceType_Returns400`
  - Act: POST /api/messages/99/{id}/replay
  - Assert: 400 BadRequest
- [ ] Test: `ReplayAll_ValidServiceType_Returns202WithCount`
  - Arrange: Mock service returns 10
  - Act: POST /api/messages/Feeding/replay-all
  - Assert: 202 Accepted, body: {published: 10}

### GREEN - Make Tests Pass

#### Implementation 3.1: Create DTOs
- [ ] **File:** `TripleDerby.SharedKernel/MessageRequestSummary.cs` (create)
  ```csharp
  public record MessageRequestSummary
  {
      public Guid Id { get; init; }
      public RequestServiceType ServiceType { get; init; }
      public RequestStatus Status { get; init; }
      public DateTimeOffset CreatedDate { get; init; }
      public DateTimeOffset? ProcessedDate { get; init; }
      public string? FailureReason { get; init; }
      public string TargetDescription { get; init; } = string.Empty;
      public string? TargetLink { get; init; }
      public Guid OwnerId { get; init; }
      public Guid? ResultId { get; init; }
      public string? ResultLink { get; init; }
  }
  ```

- [ ] **File:** `TripleDerby.SharedKernel/MessageRequestsSummaryResult.cs` (create)
  ```csharp
  public record MessageRequestsSummaryResult
  {
      public ServiceSummary Breeding { get; init; } = new();
      public ServiceSummary Feeding { get; init; } = new();
      public ServiceSummary Racing { get; init; } = new();
      public ServiceSummary Training { get; init; } = new();
      public int TotalPending { get; init; }
      public int TotalFailed { get; init; }
  }

  public record ServiceSummary
  {
      public int Pending { get; init; }
      public int Failed { get; init; }
      public int Completed { get; init; }
      public int InProgress { get; init; }
  }
  ```

- [ ] **File:** `TripleDerby.SharedKernel/Enums/RequestServiceType.cs` (create)
  ```csharp
  public enum RequestServiceType : byte
  {
      Breeding = 1,
      Feeding = 2,
      Racing = 3,
      Training = 4
  }
  ```

- [ ] **File:** `TripleDerby.SharedKernel/Enums/RequestStatus.cs` (create)
  ```csharp
  public enum RequestStatus : byte
  {
      Pending = 0,
      InProgress = 1,
      Completed = 2,
      Failed = 3,
      Cancelled = 4
  }
  ```

#### Implementation 3.2: Create IMessageService Interface
- [ ] **File:** `TripleDerby.Core/Abstractions/Services/IMessageService.cs` (create)
  ```csharp
  public interface IMessageService
  {
      Task<MessageRequestsSummaryResult> GetSummaryAsync(
          CancellationToken cancellationToken = default);

      Task<PagedList<MessageRequestSummary>> GetAllRequestsAsync(
          PaginationRequest pagination,
          RequestStatus? statusFilter = null,
          RequestServiceType? serviceTypeFilter = null,
          CancellationToken cancellationToken = default);
  }
  ```

#### Implementation 3.3: Implement MessageService
- [ ] **File:** `TripleDerby.Core/Services/MessageService.cs` (create)
  - Constructor: Inject ITripleDerbyRepository, ILogger
  - Method: `GetSummaryAsync`
    - Query each request table with CountAsync grouped by status
    - Use specifications or LINQ: `repository.CountAsync<BreedingRequest>(br => br.Status == BreedingRequestStatus.Pending)`
    - Map counts to ServiceSummary for each service
    - Calculate TotalPending, TotalFailed
    - Return MessageRequestsSummaryResult

  - Method: `GetAllRequestsAsync`
    - Query all four request tables
    - Apply statusFilter if provided
    - Apply serviceTypeFilter if provided (skip irrelevant tables)
    - Union results from all tables
    - Map entities to MessageRequestSummary DTOs
      - Breeding: TargetDescription = "Sire: {SireName}, Dam: {DamName}"
      - Feeding: TargetDescription = "Horse: {HorseName}, Feeding: {FeedingName}"
      - Racing: TargetDescription = "Horse: {HorseName}, Race: {RaceName}"
      - Training: TargetDescription = "Horse: {HorseName}, Training: {TrainingName}"
    - Order by CreatedDate descending
    - Apply pagination (Skip/Take)
    - Return PagedList<MessageRequestSummary>

#### Implementation 3.4: Register MessageService in DI
- [ ] **File:** `TripleDerby.Core/DependencyInjection.cs` or `TripleDerby.Api/Program.cs`
  - Add: `services.AddScoped<IMessageService, MessageService>();`

#### Implementation 3.5: Create MessagesController
- [ ] **File:** `TripleDerby.Api/Controllers/MessagesController.cs` (create)
  ```csharp
  [ApiController]
  [Route("api/messages")]
  public class MessagesController(
      IMessageService messageService,
      IBreedingService breedingService,
      IFeedingService feedingService,
      IRaceService raceService,
      ITrainingService trainingService) : ControllerBase
  {
      [HttpGet("summary")]
      [ProducesResponseType(StatusCodes.Status200OK)]
      public async Task<ActionResult<MessageRequestsSummaryResult>> GetSummary(
          CancellationToken cancellationToken)
      {
          var summary = await messageService.GetSummaryAsync(cancellationToken);
          return Ok(summary);
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      public async Task<ActionResult<PagedList<MessageRequestSummary>>> GetAll(
          [FromQuery] PaginationRequest pagination,
          [FromQuery] RequestStatus? status = null,
          [FromQuery] RequestServiceType? serviceType = null,
          CancellationToken cancellationToken = default)
      {
          var requests = await messageService.GetAllRequestsAsync(
              pagination, status, serviceType, cancellationToken);
          return Ok(requests);
      }

      [HttpPost("{serviceType}/{id}/replay")]
      [ProducesResponseType(StatusCodes.Status202Accepted)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async Task<IActionResult> ReplayRequest(
          RequestServiceType serviceType,
          Guid id,
          CancellationToken cancellationToken)
      {
          bool success = serviceType switch
          {
              RequestServiceType.Breeding => await breedingService.ReplayBreedingRequest(id, cancellationToken),
              RequestServiceType.Feeding => await feedingService.ReplayFeedingRequest(id, cancellationToken),
              RequestServiceType.Racing => await raceService.ReplayRaceRequest(id, cancellationToken),
              RequestServiceType.Training => await trainingService.ReplayTrainingRequest(id, cancellationToken),
              _ => throw new ArgumentException("Invalid service type", nameof(serviceType))
          };

          return success ? Accepted() : NotFound();
      }

      [HttpPost("{serviceType}/replay-all")]
      [ProducesResponseType(StatusCodes.Status202Accepted)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async Task<ActionResult> ReplayAll(
          RequestServiceType serviceType,
          [FromQuery] int maxDegreeOfParallelism = 10,
          CancellationToken cancellationToken = default)
      {
          int published = serviceType switch
          {
              RequestServiceType.Breeding => await breedingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
              RequestServiceType.Feeding => await feedingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
              RequestServiceType.Racing => await raceService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
              RequestServiceType.Training => await trainingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
              _ => throw new ArgumentException("Invalid service type", nameof(serviceType))
          };

          return Accepted(new { published });
      }
  }
  ```

### REFACTOR - Clean Up

- [ ] Consider caching GetSummaryAsync results (30s TTL) for dashboard widget performance
- [ ] Extract TargetDescription mapping to helper methods (reduce MessageService complexity)
- [ ] Add comprehensive logging to MessageService queries
- [ ] Verify no N+1 queries (use EF Include for related entities like Horse names)

### Acceptance Criteria - Phase 3

- [ ] All unit tests pass (8+ new tests)
- [ ] GET /api/messages/summary returns correct counts
- [ ] GET /api/messages returns unified list of requests
- [ ] GET /api/messages?status=Failed filters correctly
- [ ] GET /api/messages?serviceType=Breeding filters correctly
- [ ] POST /api/messages/Breeding/{id}/replay delegates to BreedingService
- [ ] POST /api/messages/Feeding/replay-all delegates to FeedingService
- [ ] Manual test: Postman/curl can query all endpoints successfully
- [ ] Performance: GetSummaryAsync completes in <500ms with 1000+ requests

**Deliverable:** Unified MessageService and MessagesController with aggregated query and replay capabilities

**STOP HERE** - Wait for user review and approval before proceeding to Phase 4

---

## Phase 4: Dashboard Widget

**Goal:** Add message queue health widget to Home page with auto-refresh

**Vertical Slice:** Home page displays real-time pending/failed counts per service, clicks navigate to management page

**Duration:** 2-3 hours

### RED - Write Failing Tests

#### Test 4.1: MessagesApiClient
- [ ] **File:** `TripleDerby.Tests.Unit/ApiClients/MessagesApiClientTests.cs` (create)
- [ ] Test: `GetSummaryAsync_ValidResponse_ReturnsSummary`
  - Arrange: Mock HttpClient returns 200 with JSON summary
  - Act: Call GetSummaryAsync
  - Assert: Returns MessageRequestsSummaryResult
- [ ] Test: `GetSummaryAsync_ApiError_ThrowsException`
  - Arrange: Mock HttpClient returns 500
  - Act: Call GetSummaryAsync
  - Assert: Throws HttpRequestException

#### Test 4.2: MessageRequestWidget Component (Manual/Integration)
- [ ] Manual Test: Widget renders with skeleton loading state
- [ ] Manual Test: Widget displays counts after API call
- [ ] Manual Test: Widget shows error state on API failure
- [ ] Manual Test: Click navigates to /messages

### GREEN - Make Tests Pass

#### Implementation 4.1: Create IMessagesApiClient Interface
- [ ] **File:** `TripleDerby.Web/ApiClients/Abstractions/IMessagesApiClient.cs` (create)
  ```csharp
  public interface IMessagesApiClient
  {
      Task<MessageRequestsSummaryResult> GetSummaryAsync(
          CancellationToken cancellationToken = default);
  }
  ```

#### Implementation 4.2: Implement MessagesApiClient
- [ ] **File:** `TripleDerby.Web/ApiClients/MessagesApiClient.cs` (create)
  - Follow pattern from HorsesApiClient
  - Constructor: Inject HttpClient (named client configured in Program.cs)
  - Method: `GetSummaryAsync`
    ```csharp
    public async Task<MessageRequestsSummaryResult> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/messages/summary", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageRequestsSummaryResult>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize summary");
    }
    ```

#### Implementation 4.3: Register MessagesApiClient in DI
- [ ] **File:** `TripleDerby.Web/Program.cs`
  - Add: `builder.Services.AddScoped<IMessagesApiClient, MessagesApiClient>();`

#### Implementation 4.4: Create MessageRequestWidget Component
- [ ] **File:** `TripleDerby.Web/Components/Widgets/MessageRequestWidget.razor` (create)
  ```razor
  @using TripleDerby.Web.ApiClients.Abstractions
  @using TripleDerby.SharedKernel
  @inject IMessagesApiClient MessagesClient
  @inject NavigationManager Navigation

  <div role="region" aria-label="@Title">
      <div class="hc-header">
          <h3 class="hc-title">@Title</h3>
      </div>

      @if (!string.IsNullOrEmpty(errorMessage))
      {
          <ErrorWidget ErrorMessage="@errorMessage" OnRetry="HandleRetryAsync" />
      }
      else if (loading)
      {
          <div class="hc-skeleton" aria-live="polite" aria-label="Loading message queue status">
              <!-- Skeleton UI for 4 services -->
              @for (int i = 0; i < 4; i++)
              {
                  <div class="service-row">
                      <FluentSkeleton Width="80px" Height="16px" Shimmer="true" />
                      <FluentSkeleton Width="50px" Height="20px" Shimmer="true" />
                  </div>
              }
          </div>
      }
      else
      {
          <div class="message-widget-content" @onclick="NavigateToManagement">
              <div class="service-row">
                  <span class="service-label">🏠 Breeding</span>
                  <div class="badges">
                      <FluentBadge Appearance="Appearance.Accent">@summary.Breeding.Pending P</FluentBadge>
                      <FluentBadge Appearance="Appearance.Lightweight" BackgroundColor="var(--error)">@summary.Breeding.Failed F</FluentBadge>
                  </div>
              </div>
              <div class="service-row">
                  <span class="service-label">🥕 Feeding</span>
                  <div class="badges">
                      <FluentBadge Appearance="Appearance.Accent">@summary.Feeding.Pending P</FluentBadge>
                      <FluentBadge Appearance="Appearance.Lightweight" BackgroundColor="var(--error)">@summary.Feeding.Failed F</FluentBadge>
                  </div>
              </div>
              <div class="service-row">
                  <span class="service-label">🏁 Racing</span>
                  <div class="badges">
                      <FluentBadge Appearance="Appearance.Accent">@summary.Racing.Pending P</FluentBadge>
                      <FluentBadge Appearance="Appearance.Lightweight" BackgroundColor="var(--error)">@summary.Racing.Failed F</FluentBadge>
                  </div>
              </div>
              <div class="service-row">
                  <span class="service-label">💪 Training</span>
                  <div class="badges">
                      <FluentBadge Appearance="Appearance.Accent">@summary.Training.Pending P</FluentBadge>
                      <FluentBadge Appearance="Appearance.Lightweight" BackgroundColor="var(--error)">@summary.Training.Failed F</FluentBadge>
                  </div>
              </div>
          </div>
      }
  </div>

  @code {
      [Parameter] public string Title { get; set; } = "Message Queue";

      private MessageRequestsSummaryResult summary = new();
      private bool loading = true;
      private string? errorMessage;
      private PeriodicTimer? timer;

      protected override async Task OnInitializedAsync()
      {
          await LoadDataAsync();
          StartAutoRefresh();
      }

      private async Task LoadDataAsync()
      {
          loading = true;
          errorMessage = null;

          try
          {
              summary = await MessagesClient.GetSummaryAsync();
          }
          catch (Exception ex)
          {
              errorMessage = "Failed to load message queue status";
          }
          finally
          {
              loading = false;
          }
      }

      private async Task HandleRetryAsync()
      {
          await LoadDataAsync();
      }

      private void StartAutoRefresh()
      {
          timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
          _ = Task.Run(async () =>
          {
              while (await timer.WaitForNextTickAsync())
              {
                  await InvokeAsync(async () =>
                  {
                      await LoadDataAsync();
                      StateHasChanged();
                  });
              }
          });
      }

      private void NavigateToManagement()
      {
          Navigation.NavigateTo("/messages");
      }

      public void Dispose()
      {
          timer?.Dispose();
      }
  }
  ```

- [ ] **File:** `TripleDerby.Web/Components/Widgets/MessageRequestWidget.razor.css` (create)
  ```css
  .hc-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
  }

  .hc-title {
      font-size: 16px;
      font-weight: 600;
      margin: 0;
  }

  .message-widget-content {
      display: flex;
      flex-direction: column;
      gap: 8px;
      cursor: pointer;
      padding: 8px;
      border-radius: 4px;
      transition: background-color 0.2s;
  }

  .message-widget-content:hover {
      background-color: var(--neutral-layer-2);
  }

  .service-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
  }

  .service-label {
      font-size: 14px;
      font-weight: 500;
  }

  .badges {
      display: flex;
      gap: 6px;
  }

  .hc-skeleton {
      display: flex;
      flex-direction: column;
      gap: 8px;
  }
  ```

#### Implementation 4.5: Add Widget to Home Page
- [ ] **File:** `TripleDerby.Web/Components/Pages/Home.razor`
  - **Decision point:** Replace HorseGenderStats or HorseLegTypeStats? (Ask user)
  - Add import: `@using TripleDerby.Web.Components.Widgets`
  - Add widget (example replacing HorseGenderStats):
    ```razor
    <FluentGridItem xs="12">
        <FluentCard Height="156px;">
            <ErrorBoundary>
                <ChildContent>
                    <MessageRequestWidget Title="Message Queue" />
                </ChildContent>
                <ErrorContent>
                    <ErrorWidget ErrorMessage="An unexpected error occurred in the Message Queue widget." />
                </ErrorContent>
            </ErrorBoundary>
        </FluentCard>
    </FluentGridItem>
    ```

### REFACTOR - Clean Up

- [ ] Extract service row rendering to child component if duplication is high
- [ ] Consider adding total counts to widget header (TotalPending, TotalFailed)
- [ ] Add aria-labels for accessibility
- [ ] Test auto-refresh behavior (timer disposal on navigate away)

### Acceptance Criteria - Phase 4

- [ ] Widget displays on Home page
- [ ] Widget shows correct counts for all 4 services
- [ ] Widget shows skeleton loading state on initial load
- [ ] Widget shows error state if API fails, with retry button
- [ ] Widget auto-refreshes every 30 seconds
- [ ] Clicking widget navigates to /messages
- [ ] Widget is responsive (mobile-friendly)
- [ ] Widget meets accessibility standards (screen reader friendly)
- [ ] Manual test: Navigate to Home, widget loads within 500ms

**Deliverable:** Functional dashboard widget on Home page with auto-refresh and click-through to management page

**STOP HERE** - Wait for user review and approval before proceeding to Phase 5

---

## Phase 5: Management Page

**Goal:** Full admin UI for viewing, filtering, and replaying message requests

**Vertical Slice:** Dedicated /messages page with summary cards, filterable grid, and replay actions

**Duration:** 3-4 hours

### RED - Write Failing Tests

#### Test 5.1: MessagesApiClient Extensions
- [ ] **File:** `TripleDerby.Tests.Unit/ApiClients/MessagesApiClientTests.cs`
- [ ] Test: `GetAllRequestsAsync_ReturnsPagedList`
- [ ] Test: `GetAllRequestsAsync_WithFilters_AppliesQueryString`
- [ ] Test: `ReplayRequestAsync_Returns202`
- [ ] Test: `ReplayAllAsync_Returns202WithCount`

#### Test 5.2: Messages Page (Manual/Integration)
- [ ] Manual Test: Page renders with summary cards
- [ ] Manual Test: Grid displays requests
- [ ] Manual Test: Status filter works
- [ ] Manual Test: Replay button calls API and refreshes grid
- [ ] Manual Test: Bulk replay shows confirmation dialog

### GREEN - Make Tests Pass

#### Implementation 5.1: Extend IMessagesApiClient
- [ ] **File:** `TripleDerby.Web/ApiClients/Abstractions/IMessagesApiClient.cs`
  - Add methods:
    ```csharp
    Task<PagedList<MessageRequestSummary>> GetAllRequestsAsync(
        PaginationRequest pagination,
        RequestStatus? status = null,
        RequestServiceType? serviceType = null,
        CancellationToken cancellationToken = default);

    Task ReplayRequestAsync(
        RequestServiceType serviceType,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<int> ReplayAllAsync(
        RequestServiceType serviceType,
        int maxDegreeOfParallelism = 10,
        CancellationToken cancellationToken = default);
    ```

#### Implementation 5.2: Implement API Client Methods
- [ ] **File:** `TripleDerby.Web/ApiClients/MessagesApiClient.cs`
  - Implement `GetAllRequestsAsync` (build query string for filters)
  - Implement `ReplayRequestAsync` (POST to /api/messages/{serviceType}/{id}/replay)
  - Implement `ReplayAllAsync` (POST to /api/messages/{serviceType}/replay-all)

#### Implementation 5.3: Create Messages Page
- [ ] **File:** `TripleDerby.Web/Components/Pages/Messages.razor` (create)
  ```razor
  @page "/messages"
  @using TripleDerby.Web.ApiClients.Abstractions
  @using TripleDerby.SharedKernel
  @using TripleDerby.SharedKernel.Pagination
  @inject IMessagesApiClient MessagesClient
  @inject IToastService ToastService

  <PageTitle>Message Queue Management</PageTitle>

  <h1>Message Queue Management</h1>

  <!-- Summary Cards -->
  <FluentGrid>
      <FluentGridItem xs="12" sm="6" md="3">
          <FluentCard>
              <h4>🏠 Breeding</h4>
              <div>Pending: @summary.Breeding.Pending</div>
              <div>Failed: @summary.Breeding.Failed</div>
              <FluentButton OnClick="() => ReplayAll(RequestServiceType.Breeding)">
                  Replay All
              </FluentButton>
          </FluentCard>
      </FluentGridItem>
      <FluentGridItem xs="12" sm="6" md="3">
          <FluentCard>
              <h4>🥕 Feeding</h4>
              <div>Pending: @summary.Feeding.Pending</div>
              <div>Failed: @summary.Feeding.Failed</div>
              <FluentButton OnClick="() => ReplayAll(RequestServiceType.Feeding)">
                  Replay All
              </FluentButton>
          </FluentCard>
      </FluentGridItem>
      <FluentGridItem xs="12" sm="6" md="3">
          <FluentCard>
              <h4>🏁 Racing</h4>
              <div>Pending: @summary.Racing.Pending</div>
              <div>Failed: @summary.Racing.Failed</div>
              <FluentButton OnClick="() => ReplayAll(RequestServiceType.Racing)">
                  Replay All
              </FluentButton>
          </FluentCard>
      </FluentGridItem>
      <FluentGridItem xs="12" sm="6" md="3">
          <FluentCard>
              <h4>💪 Training</h4>
              <div>Pending: @summary.Training.Pending</div>
              <div>Failed: @summary.Training.Failed</div>
              <FluentButton OnClick="() => ReplayAll(RequestServiceType.Training)">
                  Replay All
              </FluentButton>
          </FluentCard>
      </FluentGridItem>
  </FluentGrid>

  <!-- Filters -->
  <FluentToolbar>
      <FluentSelect @bind-Value="statusFilter" Label="Status">
          <FluentOption Value="@((RequestStatus?)null)">All</FluentOption>
          <FluentOption Value="@RequestStatus.Pending">Pending</FluentOption>
          <FluentOption Value="@RequestStatus.Failed">Failed</FluentOption>
          <FluentOption Value="@RequestStatus.Completed">Completed</FluentOption>
      </FluentSelect>
      <FluentButton OnClick="LoadDataAsync">Apply Filter</FluentButton>
  </FluentToolbar>

  <!-- Data Grid -->
  <FluentDataGrid Items="@requests" Pagination="@pagination">
      <PropertyColumn Property="@(r => r.ServiceType)" Title="Service" />
      <PropertyColumn Property="@(r => r.Status)" Title="Status" />
      <PropertyColumn Property="@(r => r.CreatedDate)" Title="Created" Format="g" />
      <PropertyColumn Property="@(r => r.TargetDescription)" Title="Target" />
      <PropertyColumn Property="@(r => r.FailureReason)" Title="Failure Reason" />
      <TemplateColumn Title="Actions">
          <FluentButton Disabled="@(context.Status == RequestStatus.Completed)"
                        OnClick="() => ReplayRequest(context.ServiceType, context.Id)">
              Replay
          </FluentButton>
      </TemplateColumn>
  </FluentDataGrid>

  <FluentPaginator State="@pagination" />

  @code {
      private MessageRequestsSummaryResult summary = new();
      private IQueryable<MessageRequestSummary>? requests;
      private PaginatorState pagination = new() { ItemsPerPage = 50 };
      private RequestStatus? statusFilter;

      protected override async Task OnInitializedAsync()
      {
          await LoadSummaryAsync();
          await LoadDataAsync();
      }

      private async Task LoadSummaryAsync()
      {
          summary = await MessagesClient.GetSummaryAsync();
      }

      private async Task LoadDataAsync()
      {
          var paginationRequest = new PaginationRequest
          {
              Page = pagination.CurrentPageIndex + 1,
              Size = pagination.ItemsPerPage
          };

          var pagedList = await MessagesClient.GetAllRequestsAsync(
              paginationRequest, statusFilter);

          requests = pagedList.Items.AsQueryable();
          pagination.TotalItemCount = pagedList.TotalCount;
      }

      private async Task ReplayRequest(RequestServiceType serviceType, Guid id)
      {
          try
          {
              await MessagesClient.ReplayRequestAsync(serviceType, id);
              ToastService.ShowSuccess("Request replayed successfully");
              await LoadDataAsync();
          }
          catch (Exception ex)
          {
              ToastService.ShowError($"Failed to replay request: {ex.Message}");
          }
      }

      private async Task ReplayAll(RequestServiceType serviceType)
      {
          var confirmed = await DialogService.ShowConfirmationAsync(
              $"Are you sure you want to replay all failed {serviceType} requests?");

          if (!confirmed) return;

          try
          {
              var count = await MessagesClient.ReplayAllAsync(serviceType);
              ToastService.ShowSuccess($"Replayed {count} requests");
              await LoadSummaryAsync();
              await LoadDataAsync();
          }
          catch (Exception ex)
          {
              ToastService.ShowError($"Failed to replay requests: {ex.Message}");
          }
      }
  }
  ```

#### Implementation 5.4: Add Navigation Link (Optional)
- [ ] **File:** `TripleDerby.Web/Components/Layout/NavMenu.razor`
  - Add link to Messages page (if desired for navigation menu)
  - Or rely on widget click-through only

### REFACTOR - Clean Up

- [ ] Extract summary cards to child component (ServiceSummaryCard)
- [ ] Add URL query param persistence for filters (use NavigationManager)
- [ ] Format dates with relative time ("2 hours ago")
- [ ] Add tooltips for truncated failure reasons
- [ ] Consider virtualization for large grids (FluentDataGrid supports this)

### Acceptance Criteria - Phase 5

- [ ] /messages page accessible
- [ ] Summary cards display correct counts per service
- [ ] Data grid displays requests from all services
- [ ] Status filter works (Pending, Failed, Completed, All)
- [ ] Pagination works (50 items per page)
- [ ] Individual Replay button calls API and refreshes grid
- [ ] Bulk Replay button shows confirmation, calls API, refreshes data
- [ ] Toast notifications show on success/failure
- [ ] Page is responsive (mobile-friendly)
- [ ] Manual test: Replay a failed request, verify it moves to Pending
- [ ] Manual test: Filter by Failed, see only failed requests

**Deliverable:** Full management page with filtering, pagination, and replay functionality

**STOP HERE** - Wait for user review and approval before proceeding to Phase 6

---

## Phase 6: Polish & Performance

**Goal:** Error handling, accessibility, performance optimization, and documentation

**Vertical Slice:** Production-ready feature with comprehensive error handling and optimized performance

**Duration:** 2-3 hours

### Task List

#### Task 6.1: Error Handling
- [ ] **Widget Error Resilience**
  - Add retry with exponential backoff in MessagesApiClient
  - Test widget behavior when API is down (show error, allow retry)
  - Handle network timeouts gracefully

- [ ] **Management Page Error States**
  - Empty state when no requests exist
  - Error state when API fails
  - Validation error handling (e.g., already replayed)
  - Loading states for all async operations

- [ ] **API Error Responses**
  - Return 400 BadRequest with clear message for invalid inputs
  - Return 409 Conflict if trying to replay Completed request
  - Ensure consistent error format across all endpoints

#### Task 6.2: Accessibility Audit
- [ ] **Widget Accessibility**
  - Add aria-labels to all interactive elements
  - Ensure badges have accessible text (screen reader friendly)
  - Test keyboard navigation
  - Verify color contrast for status badges (WCAG AA compliance)

- [ ] **Management Page Accessibility**
  - FluentDataGrid keyboard navigation works
  - Filter controls accessible via keyboard
  - Replay buttons have clear aria-labels
  - Toast notifications announced to screen readers

#### Task 6.3: Performance Optimization
- [ ] **Backend Performance**
  - Add database indexes on Status, CreatedDate columns (already done in Phase 2)
  - Verify no N+1 queries in MessageService (use EF logging)
  - Add caching for GetSummaryAsync (30s TTL)
  - Test with 1000+ requests, verify <500ms response time

- [ ] **Frontend Performance**
  - Lazy load Messages page (defer loading until navigated to)
  - Debounce filter changes (avoid excessive API calls)
  - Optimize grid rendering (consider virtualization if needed)

#### Task 6.4: Integration Testing
- [ ] **API Integration Tests**
  - Test MessagesController endpoints with in-memory database
  - Verify filtering and pagination work correctly
  - Test replay delegation to service-specific methods

- [ ] **UI Integration Tests** (optional, if time permits)
  - Test widget auto-refresh behavior
  - Test management page filter interactions
  - Test replay button success/failure flows

#### Task 6.5: Documentation
- [ ] **Update README**
  - Document new `/api/messages/*` endpoints
  - Add examples for querying and replaying requests
  - Update architecture diagrams if needed

- [ ] **Code Comments**
  - Add XML doc comments to MessagesController methods
  - Document MessageService aggregation logic
  - Add comments explaining complex LINQ queries

- [ ] **Feature Documentation**
  - Add screenshots to feature spec (widget, management page)
  - Document admin workflow (how to use the feature)
  - Add troubleshooting section (common issues)

#### Task 6.6: Manual Testing Checklist
- [ ] Test widget on Home page (loads, refreshes, navigates)
- [ ] Test management page (summary cards, grid, filters)
- [ ] Test individual replay (Breeding, Feeding, Racing, Training)
- [ ] Test batch replay with confirmation
- [ ] Test error states (API down, invalid request)
- [ ] Test with large dataset (1000+ requests)
- [ ] Test on mobile (responsive layout)
- [ ] Test accessibility (screen reader, keyboard navigation)

### Acceptance Criteria - Phase 6

- [ ] All error states tested and handled gracefully
- [ ] Widget and management page meet WCAG 2.1 AA standards
- [ ] GetSummaryAsync completes in <500ms with caching
- [ ] Management page pagination handles 1000+ requests smoothly
- [ ] No N+1 queries in EF (verified via logging)
- [ ] All integration tests pass
- [ ] README updated with new endpoints
- [ ] Feature spec updated with screenshots
- [ ] Manual testing checklist completed
- [ ] Code review feedback addressed

**Deliverable:** Production-ready Message Request Management feature

**STOP HERE** - Feature complete, ready for final user review and commit

---

## Post-Implementation Checklist

### Before Committing Each Phase

1. **Run all tests:** `dotnet test`
2. **Verify build:** `dotnet build`
3. **Manual smoke test:** Test key functionality for the phase
4. **Review changes:** `git diff` to ensure no unintended changes
5. **Ask user:** "Would you like me to commit these changes?"
6. **Wait for approval** before running git commands

### Commit Message Pattern

Follow existing patterns (professional, technical, as if developer wrote it):

```
Phase 1: Add replay endpoints for Feeding and Training services

- Implement FeedingService.ReplayFeedingRequest and ReplayAllNonComplete
- Implement TrainingService.ReplayAllNonComplete
- Add FeedingsController replay endpoints (individual and batch)
- Add TrainingsController batch replay endpoint
- Add unit tests for replay methods (12 tests)

All four services now have complete replay functionality with consistent API patterns.
```

### Final Commit (All Phases Complete)

```
Feature 029: Message Request Management & Monitoring

Implement admin dashboard widget and management page for viewing, monitoring,
and replaying async message requests across all microservices.

Key changes:
- Complete replay endpoints for Feeding/Training services
- Move FeedingRequest to 'fed' schema for consistency
- Add MessageService for cross-service aggregation
- Add MessagesController with unified query and replay APIs
- Add MessageRequestWidget to Home page with auto-refresh
- Add /messages management page with filtering and bulk actions
- Add comprehensive error handling and accessibility features

Closes #029
```

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| MessageService aggregation is slow | Add caching (30s TTL), optimize queries with indexes |
| Widget polling overloads API | Use 30s interval, consider debouncing, cache summary results |
| Replay endpoint inconsistencies | Follow BreedingService pattern exactly, write tests first |
| Schema migration breaks queries | Test thoroughly, use EF migrations, verify all CRUD still works |
| Large grids cause UI lag | Implement pagination (50 per page), consider virtualization |

## Performance Targets

- **GetSummaryAsync:** <500ms (with 1000+ requests)
- **GetAllRequestsAsync:** <1s (50 items per page)
- **Widget load:** <500ms
- **Management page load:** <2s
- **Individual replay:** <1s
- **Batch replay:** <5s for 100 requests (parallel processing)

## Testing Strategy

### Unit Tests
- All service methods (replay, aggregation)
- All controller endpoints
- API client methods
- Edge cases (null, empty, invalid inputs)

### Integration Tests
- MessagesController with in-memory database
- End-to-end replay flow (create request, fail, replay)
- Filtering and pagination

### Manual Tests
- Widget auto-refresh
- Management page interactions
- Error states (API down, network issues)
- Accessibility (screen reader, keyboard)
- Performance with large datasets

## Success Metrics

- [ ] All acceptance criteria met across 6 phases
- [ ] 40+ unit tests passing
- [ ] No performance regressions
- [ ] Feature spec acceptance criteria 100% complete
- [ ] User can view and replay message requests via widget and management page
- [ ] Zero critical bugs found in manual testing

---

## Notes

- **User confirmed:** No auth/auth yet, so no access restrictions in Phase 1
- **User confirmed:** Drop and recreate database for migrations (no production data)
- **Pattern consistency:** Follow BreedingService replay pattern exactly
- **Code reuse:** MessageService aggregates existing service methods, no duplication
- **TDD discipline:** Write tests first, implement minimum code to pass, then refactor

## Next Steps

1. Review this implementation plan with user
2. Get approval to start Phase 1
3. Populate TodoWrite with Phase 1 tasks
4. Begin RED-GREEN-REFACTOR cycle for Phase 1
