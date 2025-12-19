# Implementation Workflow Guide

A guide for going from idea → feature spec → implementation plan → working code.

## Overview

This project uses two Claude Code skills to streamline feature development:

1. **feature-discovery**: Understand requirements and create feature specifications
2. **plan-implementation**: Break down specs into actionable implementation phases

## The Complete Workflow

```
Idea/Requirement
      ↓
[feature-discovery skill]
      ↓
Feature Specification (docs/features/)
      ↓
[plan-implementation skill]
      ↓
Implementation Plan (docs/implementation/)
      ↓
TodoWrite Task List
      ↓
Implementation (TDD: Red-Green-Refactor)
      ↓
Working Feature ✅
```

---

## Step 1: Feature Discovery

### When to Use
- You have a new feature idea
- Requirements need clarification
- Complex feature needs design work
- You want to validate feasibility

### How to Use
```
User: "I want to add a horse training system"

or

User: "/feature-discovery"
```

### What Happens
1. Claude asks clarifying questions about requirements
2. Explores codebase to understand integration points
3. Identifies risks and technical challenges
4. Creates detailed feature specification

### Output
**Location**: `/docs/features/[feature-name].md`

**Contains**:
- Feature summary and goals
- Functional requirements
- Technical approach
- Success criteria
- Open questions

### Example Features
- [Race Engine](features/001-race-engine.md)
- [Core Race Simulation](features/001a-core-race-simulation.md)
- [Race Modifiers Refactor](features/race-modifiers-refactor.md)

---

## Step 2: Implementation Planning

### When to Use
- Feature spec is complete and approved
- Ready to start coding
- Need to break down complex feature
- Want TDD roadmap

### How to Use
```
User: "/plan-implementation for race-modifiers-refactor"

or

User: "I'm ready to implement the training system. Create an implementation plan."
```

### What Happens
1. Claude reads the feature spec
2. Analyzes codebase for patterns and integration points
3. Designs vertical slices (end-to-end capabilities)
4. Breaks into TDD phases (Red-Green-Refactor)
5. Sizes tasks appropriately (30-90 min each)
6. Creates implementation plan document
7. Populates TodoWrite with Phase 1 tasks

### Output
**Location**: `/docs/implementation/[feature-name]-implementation-plan.md`

**Contains**:
- Phase breakdown with concrete tasks
- Test strategy (unit + integration)
- Files to create/modify
- Risk mitigation approach
- Acceptance criteria per phase

**Also**: TodoWrite populated with first phase tasks

---

## Step 3: Implementation

### TDD Cycle (Per Task)

#### RED Phase (10-20 min)
1. Write failing test(s) for desired behavior
2. Run tests → Verify they fail
3. Confirm failure reason is correct

#### GREEN Phase (20-45 min)
1. Write minimal code to make tests pass
2. Don't optimize yet, just make it work
3. Run tests → Verify they pass

#### REFACTOR Phase (10-25 min)
1. Clean up code while keeping tests green
2. Extract methods, improve naming
3. Remove duplication
4. Run tests → Verify still passing

#### COMMIT
```bash
git add .
git commit -m "Add [feature component]: [what was accomplished]"
```

### Task Management

#### During a Phase
- [ ] Work through tasks in TodoWrite sequentially
- [ ] Mark task complete immediately after finishing
- [ ] Commit after each Red-Green-Refactor cycle
- [ ] Keep tests green (never commit failing tests)

#### Completing a Phase
- [ ] Verify all acceptance criteria met
- [ ] Run full test suite (unit + integration)
- [ ] Commit: "Complete Phase N: [Deliverable]"
- [ ] Add next phase tasks to TodoWrite

---

## Task Sizing Guide

### ✅ Just Right (Target)
**Size**: 30-90 minutes

**Examples**:
- Implement `Horse.CalculateEffectiveStats()` with tests
- Add Speed stat modifier with validation
- Wire up RaceService to use new modifiers

**Characteristics**:
- Clear acceptance criteria
- Independently testable
- Delivers visible progress

### ❌ Too Small (Avoid)
**Size**: < 15 minutes

**Examples**:
- Add getter for one property
- Write one test case
- Rename a variable

**Problem**: Tracking overhead exceeds value

### ❌ Too Large (Break Down)
**Size**: > 2 hours

**Examples**:
- Implement entire training system
- Refactor all of RaceService
- Add all API endpoints

**Problem**: Too risky, unclear when done, hard to test

---

## Vertical Slice Approach

### What is a Vertical Slice?
End-to-end functionality for **one capability**:
- **Data**: Entity/model for the concept
- **Logic**: Business rules and calculations
- **Persistence**: Save/load from database
- **Tests**: Unit and integration coverage

### Example: Speed Training

✅ **Good Vertical Slice** (Phase 1)
```
Goal: Implement Speed training that affects race outcomes

Includes:
- Training entity with Speed type
- Horse.CalculateEffectiveStats() applies Speed bonus
- Save/load Speed training from database
- Unit tests for calculation
- Integration test showing faster race times

Deliverable: Can train a horse's Speed and see it improve in races
```

❌ **Bad Horizontal Slice** (Avoid)
```
Goal: Create all training entities

Includes:
- Training base class
- SpeedTraining class
- StaminaTraining class
- AgilityTraining class

Problem: No working feature, can't test behavior, just busy work
```

---

## Phase Structure

Each implementation phase includes:

### 1. Phase Goal
One sentence describing what this delivers

### 2. RED - Write Failing Tests
- List of test cases to write first
- Expected behaviors being defined
- Edge cases to cover

### 3. GREEN - Make Tests Pass
- Files to create
- Files to modify
- Implementation steps
- Integration points

