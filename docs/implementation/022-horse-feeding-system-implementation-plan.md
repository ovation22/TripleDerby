# Feature 022: Horse Feeding System - Implementation Plan

## Overview

**Feature**: [022-horse-feeding-system.md](../features/022-horse-feeding-system.md)
**Approach**: TDD with Vertical Slices
**Total Phases**: 10

## Summary

This implementation plan breaks down the Feeding System into vertical slices that mirror the Training System architecture. Each phase delivers testable functionality, building from data model through calculator, service layer, microservice with RabbitMQ messaging, and finally Blazor admin UI. The preference discovery system is the most complex component and is implemented incrementally.

---

## Phase 1: Data Model Foundation

**Goal**: Establish the enhanced Feeding entity with categories and effect ranges.

**Vertical Slice**: Feeding entity can store category and happiness/stat effect ranges.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Entities/FeedingTests.cs`

- [ ] Test: Feeding entity has CategoryId property
- [ ] Test: Feeding entity has HappinessMin/HappinessMax properties
- [ ] Test: Feeding entity has stat range properties (StaminaMin/Max, etc.)
- [ ] Test: FeedingCategoryId enum has all 6 categories

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.SharedKernel/Enums/FeedingCategoryId.cs` - Category enum

**Files to Modify**:
- `TripleDerby.Core/Entities/Feeding.cs` - Add category and effect range properties
- `TripleDerby.Infrastructure/Data/ModelBuilderExtensions.cs` - Update seed data with 18 feeds

**Tasks**:
- [ ] Create `FeedingCategoryId` enum (Treats=1, Fruits=2, Grains=3, Proteins=4, Supplements=5, Premium=6)
- [ ] Add `CategoryId`, `HappinessMin`, `HappinessMax` to Feeding entity
- [ ] Add stat range properties: `StaminaMin/Max`, `DurabilityMin/Max`, `SpeedMin/Max`, `AgilityMin/Max`
- [ ] Update seed data to include all 18 feeds with correct categories and effect ranges

### REFACTOR - Clean Up

- [ ] Ensure property naming consistency with Training entity

### Acceptance Criteria

- [ ] All entity tests pass
- [ ] Build succeeds with new properties
- [ ] 18 feeds seeded with categories and ranges

**Deliverable**: Enhanced Feeding entity ready for calculator implementation

---

## Phase 2: FeedingSession Enhancement & Horse Flag

**Goal**: Enhance FeedingSession to track detailed effects and add HasFedSinceLastRace flag.

**Vertical Slice**: FeedingSession records detailed gains; Horse tracks feeding status.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Entities/FeedingSessionTests.cs`

- [ ] Test: FeedingSession has SessionDate property
- [ ] Test: FeedingSession has stat gain properties (HappinessGain, StaminaGain, etc.)
- [ ] Test: FeedingSession has PreferenceDiscovered flag

**Test File**: `TripleDerby.Tests.Unit/Entities/HorseTests.cs`

- [ ] Test: Horse has HasFedSinceLastRace property (default false)

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Core/Entities/FeedingSession.cs` - Add detailed fields
- `TripleDerby.Core/Entities/Horse.cs` - Add HasFedSinceLastRace flag

**Tasks**:
- [ ] Add `SessionDate`, `RaceStartsAtTime` to FeedingSession
- [ ] Add stat gain properties: `HappinessGain`, `StaminaGain`, `DurabilityGain`, `SpeedGain`, `AgilityGain`
- [ ] Add `PreferenceDiscovered` boolean flag
- [ ] Add `Horse.HasFedSinceLastRace` boolean property

### REFACTOR - Clean Up

- [ ] Ensure FeedingSession mirrors TrainingSession structure where applicable

### Acceptance Criteria

- [ ] All entity tests pass
- [ ] Horse flag works correctly
- [ ] Build succeeds

**Deliverable**: Complete data model for feeding sessions

---

## Phase 3: HorseFeedingPreference Entity

**Goal**: Create entity to store discovered preferences per horse per feed.

