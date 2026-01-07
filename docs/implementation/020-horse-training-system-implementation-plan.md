# Feature 020: Horse Training System - Implementation Plan

**Created**: 2026-01-05
**Feature Spec**: [docs/features/020-horse-training-system.md](../features/020-horse-training-system.md)
**Complexity**: High (multiple integration points, RabbitMQ, Redis caching)
**Estimated Time**: 16-24 hours (across 7 phases)

---

## Overview

This implementation plan breaks down Feature 020 (Horse Training System) into **7 incremental TDD phases**, each delivering end-to-end functionality. The system allows players to train horses between races with targeted stat improvements, happiness management, and strategic choice through random training options.

### Key Architectural Decisions

1. **Clean Service Layer**: Follow RaceOrchestrator pattern (not RaceExecutor anti-pattern)
2. **Pure Calculation Logic**: TrainingCalculator has no infrastructure dependencies
3. **RabbitMQ Integration**: Async command pattern for training execution
4. **Redis Caching**: Stable 3-option selection prevents refresh-scumming
5. **LegType Bonuses**: "Should Have" feature for strategic depth

### Prerequisites

- ✅ Feature 018 (Race Stat Progression) completed
- ✅ Happiness stat already exists and working
- ✅ Training and TrainingSession entities exist (need expansion)
- ✅ RabbitMQ and Redis infrastructure in place

---

## Phase 1: Core Data Model & Configuration

**Goal**: Establish data foundation with expanded entities, configuration, and database migration.

**Vertical Slice**: Database schema supports all training features.

### TDD Cycle

#### RED - Write Failing Tests

- [ ] Test: Training entity has stat modifier properties (Speed, Stamina, Agility, Durability)
- [ ] Test: Training entity has HappinessCost property
- [ ] Test: Training entity has OverworkRisk and IsRecovery properties
- [ ] Test: TrainingSession entity has detailed stat gain fields
- [ ] Test: TrainingSession entity has SessionDate and RaceStartsAtTime
- [ ] Test: TrainingSession entity has OverworkOccurred boolean
- [ ] Test: Horse entity has HasTrainedSinceLastRace boolean flag
- [ ] Test: TrainingConfig constants are correctly defined

**Why these tests**: Ensure schema changes are tested before migration runs.

#### GREEN - Make Tests Pass

1. **Expand Training entity** (`TripleDerby.Core/Entities/Training.cs`):
   ```csharp
   public double SpeedModifier { get; set; }
   public double StaminaModifier { get. set; }
   public double AgilityModifier { get; set; }
   public double DurabilityModifier { get; set; }
   public double HappinessCost { get; set; }
   public double OverworkRisk { get; set; }
   public bool IsRecovery { get; set; }
   ```

2. **Enhance TrainingSession entity** (`TripleDerby.Core/Entities/TrainingSession.cs`):
   ```csharp
   public DateTime SessionDate { get; set; }
   public short RaceStartsAtTime { get; set; }
   public double SpeedGain { get; set; }
   public double StaminaGain { get; set; }
   public double AgilityGain { get; set; }
   public double DurabilityGain { get; set; }
   public double HappinessChange { get; set; }
   public bool OverworkOccurred { get; set; }
   ```

3. **Add Horse flag** (`TripleDerby.Core/Entities/Horse.cs`):
   ```csharp
   public bool HasTrainedSinceLastRace { get; set; }
   ```

4. **Create TrainingConfig** (`TripleDerby.Services.Training/Config/TrainingConfig.cs`):
   ```csharp
   public static class TrainingConfig
   {
       public const double BaseTrainingGrowthRate = 0.015;
       public const double MinimumHappinessToTrain = 15.0;
       public const double HappinessWarningThreshold = 40.0;
       public const double OverworkHappinessPenalty = 5.0;
       public const double OverworkGainReduction = 0.50;

       public const double YoungHorseTrainingMultiplier = 1.20;
       public const double PrimeHorseTrainingMultiplier = 1.40;
       public const double VeteranHorseTrainingMultiplier = 0.80;
       public const double OldHorseTrainingMultiplier = 0.40;

       public const int TrainingOptionsOffered = 3;
       public const double LegTypeBonusMultiplier = 1.20;

       public static readonly Dictionary<LegTypeId, byte> LegTypePreferredTraining = new()
       {
           { LegTypeId.StartDash, 1 },
           { LegTypeId.FrontRunner, 6 },
           { LegTypeId.StretchRunner, 2 },
           { LegTypeId.LastSpurt, 5 },
           { LegTypeId.RailRunner, 3 }
       };
   }
   ```

