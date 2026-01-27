# Feature 024: Season Career Progression - Implementation Plan

## Overview

**Feature**: [Season Career Progression System](../features/024-season-career-progression.md)
**Approach**: TDD with Vertical Slices
**Total Phases**: 10

## Summary

This implementation breaks down the Season Career Progression System into 10 vertical slices, each delivering working, testable functionality. The approach follows Test-Driven Development (Red-Green-Refactor) cycles with tasks sized at 30-90 minutes each. The system uses event-driven microservice architecture where a Points Service consumes events from Race, Training, Feeding, and Career services to award points. Each phase builds on the previous, ensuring the system remains testable and functional throughout development.

**Critical**: After completing each phase, STOP and wait for user review/approval before proceeding to the next phase.

---

## Phase 1: Core Domain Entities

**Goal**: Establish the foundational data model with full test coverage

**Vertical Slice**: Can create HorseCareer records and query career state

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Entities/HorseCareerTests.cs`

- [ ] Test: HorseCareer can be created with required properties (HorseId, CurrentSeasonNumber=1, CurrentRaceOrder=1, CurrentStepType=Race, Status=Active)
- [ ] Test: HorseCareer validates SeasonNumber between 1-5
- [ ] Test: HorseCareer validates RaceOrder between 1-10
- [ ] Test: HorseCareer tracks CreatedDate and UpdatedDate
- [ ] Test: HorseCareer has one-to-one relationship with Horse
- [ ] Test: HorseCareer has collection of CareerEvents

**Test File**: `TripleDerby.Tests.Unit/Entities/CareerEventTests.cs`

- [ ] Test: CareerEvent can be created with required properties
- [ ] Test: CareerEvent tracks state transitions (FromStep, ToStep)
- [ ] Test: CareerEvent can reference RaceRunId, TrainingSessionId, or FeedingSessionId
- [ ] Test: CareerEvent belongs to a Horse via HorseId

**Test File**: `TripleDerby.Tests.Unit/Entities/SeasonTemplateTests.cs`

- [ ] Test: SeasonTemplate maps (SeasonNumber, RaceOrder) to RaceId
- [ ] Test: SeasonTemplate has optional Description
- [ ] Test: SeasonTemplate belongs to a Race

**Test File**: `TripleDerby.Tests.Unit/Entities/PointsTransactionTests.cs`

- [ ] Test: PointsTransaction can be created with required properties
- [ ] Test: PointsTransaction validates Points > 0
- [ ] Test: PointsTransaction has unique EventId (idempotency key)
- [ ] Test: PointsTransaction belongs to User and optionally Horse

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Entities/HorseCareer.cs` - Career tracking entity
- `TripleDerby.Core/Entities/CareerEvent.cs` - Audit log entity
- `TripleDerby.Core/Entities/SeasonTemplate.cs` - Race sequence mapping
- `TripleDerby.Core/Entities/PointsTransaction.cs` - Points event sourcing
- `TripleDerby.SharedKernel/Enums/CareerStepType.cs` - Enum (Race, TrainingOption, FeedingOption)
- `TripleDerby.SharedKernel/Enums/CareerStatus.cs` - Enum (Active, Retired, Completed)
- `TripleDerby.SharedKernel/Enums/CareerEventType.cs` - Enum (all career transitions)
- `TripleDerby.SharedKernel/Enums/PointsReasonType.cs` - Enum (all point award reasons)

**Files to Modify**:
- `TripleDerby.Core/Entities/User.cs` - Add `public int Points { get; set; } = 0;` and navigation property
- `TripleDerby.Core/Entities/Horse.cs` - Add `public virtual HorseCareer? Career { get; set; }`

**Tasks**:
- [ ] Create HorseCareer entity with properties and data annotations
- [ ] Create CareerEvent entity with properties and relationships
- [ ] Create SeasonTemplate entity with composite unique key
- [ ] Create PointsTransaction entity with EventId uniqueness
- [ ] Create all enum types
- [ ] Update User entity with Points field
- [ ] Update Horse entity with Career navigation property

### REFACTOR - Clean Up

- [ ] Ensure all entities use consistent naming conventions
- [ ] Verify navigation properties are properly configured
- [ ] Add XML documentation comments to all entities
- [ ] Ensure enums have Description attributes where needed

### Acceptance Criteria

- [ ] All entity unit tests pass
- [ ] All entities have proper validation attributes
- [ ] All enums defined with correct values
- [ ] No compilation errors

**Deliverable**: Complete entity model with passing unit tests (no database yet)

**Estimated Time**: 60-75 minutes

---

## Phase 2: Entity Framework Configuration

**Goal**: Configure EF Core mappings, relationships, and create database migration

**Vertical Slice**: Can persist and query all career entities from database

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Infrastructure/Data/HorseCareerConfigurationTests.cs`

- [ ] Test: HorseCareer uses HorseId as primary key
- [ ] Test: HorseCareer has check constraint for SeasonNumber (1-5)
- [ ] Test: HorseCareer has check constraint for RaceOrder (1-10)
- [ ] Test: HorseCareer has index on Status
- [ ] Test: HorseCareer has index on (CurrentSeasonNumber, CurrentRaceOrder)

**Test File**: `TripleDerby.Tests.Unit/Infrastructure/Data/SeasonTemplateConfigurationTests.cs`

- [ ] Test: SeasonTemplate has unique constraint on (SeasonNumber, RaceOrder)
- [ ] Test: SeasonTemplate has clustered index on lookup columns

**Test File**: `TripleDerby.Tests.Unit/Infrastructure/Data/PointsTransactionConfigurationTests.cs`

- [ ] Test: PointsTransaction has unique constraint on EventId
- [ ] Test: PointsTransaction has index on (UserId, CreatedDate DESC)
- [ ] Test: PointsTransaction has index on HorseId

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Infrastructure/Data/Configurations/HorseCareerConfiguration.cs` - EF configuration
- `TripleDerby.Infrastructure/Data/Configurations/CareerEventConfiguration.cs` - EF configuration
- `TripleDerby.Infrastructure/Data/Configurations/SeasonTemplateConfiguration.cs` - EF configuration
- `TripleDerby.Infrastructure/Data/Configurations/PointsTransactionConfiguration.cs` - EF configuration

