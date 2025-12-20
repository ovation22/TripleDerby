# Plan Implementation Skill

Break down feature specifications into concrete, manageable implementation phases with TDD vertical slices.

## ⚠️ Project-Specific Workflow

**IMPORTANT**: For this project, **DO NOT commit automatically** after implementation phases. Always:
1. Complete the implementation tasks
2. Show the user what was done
3. **Wait for explicit approval** before creating any commits
4. Only commit when the user explicitly requests it

This allows the project owner to review all changes before they are committed to git.

## Purpose

This skill helps you transition from **planning** (feature-discovery) to **execution** by creating a detailed, actionable implementation plan with appropriately sized tasks.

## When to Use

✅ **Use this skill when:**
- You have a feature spec in `/docs/features/` and are ready to implement
- You need to break a complex feature into manageable phases
- You want a TDD-focused implementation roadmap
- You're starting a new sprint and need to plan work

❌ **Don't use when:**
- Feature requirements aren't clear yet (use `feature-discovery` first)
- Feature is trivial (< 3 tasks total)
- You're already mid-implementation (just continue)

## How It Works

### Input
- Feature specification document from `/docs/features/[feature-name].md`
- Existing codebase (analyzed to understand integration points)

### Process
1. Analyzes feature spec and extracts requirements
2. Explores codebase to understand existing patterns
3. Designs vertical slices (end-to-end capabilities)
4. Breaks each slice into TDD phases (Red-Green-Refactor)
5. Sizes tasks appropriately (30-90 min each)
6. Sequences phases to manage risk and deliver value early

### Output
1. **Implementation Plan**: `/docs/implementation/[feature-name]-implementation-plan.md`
   - Detailed phase breakdown with tasks
   - Test strategy
   - Files to create/modify
   - Risk mitigation

2. **TodoWrite Task List**: Populates with Phase 1 tasks
   - Ready to start implementing immediately
   - Clear, concrete, testable tasks
   - Next phases added as you progress

## Usage

### Invoke the Skill
```
User: "/plan-implementation for race-modifiers-refactor"
```

or

```
User: "I'm ready to implement the training system. Can you create an implementation plan?"
```

### What Happens
1. Claude reads the feature spec
2. Analyzes the codebase for integration points
3. Creates phased implementation plan
4. Writes plan to `/docs/implementation/`
5. Populates TodoWrite with first phase tasks
6. Reports: "Ready to implement!"

### Implementation Flow
```
feature-discovery → plan-implementation → implement → repeat
        ↓                    ↓                ↓
  Feature Spec      Implementation Plan   Working Code
```

## Key Principles

### 1. TDD with Vertical Slices
- **Test-First**: Write tests before implementation
- **End-to-End**: Each phase delivers working functionality
- **Incremental**: Build on previous phases

### 2. Appropriate Sizing
- **Small enough**: 30-90 minutes per task
- **Large enough**: Delivers meaningful value
- **Concrete**: Clear acceptance criteria

### 3. Risk-Aware Sequencing
- **Validate early**: High-risk items first
- **Foundation first**: Core models before complex logic
- **Testable always**: Can validate at each step

## Example Output

### Phase Structure
Each phase includes:
- **Goal**: What this delivers
- **RED tasks**: Tests to write first
- **GREEN tasks**: Code to make tests pass
- **REFACTOR tasks**: Cleanup and improvements
- **Acceptance criteria**: How to validate completion
- **Deliverable**: Observable working functionality

### Sample Phase
```markdown
### Phase 2: Speed Stat Modifier Calculation
**Goal**: Implement Speed stat modifier with linear scaling

**Vertical Slice**: Can calculate speed bonus for any horse

#### RED - Write Failing Tests
- [ ] Test: Speed 50 returns 1.0x (neutral)
- [ ] Test: Speed 0 returns 0.9x (slowest)
- [ ] Test: Speed 100 returns 1.1x (fastest)

#### GREEN - Make Tests Pass
- [ ] Implement CalculateSpeedModifier(int speed)
- [ ] Apply formula: 1.0 + ((speed - 50) * 0.002)
- [ ] Wire into RaceService.UpdateHorsePosition()

#### REFACTOR - Clean Up
- [ ] Extract magic number 0.002 to RaceModifierConfig.SpeedModifierPerPoint
- [ ] Add XML doc comments

**Acceptance Criteria**:
- [ ] All 3 tests pass
- [ ] Integration test: Horse with Speed 80 finishes faster than Speed 40
- [ ] No regressions in existing race tests

**Deliverable**: Speed stat affects race outcomes correctly
```

## Files and Structure

```
.claude/skills/plan-implementation/
├── SKILL.md                    # Main skill instructions
├── IMPLEMENTATION_TEMPLATE.md  # Template for output documents
├── TASK_SIZING_GUIDE.md       # Quick reference for sizing
└── README.md                   # This file

docs/implementation/            # Output location
└── [feature-name]-implementation-plan.md
```

## Integration with Other Skills

### Works With feature-discovery
```
User: "I want to add a horse breeding system"

Step 1: Use feature-discovery
→ Creates: docs/features/horse-breeding.md

Step 2: Use plan-implementation
→ Creates: docs/implementation/horse-breeding-implementation-plan.md
→ Adds Phase 1 tasks to TodoWrite

Step 3: Implement
→ Follow tasks, mark complete, add next phase
```