5. **Update ModelBuilderExtensions** (`TripleDerby.Infrastructure/Data/ModelBuilderExtensions.cs`):
   - Expand existing 6 training types with new properties
   - Add 4 new training types (Gate Practice, Light Exercise, Rest Day, + 1 more)
   - Total: 10 training types with full configuration

6. **Create EF Core migration**:
   ```bash
   dotnet ef migrations add Feature020_TrainingSystemDataModel
   ```

#### REFACTOR - Clean Up

- [ ] Extract training type definitions into constants for readability
- [ ] Add XML documentation comments to all new properties
- [ ] Ensure all navigation properties are configured correctly

### Acceptance Criteria

- [ ] All entity tests pass
- [ ] EF Core migration applies successfully to test database
- [ ] 10 training types seeded with correct stat modifiers
- [ ] TrainingConfig constants are accessible and correct
- [ ] No breaking changes to existing Training/TrainingSession usage

**Deliverable**: Database schema supports full training system.

**Estimated Complexity**: Medium
**Risks**: Migration might conflict with existing data; test in clean DB first.

---

## Phase 2: TrainingCalculator (Pure Domain Logic)

**Goal**: Implement pure calculation logic for training gains, happiness impact, and career multipliers.

**Vertical Slice**: All training formulas work correctly in isolation (unit testable).

### TDD Cycle

#### RED - Write Failing Tests

