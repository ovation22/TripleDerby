# Unit of Work Pattern - Implementation Plan

## Overview

**Feature**: [Feature 025: Unit of Work Pattern](../features/025-unit-of-work-pattern.md)
**Approach**: TDD with Vertical Slices
**Total Phases**: 4

## Summary

Replace `repository.ExecuteInTransactionAsync()` with a dedicated `IUnitOfWork` abstraction following the standard Unit of Work pattern. Each phase delivers working, tested functionality: first the core infrastructure, then DI registration, then migrating request processors, and finally cleaning up old code. All repository methods continue to auto-save, keeping the implementation straightforward while achieving better separation of concerns.

---

## Phase 1: Core Infrastructure - IUnitOfWork with Tests

**Goal**: Create and test the IUnitOfWork abstraction with full transaction lifecycle support

**Vertical Slice**: Working Unit of Work that can execute operations in transactions with automatic commit/rollback

**Duration**: 45-60 minutes

### RED - Write Failing Tests

**Test File**: `TripleDerby.Tests.Unit/Infrastructure/Data/UnitOfWorkTests.cs`

- [ ] Test: `ExecuteAsync_SuccessfulOperation_CommitsTransaction` - Given a successful operation, when ExecuteAsync is called, then transaction is committed
- [ ] Test: `ExecuteAsync_OperationThrowsException_RollsBackTransaction` - Given operation throws, when ExecuteAsync is called, then transaction rolls back and exception propagates
- [ ] Test: `ExecuteAsync_WithResult_ReturnsResultAndCommits` - Given operation returns value, when ExecuteAsync is called, then result is returned and transaction commits
- [ ] Test: `ExecuteAsync_NullOperation_ThrowsArgumentNullException` - Given null operation, when ExecuteAsync is called, then ArgumentNullException is thrown
- [ ] Test: `BeginTransactionAsync_WhenNoTransaction_StartsTransaction` - Given no active transaction, when BeginTransactionAsync is called, then transaction starts successfully
- [ ] Test: `BeginTransactionAsync_WhenTransactionActive_ThrowsInvalidOperationException` - Given active transaction, when BeginTransactionAsync called again, then throws InvalidOperationException
- [ ] Test: `CommitAsync_WhenNoTransaction_ThrowsInvalidOperationException` - Given no active transaction, when CommitAsync is called, then throws InvalidOperationException
- [ ] Test: `RollbackAsync_WhenNoTransaction_ThrowsInvalidOperationException` - Given no active transaction, when RollbackAsync is called, then throws InvalidOperationException

### GREEN - Make Tests Pass

**Files to Create**:
- `TripleDerby.Core/Abstractions/Data/IUnitOfWork.cs` - Interface defining transaction lifecycle
- `TripleDerby.Infrastructure/Data/UnitOfWork.cs` - EF Core implementation wrapping DbContext transactions
- `TripleDerby.Tests.Unit/Infrastructure/Data/UnitOfWorkTests.cs` - Unit tests

**Tasks**:
- [ ] Create `IUnitOfWork` interface with 5 methods: BeginTransactionAsync, CommitAsync, RollbackAsync, ExecuteAsync, ExecuteAsync<T>
- [ ] Implement `UnitOfWork` class that wraps `DbContext.Database.BeginTransactionAsync()`
- [ ] Add proper null checks, exception handling, and logging
- [ ] Ensure transaction disposal happens in finally blocks
- [ ] Write all unit tests using mock DbContext

### REFACTOR - Clean Up

- [ ] Ensure proper async disposal of transactions
- [ ] Verify logging is informative (debug for start/commit, warning for rollback, error for failures)
- [ ] Check XML documentation comments are complete
- [ ] Verify consistent exception handling pattern

### Acceptance Criteria

- [ ] All 8 unit tests pass
- [ ] `IUnitOfWork` interface is clean and well-documented
- [ ] `UnitOfWork` implementation handles all error cases
- [ ] Transaction lifecycle is correct (begin → commit/rollback → dispose)
- [ ] No memory leaks (transactions properly disposed)

