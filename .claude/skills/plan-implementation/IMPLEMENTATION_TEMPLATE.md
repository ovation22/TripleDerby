# [Feature Name] - Implementation Plan

## Overview
**Feature**: [Link to feature spec in /docs/features/]
**Approach**: TDD with Vertical Slices
**Total Phases**: [N]
**Estimated Complexity**: [Simple / Medium / Complex]

## Summary
[2-3 paragraph summary of the implementation approach, highlighting key decisions and the overall strategy]

---

## Prerequisites
Before starting implementation:
- [ ] Feature specification approved
- [ ] All open questions resolved
- [ ] Technical approach validated
- [ ] Codebase exploration complete
- [ ] Test infrastructure ready (or Phase 1 sets it up)

---

## Implementation Strategy

### Vertical Slicing Approach
[Explain how the feature is broken into vertical slices - what end-to-end capabilities are delivered in what order]

### TDD Methodology
Each phase follows Red-Green-Refactor:
1. **RED**: Write failing tests that define desired behavior
2. **GREEN**: Write minimal code to make tests pass
3. **REFACTOR**: Clean up code while keeping tests green
4. **COMMIT**: Commit after each complete cycle

### Risk Mitigation
[Key risks and how the phase sequencing addresses them]

---

## Phase Breakdown

### Phase 1: [Phase Name]
**Goal**: [One sentence describing what this phase delivers]

**Vertical Slice**: [What end-to-end capability does this enable?]

**Estimated Complexity**: [Simple / Medium / Complex]

#### RED - Write Failing Tests
**Test File(s)**: `[path/to/test/file.cs]`

- [ ] Test: [Specific test case describing expected behavior]
  - **Given**: [Initial conditions]
  - **When**: [Action taken]
  - **Then**: [Expected result]
- [ ] Test: [Another test case]
- [ ] Test: [Edge case test]