- [ ] Test: CalculateTrainingGain returns 0 when stat is at DominantPotential
- [ ] Test: CalculateTrainingGain calculates correct growth for young horse (1.20x)
- [ ] Test: CalculateTrainingGain calculates correct growth for prime horse (1.40x)
- [ ] Test: CalculateTrainingGain calculates correct growth for veteran horse (0.80x)
- [ ] Test: CalculateTrainingGain calculates correct growth for old horse (0.40x)
- [ ] Test: CalculateTrainingGain respects happiness modifier (0.5x at 0% to 1.0x at 100%)
- [ ] Test: CalculateTrainingGain enforces ceiling (doesn't exceed DominantPotential)
- [ ] Test: CalculateTrainingCareerMultiplier returns correct value for each phase
- [ ] Test: CalculateHappinessEffectivenessModifier returns 1.0 at 100% happiness
- [ ] Test: CalculateHappinessEffectivenessModifier returns 0.5 at 0% happiness
- [ ] Test: CalculateHappinessImpact applies base happiness cost correctly
- [ ] Test: CalculateHappinessImpact doubles overwork risk at 50% happiness
- [ ] Test: CalculateHappinessImpact quadruples overwork risk at 25% happiness
- [ ] Test: CalculateHappinessImpact applies overwork penalty (extra -5 happiness)
- [ ] Test: CalculateLegTypeBonus returns 1.20 for matching training type
- [ ] Test: CalculateLegTypeBonus returns 1.0 for non-matching training type

**Why these tests**: Cover all formula branches, edge cases, and boundary conditions.

#### GREEN - Make Tests Pass

1. **Create ITrainingCalculator interface** (`TripleDerby.Services.Training/Abstractions/ITrainingCalculator.cs`):
   ```csharp
   public interface ITrainingCalculator
   {
       double CalculateTrainingGain(
           double actualStat,
           double dominantPotential,
           double trainingModifier,
           double careerMultiplier,
           double happinessModifier,
           double legTypeBonus);

       double CalculateTrainingCareerMultiplier(short raceStarts);

       (double happinessChange, bool overwork) CalculateHappinessImpact(
           double baseHappinessCost,
           double currentHappiness,
           double overworkRisk);

       double CalculateHappinessEffectivenessModifier(double happiness);

       double CalculateLegTypeBonus(LegTypeId legType, byte trainingId);
   }
   ```

2. **Implement TrainingCalculator** (`TripleDerby.Services.Training/Calculators/TrainingCalculator.cs`):
   - Implement all interface methods
   - Follow StatProgressionCalculator pattern (pure functions, no dependencies)
   - Use TrainingConfig constants for all magic numbers
   - Add comprehensive XML documentation

3. **Create unit test file** (`TripleDerby.Tests.Unit/Training/TrainingCalculatorTests.cs`):
   - Test class setup with test data builder pattern
   - Parameterized tests for career multipliers
   - Edge case tests (0% happiness, 100% happiness, at ceiling, etc.)
   - Overwork probability tests (use fixed random seed for determinism)

#### REFACTOR - Clean Up

- [ ] Extract helper methods for repeated calculations
- [ ] Ensure all calculations use Math.Clamp for bounds checking
- [ ] Add theory-based tests for multiple stat/happiness combinations
- [ ] Verify formula matches specification examples

### Acceptance Criteria

- [ ] All TrainingCalculator tests pass (target: 25+ test cases)
- [ ] Code coverage > 95% for TrainingCalculator
- [ ] Calculations match example from feature spec (0.918 Speed gain)
- [ ] No dependencies on infrastructure (pure domain logic)
- [ ] Career multipliers differ from racing (young = 1.20, not 0.80)

**Deliverable**: Fully tested training calculation engine.

**Estimated Complexity**: Medium
**Risks**: Formula complexity; careful with floating-point precision.

---

## Phase 3: TrainingService (Core Orchestration)

**Goal**: Implement core training execution logic without RabbitMQ/Redis (add those in Phase 4).

**Vertical Slice**: Can execute training session end-to-end (database in → calculations → database out).

### TDD Cycle

#### RED - Write Failing Tests

- [ ] Test: TrainAsync throws if horse has already trained since last race
- [ ] Test: TrainAsync throws if horse happiness below minimum (15%)
- [ ] Test: TrainAsync calculates correct stat gains using TrainingCalculator
- [ ] Test: TrainAsync applies stat changes to horse entity
- [ ] Test: TrainAsync saves TrainingSession with correct details
- [ ] Test: TrainAsync sets HasTrainedSinceLastRace to true
- [ ] Test: TrainAsync returns TrainingSessionResult with stat changes
- [ ] Test: TrainAsync detects stats at ceiling and returns flags
- [ ] Test: CanTrain returns false if already trained since last race
- [ ] Test: CanTrain returns false if happiness below minimum
- [ ] Test: CanTrain returns true if eligible to train
- [ ] Test: GetTrainingHistoryAsync returns sessions ordered by date
- [ ] Test: GetTrainingHistoryAsync limits to specified count

**Why these tests**: Ensure business rules enforced and data persisted correctly.

#### GREEN - Make Tests Pass

1. **Create ITrainingService interface** (`TripleDerby.Services.Training/Abstractions/ITrainingService.cs`):
   ```csharp
   public interface ITrainingService
   {
       Task<TrainingSessionResult> TrainAsync(
           Guid horseId,
           byte trainingId,
           CancellationToken cancellationToken = default);

       Task<List<TrainingSession>> GetTrainingHistoryAsync(
           Guid horseId,
           int limit = 20,
           CancellationToken cancellationToken = default);

       bool CanTrain(Horse horse);
   }
   ```

2. **Create TrainingSessionResult DTO** (`TripleDerby.Services.Training/DTOs/TrainingSessionResult.cs`):
   ```csharp
   public record TrainingSessionResult
   {
       public Guid SessionId { get; init; }
       public string TrainingName { get; init; } = null!;
       public bool Success { get; init; }
       public string Message { get; init; }
       public double SpeedGain { get; init; }
       public double StaminaGain { get; init; }
       public double AgilityGain { get; init; }
       public double DurabilityGain { get; init; }
       public double HappinessChange { get; init; }
       public bool OverworkOccurred { get. init; }
       public double CurrentHappiness { get; init; }
       public bool SpeedAtCeiling { get. init; }
       public bool StaminaAtCeiling { get; init; }
       public bool AgilityAtCeiling { get. init; }
       public bool DurabilityAtCeiling { get. init; }
   }
   ```

3. **Implement TrainingService** (`TripleDerby.Services.Training/TrainingService.cs`):
   ```csharp
   public class TrainingService : ITrainingService
   {
       private readonly ITrainingCalculator _calculator;
       private readonly IHorseRepository _horseRepository;
       private readonly ITrainingRepository _trainingRepository;
       private readonly ITrainingSessionRepository _sessionRepository;
       private readonly IRandomGenerator _randomGenerator;

       // Implement TrainAsync:
       // 1. Load horse with stats and training history
       // 2. Validate: CanTrain(), happiness >= minimum
       // 3. Load training type
       // 4. Get DominantPotential stats from HorseStatistics
       // 5. Calculate career multiplier
       // 6. Calculate happiness modifier
       // 7. Calculate LegType bonus
       // 8. For each stat: calculate training gain
       // 9. Calculate happiness impact (with overwork check)
       // 10. Apply stat changes to horse
       // 11. Create TrainingSession record
       // 12. Set HasTrainedSinceLastRace = true
       // 13. Save changes
       // 14. Return result DTO

       public async Task<TrainingSessionResult> TrainAsync(...) { }
       public bool CanTrain(Horse horse) { }
       public async Task<List<TrainingSession>> GetTrainingHistoryAsync(...) { }
   }
   ```

4. **Create unit tests** (`TripleDerby.Tests.Unit/Training/TrainingServiceTests.cs`):
   - Use mocks for repositories and calculator
   - Test happy path (successful training)
   - Test validation failures (already trained, low happiness)
   - Test overwork scenarios
   - Test stat ceiling detection

#### REFACTOR - Clean Up

- [ ] Extract validation logic into private helper methods
- [ ] Extract stat application logic into private helper
- [ ] Ensure method lengths < 50 lines (follow RaceOrchestrator pattern)
- [ ] Add comprehensive logging for debugging

### Acceptance Criteria

- [ ] All TrainingService tests pass (target: 15+ test cases)
- [ ] Code coverage > 85% for TrainingService
- [ ] TrainAsync method < 50 lines (orchestration only)
- [ ] Validation errors throw descriptive exceptions
- [ ] Training session persisted with all details

**Deliverable**: Core training execution works end-to-end (in-memory).

**Estimated Complexity**: High
**Risks**: Complex orchestration; careful with transaction boundaries.

---

## Phase 4: Random Training Options & Redis Caching

**Goal**: Implement 3-option random selection with Redis caching to prevent refresh-scumming.

**Vertical Slice**: Players get stable, random training choices that persist across page refreshes.

### TDD Cycle

#### RED - Write Failing Tests

- [ ] Test: GetAvailableTrainingOptionsAsync returns exactly 3 options
- [ ] Test: GetAvailableTrainingOptionsAsync includes recovery option when happiness < 50
- [ ] Test: GetAvailableTrainingOptionsAsync caches options with correct key
- [ ] Test: GetAvailableTrainingOptionsAsync returns cached options on second call
- [ ] Test: GetAvailableTrainingOptionsAsync generates new options after cache expires
- [ ] Test: Random selection doesn't include duplicates
- [ ] Test: Random selection is deterministic with fixed seed (for testing)

**Why these tests**: Ensure random selection works and caching prevents cheating.

#### GREEN - Make Tests Pass

1. **Add GetAvailableTrainingOptionsAsync to ITrainingService**:
   ```csharp
   Task<List<Training>> GetAvailableTrainingOptionsAsync(
       Guid horseId,
       Guid sessionId,
       CancellationToken cancellationToken = default);
   ```

2. **Implement random selection logic in TrainingService**:
   ```csharp
   public async Task<List<Training>> GetAvailableTrainingOptionsAsync(
       Guid horseId,
       Guid sessionId,
       CancellationToken cancellationToken = default)
   {
       var cacheKey = $"training:options:{horseId}:{sessionId}";

       // Try cache first
       var cached = await _cache.GetAsync<List<Training>>(cacheKey, cancellationToken);
       if (cached != null) return cached;

       // Cache miss: generate new options
       var horse = await _horseRepository.GetByIdWithStatsAsync(horseId, cancellationToken);
       var allTrainings = await _trainingRepository.GetAllAsync(cancellationToken);

       var options = GenerateRandomOptions(horse, allTrainings);

       // Cache for 30 minutes
       await _cache.SetAsync(cacheKey, options, TimeSpan.FromMinutes(30), cancellationToken);

       return options;
   }

   private List<Training> GenerateRandomOptions(Horse horse, List<Training> allTrainings)
   {
       var options = new List<Training>();
       var regular = allTrainings.Where(t => !t.IsRecovery).ToList();
       var recovery = allTrainings.Where(t => t.IsRecovery).ToList();

       // If happiness < 50, guarantee 1 recovery option
       if (horse.Happiness < 50 && recovery.Any())
       {
           options.Add(recovery[_randomGenerator.Next(recovery.Count)]);

           // Add 2 more random (non-duplicate)
           var remaining = regular.Where(t => !options.Contains(t)).ToList();
           options.AddRange(remaining.OrderBy(_ => _randomGenerator.Next()).Take(2));
       }
       else
       {
           // Random 3 from all
           options.AddRange(allTrainings.OrderBy(_ => _randomGenerator.Next()).Take(3));
       }

       return options;
   }
   ```

3. **Create ITrainingRepository interface** (`TripleDerby.Core/Abstractions/Repositories/ITrainingRepository.cs`):
   ```csharp
   public interface ITrainingRepository
   {
       Task<List<Training>> GetAllAsync(CancellationToken cancellationToken = default);
       Task<Training?> GetByIdAsync(byte id, CancellationToken cancellationToken = default);
   }
   ```

4. **Implement TrainingRepository** (`TripleDerby.Infrastructure/Repositories/TrainingRepository.cs`):
   - Standard EF Core repository implementation
   - GetAll with caching (trainings rarely change)

5. **Update unit tests**:
   - Mock IDistributedCache
   - Test cache hit/miss scenarios
   - Test recovery option logic

#### REFACTOR - Clean Up

- [ ] Extract GenerateRandomOptions into separate class (TrainingOptionGenerator)
- [ ] Add integration tests with real Redis instance
- [ ] Ensure cache keys follow consistent naming convention
- [ ] Add cache expiration configuration to appsettings

### Acceptance Criteria

- [ ] All random selection tests pass
- [ ] Cache hit returns same options without DB call
- [ ] Recovery option guaranteed when happiness < 50
- [ ] No duplicate training types in 3 options
- [ ] Cache expires after 30 minutes

**Deliverable**: Stable random training selection with Redis caching.

**Estimated Complexity**: Medium
**Risks**: Redis cache misses; ensure fallback works correctly.

---

## Phase 5: RabbitMQ Integration & Commands

**Goal**: Integrate RabbitMQ for async training execution following racing service pattern.

**Vertical Slice**: Training requests go through message queue for consistency and reliability.

### TDD Cycle

#### RED - Write Failing Tests

- [ ] Test: TrainHorseCommand has all required properties
- [ ] Test: TrainingConsumer receives command from queue
- [ ] Test: TrainingConsumer calls TrainingService.TrainAsync
- [ ] Test: TrainingConsumer handles exceptions and retries
- [ ] Test: TrainingController publishes TrainHorseCommand to queue
- [ ] Test: TrainingController returns 202 Accepted

**Why these tests**: Ensure async messaging works correctly with error handling.

#### GREEN - Make Tests Pass

1. **Create TrainHorseCommand** (`TripleDerby.Messaging/Commands/TrainHorseCommand.cs`):
   ```csharp
   public record TrainHorseCommand
   {
       public Guid HorseId { get; init; }
       public byte TrainingId { get. init; }
       public Guid SessionId { get. init; }  // Links to cached options
       public Guid UserId { get; init; }
   }
   ```

2. **Create TrainingConsumer** (`TripleDerby.Services.Training/Consumers/TrainingConsumer.cs`):
   ```csharp
   public class TrainingConsumer : IConsumer<TrainHorseCommand>
   {
       private readonly ITrainingService _trainingService;
       private readonly ILogger<TrainingConsumer> _logger;

       public async Task Consume(ConsumeContext<TrainHorseCommand> context)
       {
           var command = context.Message;

           try
           {
               _logger.LogInformation(
                   "Training horse {HorseId} with training {TrainingId}",
                   command.HorseId, command.TrainingId);

               var result = await _trainingService.TrainAsync(
                   command.HorseId,
                   command.TrainingId,
                   context.CancellationToken);

               _logger.LogInformation(
                   "Training completed. Session: {SessionId}, Gains: Speed={Speed}, Stamina={Stamina}",
                   result.SessionId, result.SpeedGain, result.StaminaGain);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Training failed for horse {HorseId}", command.HorseId);
               throw; // Let RabbitMQ retry
           }
       }
   }
   ```

3. **Create TrainingController** (`TripleDerby.Api/Controllers/TrainingController.cs`):
   ```csharp
   [ApiController]
   [Route("api/training")]
   public class TrainingController : ControllerBase
   {
       private readonly IPublishEndpoint _publisher;
       private readonly ITrainingService _trainingService;

       [HttpGet("options/{horseId}")]
       public async Task<ActionResult<List<Training>>> GetTrainingOptions(
           Guid horseId,
           [FromQuery] Guid sessionId)
       {
           var options = await _trainingService.GetAvailableTrainingOptionsAsync(
               horseId, sessionId);
           return Ok(options);
       }

       [HttpPost("train")]
       public async Task<IActionResult> TrainHorse([FromBody] TrainHorseRequest request)
       {
           var command = new TrainHorseCommand
           {
               HorseId = request.HorseId,
               TrainingId = request.TrainingId,
               SessionId = request.SessionId,
               UserId = User.GetUserId()
           };

           await _publisher.Publish(command);

           return Accepted(); // 202 Accepted
       }

       [HttpGet("history/{horseId}")]
       public async Task<ActionResult<List<TrainingSession>>> GetTrainingHistory(
           Guid horseId,
           [FromQuery] int limit = 20)
       {
           var history = await _trainingService.GetTrainingHistoryAsync(horseId, limit);
           return Ok(history);
       }
   }
   ```

4. **Register consumer in Program.cs**:
   ```csharp
   services.AddMassTransit(x =>
   {
       x.AddConsumer<TrainingConsumer>();
       // ... existing consumers

       x.UsingRabbitMq((context, cfg) =>
       {
           cfg.ReceiveEndpoint("training-queue", e =>
           {
               e.ConfigureConsumer<TrainingConsumer>(context);
           });
       });
   });
   ```

5. **Create integration tests** (`TripleDerby.Tests.Integration/Training/TrainingFlowTests.cs`):
   - Test end-to-end: API → RabbitMQ → Consumer → Service → Database
   - Use test containers for RabbitMQ and database

#### REFACTOR - Clean Up

- [ ] Extract request validation into FluentValidation validators
- [ ] Add retry policy configuration for consumer
- [ ] Add dead-letter queue handling for failed training commands
- [ ] Ensure correlation IDs for distributed tracing

### Acceptance Criteria

- [ ] Training command successfully publishes to RabbitMQ
- [ ] Consumer processes command and executes training
- [ ] Integration tests pass with real message queue
- [ ] Retries work correctly on transient failures
- [ ] API returns 202 Accepted immediately (async)

**Deliverable**: Async training execution via RabbitMQ.

**Estimated Complexity**: High
**Risks**: RabbitMQ configuration; ensure consumer registration correct.

---

## Phase 6: Race Integration (Reset Training Flag)

**Goal**: Integrate training system with race completion to reset `HasTrainedSinceLastRace` flag.

**Vertical Slice**: Training → Race → Training flow works correctly.

### TDD Cycle

#### RED - Write Failing Tests

- [ ] Test: RaceExecutor resets HasTrainedSinceLastRace after race completion
- [ ] Test: Can train again after completing a race
- [ ] Test: Cannot train twice before racing
- [ ] Test: Integration test: Train → Race → Train succeeds
- [ ] Test: Integration test: Train → Train fails

**Why these tests**: Ensure training frequency limit enforced correctly.

#### GREEN - Make Tests Pass

1. **Update RaceExecutor.DetermineRaceResults()** (`TripleDerby.Services.Racing/RaceExecutor.cs`):
   ```csharp
   private void DetermineRaceResults(RaceRun raceRun)
   {
       // ... existing place assignment logic

       // ... existing stat progression logic

       // ... existing happiness logic

       // NEW: Reset training flag for all horses
       foreach (var raceRunHorse in raceRun.Horses)
       {
           raceRunHorse.Horse.HasTrainedSinceLastRace = false;
       }
   }
   ```

2. **Create integration test** (`TripleDerby.Tests.Integration/Training/TrainingRaceIntegrationTests.cs`):
   ```csharp
   [Fact]
   public async Task Training_Race_Training_Flow_Works()
   {
       // Arrange: Create horse
       var horse = await CreateTestHorse();

       // Act 1: Train horse
       var training1 = await TrainHorse(horse.Id, trainingId: 1);
       Assert.True(training1.Success);

       // Assert 1: Cannot train again
       var training2 = await TrainHorse(horse.Id, trainingId: 2);
       Assert.False(training2.Success);
       Assert.Contains("already trained", training2.Message);

       // Act 2: Run race
       await ExecuteRace(horse.Id);

       // Act 3: Train again
       var training3 = await TrainHorse(horse.Id, trainingId: 3);
       Assert.True(training3.Success);
   }
   ```

3. **Update TrainingService validation**:
   - Ensure CanTrain checks HasTrainedSinceLastRace flag
   - Return clear error message when already trained

#### REFACTOR - Clean Up

- [ ] Extract flag reset logic if DetermineRaceResults grows too large
- [ ] Add logging for training flag resets
- [ ] Consider event-driven approach (RaceCompletedEvent → ResetTrainingFlagHandler) for future

### Acceptance Criteria

- [ ] HasTrainedSinceLastRace resets after every race
- [ ] Integration test passes: Train → Race → Train
- [ ] Cannot train twice without racing
- [ ] Error message clear when attempting duplicate training

**Deliverable**: Training frequency limit works across race cycles.

**Estimated Complexity**: Simple
**Risks**: Low; straightforward integration point.

---

## Phase 7: UI Implementation & Polish

**Goal**: Create training menu UI, results display, and training history views.

**Vertical Slice**: Players can access full training system through UI.

### TDD Cycle

#### RED - Write E2E Tests (if applicable)

- [ ] Test: Training menu displays 3 random training options
- [ ] Test: Training menu shows current horse stats
- [ ] Test: Training menu indicates which stats are at ceiling
- [ ] Test: Clicking training option sends request and shows loading state
- [ ] Test: Training results display stat changes with animations
- [ ] Test: Training history page shows past sessions
- [ ] Test: LegType bonus indicator shows when training matches

**Why these tests**: Ensure UI flows work end-to-end.

#### GREEN - Make UI Work

1. **Create Training Menu Component** (`client/src/components/Training/TrainingMenu.tsx`):
   - Fetch horse with stats
   - Fetch 3 training options (with sessionId)
   - Display training cards with stat effects, happiness cost, overwork risk
   - Highlight LegType bonus if applicable
   - Show "At Ceiling" badges for maxed stats
   - Handle training selection → publish command → show loading

2. **Create Training Results Component** (`client/src/components/Training/TrainingResults.tsx`):
   - Display training name
   - Show before/after stats with animations
   - Highlight stat gains (green) and happiness loss (yellow/red)
   - Show overwork warning if occurred
   - Show LegType bonus message if applicable
   - "Train Again" button (disabled if already trained)

3. **Create Training History Component** (`client/src/components/Training/TrainingHistory.tsx`):
   - Fetch training history for horse
   - Display table: Date, Training Type, Stat Changes, Happiness Impact
   - Filter/sort options
   - Pagination (20 per page)

4. **Add navigation to Training Menu**:
   - Add "Train" button to horse detail page
   - Check if training available (not already trained)
   - Show tooltip if training unavailable

5. **Add visual polish**:
   - Stat change animations (numbers count up)
   - Happiness bar with color gradient (green→yellow→red)
   - Training type icons
   - LegType badge/icon
   - Overwork warning icon

#### REFACTOR - Clean Up

- [ ] Extract reusable stat display component
- [ ] Extract reusable training card component
- [ ] Add loading skeletons
- [ ] Add error boundaries
- [ ] Optimize re-renders with React.memo

### Acceptance Criteria

- [ ] Training menu loads and displays 3 options
- [ ] Training execution works end-to-end from UI
- [ ] Results display correctly with animations
- [ ] Training history loads and displays past sessions
- [ ] UI is responsive and accessible
- [ ] Loading states and error handling work

**Deliverable**: Full training system accessible through UI.

**Estimated Complexity**: Medium-High
**Risks**: UI complexity; ensure good UX for stat changes and feedback.

---

## Implementation Order Summary

| Phase | Focus | Time Estimate | Risk Level |
|-------|-------|---------------|------------|
| 1 | Data Model & Config | 2-3 hours | Low |
| 2 | TrainingCalculator | 3-4 hours | Medium |
| 3 | TrainingService | 4-5 hours | High |
| 4 | Random Options & Redis | 2-3 hours | Medium |
| 5 | RabbitMQ Integration | 3-4 hours | High |
| 6 | Race Integration | 1-2 hours | Low |
| 7 | UI & Polish | 4-6 hours | Medium |
| **Total** | **All Phases** | **19-27 hours** | **Medium-High** |

---

## Testing Strategy

### Unit Tests
- **TrainingCalculator**: 25+ tests (all formulas, edge cases)
- **TrainingService**: 15+ tests (orchestration, validation)
- **Random selection**: 5+ tests (distribution, caching)

### Integration Tests
- **Database**: Entity changes, migrations
- **RabbitMQ**: Command publishing, consumer processing
- **Redis**: Cache hit/miss scenarios
- **Race Integration**: Training → Race → Training flow

### E2E Tests
- **UI flows**: Training menu → selection → results → history
- **Error scenarios**: Low happiness, already trained, at ceiling

### Performance Tests
- **Training execution**: < 100ms target
- **Random selection with cache**: < 10ms
- **History queries**: < 50ms for 20 records

---

## Rollout Plan

### Phase 1-2: Internal Testing
- Deploy to dev environment
- Test calculator formulas match specification
- Validate database schema

### Phase 3-4: Alpha Testing
- Deploy core training execution
- Test with small group of users
- Monitor RabbitMQ queue performance
- Validate Redis caching works

### Phase 5-6: Beta Testing
- Deploy race integration
- Test training → race → training cycles
- Monitor for edge cases

### Phase 7: Production Release
- Deploy UI
- Monitor usage patterns
- Collect feedback on training balance
- Tune happiness costs and stat gains if needed

---

## Risks & Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Training gains too powerful | Medium | High | Monitor stat growth rates; ready to adjust BaseTrainingGrowthRate |
| RabbitMQ consumer fails silently | Low | High | Comprehensive logging, dead-letter queue, monitoring alerts |
| Redis cache issues | Medium | Medium | Fallback to database if cache unavailable |
| Overwork system too punishing | Medium | Medium | Start conservative (low risk values), adjust based on feedback |
| LegType bonuses create imbalance | Low | Medium | Make it "Should Have" so can deploy MVP without it first |
| Performance degradation | Low | High | Load testing before release; optimize queries |

---

## Dependencies

### External Services
- ✅ RabbitMQ (already configured)
- ✅ Redis (already configured)
- ✅ PostgreSQL (EF Core migrations)

### Internal Features
- ✅ Feature 018 (Race Stat Progression) - provides pattern
- ✅ Happiness stat system
- ✅ Feeding system (integrates with happiness)

### NuGet Packages (already installed)
- MassTransit.RabbitMQ
- StackExchange.Redis
- Entity Framework Core

---

## Post-MVP Enhancements

### Phase 8 (Future): LegType Bonuses
- Add LegType bonus calculation to TrainingCalculator
- Update UI to show bonus indicator
- Add tests for all 5 LegType × 10 Training combinations

### Phase 9 (Future): Advanced Features
- Mini-games for training
- Training equipment system
- Hired trainers (specialists)
- Training analytics/graphs
- Overwork penalties for maxed stats

---

## Success Metrics

### Technical Metrics
- [ ] All unit tests pass (60+ tests)
- [ ] Integration tests pass (10+ scenarios)
- [ ] Code coverage > 80% for new code
- [ ] Training execution < 100ms
- [ ] Zero data loss or corruption

### Business Metrics
- [ ] Players use training system (>50% of active users)
- [ ] Training sessions per race ratio (target: 0.8-1.0)
- [ ] Happiness management effective (players use recovery options)
- [ ] No major balance complaints in first 2 weeks

### Quality Metrics
- [ ] Zero critical bugs in first week
- [ ] < 5 minor bugs reported
- [ ] Positive user feedback on UX
- [ ] Performance meets targets

---

**Plan Status**: Ready for implementation
**Next Step**: Begin Phase 1 (Data Model & Configuration)
**Estimated Completion**: 3-4 working days (full-time) or 1-2 weeks (part-time)