**Vertical Slice**: Preference can be stored and retrieved for a horse/feed combination.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Entities/HorseFeedingPreferenceTests.cs`

- [ ] Test: HorseFeedingPreference has HorseId and FeedingId
- [ ] Test: HorseFeedingPreference has Preference (FeedResponse)
- [ ] Test: HorseFeedingPreference has DiscoveredDate

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Entities/HorseFeedingPreference.cs` - New entity

**Files to Modify**:
- `TripleDerby.Core/Entities/Horse.cs` - Add navigation property
- `TripleDerby.Infrastructure/Data/TripleDerbyContext.cs` - Add DbSet
- `TripleDerby.Infrastructure/Data/ModelBuilderExtensions.cs` - Configure entity

**Tasks**:
- [ ] Create `HorseFeedingPreference` entity with Id, HorseId, FeedingId, Preference, DiscoveredDate
- [ ] Add `ICollection<HorseFeedingPreference> FeedingPreferences` to Horse
- [ ] Configure entity in DbContext with appropriate indexes
- [ ] Add composite unique index on (HorseId, FeedingId)

### REFACTOR - Clean Up

- [ ] Ensure proper foreign key configuration

### Acceptance Criteria

- [ ] All entity tests pass
- [ ] Entity properly configured in EF
- [ ] Build succeeds

**Deliverable**: Preference storage ready for service layer

---

## Phase 4: FeedingConfig & FeedingCalculator Core

**Goal**: Implement configuration and core calculator for preference generation and effect calculation.

**Vertical Slice**: Calculator can generate deterministic preferences and calculate feeding effects.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Feeding/FeedingCalculatorTests.cs`

- [ ] Test: CalculatePreference returns deterministic result for same horse+feed
- [ ] Test: CalculatePreference returns different results for different feeds
- [ ] Test: CalculateHappinessGain applies preference multiplier correctly
- [ ] Test: CalculateHappinessGain with Favorite returns 1.5x effect
- [ ] Test: CalculateHappinessGain with Hated returns 0.5x effect
- [ ] Test: CalculateStatGain respects DominantPotential ceiling
- [ ] Test: CalculateStatGain returns 0 when at ceiling
- [ ] Test: CalculateHappinessEffectivenessModifier returns 1.0 at 100 happiness
- [ ] Test: CalculateHappinessEffectivenessModifier returns 0.5 at 0 happiness
- [ ] Test: RollUpsetStomach is deterministic for same inputs

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Configuration/FeedingConfig.cs` - Configuration constants
- `TripleDerby.Core/Abstractions/Services/IFeedingCalculator.cs` - Interface
- `TripleDerby.Core/Calculators/FeedingCalculator.cs` - Implementation

**Tasks**:
- [ ] Create `FeedingConfig` with preference multipliers, upset stomach constants, category weights
- [ ] Create `IFeedingCalculator` interface with methods from spec
- [ ] Implement `CalculatePreference()` with deterministic seeding (HashCode.Combine)
- [ ] Implement `CalculateHappinessGain()` with preference multiplier and random range
- [ ] Implement `CalculateStatGain()` with ceiling enforcement
- [ ] Implement `CalculateHappinessEffectivenessModifier()` (linear 0.5-1.0)
- [ ] Implement `RollUpsetStomach()` with deterministic seeding

### REFACTOR - Clean Up

- [ ] Extract preference weight lookup into helper method
- [ ] Ensure calculator is fully unit testable (inject IRandomGenerator if needed)

### Acceptance Criteria

- [ ] All calculator tests pass
- [ ] Deterministic seeding verified
- [ ] Preference multipliers applied correctly

**Deliverable**: Core calculation logic for feeding system

---

## Phase 5: FeedingCalculator Advanced (Career Phase & LegType)

**Goal**: Add career phase modifiers and LegType category bonuses to preference generation.