**Why these tests**: [Explain what behavior we're defining and why]

#### GREEN - Make Tests Pass
**Files to Create**:
- `[path/to/new/file.cs]` - [Purpose]

**Files to Modify**:
- `[path/to/existing/file.cs]` - [What changes]

**Implementation Tasks**:
- [ ] Create `[ClassName]` with properties: [list key properties]
- [ ] Implement `[MethodName]([params])` to [what it does]
- [ ] Wire up `[Component]` to `[OtherComponent]` via [how]
- [ ] Add validation for [what input/rules]
- [ ] Handle edge case: [describe]

**Implementation Notes**:
- [Key decisions or patterns to follow]
- [Gotchas or things to watch out for]
- [References to existing code patterns to follow]

#### REFACTOR - Clean Up
- [ ] Extract `[MethodName]` for [reason - clarity, reuse, testing]
- [ ] Rename `[variable/method]` to `[better name]` for clarity
- [ ] Remove duplication between `[Place1]` and `[Place2]`
- [ ] Consolidate `[related items]` into `[single concept]`
- [ ] Apply `[pattern name]` pattern for `[benefit]`

**Refactoring Focus**: [What quality we're improving - readability, testability, performance]

#### Acceptance Criteria
- [ ] All tests in this phase pass
- [ ] Code coverage for new code ≥ 80%
- [ ] [Observable behavior]: [What you can demonstrate/see working]
- [ ] No regressions: Existing tests still pass
- [ ] [Additional criteria specific to this phase]

**Deliverable**: [Concrete description of working functionality]

**Dependencies**: [What must be complete before this phase, or "None - can start immediately"]

**Risks/Unknowns**: [Any concerns, or "None identified"]

---

### Phase 2: [Phase Name]
[Same structure as Phase 1]

---

### Phase 3: [Phase Name]
[Same structure as Phase 1]

---

## Testing Strategy

### Unit Tests
**Focus**: Individual components in isolation
- [Component 1]: [What aspects are tested]
- [Component 2]: [What aspects are tested]
- [Component 3]: [What aspects are tested]

**Coverage Goal**: >80% for all new code

### Integration Tests
**Focus**: Components working together
- [Integration point 1]: [What interaction is tested]
- [Integration point 2]: [What interaction is tested]

**Coverage Goal**: All major workflows tested end-to-end

### Performance Tests (if applicable)
- [Performance requirement 1]: [How it's validated]
- [Performance requirement 2]: [How it's validated]

### Manual Testing Checklist
After all phases complete:
- [ ] [User scenario 1]
- [ ] [User scenario 2]
- [ ] [Edge case scenario]

---

## Files Roadmap

### New Files to Create
**Test Files** (created first):
- `[path/to/test1.cs]` - Tests for [component]
- `[path/to/test2.cs]` - Tests for [component]

**Implementation Files** (created to make tests pass):
- `[path/to/implementation1.cs]` - [Purpose]
- `[path/to/implementation2.cs]` - [Purpose]

**Configuration/Data Files**:
- `[path/to/config.cs]` - [Purpose]
- `[path/to/migration.cs]` - [Purpose]

### Existing Files to Modify
- `[path/to/existing1.cs]` - [What changes and why]
- `[path/to/existing2.cs]` - [What changes and why]

### Files to Delete (if refactoring)
- `[path/to/deprecated.cs]` - [Why it's no longer needed]

---

## Milestones

### Milestone 1: [Name] (After Phase N)
**What's Working**: [Observable functionality]
**Tests Passing**: [Which test suites]
**Demo**: [What can be shown to stakeholders]

### Milestone 2: [Name] (After Phase N)
[Same structure]

### Milestone 3: Feature Complete (After Final Phase)
**What's Working**: Full feature functionality
**Tests Passing**: All unit and integration tests
**Demo**: Complete feature walkthrough
**Ready For**: [Production / Beta / Review]

---

## Phase Progression Workflow

### Starting a Phase
1. Review phase goals and acceptance criteria
2. Ensure prerequisites are met
3. Add phase tasks to TodoWrite
4. Begin with RED: Write first failing test

### During a Phase
1. Follow Red-Green-Refactor cycles
2. Mark todos complete as you go
3. Commit after each complete cycle
4. Keep tests green (never commit failing tests)

### Completing a Phase
1. Verify all acceptance criteria met
2. Run all tests (unit + integration)
3. Mark phase complete
4. Add next phase tasks to TodoWrite
5. Commit: "Complete [Phase Name]: [Deliverable]"

---

## Dependencies & Prerequisites

### External Dependencies
- [Library/package name]: [Version, purpose]

### Internal Dependencies
- [Existing component]: [How it's used]
- [Existing service]: [What it provides]

### Database Changes
- [Migration name]: [What it adds/changes]

### Configuration Required
- [Setting name]: [What value, where to set it]

---

## Risk Management

### High-Risk Areas
**Risk**: [Description of risk]
**Mitigation**: [How phase sequencing or approach addresses it]
**Phase**: Addressed in Phase [N]

### Technical Unknowns
**Unknown**: [What we're not sure about]
**Investigation**: [How/when we'll figure it out]
**Phase**: Explored in Phase [N]

### Integration Risks
**Risk**: [Could break existing feature X]
**Mitigation**: [Characterization tests, gradual rollout, etc.]
**Validation**: [How we verify no breakage]

---

## Success Criteria

### Feature Complete When:
- [ ] All phases implemented
- [ ] All tests passing (unit + integration)
- [ ] Code coverage ≥ 80% for new code
- [ ] No regressions in existing functionality
- [ ] All acceptance criteria met
- [ ] Code reviewed and approved
- [ ] Documentation updated

### Quality Metrics:
- **Test Coverage**: [Target %]
- **Performance**: [Benchmarks that must be met]
- **Code Quality**: [Standards - naming, patterns, documentation]

---

## Post-Implementation

### Follow-Up Work (Out of Scope)
- [Future enhancement 1]
- [Future enhancement 2]
- [Technical debt to address later]

### Monitoring & Validation
- [Metrics to track]
- [Behaviors to observe]
- [How to validate in production]

---

## Notes

- **TDD Discipline**: Always write tests before implementation
- **Small Commits**: Commit after each Red-Green-Refactor cycle
- **Keep Tests Green**: Never leave tests failing
- **Refactor Fearlessly**: Tests give confidence to improve code
- **Ask Questions**: If unclear, clarify before proceeding

---

## Document History
- **Created**: [Date] by [Author]
- **Last Updated**: [Date]
- **Status**: [Draft / Approved / In Progress / Complete]