### 4. REFACTOR - Clean Up
- Code improvements to make
- Duplication to remove
- Patterns to apply

### 5. Acceptance Criteria
- All tests pass
- Code coverage target met
- Observable behavior works
- No regressions

### 6. Deliverable
Concrete description of working functionality

---

## Example: Race Modifiers Refactor

### Phase 1: Setup (Preparation)
**Goal**: Infrastructure ready without breaking current code

**Tasks**:
1. Create RaceModifierConfig with constants
2. Create SpeedModifierCalculator class
3. Add unit test project
4. Write characterization tests

**Deliverable**: New infrastructure exists, old code still works

### Phase 2: Stat Modifiers
**Goal**: Replace stat-based modifiers with new implementation

**Tasks**:
1. Implement CalculateStatModifiers() with tests
2. Replace old ApplySpeedModifier calls
3. Test race outcomes match expected behavior

**Deliverable**: Stat modifiers use new system, races still work

### Phase 3: Environmental Modifiers
[Continue for remaining phases...]

---

## Best Practices

### Planning Phase
- [ ] Use feature-discovery for requirements gathering
- [ ] Ensure feature spec is complete and approved
- [ ] Use plan-implementation to create concrete roadmap
- [ ] Review implementation plan before starting

### Implementation Phase
- [ ] Always write tests first (TDD discipline)
- [ ] Keep tests green (never commit failing tests)
- [ ] Commit after each Red-Green-Refactor cycle
- [ ] Mark todos complete immediately
- [ ] Work on one task at a time

### Quality Standards
- [ ] Code coverage ≥ 80% for new code
- [ ] All tests pass (unit + integration)
- [ ] No regressions in existing functionality
- [ ] Code follows existing patterns
- [ ] Documentation updated

---

## Common Patterns

### Pattern 1: New Game System
```
1. feature-discovery: Understand requirements, design approach
2. plan-implementation: Break into phases
3. Phase 1: Core domain model with tests
4. Phase 2: Business logic with tests
5. Phase 3: Database integration with tests
6. Phase 4: UI integration (if needed)
7. Phase 5: Edge cases and validation
```

### Pattern 2: Refactoring Existing Code
```
1. feature-discovery: Document current problems, design solution
2. plan-implementation: Create refactor roadmap
3. Phase 1: Characterization tests for current behavior
4. Phase 2: New implementation alongside old
5. Phase 3: Migrate call sites one by one
6. Phase 4: Delete old code
7. Phase 5: Clean up and optimize
```

### Pattern 3: Enhancement to Existing Feature
```
1. feature-discovery: Clarify requirements, assess impact
2. plan-implementation: Plan integration approach
3. Phase 1: Extend data model
4. Phase 2: Update business logic
5. Phase 3: Migrate existing data (if needed)
6. Phase 4: Update UI
```

---

## Troubleshooting

### "Feature spec isn't detailed enough"
Run feature-discovery again:
```
"Can you expand the feature spec for [feature] with more details on [aspect]?"
```

### "Implementation plan tasks are too large"
Ask for rebalancing:
```
"Can you break down Phase 3 into smaller tasks? These are too complex."
```

### "I found a better approach mid-implementation"
Update the plan:
```
"I'm changing Phase 4 to use [different approach]. Can you update the implementation plan?"
```

### "I'm stuck on a task"
Ask for guidance:
```
"I'm working on Phase 2, Task 3 (wire up RaceService). Can you help me understand how to do this?"
```

---

## File Organization

```
docs/
├── features/                    # Feature specifications
│   ├── 001-race-engine.md
│   ├── 001a-core-race-simulation.md
│   └── race-modifiers-refactor.md
│
├── implementation/              # Implementation plans
│   └── [feature]-implementation-plan.md
│
└── IMPLEMENTATION_WORKFLOW.md  # This guide

.claude/skills/
├── feature-discovery/          # Discovery skill
│   ├── SKILL.md
│   ├── PLANNING_TEMPLATE.md
│   └── DISCOVERY_CHECKLIST.md
│
└── plan-implementation/        # Implementation planning skill
    ├── SKILL.md
    ├── IMPLEMENTATION_TEMPLATE.md
    ├── TASK_SIZING_GUIDE.md
    └── README.md
```

---

## Quick Reference

### Starting a New Feature
```bash
# 1. Discovery
"/feature-discovery"

# 2. Planning
"/plan-implementation for [feature-name]"

# 3. Implement
# Work through todos, follow TDD, commit frequently
```

### TDD Cycle Timing
- RED: 10-20 min (write tests)
- GREEN: 20-45 min (pass tests)
- REFACTOR: 10-25 min (clean up)
- **Total**: 40-90 min = One task

### Task Sizing
- **Target**: 30-90 minutes
- **Too Small**: < 15 minutes
- **Too Large**: > 2 hours

### Commit Messages
```bash
# During implementation
git commit -m "Add [component]: [what was accomplished]"

# Completing a phase
git commit -m "Complete Phase N: [deliverable]"

# Example
git commit -m "Add Speed modifier calculation with unit tests"
git commit -m "Complete Phase 2: Stat modifiers use new system"
```

---

## Getting Help

### About the Skills
```
"How does feature-discovery work?"
"What's the difference between a phase and a task?"
"Show me an example implementation plan"
```

### During Implementation
```
"I'm stuck on [specific task]. Can you help?"
"This phase is taking longer than expected. Can you break it down further?"
"I want to change the approach for Phase 4. Here's what I'm thinking..."
```

---

**Ready to build?** Start with `/feature-discovery` to create your feature spec!