**Vertical Slice**: Preference probabilities vary by career phase and LegType.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Feeding/FeedingCalculatorTests.cs` (add to existing)

- [ ] Test: Young horse (0-9 races) has +5% favorite/liked chance
- [ ] Test: Old horse (50+ races) has +5% disliked/hated chance
- [ ] Test: Prime/Veteran horses have no modifier
- [ ] Test: LegType matching category gets +10% favorite chance
- [ ] Test: StartDash + Treats category = bonus
- [ ] Test: FrontRunner + Grains category = bonus
- [ ] Test: Non-matching LegType + category = no bonus
- [ ] Test: Favorite treat adds bonus stat (0.1-0.2)

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Core/Configuration/FeedingConfig.cs` - Add LegType mappings
- `TripleDerby.Core/Calculators/FeedingCalculator.cs` - Implement modifiers

**Tasks**:
- [ ] Add career phase modifier logic to `CalculatePreference()`
- [ ] Add `LegTypePreferredCategory` dictionary to FeedingConfig
- [ ] Add LegType bonus logic to `CalculatePreference()`
- [ ] Implement `CalculateFavoriteTreatBonus()` method

### REFACTOR - Clean Up

- [ ] Extract career phase logic into helper method

### Acceptance Criteria

- [ ] All modifier tests pass
- [ ] Career phase affects preference probabilities
- [ ] LegType affects category preferences

**Deliverable**: Full preference generation with all modifiers

---

## Phase 6: FeedingService Core

**Goal**: Implement core FeedingService with feeding execution and preference discovery.

**Vertical Slice**: Horse can be fed, effects applied, preference discovered and stored.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Feeding/FeedingServiceTests.cs`

- [ ] Test: Feed_WhenHorseCanFeed_AppliesHappinessGain
- [ ] Test: Feed_WhenHorseCanFeed_AppliesStatGains
- [ ] Test: Feed_WhenFirstTimeFeeding_DiscoversPreference
- [ ] Test: Feed_WhenAlreadyFedSinceLastRace_ThrowsInvalidOperation
- [ ] Test: Feed_SetsHasFedSinceLastRace_True
- [ ] Test: Feed_WithHatedFood_MayTriggerUpsetStomach
- [ ] Test: Feed_RespectsStatCeiling
- [ ] Test: CanFeed_WhenNotFedSinceLastRace_ReturnsTrue
- [ ] Test: CanFeed_WhenAlreadyFed_ReturnsFalse
- [ ] Test: GetDiscoveredPreferences_ReturnsOnlyDiscoveredPreferences

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Specifications/HorseFeedingPreferenceSpecification.cs` - Query spec

**Files to Modify**:
- `TripleDerby.Core/Abstractions/Services/IFeedingService.cs` - Add new methods
- `TripleDerby.Core/Services/FeedingService.cs` - Implement full feeding logic
- `TripleDerby.SharedKernel/FeedingSessionResult.cs` - Enhance with new fields

**Tasks**:
- [ ] Create specification for querying horse preferences
- [ ] Add `IFeedingCalculator` dependency to FeedingService
- [ ] Implement full `Feed()` method:
  - Validate CanFeed
  - Get/generate preference
  - Calculate effects using calculator
  - Apply to horse stats (with ceiling check)
  - Create FeedingSession record
  - Create HorseFeedingPreference if first time
  - Set HasFedSinceLastRace = true
- [ ] Implement `CanFeed()` method
- [ ] Implement `GetDiscoveredPreferencesAsync()`
- [ ] Enhance `FeedingSessionResult` with all effect fields

### REFACTOR - Clean Up

- [ ] Extract stat application logic into helper
- [ ] Ensure proper transaction handling

### Acceptance Criteria

- [ ] All service tests pass
- [ ] Feeding execution works end-to-end
- [ ] Preferences discovered and stored correctly

**Deliverable**: Working feeding execution with preference discovery

---

## Phase 7: Daily Options & Race Integration

**Goal**: Implement 3 random daily options and race flag reset.

