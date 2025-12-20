# Quick Start: plan-implementation

Get from feature spec to working code in minutes.

## Usage

### Invoke the Skill
```
/plan-implementation for [feature-name]
```

**Example**:
```
/plan-implementation for race-modifiers-refactor
```

### What You Get
1. **Implementation plan** in `/docs/implementation/[feature-name]-implementation-plan.md`
2. **TodoWrite tasks** for Phase 1 (ready to start coding)

---

## The 5-Minute Implementation Start

### 1. Have a Feature Spec (2 min)
Ensure `/docs/features/[feature-name].md` exists.

Don't have one? Run `/feature-discovery` first.

### 2. Run the Skill (30 sec)
```
/plan-implementation for [feature-name]
```

### 3. Review the Plan (2 min)
Read the generated implementation plan:
- Understand the phases
- Check the first phase tasks
- Verify approach makes sense

### 4. Start Coding (immediately)
Your TodoWrite is populated with Phase 1 tasks. Follow TDD:
```
RED → Write failing tests
GREEN → Make tests pass
REFACTOR → Clean up code
COMMIT → Save your work
```

### 5. Progress Through Phases
Complete Phase 1 → Add Phase 2 todos → Repeat

---

## TDD Cycle (40-90 min per task)

```
┌─────────────────────────────────────┐
│ RED (10-20 min)                     │
│ • Write 3-5 failing tests           │
│ • Run tests, verify they fail       │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ GREEN (20-45 min)                   │
│ • Write code to pass tests          │
│ • Don't optimize, just make it work │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ REFACTOR (10-25 min)                │
│ • Clean up while keeping tests green│
│ • Extract, rename, remove duplicate │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ COMMIT                              │
│ • git commit -m "Add [component]"   │
│ • Mark todo complete                │
└─────────────────────────────────────┘
```

---

## Phase Progression

```
Phase 1: Foundation
├─ Task 1 ✅
├─ Task 2 ✅
├─ Task 3 ✅
└─ All acceptance criteria met ✅
    ↓
    Commit: "Complete Phase 1: [deliverable]"
    ↓
Phase 2: Core Logic
├─ Add Phase 2 tasks to TodoWrite
├─ Task 1 ✅
├─ Task 2 ✅
...
```

---

## Example Session

### User
```
/plan-implementation for horse-training
```

### Claude
```
I'll create an implementation plan for the horse training feature.

[Reads docs/features/horse-training.md]
[Analyzes codebase]
[Designs 5 vertical slice phases]

✅ Created: docs/implementation/horse-training-implementation-plan.md

📋 Added to TodoWrite (Phase 1: Speed Training Foundation):
  1. Create Training entity with properties and validation tests
  2. Implement Horse.CalculateEffectiveStats() for Speed bonus
  3. Add unit tests for Speed calculation edge cases
  4. Wire TrainingService.AddTraining() for Speed stat

Ready to implement! Start with the first todo.
```

### User (starts coding)
```
[Opens test file]
[Writes failing test for Training entity]
[Runs test → RED]
[Writes Training class]
[Runs test → GREEN]
[Cleans up code → REFACTOR]
[Commits]
[Marks todo #1 complete]
[Continues with todo #2...]
```

---

## Task Examples

### ✅ Well-Sized Task
```
Implement Horse.CalculateEffectiveStats() with tests for:
- No training returns base stats
- Speed training adds correct bonus
- Multiple trainings stack properly
- Max stat cap is respected
```

**Time**: 60 minutes
**Tests**: 4-5 test cases
**Code**: 1 method, ~20 lines
**Deliverable**: Speed bonuses calculate correctly

### ❌ Too Small
```
Add getter for Training.XP property
```

**Problem**: 2-minute task, not worth tracking

### ❌ Too Large
```
Implement entire training system with all stats and UI
```

**Problem**: Multi-day task, unclear when done, too risky

---

## Checklist

### Before Running Skill
- [ ] Feature spec exists in `/docs/features/`
- [ ] Requirements are clear
- [ ] Ready to start coding

### During Implementation
- [ ] Write tests first (RED)
- [ ] Make tests pass (GREEN)
- [ ] Clean up code (REFACTOR)
- [ ] Commit after each cycle
- [ ] Mark todos complete immediately
- [ ] Keep tests green always

### Completing a Phase
- [ ] All tasks done
- [ ] All tests passing
- [ ] Acceptance criteria met
- [ ] Commit: "Complete Phase N"
- [ ] Add next phase to TodoWrite

---

## Common Commands

### Start Implementation
```
/plan-implementation for [feature-name]
```

### During Implementation
```
# Run tests
dotnet test

# Commit
git add .
git commit -m "Add [component]: [what changed]"

# Complete phase
git commit -m "Complete Phase 2: [deliverable]"
```

### Get Help
```
"Break down Phase 3 into smaller tasks"
"I'm stuck on task X, can you help?"
"Update implementation plan - I'm using a different approach"
```

---

## File Locations

```
docs/features/           ← Feature specs (input)
docs/implementation/     ← Implementation plans (output)
```

---

## Skill Files Reference

- **SKILL.md**: Full skill documentation
- **IMPLEMENTATION_TEMPLATE.md**: Template for output docs
- **TASK_SIZING_GUIDE.md**: How to size tasks appropriately
- **README.md**: Detailed usage guide
- **QUICK_START.md**: This file

---

## Remember

1. **Test first, always** - No code without failing tests
2. **Keep tests green** - Never commit failing tests
3. **Small commits** - After each TDD cycle
4. **One task at a time** - Focus
5. **Mark complete immediately** - Don't let todos go stale

---

**That's it! Run `/plan-implementation for [feature-name]` and start coding.**