**Deliverable**: Fully tested Unit of Work infrastructure ready for DI registration

---

## Phase 2: DI Registration and Infrastructure Setup

**Goal**: Register IUnitOfWork in all microservices and API, ready for use

**Vertical Slice**: All applications have IUnitOfWork available for dependency injection

**Duration**: 20-30 minutes

### RED - Write Failing Tests

**Test Files**: Build verification (no new unit tests needed, but we'll verify compilation)

- [ ] Verify: All microservices compile after adding IUnitOfWork registration
- [ ] Verify: DI container can resolve IUnitOfWork in each microservice

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Services.Breeding/Program.cs` - Add IUnitOfWork registration
- `TripleDerby.Services.Training/Program.cs` - Add IUnitOfWork registration
- `TripleDerby.Services.Feeding/Program.cs` - Add IUnitOfWork registration
- `TripleDerby.Services.Racing/Program.cs` - Add IUnitOfWork registration
- `TripleDerby.Api/Program.cs` - Add IUnitOfWork registration

**Tasks**:
- [ ] Add `builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();` after DbContext registration in Breeding
- [ ] Add `builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();` after DbContext registration in Training
- [ ] Add `builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();` after DbContext registration in Feeding
- [ ] Add `builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();` after DbContext registration in Racing
- [ ] Add `builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();` after DbContext registration in API
- [ ] Add necessary using statements: `using TripleDerby.Core.Abstractions.Data;`
- [ ] Build solution to verify no compilation errors

### REFACTOR - Clean Up

- [ ] Ensure consistent placement of registration (right after DbContext in all files)
- [ ] Verify all necessary using statements are present
- [ ] Check for any DI container warnings

### Acceptance Criteria

- [ ] Solution builds without errors
- [ ] IUnitOfWork registered in all 5 applications (4 microservices + API)
- [ ] Registration uses correct lifetime (Scoped)
- [ ] No DI container warnings or errors

**Deliverable**: All applications can inject and use IUnitOfWork

---

## Phase 3: Migrate Request Processors to IUnitOfWork

**Goal**: Update Breeding, Training, and Feeding processors to use IUnitOfWork instead of repository.ExecuteInTransactionAsync

**Vertical Slice**: All 3 request processors use the new Unit of Work pattern

**Duration**: 45-60 minutes

### RED - Write Failing Tests

**Test Files**: Existing integration tests should still pass after migration

- [ ] Verify: `BreedingRequestProcessorTests` - ensure existing tests are identified
- [ ] Verify: `TrainingRequestProcessorTests` - ensure existing tests are identified
- [ ] Verify: `FeedingRequestProcessorTests` - ensure existing tests are identified
- [ ] Note: These are verification steps - tests should continue passing after changes

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Services.Breeding/BreedingRequestProcessor.cs` - Inject IUnitOfWork, replace ExecuteInTransactionAsync
- `TripleDerby.Services.Training/TrainingRequestProcessor.cs` - Inject IUnitOfWork, replace ExecuteInTransactionAsync
- `TripleDerby.Services.Feeding/FeedingRequestProcessor.cs` - Inject IUnitOfWork, replace ExecuteInTransactionAsync

**Tasks**:

**BreedingRequestProcessor**:
- [ ] Add `private readonly IUnitOfWork _unitOfWork;` field
- [ ] Add `IUnitOfWork unitOfWork` parameter to constructor
- [ ] Add null check: `_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));`
- [ ] Replace `await repository.ExecuteInTransactionAsync(async () =>` with `await _unitOfWork.ExecuteAsync(async () =>`
- [ ] Add using statement: `using TripleDerby.Core.Abstractions.Data;`
- [ ] Verify method still returns the correct result type

**TrainingRequestProcessor**:
- [ ] Add `private readonly IUnitOfWork _unitOfWork;` field
- [ ] Add `IUnitOfWork unitOfWork` parameter to constructor
- [ ] Add null check: `_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));`
- [ ] Replace `await repository.ExecuteInTransactionAsync(async () =>` with `await _unitOfWork.ExecuteAsync(async () =>`
- [ ] Add using statement: `using TripleDerby.Core.Abstractions.Data;`
- [ ] Verify method still returns the correct result type

**FeedingRequestProcessor**:
- [ ] Add `private readonly IUnitOfWork _unitOfWork;` field
- [ ] Add `IUnitOfWork unitOfWork` parameter to constructor
- [ ] Add null check: `_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));`
- [ ] Replace `await repository.ExecuteInTransactionAsync(async () =>` with `await _unitOfWork.ExecuteAsync(async () =>`
- [ ] Add using statement: `using TripleDerby.Core.Abstractions.Data;`
- [ ] Verify method still returns the correct result type

### REFACTOR - Clean Up

- [ ] Ensure consistent field naming (`_unitOfWork` in all processors)
- [ ] Verify constructor parameter order is logical
- [ ] Check that all null checks are present
- [ ] Ensure proper using statements

### Acceptance Criteria

- [ ] All 3 processors successfully inject IUnitOfWork
- [ ] All 3 processors use `_unitOfWork.ExecuteAsync` instead of `repository.ExecuteInTransactionAsync`
- [ ] Solution compiles without errors
- [ ] Existing tests still pass (integration tests verify transactional behavior)
- [ ] No behavioral changes - transactions work identically

**Deliverable**: All request processors migrated to Unit of Work pattern with passing tests

---

## Phase 4: Remove Legacy Transaction Methods

**Goal**: Remove ExecuteInTransactionAsync from repository interface and implementation

**Vertical Slice**: Clean codebase with no references to old transaction methods

**Duration**: 20-30 minutes

### RED - Write Failing Tests

**Verification Steps** (no new tests, but verify removal):
- [ ] Verify: Grep for `ExecuteInTransactionAsync` returns only doc file references
- [ ] Verify: Solution compiles after removing methods
- [ ] Verify: All existing tests still pass

### GREEN - Make Tests Pass

**Files to Modify**:
- `TripleDerby.Core/Abstractions/Repositories/IEFRepository.cs` - Remove transaction methods
- `TripleDerby.Infrastructure/Data/Repositories/EFRepository.cs` - Remove transaction implementations

**Tasks**:

**Remove from IEFRepository.cs**:
- [ ] Delete method: `Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);` (lines ~330)
- [ ] Delete method: `Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);` (lines ~340)
- [ ] Delete XML documentation for both methods

**Remove from EFRepository.cs**:
- [ ] Delete entire `ExecuteInTransactionAsync(Func<Task> operation, ...)` method (lines ~320-345)
- [ ] Delete entire `ExecuteInTransactionAsync<T>(Func<Task<T>> operation, ...)` method (lines ~351-377)
- [ ] Remove any related XML documentation comments

**Verification**:
- [ ] Build solution - should compile successfully
- [ ] Run all tests - should pass
- [ ] Search codebase for "ExecuteInTransactionAsync" - should only find doc references

### REFACTOR - Clean Up

- [ ] Remove any orphaned using statements from modified files
- [ ] Verify no empty regions or unnecessary whitespace
- [ ] Check git diff to ensure only intended lines removed

### Acceptance Criteria

- [ ] `ExecuteInTransactionAsync` methods removed from IEFRepository interface
- [ ] `ExecuteInTransactionAsync` implementations removed from EFRepository class
- [ ] Solution builds without errors
- [ ] All tests pass
- [ ] No compilation warnings
- [ ] Grep search confirms no code references remain (only docs)

**Deliverable**: Clean codebase with Unit of Work pattern fully implemented and legacy code removed

---

## Files Summary

### New Files
- `TripleDerby.Core/Abstractions/Data/IUnitOfWork.cs` - Unit of Work interface
- `TripleDerby.Infrastructure/Data/UnitOfWork.cs` - EF Core Unit of Work implementation
- `TripleDerby.Tests.Unit/Infrastructure/Data/UnitOfWorkTests.cs` - Unit tests for UnitOfWork

### Modified Files
- `TripleDerby.Services.Breeding/Program.cs` - Register IUnitOfWork
- `TripleDerby.Services.Training/Program.cs` - Register IUnitOfWork
- `TripleDerby.Services.Feeding/Program.cs` - Register IUnitOfWork
- `TripleDerby.Services.Racing/Program.cs` - Register IUnitOfWork
- `TripleDerby.Api/Program.cs` - Register IUnitOfWork
- `TripleDerby.Services.Breeding/BreedingRequestProcessor.cs` - Use IUnitOfWork
- `TripleDerby.Services.Training/TrainingRequestProcessor.cs` - Use IUnitOfWork
- `TripleDerby.Services.Feeding/FeedingRequestProcessor.cs` - Use IUnitOfWork
- `TripleDerby.Core/Abstractions/Repositories/IEFRepository.cs` - Remove ExecuteInTransactionAsync
- `TripleDerby.Infrastructure/Data/Repositories/EFRepository.cs` - Remove ExecuteInTransactionAsync

---

## Milestones

| Milestone | After Phase | What's Working |
|-----------|-------------|----------------|
| Core Infrastructure | Phase 1 | IUnitOfWork interface and implementation fully tested |
| DI Ready | Phase 2 | All applications can inject IUnitOfWork |
| Migration Complete | Phase 3 | All request processors using Unit of Work pattern |
| Feature Complete | Phase 4 | Legacy code removed, clean implementation |

---

## Risks

| Risk | Mitigation | Phase |
|------|------------|-------|
| Tests might fail after migration | Keep transaction semantics identical; ExecuteAsync is drop-in replacement | 3 |
| Missing DI registration breaks runtime | Register in Phase 2 before usage in Phase 3 | 2 |
| Removing old code too early | Keep ExecuteInTransactionAsync until all usages migrated | 4 |
| Integration test failures | Verify transactional behavior is preserved | 3 |

---

## Testing Strategy

### Unit Tests (Phase 1)
- Mock DbContext and IDbContextTransaction
- Test all transaction lifecycle methods
- Test error cases and edge cases
- Verify proper disposal

### Build Verification (Phase 2)
- Ensure solution compiles
- Verify DI registration is correct

### Integration Tests (Phase 3)
- Run existing request processor tests
- Verify transactions commit on success
- Verify transactions rollback on failure
- Ensure no behavioral regressions

### Regression Testing (Phase 4)
- Run full test suite
- Verify no compilation errors
- Grep for remaining references

---

## Success Criteria

- [ ] All 4 phases completed successfully
- [ ] IUnitOfWork interface and implementation created and tested
- [ ] All 8 unit tests for UnitOfWork pass
- [ ] IUnitOfWork registered in all 5 applications
- [ ] All 3 request processors migrated to IUnitOfWork
- [ ] ExecuteInTransactionAsync completely removed from repository
- [ ] All existing tests still pass
- [ ] No compilation errors or warnings
- [ ] Code is cleaner with better separation of concerns
- [ ] Transaction behavior unchanged (commits on success, rolls back on failure)

---

## Phase Execution Notes

**IMPORTANT**: STOP after each phase for user review and approval before proceeding.

After completing each phase:
1. Run tests to verify everything works
2. Report results to user
3. **STOP and WAIT** for user review
4. Ask: "Would you like me to commit these changes?"
5. Wait for approval before committing
6. Wait for approval before starting next phase

Do not proceed to the next phase without explicit user approval.

---

## Implementation Workflow

```
Phase 1 (Core) → Review → Commit →
Phase 2 (DI) → Review → Commit →
Phase 3 (Migrate) → Review → Commit →
Phase 4 (Cleanup) → Review → Commit → Done
```

---

## References

- [Feature Specification](../features/025-unit-of-work-pattern.md)
- [Unit of Work Pattern - DevIQ](https://deviq.com/design-patterns/unit-of-work-pattern)
- [EF Core Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