**Vertical Slice**: Players get 3 cached daily options; racing resets feeding flag.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Feeding/FeedingServiceTests.cs` (add to existing)

- [ ] Test: GetAvailableFeedingOptions_Returns3Options
- [ ] Test: GetAvailableFeedingOptions_CachedForSameDay
- [ ] Test: GetAvailableFeedingOptions_DifferentOnDifferentDays
- [ ] Test: GetAvailableFeedingOptions_WhenLowHappinessAndFavoriteKnown_IncludesFavorite
- [ ] Test: GetFeedingHistory_ReturnsRecentSessions

**Test File**: `TripleDerby.Tests.Unit/Services/RaceExecutorTests.cs` (add test)

- [ ] Test: CompleteRace_ResetsHasFedSinceLastRace

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Specifications/FeedingSessionHistorySpecification.cs` - History query

**Files to Modify**:
- `TripleDerby.Core/Services/FeedingService.cs` - Add options and history methods
- `TripleDerby.Services.Racing/RaceExecutor.cs` - Reset feeding flag on race complete
- `TripleDerby.Core/Cache/CacheKeys.cs` - Add feeding options cache key

**Tasks**:
- [ ] Implement `GetAvailableFeedingOptionsAsync()`:
  - Generate cache key using date
  - Return cached options if available
  - Generate 3 random options from 18 feeds
  - If happiness < 50 and favorite known, guarantee favorite
  - Cache for remainder of day
- [ ] Implement `GetFeedingHistoryAsync()` with limit
- [ ] Update RaceExecutor to reset `HasFedSinceLastRace` on race completion
- [ ] Add `FeedingOptions:{date}` cache key pattern

### REFACTOR - Clean Up

- [ ] Ensure cache key uses UTC date for consistency

### Acceptance Criteria

- [ ] Daily options cached correctly
- [ ] Favorite guaranteed when happiness low
- [ ] Race completion resets feeding flag
- [ ] History retrieval works

**Deliverable**: Complete feeding system with daily options and race integration

---

## Phase 8: Microservice Foundation & Messages

**Goal**: Create TripleDerby.Services.Feeding microservice project with RabbitMQ message types.

**Vertical Slice**: FeedingRequested/FeedingCompleted messages defined; microservice project created.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Feeding/FeedingMessagesTests.cs`

- [ ] Test: FeedingRequested record has all required properties
- [ ] Test: FeedingCompleted record has all required properties
- [ ] Test: FeedingRequest entity has correct status tracking fields

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.SharedKernel/Messages/FeedingRequested.cs` - Request message
- `TripleDerby.SharedKernel/Messages/FeedingCompleted.cs` - Completion event
- `TripleDerby.SharedKernel/Enums/FeedingRequestStatus.cs` - Status enum
- `TripleDerby.Core/Entities/FeedingRequest.cs` - Request tracking entity
- `TripleDerby.Services.Feeding/TripleDerby.Services.Feeding.csproj` - New project
- `TripleDerby.Services.Feeding/Program.cs` - Worker host setup
- `TripleDerby.Services.Feeding/Worker.cs` - Background service

**Tasks**:
- [ ] Create `FeedingRequested` record (RequestId, HorseId, FeedingId, SessionId, OwnerId, RequestedDate)
- [ ] Create `FeedingCompleted` record (RequestId, HorseId, FeedingId, SessionId, OwnerId, FeedingSessionId, CompletedDate, PreferenceDiscovered, DiscoveredPreference)
- [ ] Create `FeedingRequestStatus` enum (Pending, InProgress, Completed, Failed)
- [ ] Create `FeedingRequest` entity for tracking async requests
- [ ] Create new microservice project with ServiceDefaults reference
- [ ] Configure project in solution and AppHost

### REFACTOR - Clean Up

- [ ] Ensure message patterns match TrainingRequested/TrainingCompleted

### Acceptance Criteria

- [ ] Messages compile and follow existing patterns
- [ ] Microservice project builds
- [ ] Solution compiles with new project

**Deliverable**: Microservice shell with message definitions

---

## Phase 9: FeedingExecutor & FeedingRequestProcessor

**Goal**: Implement async feeding execution via RabbitMQ consumer.