**Files to Modify**:
- `TripleDerby.Infrastructure/Data/AppDbContext.cs` - Add DbSets for new entities
- `TripleDerby.Infrastructure/Data/ModelBuilderExtensions.cs` - Add Seed method for SeasonTemplates

**Tasks**:
- [ ] Create HorseCareerConfiguration with one-to-one relationship, constraints, indexes
- [ ] Create CareerEventConfiguration with relationships and indexes
- [ ] Create SeasonTemplateConfiguration with unique constraint and clustered index
- [ ] Create PointsTransactionConfiguration with unique EventId, check constraint (Points > 0), indexes
- [ ] Add DbSets to AppDbContext (HorseCareers, CareerEvents, SeasonTemplates, PointsTransactions)
- [ ] Create seed data method for 50 SeasonTemplate records (all 5 seasons × 10 races)
- [ ] Update User configuration to add index on Points DESC
- [ ] Create migration using `dotnet ef migrations add AddCareerSystem`

### REFACTOR - Clean Up

- [ ] Extract common configuration patterns (indexes, foreign keys)
- [ ] Ensure consistent naming for constraints and indexes
- [ ] Verify cascade delete behaviors are correct
- [ ] Optimize seed data generation (use loops for repetitive data)

### Acceptance Criteria

- [ ] All configuration unit tests pass
- [ ] Migration created successfully
- [ ] Can apply migration to database
- [ ] All 50 SeasonTemplate records seeded
- [ ] All indexes and constraints present in database schema
- [ ] Can query HorseCareer, CareerEvent, SeasonTemplate, PointsTransaction via EF

**Deliverable**: Complete database schema with seed data

**Estimated Time**: 75-90 minutes

---

## Phase 3: Career Service - Basic Operations

**Goal**: Implement career lifecycle management (start, query, state tracking)

**Vertical Slice**: Can create a career and query its current state

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/CareerServiceTests.cs`

- [ ] Test: StartCareerAsync creates HorseCareer with Season=1, Race=1, Step=Race, Status=Active
- [ ] Test: StartCareerAsync logs CareerStarted event
- [ ] Test: StartCareerAsync throws if career already exists
- [ ] Test: GetCareerAsync returns existing career
- [ ] Test: GetCareerAsync returns null if no career exists
- [ ] Test: GetCurrentRaceAsync queries SeasonTemplate for (Season, RaceOrder) and returns Race
- [ ] Test: GetCurrentRaceAsync throws if no template found

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Interfaces/ICareerService.cs` - Service interface
- `TripleDerby.Core/Services/CareerService.cs` - Service implementation
- `TripleDerby.Core/Specifications/HorseCareerByIdSpecification.cs` - Query specification
- `TripleDerby.Core/Specifications/SeasonTemplateLookupSpecification.cs` - Query specification

**Tasks**:
- [ ] Create ICareerService interface with StartCareerAsync, GetCareerAsync, GetCurrentRaceAsync methods
- [ ] Implement StartCareerAsync(Guid horseId):
  - [ ] Check if HorseCareer already exists (throw if so)
  - [ ] Create HorseCareer with initial values
  - [ ] Create CareerEvent with EventType=CareerStarted
  - [ ] Save both via repository
- [ ] Implement GetCareerAsync(Guid horseId): Query HorseCareer by HorseId
- [ ] Implement GetCurrentRaceAsync(Guid horseId):
  - [ ] Get HorseCareer
  - [ ] Query SeasonTemplate by (CurrentSeasonNumber, CurrentRaceOrder)
  - [ ] Return Race from template
- [ ] Create specifications for queries

### REFACTOR - Clean Up

- [ ] Extract validation logic to private helper methods
- [ ] Ensure async/await best practices (ConfigureAwait)
- [ ] Add proper exception types (CareerNotFoundException, CareerAlreadyExistsException)
- [ ] Add logging for key operations

### Acceptance Criteria

- [ ] All unit tests pass
- [ ] Can create a career for a horse
- [ ] Can query career state
- [ ] Can lookup current race from SeasonTemplate
- [ ] CareerStarted event logged
- [ ] Appropriate exceptions thrown for invalid operations

**Deliverable**: Basic career management working end-to-end

**Estimated Time**: 60-75 minutes

---

## Phase 4: Career Service - State Machine (Race → Train → Feed → Next Race)

**Goal**: Implement career progression through all state transitions

**Vertical Slice**: Can advance a horse through one complete cycle (Race → Train → Feed → Next Race)

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/CareerServiceTests.cs` (continued)

- [ ] Test: AdvanceToTrainingAsync changes CurrentStepType to TrainingOption
- [ ] Test: AdvanceToTrainingAsync logs AdvancedToTraining event
- [ ] Test: AdvanceToTrainingAsync throws if not at Race step
- [ ] Test: AdvanceToFeedingAsync changes CurrentStepType to FeedingOption
- [ ] Test: AdvanceToFeedingAsync logs AdvancedToFeeding event
- [ ] Test: AdvanceToFeedingAsync throws if not at TrainingOption or FeedingOption step
- [ ] Test: AdvanceToNextRaceAsync increments CurrentRaceOrder (if < 10)
- [ ] Test: AdvanceToNextRaceAsync increments CurrentSeasonNumber and resets RaceOrder to 1 (if RaceOrder == 10)
- [ ] Test: AdvanceToNextRaceAsync changes CurrentStepType to Race
- [ ] Test: AdvanceToNextRaceAsync logs SeasonCompleted event when transitioning seasons
- [ ] Test: AdvanceToNextRaceAsync throws if not at FeedingOption step
- [ ] Test: SkipTrainingAsync advances from TrainingOption to FeedingOption without training
- [ ] Test: SkipFeedingAsync advances from FeedingOption to next race without feeding

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Core/Interfaces/ICareerService.cs` - Add progression methods
- `TripleDerby.Core/Services/CareerService.cs` - Implement progression methods

