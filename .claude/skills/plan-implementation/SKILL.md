---
name: plan-implementation
description: Break down feature specifications into manageable implementation phases with TDD vertical slices. Creates actionable task lists for immediate development. Use when you have a feature spec and need a concrete implementation roadmap.
---

# Implementation Planning

## When to Use

**Use when:**
- You have a feature spec in `/docs/features/` and are ready to implement
- You need to break a complex feature into manageable phases
- You want a TDD-focused implementation roadmap

**Don't use when:**
- Feature requirements aren't clear yet (use `feature-discovery` first)
- Feature is trivial (< 3 tasks total)

## Process

### 1. Analyze Feature Specification
- Read the feature spec from `/docs/features/[feature-name].md`
- Extract requirements and acceptance criteria
- Identify complexity areas and risks
- Review codebase for integration points

### 2. Design Phase Breakdown
- Identify vertical slices (end-to-end capabilities)
- Sequence for value delivery (testable functionality earliest)
- Size appropriately (30-90 min per task)
- Define deliverables for each phase

### 3. Create Implementation Tasks
For each phase, define:
- **Phase Goal**: One sentence describing what this phase accomplishes
- **RED tasks**: Tests to write first
- **GREEN tasks**: Code to make tests pass
- **REFACTOR tasks**: Cleanup and improvements
- **Acceptance Criteria**: How to verify completion

### 4. Generate Output
- Write plan to `/docs/implementation/[feature-name]-implementation-plan.md`
- Populate TodoWrite with Phase 1 tasks only
- Add subsequent phases as each completes

## Core Principles

### TDD Vertical Slices
- **Test-First**: Write tests before implementation
- **End-to-End**: Each phase delivers working functionality
- **Incremental**: Build on previous phases

### Appropriate Sizing
- **30-90 minutes** per task
- **Concrete** acceptance criteria
- **Independently testable**

See TASK_SIZING_GUIDE.md for detailed guidance.

### Risk-Aware Sequencing
- Validate assumptions early (high-risk items first)
- Build foundation first (core models before complex logic)
- Enable testing at each step

## Phase Template

```markdown
### Phase N: [Descriptive Name]
**Goal**: [One sentence describing what this phase delivers]

**Vertical Slice**: [What end-to-end capability does this deliver?]

#### RED - Write Failing Tests
- [ ] Test: [Specific test case]
- [ ] Test: [Edge case]

#### GREEN - Make Tests Pass
- [ ] Create/modify: [File or class]
- [ ] Implement: [Method or functionality]

#### REFACTOR - Clean Up
- [ ] Extract: [What to pull out]
- [ ] Remove: [Duplication to eliminate]

**Acceptance Criteria**:
- [ ] All tests pass
- [ ] [Observable behavior works]

**Deliverable**: [Working functionality that can be demonstrated]
```

## Output

### Implementation Plan Document
`/docs/implementation/[feature-name]-implementation-plan.md`
- Phase breakdown with tasks
- Testing strategy
- Files to create/modify
- Risk mitigation

Use IMPLEMENTATION_TEMPLATE.md as a starting point.

### TodoWrite Task List
- Populated with Phase 1 tasks only
- Ready to start implementing immediately
- Next phases added as you progress

## Workflow

```
feature-discovery → plan-implementation → implement → repeat
       ↓                    ↓                 ↓
  Feature Spec      Implementation Plan   Working Code
```

### Phase Progression
```
Phase 1 Complete → Mark todos done → Add Phase 2 todos → Repeat
```

## Reference Files

- **QUICK_START.md**: Fast reference for getting started
- **TASK_SIZING_GUIDE.md**: How to size tasks appropriately
- **IMPLEMENTATION_TEMPLATE.md**: Template for output documents

## Tools Used
- Read, Grep, Glob to analyze specs and codebase
- TodoWrite to create task list
- AskUserQuestion to clarify implementation choices