**Vertical Slice**: Feeding requests processed asynchronously through message queue.

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Feeding/FeedingExecutorTests.cs`

- [ ] Test: ExecuteFeedingAsync_WhenHorseCanFeed_AppliesEffects
- [ ] Test: ExecuteFeedingAsync_WhenFirstTimeFeeding_DiscoversPreference
- [ ] Test: ExecuteFeedingAsync_WhenAlreadyFed_ThrowsInvalidOperation
- [ ] Test: ExecuteFeedingAsync_CreatesSessionRecord

**Test File**: `TripleDerby.Tests.Unit/Feeding/FeedingRequestProcessorTests.cs`

- [ ] Test: ProcessAsync_WhenRequestPending_ExecutesFeeding
- [ ] Test: ProcessAsync_WhenRequestCompleted_SkipsProcessing
- [ ] Test: ProcessAsync_WhenRequestFailed_RetriesExecution
- [ ] Test: ProcessAsync_PublishesFeedingCompletedOnSuccess

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Services.Feeding/Abstractions/IFeedingExecutor.cs` - Executor interface
- `TripleDerby.Services.Feeding/Abstractions/IFeedingRequestProcessor.cs` - Processor interface
- `TripleDerby.Services.Feeding/FeedingExecutor.cs` - Core execution logic
- `TripleDerby.Services.Feeding/FeedingRequestProcessor.cs` - Message processor
- `TripleDerby.Services.Feeding/DTOs/FeedingSessionResult.cs` - Microservice result DTO

**Files to Modify**:
- `TripleDerby.Services.Feeding/Program.cs` - Register services and consumers

**Tasks**:
- [ ] Create `IFeedingExecutor` interface with `ExecuteFeedingAsync()`
- [ ] Implement `FeedingExecutor` (mirrors TrainingExecutor pattern):
  - Load horse with statistics
  - Validate CanFeed
  - Load feeding type
  - Calculate/generate preference
  - Calculate effects with calculator
  - Apply stat changes
  - Create FeedingSession and HorseFeedingPreference records
  - Set HasFedSinceLastRace = true
- [ ] Create `IFeedingRequestProcessor` interface
- [ ] Implement `FeedingRequestProcessor` (mirrors TrainingRequestProcessor):
  - Check request status (skip if completed/in-progress)
  - Mark as InProgress
  - Execute via FeedingExecutor
  - Mark as Completed
  - Publish FeedingCompleted message
- [ ] Register consumer in Program.cs

### REFACTOR - Clean Up

- [ ] Extract common patterns between FeedingExecutor and TrainingExecutor
- [ ] Ensure proper error handling and status tracking

### Acceptance Criteria

- [ ] FeedingExecutor tests pass
- [ ] FeedingRequestProcessor tests pass
- [ ] Message consumption works end-to-end
- [ ] Preference discovery works through async path

**Deliverable**: Working async feeding execution via RabbitMQ

---

## Phase 10: Blazor Admin UI

**Goal**: Implement full Blazor admin UI for feeding management.

**Vertical Slice**: Admin can feed horses, view options, see preferences, view history.

### RED - Write Failing Tests

(Blazor UI tests are typically integration/E2E - focus on component compilation)