**Tasks**:
- [ ] Implement AdvanceToTrainingAsync(Guid horseId):
  - [ ] Get HorseCareer, validate CurrentStepType == Race
  - [ ] Set CurrentStepType = TrainingOption, UpdatedDate = now
  - [ ] Create CareerEvent with EventType=AdvancedToTraining
  - [ ] Save via repository
- [ ] Implement AdvanceToFeedingAsync(Guid horseId):
  - [ ] Get HorseCareer, validate CurrentStepType == TrainingOption
  - [ ] Set CurrentStepType = FeedingOption, UpdatedDate = now
  - [ ] Create CareerEvent with EventType=AdvancedToFeeding
  - [ ] Save via repository
- [ ] Implement AdvanceToNextRaceAsync(Guid horseId):
  - [ ] Get HorseCareer, validate CurrentStepType == FeedingOption
  - [ ] If CurrentRaceOrder < 10: increment RaceOrder
  - [ ] If CurrentRaceOrder == 10: increment SeasonNumber, reset RaceOrder = 1, log SeasonCompleted event
  - [ ] Set CurrentStepType = Race, UpdatedDate = now
  - [ ] Create CareerEvent with EventType=AdvancedToNextRace (and SeasonCompleted if applicable)
  - [ ] Save via repository
- [ ] Implement SkipTrainingAsync(Guid horseId): Validate step, advance to FeedingOption, log TrainingSkipped event
- [ ] Implement SkipFeedingAsync(Guid horseId): Validate step, call AdvanceToNextRaceAsync, log FeedingSkipped event

### REFACTOR - Clean Up

- [ ] Extract state validation to private ValidateCurrentStep(career, expectedStep) method
- [ ] Extract event creation to private CreateCareerEvent(horseId, eventType, ...) method
- [ ] Remove duplication in state transition logic
- [ ] Consider state machine pattern if complexity increases

### Acceptance Criteria

- [ ] All unit tests pass
- [ ] Can advance through full cycle: Race → Training → Feeding → Next Race
- [ ] Race order increments correctly (1-10)
- [ ] Season transitions correctly after race 10
- [ ] All state transitions logged as CareerEvents
- [ ] Cannot skip steps (validation enforced)
- [ ] Skip methods work correctly

**Deliverable**: Complete career state machine working

**Estimated Time**: 75-90 minutes

---

## Phase 5: Career Service - Retirement & Stat Decline

**Goal**: Implement retirement mechanics with bonuses and veteran stat decline

**Vertical Slice**: Can retire a horse and see retirement bonuses calculated; stat decline applies in Seasons 4-5

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/CareerServiceTests.cs` (continued)

- [ ] Test: RetireHorseAsync(voluntary=true) sets Status=Retired, IsRetired=true, RetiredDate=now
- [ ] Test: RetireHorseAsync logs RetiredVoluntary event with final season
- [ ] Test: CalculateRetirementBonus returns +50 for Seasons 1-2 (early retirement)
- [ ] Test: CalculateRetirementBonus returns +200 for Season 3 (optimal retirement)
- [ ] Test: CalculateRetirementBonus returns +100 for Seasons 4-5 (veteran retirement)
- [ ] Test: RetireHorseAsync(voluntary=false) sets Status=Completed, logs RetiredForced event
- [ ] Test: RetireHorseAsync throws if already retired
- [ ] Test: ApplyStatDecline returns stats × 1.0 for Seasons 1-3 (no decline)
- [ ] Test: ApplyStatDecline returns stats × 0.95 for Season 4 (5% decline)
- [ ] Test: ApplyStatDecline returns stats × 0.90 for Season 5 (10% decline)

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Core/Interfaces/ICareerService.cs` - Add RetireHorseAsync, CalculateRetirementBonus, ApplyStatDecline
- `TripleDerby.Core/Services/CareerService.cs` - Implement retirement methods

**Tasks**:
- [ ] Implement RetireHorseAsync(Guid horseId, bool voluntary):
  - [ ] Get HorseCareer, validate Status == Active
  - [ ] Set Status = (voluntary ? Retired : Completed)
  - [ ] Set RetiredDate = now
  - [ ] Update Horse.IsRetired = true
  - [ ] Create CareerEvent with EventType = (voluntary ? RetiredVoluntary : RetiredForced)
  - [ ] Save via repository
  - [ ] Return retirement bonus points
- [ ] Implement CalculateRetirementBonus(int seasonNumber):
  - [ ] Season 1-2: return 50
  - [ ] Season 3: return 200
  - [ ] Season 4-5: return 100
- [ ] Implement ApplyStatDecline(Horse horse, int seasonNumber):
  - [ ] Season 1-3: multiplier = 1.0
  - [ ] Season 4: multiplier = 0.95
  - [ ] Season 5: multiplier = 0.90
  - [ ] Return new stats object with Speed/Stamina/Agility/Durability × multiplier

### REFACTOR - Clean Up