### Standalone Usage
Can also be used on existing feature specs:
```
User: "Create an implementation plan for the race modifiers refactor"

→ Reads: docs/features/race-modifiers-refactor.md
→ Creates: docs/implementation/race-modifiers-refactor-implementation-plan.md
→ Adds tasks to TodoWrite
```

## Best Practices

### Before Using Skill
- [ ] Feature spec exists and is complete
- [ ] Requirements are clear and approved
- [ ] Open questions are resolved
- [ ] You're ready to start coding

### During Implementation
- [ ] Work on Phase 1 tasks from TodoWrite
- [ ] Follow Red-Green-Refactor discipline
- [ ] Mark todos complete as you go
- [ ] **Wait for user review before committing** (project-specific)
- [ ] When phase complete, add next phase to TodoWrite

### After Each Phase
- [ ] Run all tests to verify no regressions
- [ ] Review acceptance criteria - all met?
- [ ] Present changes to user for review
- [ ] Commit only when user explicitly requests it
- [ ] Add next phase tasks to TodoWrite

## Customization

### Modify Approach
If the default TDD vertical slice approach doesn't fit:
- Ask Claude to adjust methodology
- Request different phase sequencing
- Specify custom acceptance criteria

### Example Customizations
```
"Create implementation plan but use layer-based phases instead of vertical slices"

"Create implementation plan but make each phase smaller (15-30 min tasks)"

"Create implementation plan focusing on performance optimization phases"
```

## Tips for Success

### ✅ Do This
- Keep tests green (never commit failing tests)
- Work on one phase at a time
- Mark todos complete immediately after finishing
- **Present changes for review before committing** (project-specific)
- Update implementation plan if you discover new requirements

### ❌ Avoid This
- Skipping tests ("I'll add them later")
- Working on multiple phases simultaneously
- Letting todos go stale (mark complete promptly)
- **Committing without user review** (project-specific)
- Deviating from plan without documenting why

## Troubleshooting

### "Tasks are too small/large"
Ask Claude to rebalance:
```
"Can you re-do the implementation plan with larger tasks? Current ones are too granular."
```

### "I don't understand a task"
Tasks should be concrete. If unclear:
```
"Can you elaborate on Phase 3, Task 2? I'm not sure what 'Wire up RaceService' means specifically."
```

### "I found a better approach mid-implementation"
Update the plan:
```
"I'm going to implement Phase 4 differently. Here's what I'm thinking...
Can you update the implementation plan?"
```

### "A phase is taking much longer than expected"
Reassess and break down:
```
"Phase 2 is more complex than planned. Can you break it into smaller phases?"
```

## Examples

### Example 1: Race Modifiers Refactor
- **Feature Spec**: Complex refactoring of race speed modifiers
- **Phases**: 7 phases from setup → implementation → cleanup
- **Approach**: TDD vertical slices, foundation first, risky items early
- **Outcome**: Clean, testable modifier system

### Example 2: Horse Training System (Hypothetical)
- **Feature Spec**: Add training to improve horse stats
- **Phases**:
  1. Speed training (data + calc + tests)
  2. Training persistence (DB + migration)
  3. Race integration (training affects outcomes)
  4. UI for training interface
  5. Additional stats (Stamina, Agility)
- **Approach**: Vertical slices, prove concept with Speed first, then expand

### Example 3: Multi-Player Betting (Hypothetical)
- **Feature Spec**: Players bet on each other's races
- **Phases**:
  1. Basic betting (place bet, calculate payout)
  2. Bet validation (rules + limits)
  3. Multiplayer sync (real-time updates)
  4. Betting history (view past bets)
- **Approach**: Vertical slices, single-player first, then multi-player

## Reference

### Task Sizing Cheat Sheet
- **Too Small**: < 15 min, trivial changes
- **Just Right**: 30-90 min, meaningful + testable
- **Too Large**: > 2 hours, vague or multi-concern

### Phase Naming Pattern
❌ "Create entities" (task-focused)
✅ "Speed training calculation" (outcome-focused)

### TDD Cycle Timing
- RED: 10-20 min (write tests)
- GREEN: 20-45 min (make them pass)
- REFACTOR: 10-25 min (clean up)
- **Total**: 40-90 min = One task

## Getting Help

If you need clarification on the skill:
```
"How does plan-implementation work?"
"Show me an example implementation plan"
"What's the difference between a phase and a task?"
```

If you want to customize:
```
"Can you adjust the template to include performance benchmarks?"
"I prefer different test naming conventions - can you use that?"
```

## Related Documentation

- [Feature Discovery Skill](../feature-discovery/SKILL.md) - Create feature specs
- [Planning Template](../feature-discovery/PLANNING_TEMPLATE.md) - TDD planning approach
- [Task Sizing Guide](./TASK_SIZING_GUIDE.md) - How to size tasks appropriately
- [Implementation Template](./IMPLEMENTATION_TEMPLATE.md) - Output format

---

**Ready to start implementing?** Use `/plan-implementation` and let Claude create your roadmap!