- [ ] Verify: FeedHorse.razor compiles
- [ ] Verify: FeedingHistory.razor compiles
- [ ] Verify: Feedings.razor compiles
- [ ] Verify: FeedingsApiClient methods work

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Web/Components/Pages/FeedHorse.razor` - Feed selection page
- `TripleDerby.Web/Components/Pages/FeedingHistory.razor` - History view
- `TripleDerby.Web/Components/Pages/Feedings.razor` - All feedings list
- `TripleDerby.Web/ApiClients/Abstractions/IFeedingsApiClient.cs` - API client interface
- `TripleDerby.Web/ApiClients/FeedingsApiClient.cs` - API client implementation
- `TripleDerby.SharedKernel/FeedingOptionResult.cs` - Option DTO for UI
- `TripleDerby.SharedKernel/FeedingHistoryResult.cs` - History DTO for UI
- `TripleDerby.SharedKernel/FeedingRequestStatusResult.cs` - Status DTO for UI

**Files to Modify**:
- `TripleDerby.Api/Controllers/FeedingsController.cs` - Add new endpoints
- `TripleDerby.Web/Program.cs` - Register FeedingsApiClient

**Tasks**:
- [ ] Create `IFeedingsApiClient` interface:
  - `GetFeedingOptionsAsync(horseId, sessionId)` - Get 3 daily options
  - `CreateRequestAsync(horseId, feedingId, sessionId)` - Start feeding
  - `GetRequestStatusAsync(sessionId)` - Poll status
  - `GetFeedingHistoryAsync(horseId)` - Get history
  - `GetDiscoveredPreferencesAsync(horseId)` - Get preferences
- [ ] Implement `FeedingsApiClient`
- [ ] Create `FeedHorse.razor` (mirrors TrainHorse.razor):
  - Display 3 feeding options with effect ranges
  - Show discovered preferences (Favorite badge, Hated warning)
  - Show category for each option
  - Handle async feeding request with polling
  - Display results including preference discovery
- [ ] Create `FeedingHistory.razor`:
  - List past feeding sessions
  - Show effects, preferences discovered
  - Filter by date range
- [ ] Create `Feedings.razor`:
  - Admin view of all feeding types
  - Show categories, effect ranges
- [ ] Update `FeedingsController` with endpoints:
  - `GET /api/feedings/options/{horseId}` - Get 3 options
  - `POST /api/feedings/request` - Create feeding request
  - `GET /api/feedings/request/{sessionId}/status` - Get status
  - `GET /api/feedings/history/{horseId}` - Get history
  - `GET /api/feedings/preferences/{horseId}` - Get preferences

### REFACTOR - Clean Up

- [ ] Extract common UI patterns into shared components
- [ ] Ensure consistent styling with Training pages

### Acceptance Criteria

- [ ] FeedHorse page loads and displays 3 options
- [ ] Feeding request executes asynchronously
- [ ] Preference discovery displayed in results
- [ ] History page shows past sessions
- [ ] Preferences page shows discovered preferences

**Deliverable**: Complete admin UI for feeding system

---

## Files Summary

### New Files (Core)
- `TripleDerby.SharedKernel/Enums/FeedingCategoryId.cs`
- `TripleDerby.SharedKernel/Enums/FeedingRequestStatus.cs`
- `TripleDerby.SharedKernel/Messages/FeedingRequested.cs`
- `TripleDerby.SharedKernel/Messages/FeedingCompleted.cs`
- `TripleDerby.SharedKernel/FeedingOptionResult.cs`
- `TripleDerby.SharedKernel/FeedingHistoryResult.cs`
- `TripleDerby.SharedKernel/FeedingRequestStatusResult.cs`
- `TripleDerby.Core/Entities/HorseFeedingPreference.cs`
- `TripleDerby.Core/Entities/FeedingRequest.cs`
- `TripleDerby.Core/Configuration/FeedingConfig.cs`
- `TripleDerby.Core/Abstractions/Services/IFeedingCalculator.cs`
- `TripleDerby.Core/Calculators/FeedingCalculator.cs`
- `TripleDerby.Core/Specifications/HorseFeedingPreferenceSpecification.cs`
- `TripleDerby.Core/Specifications/FeedingSessionHistorySpecification.cs`

### New Files (Microservice)
- `TripleDerby.Services.Feeding/TripleDerby.Services.Feeding.csproj`
- `TripleDerby.Services.Feeding/Program.cs`
- `TripleDerby.Services.Feeding/Worker.cs`
- `TripleDerby.Services.Feeding/Abstractions/IFeedingExecutor.cs`
- `TripleDerby.Services.Feeding/Abstractions/IFeedingRequestProcessor.cs`
- `TripleDerby.Services.Feeding/FeedingExecutor.cs`
- `TripleDerby.Services.Feeding/FeedingRequestProcessor.cs`
- `TripleDerby.Services.Feeding/Config/FeedingConfig.cs`
- `TripleDerby.Services.Feeding/Calculators/FeedingCalculator.cs`
- `TripleDerby.Services.Feeding/DTOs/FeedingSessionResult.cs`

### New Files (Blazor UI)
- `TripleDerby.Web/Components/Pages/FeedHorse.razor`
- `TripleDerby.Web/Components/Pages/FeedingHistory.razor`
- `TripleDerby.Web/Components/Pages/Feedings.razor`
- `TripleDerby.Web/ApiClients/Abstractions/IFeedingsApiClient.cs`
- `TripleDerby.Web/ApiClients/FeedingsApiClient.cs`

### New Files (Tests)
- `TripleDerby.Tests.Unit/Entities/FeedingTests.cs`
- `TripleDerby.Tests.Unit/Entities/HorseFeedingPreferenceTests.cs`
- `TripleDerby.Tests.Unit/Feeding/FeedingCalculatorTests.cs`
- `TripleDerby.Tests.Unit/Feeding/FeedingServiceTests.cs`
- `TripleDerby.Tests.Unit/Feeding/FeedingMessagesTests.cs`
- `TripleDerby.Tests.Unit/Feeding/FeedingExecutorTests.cs`
- `TripleDerby.Tests.Unit/Feeding/FeedingRequestProcessorTests.cs`

### Modified Files
- `TripleDerby.Core/Entities/Feeding.cs`
- `TripleDerby.Core/Entities/FeedingSession.cs`
- `TripleDerby.Core/Entities/Horse.cs`
- `TripleDerby.Core/Abstractions/Services/IFeedingService.cs`
- `TripleDerby.Core/Services/FeedingService.cs`
- `TripleDerby.SharedKernel/FeedingSessionResult.cs`
- `TripleDerby.Infrastructure/Data/TripleDerbyContext.cs`
- `TripleDerby.Infrastructure/Data/ModelBuilderExtensions.cs`
- `TripleDerby.Services.Racing/RaceExecutor.cs`
- `TripleDerby.Core/Cache/CacheKeys.cs`
- `TripleDerby.Api/Controllers/FeedingsController.cs`
- `TripleDerby.Web/Program.cs`
- `TripleDerby.AppHost/Program.cs`
- `TripleDerby.sln`
- `TripleDerby.Tests.Unit/Entities/HorseTests.cs`
- `TripleDerby.Tests.Unit/Services/RaceExecutorTests.cs`

---

## Milestones

| Milestone | After Phase | What's Working |
|-----------|-------------|----------------|
| Data Model Complete | Phase 3 | All entities ready, 18 feeds seeded |
| Calculator Complete | Phase 5 | Full preference generation with modifiers |
| Core Feeding Works | Phase 6 | Feed horse, effects applied, preference discovered |
| Sync System Complete | Phase 7 | Daily options, race integration |
| Microservice Ready | Phase 9 | Async feeding via RabbitMQ |
| Feature Complete | Phase 10 | Full admin UI for feeding |

---

## Risks

| Risk | Mitigation | Phase |
|------|------------|-------|
| Deterministic seeding inconsistent across platforms | Use HashCode.Combine which is stable | Phase 4 |
| Cache key collisions for daily options | Include horse ID in cache key | Phase 7 |
| FeedResponse enum values conflict | Verify Neutral (Meh=4) mapping works | Phase 4 |
| Race integration breaks existing tests | Run full test suite after Phase 7 | Phase 7 |
| Microservice configuration complexity | Mirror TrainingService patterns exactly | Phase 8 |
| RabbitMQ consumer registration issues | Follow existing MessageRoutingConfig patterns | Phase 9 |
| Blazor state management with async polling | Copy TrainHorse.razor pattern directly | Phase 10 |

---

## Success Criteria

- [ ] All phases implemented
- [ ] All tests passing
- [ ] No regressions in existing functionality
- [ ] 18 feeds properly seeded with categories
- [ ] Preference discovery working deterministically
- [ ] Race completion resets feeding flag
- [ ] Microservice processes FeedingRequested messages
- [ ] FeedingCompleted events published correctly
- [ ] Blazor UI allows feeding horses
- [ ] Preference discovery shown in UI
- [ ] Feeding history viewable in UI