- [ ] Extract bonus calculation to configuration/constants
- [ ] Extract stat decline multipliers to configuration
- [ ] Ensure retirement is idempotent (can't retire twice)
- [ ] Add comprehensive validation

### Acceptance Criteria

- [ ] All unit tests pass
- [ ] Can retire a horse voluntarily with correct bonus
- [ ] Can retire a horse forcibly (after Season 5)
- [ ] Retirement bonuses calculated correctly (+50/+200/+100)
- [ ] Stat decline applies correctly (×0.95 for S4, ×0.90 for S5)
- [ ] Retired status persists to database
- [ ] Horse.IsRetired flag set

**Deliverable**: Complete retirement system with bonuses and stat decline

**Estimated Time**: 60-75 minutes

---

## Phase 6: Points Service - Core Award Logic

**Goal**: Implement points awarding with idempotency and user total tracking

**Vertical Slice**: Can manually award points and see User.Points updated correctly

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/PointsServiceTests.cs`

- [ ] Test: AwardPointsAsync creates PointsTransaction with correct properties
- [ ] Test: AwardPointsAsync updates User.Points atomically
- [ ] Test: AwardPointsAsync skips if UserId == RACERS_USER_ID
- [ ] Test: AwardPointsAsync is idempotent (duplicate EventId ignored)
- [ ] Test: AwardPointsAsync throws if Points <= 0
- [ ] Test: AwardPointsAsync throws if EventId is empty
- [ ] Test: GetUserPointsAsync returns User.Points total
- [ ] Test: GetPointsHistoryAsync returns paginated PointsTransactions for user
- [ ] Test: GetLeaderboardAsync returns top N users ordered by Points DESC

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Interfaces/IPointsService.cs` - Service interface
- `TripleDerby.Core/Services/PointsService.cs` - Service implementation
- `TripleDerby.Core/Specifications/PointsTransactionByEventIdSpecification.cs` - Idempotency check
- `TripleDerby.Core/Specifications/PointsTransactionsByUserSpecification.cs` - History query
- `TripleDerby.Core/Specifications/LeaderboardSpecification.cs` - Top users by points

**Tasks**:
- [ ] Create IPointsService interface with AwardPointsAsync, GetUserPointsAsync, GetPointsHistoryAsync, GetLeaderboardAsync
- [ ] Implement AwardPointsAsync(userId, horseId, points, reason, eventId, eventType, relatedEntityId):
  - [ ] Validate inputs (points > 0, eventId not empty)
  - [ ] Check if UserId == RACERS_USER_ID → return early (no points for AI)
  - [ ] Check if PointsTransaction with EventId already exists → return early (idempotency)
  - [ ] Begin transaction
  - [ ] Create PointsTransaction record
  - [ ] Update User.Points += points
  - [ ] Commit transaction
- [ ] Implement GetUserPointsAsync(userId): Return User.Points
- [ ] Implement GetPointsHistoryAsync(userId, paginationRequest): Return PagedList<PointsTransaction>
- [ ] Implement GetLeaderboardAsync(paginationRequest): Return PagedList<User> ordered by Points DESC
- [ ] Create specifications for all queries

### REFACTOR - Clean Up

- [ ] Extract RACERS_USER_ID to configuration constant
- [ ] Extract transaction logic to private helper method
- [ ] Add logging for point awards
- [ ] Ensure cancellation token support for all async methods

### Acceptance Criteria

- [ ] All unit tests pass
- [ ] Can award points and see User.Points increment
- [ ] Duplicate EventId ignored (idempotency works)
- [ ] AI user (RACERS_USER_ID) never receives points
- [ ] User.Points always matches SUM(PointsTransaction.Points)
- [ ] Can query points history with pagination
- [ ] Can query leaderboard with pagination

**Deliverable**: Working points service with idempotency and leaderboard

**Estimated Time**: 75-90 minutes

---

## Phase 7: Event Integration - Race Events

**Goal**: Integrate RaceService with career/points via events

**Vertical Slice**: When a race completes, career advances and points are awarded automatically

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/RaceServiceTests.cs` (integration tests)

- [ ] Test: Race start publishes RaceStartedEvent for each horse
- [ ] Test: Race completion publishes RaceCompletedEvent for each horse with finish position
- [ ] Test: RaceCompletedEvent handler for player horse advances career to TrainingOption
- [ ] Test: RaceStartedEvent awards +10 points to player
- [ ] Test: RaceCompletedEvent awards position-based bonus points (1st: +150, 2nd: +75, etc.)
- [ ] Test: AI horse (Racers user) receives no points

**Test File**: `TripleDerby.Tests.Unit/EventHandlers/CareerEventHandlerTests.cs`

- [ ] Test: Consumes RaceCompletedEvent and calls CareerService.AdvanceToTrainingAsync

**Test File**: `TripleDerby.Tests.Unit/EventHandlers/PointsEventHandlerTests.cs`

- [ ] Test: Consumes RaceStartedEvent and calls PointsService.AwardPointsAsync(+10 points)
- [ ] Test: Consumes RaceCompletedEvent and calls PointsService.AwardPointsAsync with position bonus

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.SharedKernel/Events/RaceStartedEvent.cs` - Event DTO
- `TripleDerby.SharedKernel/Events/RaceCompletedEvent.cs` - Event DTO
- `TripleDerby.Services.Career/EventHandlers/RaceCompletedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Points/EventHandlers/RaceStartedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Points/EventHandlers/RaceCompletedEventHandler.cs` - Event consumer

**Files to Modify**:
- `TripleDerby.Services.Race/RaceService.cs` - Publish events

**Tasks**:
- [ ] Create RaceStartedEvent class with RaceRunId, HorseId, UserId
- [ ] Create RaceCompletedEvent class with RaceRunId, HorseId, UserId, FinishPosition, TotalHorses
- [ ] Update RaceService to publish RaceStartedEvent when race is queued (for each horse)
- [ ] Update RaceService to publish RaceCompletedEvent when race completes (for each horse with finish position)
- [ ] Create RaceCompletedEventHandler in Career service:
  - [ ] Subscribe to RaceCompletedEvent via message bus
  - [ ] Call CareerService.AdvanceToTrainingAsync(horseId)
  - [ ] Log CareerEvent with RaceRunId
- [ ] Create RaceStartedEventHandler in Points service:
  - [ ] Subscribe to RaceStartedEvent via message bus
  - [ ] Call PointsService.AwardPointsAsync(userId, horseId, 10, RaceStarted, eventId, raceRunId)
- [ ] Create RaceCompletedEventHandler in Points service:
  - [ ] Subscribe to RaceCompletedEvent via message bus
  - [ ] Calculate bonus points based on FinishPosition (1st: +150, 2nd: +75, 3rd: +40, 4th: +20, 5th: +10, 6th-12th: +5)
  - [ ] Call PointsService.AwardPointsAsync(userId, horseId, bonus, RaceWin/Place/Show/Finish, eventId, raceRunId)

### REFACTOR - Clean Up

- [ ] Extract position-to-points mapping to configuration
- [ ] Ensure message bus configuration is shared (Azure Service Bus or RabbitMQ)
- [ ] Add retry policies for event handlers
- [ ] Add dead letter queue handling

### Acceptance Criteria

- [ ] All integration tests pass
- [ ] Race start publishes RaceStartedEvent
- [ ] Race completion publishes RaceCompletedEvent
- [ ] Career advances to TrainingOption after race completion
- [ ] Player earns +10 points for race start
- [ ] Player earns position bonus points (1st: +150, etc.)
- [ ] AI horses do not earn points
- [ ] Events are idempotent (duplicate processing ignored)

**Deliverable**: Race completion automatically progresses career and awards points

**Estimated Time**: 90 minutes (split into 2 sessions if needed)

---

## Phase 8: Event Integration - Training & Feeding Events

**Goal**: Integrate TrainingService and FeedingService with career/points via events

**Vertical Slice**: When training/feeding completes, career advances and points are awarded automatically

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/TrainingServiceTests.cs` (integration tests)

- [ ] Test: Training start publishes TrainingStartedEvent
- [ ] Test: Training completion publishes TrainingCompletedEvent
- [ ] Test: TrainingCompletedEvent handler advances career to FeedingOption

**Test File**: `TripleDerby.Tests.Unit/Services/FeedingServiceTests.cs` (integration tests)

- [ ] Test: Feeding start publishes FeedingStartedEvent
- [ ] Test: Feeding completion publishes FeedingCompletedEvent
- [ ] Test: FeedingCompletedEvent handler advances career to next race
- [ ] Test: FeedingCompletedEvent handler publishes SeasonCompletedEvent if transitioning seasons

**Test File**: `TripleDerby.Tests.Unit/EventHandlers/PointsEventHandlerTests.cs` (continued)

- [ ] Test: Consumes TrainingStartedEvent and awards +5 points
- [ ] Test: Consumes TrainingCompletedEvent and awards +5 points
- [ ] Test: Consumes FeedingStartedEvent and awards +5 points
- [ ] Test: Consumes FeedingCompletedEvent and awards +5 points
- [ ] Test: Consumes SeasonCompletedEvent and awards season bonus points (+200 to +600)

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.SharedKernel/Events/TrainingStartedEvent.cs` - Event DTO
- `TripleDerby.SharedKernel/Events/TrainingCompletedEvent.cs` - Event DTO
- `TripleDerby.SharedKernel/Events/FeedingStartedEvent.cs` - Event DTO
- `TripleDerby.SharedKernel/Events/FeedingCompletedEvent.cs` - Event DTO
- `TripleDerby.SharedKernel/Events/SeasonCompletedEvent.cs` - Event DTO
- `TripleDerby.Services.Career/EventHandlers/TrainingCompletedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Career/EventHandlers/FeedingCompletedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Points/EventHandlers/TrainingStartedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Points/EventHandlers/TrainingCompletedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Points/EventHandlers/FeedingStartedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Points/EventHandlers/FeedingCompletedEventHandler.cs` - Event consumer
- `TripleDerby.Services.Points/EventHandlers/SeasonCompletedEventHandler.cs` - Event consumer

**Files to Modify**:
- `TripleDerby.Services.Training/TrainingService.cs` - Publish events
- `TripleDerby.Services.Feeding/FeedingService.cs` - Publish events

**Tasks**:
- [ ] Create event DTOs for training/feeding/season (TrainingSessionId, FeedingSessionId, SeasonNumber, etc.)
- [ ] Update TrainingService to publish TrainingStartedEvent and TrainingCompletedEvent
- [ ] Update FeedingService to publish FeedingStartedEvent and FeedingCompletedEvent
- [ ] Create TrainingCompletedEventHandler in Career service:
  - [ ] Subscribe to TrainingCompletedEvent
  - [ ] Call CareerService.AdvanceToFeedingAsync(horseId)
- [ ] Create FeedingCompletedEventHandler in Career service:
  - [ ] Subscribe to FeedingCompletedEvent
  - [ ] Call CareerService.AdvanceToNextRaceAsync(horseId)
  - [ ] If season transitioned, publish SeasonCompletedEvent
- [ ] Create event handlers in Points service for all training/feeding/season events:
  - [ ] TrainingStarted: +5 points
  - [ ] TrainingCompleted: +5 points
  - [ ] FeedingStarted: +5 points
  - [ ] FeedingCompleted: +5 points
  - [ ] SeasonCompleted: +200 (S1), +300 (S2), +500 (S3), +400 (S4), +600 (S5)

### REFACTOR - Clean Up

- [ ] Extract point values to configuration (training start/complete, feeding start/complete, season bonuses)
- [ ] Consolidate event handler patterns (common base class or helper methods)
- [ ] Ensure all event handlers have proper error handling and logging
- [ ] Add integration tests for full cycle (race → train → feed → next race)

### Acceptance Criteria

- [ ] All integration tests pass
- [ ] Training start/completion publishes events and awards points (+5 each)
- [ ] Feeding start/completion publishes events and awards points (+5 each)
- [ ] Career advances from TrainingOption to FeedingOption after training
- [ ] Career advances from FeedingOption to next race after feeding
- [ ] Season transitions trigger SeasonCompletedEvent
- [ ] Season completion awards bonus points (+200 to +600)
- [ ] Full cycle works: Race → Train → Feed → Next Race with points awarded at each step

**Deliverable**: Complete career progression loop with automatic point awards

**Estimated Time**: 90 minutes (split into 2 sessions if needed)

---

## Phase 9: AI Horse Auto-Progression

**Goal**: Implement AI horse event-driven progression with random decisions

**Vertical Slice**: AI horses automatically progress through career after race completion

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Services/AIProgressionServiceTests.cs`

- [ ] Test: ProcessAIHorseAsync identifies AI horse (OwnerId == RACERS_USER_ID)
- [ ] Test: ProcessAIHorseAsync makes random training decision based on season (60% S1, 50% S2, 40% S3, 30% S4-5)
- [ ] Test: ProcessAIHorseAsync makes random feeding decision based on season (50% S1, 40% S2, 30% S3, 25% S4-5)
- [ ] Test: ProcessAIHorseAsync executes training without cost check (unlimited budget)
- [ ] Test: ProcessAIHorseAsync executes feeding without cost check (unlimited budget)
- [ ] Test: ProcessAIHorseAsync advances through full cycle: Training decision → Feeding decision → Next race
- [ ] Test: ProcessAIHorseAsync makes retirement decision after Season 3 (60% chance)
- [ ] Test: ProcessAIHorseAsync makes retirement decision after Season 4 (40% chance)
- [ ] Test: ProcessAIHorseAsync forces retirement after Season 5

**Test File**: `TripleDerby.Tests.Unit/EventHandlers/AIProgressionEventHandlerTests.cs`

- [ ] Test: Consumes RaceCompletedEvent for AI horses only
- [ ] Test: Calls AIProgressionService.ProcessAIHorseAsync

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Interfaces/IAIProgressionService.cs` - Service interface
- `TripleDerby.Core/Services/AIProgressionService.cs` - Service implementation
- `TripleDerby.Services.Career/EventHandlers/AIProgressionEventHandler.cs` - Event consumer

**Tasks**:
- [ ] Create IAIProgressionService interface with ProcessAIHorseAsync method
- [ ] Implement ProcessAIHorseAsync(Guid horseId):
  - [ ] Get Horse and HorseCareer
  - [ ] Verify Horse.OwnerId == RACERS_USER_ID (skip if not AI)
  - [ ] Advance to TrainingOption (already done by RaceCompletedEventHandler)
  - [ ] Make random training decision based on season:
    - [ ] S1: 60%, S2: 50%, S3: 40%, S4-5: 30%
    - [ ] If train: select random TrainingType, execute without cost check, publish events
    - [ ] If skip: call CareerService.SkipTrainingAsync
  - [ ] Make random feeding decision based on season:
    - [ ] S1: 50%, S2: 40%, S3: 30%, S4-5: 25%
    - [ ] If feed: select random FeedingType, execute without cost check, publish events
    - [ ] If skip: call CareerService.SkipFeedingAsync
  - [ ] Make retirement decision:
    - [ ] After Season 3: 60% chance to retire
    - [ ] After Season 4: 40% chance to retire
    - [ ] After Season 5: forced retirement (100%)
    - [ ] If retire: call CareerService.RetireHorseAsync (no retirement bonus for AI)
- [ ] Create AIProgressionEventHandler:
  - [ ] Subscribe to RaceCompletedEvent
  - [ ] Filter for AI horses only (check OwnerId == RACERS_USER_ID)
  - [ ] Call AIProgressionService.ProcessAIHorseAsync

### REFACTOR - Clean Up

- [ ] Extract probability thresholds to configuration (season-based training/feeding chances)
- [ ] Extract RACERS_USER_ID to shared constant
- [ ] Add logging for AI decisions (for debugging and analytics)
- [ ] Ensure random number generator is thread-safe

### Acceptance Criteria

- [ ] All unit tests pass
- [ ] AI horses identified correctly by OwnerId
- [ ] AI horses make random training decisions with correct probabilities
- [ ] AI horses make random feeding decisions with correct probabilities
- [ ] AI horses train/feed without cost checks
- [ ] AI horses advance to next race automatically
- [ ] AI horses retire randomly after Season 3/4
- [ ] AI horses forced to retire after Season 5
- [ ] AI horses never earn points (verified in previous phases)

**Deliverable**: AI horses fully autonomous, progressing through careers automatically

**Estimated Time**: 75-90 minutes

---

## Phase 10: API Endpoints with Pagination

**Goal**: Expose career and points functionality via REST API

**Vertical Slice**: Can query career state, leaderboard, and points history via API endpoints

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Controllers/CareerControllerTests.cs`

- [ ] Test: GET /api/horses/{id}/career returns HorseCareer summary
- [ ] Test: GET /api/horses/{id}/next-race returns Race from SeasonTemplate
- [ ] Test: GET /api/horses/{id}/career-history returns paginated CareerEvents
- [ ] Test: POST /api/horses/{id}/retire sets retirement and returns bonus points
- [ ] Test: POST /api/horses/{id}/skip-training calls SkipTrainingAsync
- [ ] Test: POST /api/horses/{id}/skip-feeding calls SkipFeedingAsync

**Test File**: `TripleDerby.Tests.Unit/Controllers/PointsControllerTests.cs`

- [ ] Test: GET /api/leaderboard returns PagedList<User> with pagination metadata
- [ ] Test: GET /api/leaderboard accepts PaginationRequest (Page, Size, SortBy, Direction)
- [ ] Test: GET /api/users/{id}/points returns User.Points total
- [ ] Test: GET /api/users/{id}/points/history returns PagedList<PointsTransaction>
- [ ] Test: GET /api/users/{id}/points/history accepts PaginationRequest

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Api/Controllers/CareerController.cs` - Career endpoints
- `TripleDerby.Api/Controllers/PointsController.cs` - Points/leaderboard endpoints
- `TripleDerby.Api/Models/CareerSummaryDto.cs` - DTO for career summary
- `TripleDerby.Api/Models/LeaderboardEntryDto.cs` - DTO for leaderboard user
- `TripleDerby.Api/Models/RetirementResponseDto.cs` - DTO for retirement response

**Tasks**:
- [ ] Create CareerController:
  - [ ] GET /api/horses/{id}/career → GetCareerAsync(id):
    - [ ] Call CareerService.GetCareerAsync
    - [ ] Return CareerSummaryDto (Season, Race, Step, Status, Stats)
  - [ ] GET /api/horses/{id}/next-race → GetNextRaceAsync(id):
    - [ ] Call CareerService.GetCurrentRaceAsync
    - [ ] Return RaceDto
  - [ ] GET /api/horses/{id}/career-history → GetCareerHistoryAsync(id, [FromQuery] PaginationRequest request):
    - [ ] Query CareerEvents with pagination
    - [ ] Return PagedList<CareerEventDto>
  - [ ] POST /api/horses/{id}/retire → RetireHorseAsync(id):
    - [ ] Call CareerService.RetireHorseAsync(voluntary: true)
    - [ ] Publish HorseRetiredEvent
    - [ ] Return RetirementResponseDto with bonus points
  - [ ] POST /api/horses/{id}/skip-training → SkipTrainingAsync(id):
    - [ ] Call CareerService.SkipTrainingAsync
    - [ ] Return Ok
  - [ ] POST /api/horses/{id}/skip-feeding → SkipFeedingAsync(id):
    - [ ] Call CareerService.SkipFeedingAsync
    - [ ] Return Ok
- [ ] Create PointsController:
  - [ ] GET /api/leaderboard → GetLeaderboardAsync([FromQuery] PaginationRequest request):
    - [ ] Call PointsService.GetLeaderboardAsync
    - [ ] Return PagedList<LeaderboardEntryDto> (Username, Points, Rank)
  - [ ] GET /api/users/{id}/points → GetUserPointsAsync(id):
    - [ ] Call PointsService.GetUserPointsAsync
    - [ ] Return PointsSummaryDto (Total, Rank)
  - [ ] GET /api/users/{id}/points/history → GetPointsHistoryAsync(id, [FromQuery] PaginationRequest request):
    - [ ] Call PointsService.GetPointsHistoryAsync
    - [ ] Return PagedList<PointsTransactionDto>

### REFACTOR - Clean Up

- [ ] Extract DTO mapping to AutoMapper or manual mapping methods
- [ ] Add [Authorize] attributes to endpoints requiring authentication
- [ ] Add API documentation (XML comments for Swagger)
- [ ] Add validation attributes to DTOs
- [ ] Ensure consistent error responses (ProblemDetails)

### Acceptance Criteria

- [ ] All controller unit tests pass
- [ ] GET /api/horses/{id}/career returns career summary
- [ ] GET /api/horses/{id}/next-race returns correct race from SeasonTemplate
- [ ] GET /api/horses/{id}/career-history returns paginated events
- [ ] POST /api/horses/{id}/retire retires horse and returns bonus
- [ ] POST /api/horses/{id}/skip-training skips training
- [ ] POST /api/horses/{id}/skip-feeding skips feeding
- [ ] GET /api/leaderboard returns paginated leaderboard with Page/Size/SortBy/Direction support
- [ ] GET /api/users/{id}/points returns user's total points
- [ ] GET /api/users/{id}/points/history returns paginated transaction history
- [ ] All paginated endpoints use PaginationRequest/PagedList pattern

**Deliverable**: Complete API surface for career and points features

**Estimated Time**: 90 minutes (split into 2 sessions if needed)

---

## Files Summary

### New Files (Entities)
- `TripleDerby.Core/Entities/HorseCareer.cs` - Career tracking
- `TripleDerby.Core/Entities/CareerEvent.cs` - Event audit log
- `TripleDerby.Core/Entities/SeasonTemplate.cs` - Race sequence mapping
- `TripleDerby.Core/Entities/PointsTransaction.cs` - Points event sourcing

### New Files (Enums)
- `TripleDerby.SharedKernel/Enums/CareerStepType.cs`
- `TripleDerby.SharedKernel/Enums/CareerStatus.cs`
- `TripleDerby.SharedKernel/Enums/CareerEventType.cs`
- `TripleDerby.SharedKernel/Enums/PointsReasonType.cs`

### New Files (Services)
- `TripleDerby.Core/Interfaces/ICareerService.cs`
- `TripleDerby.Core/Services/CareerService.cs`
- `TripleDerby.Core/Interfaces/IPointsService.cs`
- `TripleDerby.Core/Services/PointsService.cs`
- `TripleDerby.Core/Interfaces/IAIProgressionService.cs`
- `TripleDerby.Core/Services/AIProgressionService.cs`

### New Files (Specifications)
- `TripleDerby.Core/Specifications/HorseCareerByIdSpecification.cs`
- `TripleDerby.Core/Specifications/SeasonTemplateLookupSpecification.cs`
- `TripleDerby.Core/Specifications/PointsTransactionByEventIdSpecification.cs`
- `TripleDerby.Core/Specifications/PointsTransactionsByUserSpecification.cs`
- `TripleDerby.Core/Specifications/LeaderboardSpecification.cs`

### New Files (EF Configuration)
- `TripleDerby.Infrastructure/Data/Configurations/HorseCareerConfiguration.cs`
- `TripleDerby.Infrastructure/Data/Configurations/CareerEventConfiguration.cs`
- `TripleDerby.Infrastructure/Data/Configurations/SeasonTemplateConfiguration.cs`
- `TripleDerby.Infrastructure/Data/Configurations/PointsTransactionConfiguration.cs`

### New Files (Events)
- `TripleDerby.SharedKernel/Events/RaceStartedEvent.cs`
- `TripleDerby.SharedKernel/Events/RaceCompletedEvent.cs`
- `TripleDerby.SharedKernel/Events/TrainingStartedEvent.cs`
- `TripleDerby.SharedKernel/Events/TrainingCompletedEvent.cs`
- `TripleDerby.SharedKernel/Events/FeedingStartedEvent.cs`
- `TripleDerby.SharedKernel/Events/FeedingCompletedEvent.cs`
- `TripleDerby.SharedKernel/Events/SeasonCompletedEvent.cs`
- `TripleDerby.SharedKernel/Events/HorseRetiredEvent.cs`

### New Files (Event Handlers - Career Service)
- `TripleDerby.Services.Career/EventHandlers/RaceCompletedEventHandler.cs`
- `TripleDerby.Services.Career/EventHandlers/TrainingCompletedEventHandler.cs`
- `TripleDerby.Services.Career/EventHandlers/FeedingCompletedEventHandler.cs`
- `TripleDerby.Services.Career/EventHandlers/AIProgressionEventHandler.cs`

### New Files (Event Handlers - Points Service)
- `TripleDerby.Services.Points/EventHandlers/RaceStartedEventHandler.cs`
- `TripleDerby.Services.Points/EventHandlers/RaceCompletedEventHandler.cs`
- `TripleDerby.Services.Points/EventHandlers/TrainingStartedEventHandler.cs`
- `TripleDerby.Services.Points/EventHandlers/TrainingCompletedEventHandler.cs`
- `TripleDerby.Services.Points/EventHandlers/FeedingStartedEventHandler.cs`
- `TripleDerby.Services.Points/EventHandlers/FeedingCompletedEventHandler.cs`
- `TripleDerby.Services.Points/EventHandlers/SeasonCompletedEventHandler.cs`

### New Files (Controllers)
- `TripleDerby.Api/Controllers/CareerController.cs`
- `TripleDerby.Api/Controllers/PointsController.cs`

### New Files (DTOs)
- `TripleDerby.Api/Models/CareerSummaryDto.cs`
- `TripleDerby.Api/Models/LeaderboardEntryDto.cs`
- `TripleDerby.Api/Models/RetirementResponseDto.cs`

### Modified Files
- `TripleDerby.Core/Entities/User.cs` - Add Points field and navigation property
- `TripleDerby.Core/Entities/Horse.cs` - Add Career navigation property
- `TripleDerby.Infrastructure/Data/AppDbContext.cs` - Add DbSets
- `TripleDerby.Infrastructure/Data/ModelBuilderExtensions.cs` - Add seed data
- `TripleDerby.Services.Race/RaceService.cs` - Publish events
- `TripleDerby.Services.Training/TrainingService.cs` - Publish events
- `TripleDerby.Services.Feeding/FeedingService.cs` - Publish events

---

## Milestones

| Milestone | After Phase | What's Working |
|-----------|-------------|----------------|
| Data Model Ready | Phase 2 | All entities persisted to database, seed data loaded |
| Career Lifecycle | Phase 5 | Can create, progress, and retire horses through full career |
| Points Tracking | Phase 6 | Can award points manually, leaderboard queries work |
| Race Integration | Phase 7 | Race completion automatically progresses career and awards points |
| Full Game Loop | Phase 8 | Complete cycle working: Race → Train → Feed → Next Race with points |
| AI Autonomy | Phase 9 | AI horses progress automatically without player input |
| Feature Complete | Phase 10 | Full API surface available, paginated queries working |

---

## Risks

| Risk | Mitigation | Phase |
|------|------------|-------|
| Event idempotency failures | Use unique EventId from message headers, test duplicate scenarios | 6, 7, 8 |
| Career state corruption | Validate all transitions, use atomic transactions | 4 |
| AI progression loops/deadlocks | Add timeouts and circuit breakers to event handlers | 9 |
| Performance of leaderboard queries | Index User.Points DESC, use pagination | 6, 10 |
| Message bus configuration complexity | Extract shared configuration, use Feature 023 foundation | 7, 8 |
| Stat decline calculation errors | Comprehensive unit tests with multiple scenarios | 5 |
| Season template gaps | Seed validation tests to ensure all 50 records exist | 2, 3 |

---

## Success Criteria

- [ ] All 10 phases implemented
- [ ] All tests passing (unit + integration)
- [ ] No regressions in existing features
- [ ] Code coverage ≥ 80% for new code
- [ ] Event-driven architecture working (no direct service calls between microservices)
- [ ] Pagination working for all list endpoints (PaginationRequest/PagedList)
- [ ] Leaderboard reflects all point transactions accurately
- [ ] AI horses progress autonomously
- [ ] Career state machine enforces valid transitions
- [ ] Points idempotency prevents duplicates
- [ ] Performance targets met (career transitions < 10ms, leaderboard < 200ms)
- [ ] Database migration applied successfully
- [ ] All 50 SeasonTemplate records seeded

---

## Notes for Implementation

### Phase Progression (CRITICAL)

**STOP after each phase for user review and approval before proceeding.**

After completing each phase:
1. Run all tests to verify completion
2. Report test results to user
3. Summarize what was implemented
4. List files created/modified
5. **ASK**: "Would you like me to commit these changes?"
6. **WAIT** for explicit approval before committing
7. **WAIT** for approval before starting the next phase

### Matchmaking Verification (Phase 6+)

Before implementing matchmaking in Phase 6, verify if it already exists:
- [ ] Search codebase for existing matchmaking logic
- [ ] Check RaceService for opponent selection
- [ ] If exists: integrate with career system (Season, RaceOrder filtering)
- [ ] If not exists: implement as specified in feature spec

### Event Bus Configuration

- Use Azure Service Bus OR RabbitMQ (configurable per Feature 023)
- Share configuration between Career, Points, Race, Training, Feeding services
- All event handlers must support retries and dead letter queues
- EventId should come from message headers for idempotency

### Testing Strategy

- **Unit Tests**: Test each service method in isolation with mocks
- **Integration Tests**: Test event handlers with in-memory message bus
- **End-to-End Tests**: Test full workflows (race → train → feed → next race)
- Use test builders for complex objects (HorseBuilder, CareerBuilder)
- Follow AAA pattern (Arrange-Act-Assert)
- Test naming: `MethodName_Scenario_ExpectedBehavior`

### Database Strategy

- Drop and recreate database (no backfill)
- All 50 SeasonTemplate records seeded during migration
- Use byte types for SeasonNumber (1-5) and RaceOrder (1-10)
- Ensure all indexes and constraints are created
- Validate seed data with tests

### Constants to Extract

Create configuration file for:
- RACERS_USER_ID: `72115894-88CD-433E-9892-CAC22E335F1D`
- Point values: Race participation (+10), position bonuses (+150/+75/+40/+20/+10/+5), training/feeding (+5 each), season bonuses (+200 to +600)
- Retirement bonuses: Early (+50), Optimal (+200), Veteran (+100)
- Stat decline multipliers: Season 4 (0.95), Season 5 (0.90)
- AI probabilities: Training (60%/50%/40%/30%), Feeding (50%/40%/30%/25%), Retirement (60%/40%)

---

**End of Implementation Plan**
