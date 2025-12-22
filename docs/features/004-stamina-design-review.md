# Stamina Depletion - Design Review

**Date:** 2025-12-22
**Feature:** 004 - Stamina Depletion System
**Status:** Awaiting Design Approval

---

## Purpose

This document highlights key design decisions for review before implementation begins. Please review each decision and confirm or suggest adjustments.

---

## 🎯 Core Design Decisions

### 1. Stamina + Durability Synergy

**Decision:** Use both Stamina and Durability stats together for endurance system.

**Rationale:**
- **Stamina** = fuel tank size (0-100 stat)
- **Durability** = fuel efficiency (0-100 stat)
- Creates interesting stat tradeoffs and build diversity

**Example Profiles:**

| Profile | Speed | Stamina | Durability | Depletion Rate | Best For |
|---------|-------|---------|-----------|----------------|----------|
| Pure Sprinter | 100 | 0 | 0 | 1.38x | 4-6f races |
| Marathon Specialist | 50 | 100 | 100 | 0.68x | 12f+ races |
| Balanced Runner | 75 | 75 | 50 | 0.90x | 8-10f races |
| Fast but Fragile | 90 | 80 | 20 | 0.99x | One-dimensional |

**Alternative Considered:**
- Use only Stamina (simpler, but less strategic depth)
- Use only Durability (confusing, Stamina already exists)

**Questions for Review:**
- ✅ Is the Stamina/Durability synergy intuitive?
- ✅ Should Durability have other effects beyond stamina efficiency?
- ✅ Are the multiplier ranges appropriate (0.68x to 1.38x)?

---

### 2. Speed Penalty Severity (MILD)

**Decision:** Maximum 5-10% speed penalty at 0% stamina (mild severity)

**Penalty Curve:**
```
100% stamina → 1.00x speed (no penalty)
75% stamina  → 0.995x speed (0.5% penalty)
50% stamina  → 0.99x speed (1% penalty)
25% stamina  → 0.965x speed (3.5% penalty)
0% stamina   → 0.91x speed (9% penalty)
```

**Rationale:**
- Matches user preference for "mild" severity
- Exhausted horses still competitive, just slower
- Prevents frustrating collapses/DNFs
- Stamina becomes strategic, not mandatory

**Alternatives Considered:**
- **Moderate (10-20%):** More dramatic late-race collapses
- **Severe (20-40%):** Exhaustion essentially ends competitive chances

**Questions for Review:**
- ✅ Is 9% maximum penalty enough to be meaningful?
- ✅ Should the curve be steeper below 25% stamina?
- ✅ Should we use quadratic curve (current) or linear?

---

### 3. Progressive Distance Scaling

**Decision:** Different depletion rates by race distance category

**Depletion Rates:**

| Distance | Category | Base Depletion Rate | Expected Finish Stamina |
|----------|----------|---------------------|------------------------|
| 4-6f | Sprint | 0.08/100 ticks | 60-80% |
| 7-10f | Classic | 0.15/100 ticks | 30-50% |
| 11-12f | Long | 0.22/100 ticks | 15-30% |
| 13f+ | Marathon | 0.30/100 ticks | 5-20% |

**Rationale:**
- Makes race distance selection strategic
- Sprint races remain Speed/Agility focused
- Marathon races reward high Stamina/Durability
- Progressive (not linear) scaling feels more realistic

**Alternatives Considered:**
- **Linear scaling:** Same rate per furlong (simpler, less interesting)
- **Fixed threshold:** Only matters above 10f (too binary)
- **Exponential scaling:** Very punishing on long races (too harsh)

**Questions for Review:**
- ✅ Are the distance categories appropriate?
- ✅ Should 10f races have MORE stamina impact (currently moderate)?
- ✅ Are finish stamina percentages reasonable?

---

### 4. LegType Stamina Usage Patterns

**Decision:** Different LegTypes burn stamina at different rates during race phases

**Patterns:**

| LegType | Early Phase | Late Phase | Transition | Strategy |
|---------|-------------|------------|------------|----------|
| StartDash | 1.30x (0-25%) | 0.90x (25-100%) | 25% | Explosive start, cruise finish |
| FrontRunner | 1.10x (all) | 1.10x (all) | N/A | Aggressive throughout |
| StretchRunner | 0.85x (0-60%) | 1.15x (60-100%) | 60% | Conserve, push stretch |
| LastSpurt | 0.80x (0-75%) | 1.40x (75-100%) | 75% | Maximum conservation, explosive close |
| RailRunner | 0.90x (0-70%) | 1.05x (70-100%) | 70% | Steady, slight late push |

**Rationale:**
- Adds strategic variety to LegType selection
- Matches racing intuition (closers conserve energy)
- Creates interesting risk/reward for each style
- Compounds with existing LegType speed phase modifiers

**Alternatives Considered:**
- **No LegType stamina differences:** Simpler, less interesting
- **Only early vs late (binary):** Less nuanced
- **Different patterns:** Open to suggestions

**Questions for Review:**
- ✅ Do these patterns make intuitive sense?
- ✅ Are the multipliers balanced (0.80x to 1.40x range)?
- ✅ Should FrontRunner have different early/late phases?
- ✅ Do these conflict with existing LegType speed bonuses?

---

### 5. Pace-Based Depletion

**Decision:** Faster running = faster stamina depletion (linear scaling)

**Formula:**
```
paceMultiplier = currentSpeed / baseSpeed

Examples:
Running 1.10x speed (fast) → 1.10x depletion
Running 1.00x speed (neutral) → 1.00x depletion
Running 0.90x speed (slow) → 0.90x depletion
```

**Rationale:**
- Effort-based depletion feels realistic
- Fast horses with low stamina burn out quicker
- Creates natural balance (high speed = high cost)
- Simple linear relationship

**Alternatives Considered:**
- **No pace adjustment:** Only stats matter (less dynamic)
- **Quadratic scaling:** Extreme speeds punished heavily (too complex)
- **Acceleration-based:** Depletion based on speed changes (too complicated)

**Questions for Review:**
- ✅ Is linear scaling appropriate?
- ✅ Should there be a minimum/maximum pace multiplier cap?
- ✅ Does this create the right risk/reward for high-Speed horses?

---

### 6. Modifier Pipeline Integration

**Decision:** Add stamina modifier BEFORE random variance in pipeline

**Pipeline Order:**
```
Final Speed = Base Speed
  × Stat Modifiers (Speed, Agility)
  × Environmental Modifiers (Surface, Condition)
  × Phase Modifiers (LegType timing)
  × Stamina Modifier (NEW - added here)
  × Random Variance
```

**Rationale:**
- Stamina is a "state" modifier (changes during race)
- Applied after static modifiers, before randomness
- Random variance still applies to fatigued horses
- Clean separation of concerns

**Alternative Placement:**
- **After random variance:** Would make stamina more deterministic
- **With stat modifiers:** Would treat it like Speed/Agility (but it's dynamic)
- **Separate calculation:** Would complicate pipeline

**Questions for Review:**
- ✅ Is this the right placement in the pipeline?
- ✅ Should stamina multiply with other modifiers or be additive?
- ✅ Any concerns about order of operations?

---

### 7. No Stamina Regeneration (MVP)

**Decision:** Stamina only depletes during race, never regenerates

**Rationale:**
- Simpler to implement and test
- Easier to balance
- More predictable behavior
- Matches real-world fatigue (doesn't instantly recover)

**Future Enhancement Possibilities:**
- Slight regeneration during slow phases
- "Second wind" mechanic at certain thresholds
- Recovery based on Durability stat

**Questions for Review:**
- ✅ Should we add regeneration in MVP or wait for future?
- ✅ If we add it, what rate makes sense?
- ✅ Would regeneration complicate balance too much?

---

### 8. ModifierContext Enhancement

**Decision:** Add `RaceRunHorse` to ModifierContext record

**Current:**
```csharp
public record ModifierContext(
    short CurrentTick,
    short TotalTicks,
    Horse Horse,
    ConditionId RaceCondition,
    SurfaceId RaceSurface,
    decimal RaceFurlongs
);
```

**Proposed:**
```csharp
public record ModifierContext(
    short CurrentTick,
    short TotalTicks,
    Horse Horse,
    ConditionId RaceCondition,
    SurfaceId RaceSurface,
    decimal RaceFurlongs,
    RaceRunHorse RaceRunHorse  // NEW - for CurrentStamina access
);
```

**Rationale:**
- Need access to `CurrentStamina` from `RaceRunHorse`
- Clean way to pass race state to modifiers
- Maintains immutability (context is read-only)
- Minimal breaking change (one new parameter)

**Alternatives Considered:**
- **Pass CurrentStamina separately:** More parameters, messier
- **Store in Horse entity:** Horse is static, stamina is dynamic
- **Global state:** Bad practice, hard to test

**Questions for Review:**
- ✅ Is adding RaceRunHorse to context acceptable?
- ✅ Should we rename it to avoid confusion (HorseRaceState?)
- ✅ Any concerns about coupling?

---

## 📊 Balance Targets Review

### Statistical Correlation Targets

| Stat | Current | Target | Interpretation |
|------|---------|--------|----------------|
| Speed | -0.745 | -0.745 | Primary (unchanged) |
| Agility | -0.355 | -0.355 | Secondary (unchanged) |
| **Stamina** | **-0.040** | **-0.15 to -0.25** | **Weak to moderate** |
| **Durability** | **0.000** | **-0.10 to -0.20** | **Weak to moderate** |

**Questions:**
- ✅ Are these target ranges appropriate?
- ✅ Should Stamina have STRONGER correlation (more impactful)?
- ✅ Is it OK for Durability to be weaker than Stamina?

---

### Distance-Specific Impact Targets

| Distance | Stamina Correlation | Strategy |
|----------|-------------------|----------|
| 4-6f (Sprint) | ~-0.05 | Speed/Agility dominant |
| 10f (Classic) | ~-0.20 | Balanced approach |
| 16f (Marathon) | ~-0.35 | Stamina/Durability critical |

**Questions:**
- ✅ Should classic races (10f) have MORE stamina impact?
- ✅ Is -0.35 for marathons strong enough?
- ✅ Should sprints have ZERO stamina impact instead of minimal?

---

## 🧪 Testing Strategy Review

### Test Coverage Plan

**Unit Tests:**
- Stamina depletion calculations (all formulas)
- Stamina speed modifier (penalty curve)
- LegType multipliers by phase
- Distance scaling
- Edge cases (0/100 stats)

**Integration Tests:**
- Full race stamina depletion
- Cross-stat interactions (Stamina × Durability)
- LegType stamina patterns
- Distance impact validation

**Balance Validation Tests:**
- 1000-race correlation analysis
- Distance-specific impact
- No regression on existing stats

**Questions:**
- ✅ Is this test coverage sufficient?
- ✅ Should we add performance benchmarks?
- ✅ Any specific edge cases to test?

---

## 🚨 Risk Assessment

### Implementation Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Balance too weak | Medium | Tunable constants, statistical validation |
| Balance too strong | Medium | Mild penalty curve, progressive scaling |
| Regression in existing tests | Low | TDD approach, run all tests each phase |
| Performance impact | Low | Simple calculations, no complex lookups |
| Complexity creep | Medium | Clear scope, resist feature additions |

**Questions:**
- ✅ Any risks missing from this list?
- ✅ Should we implement feature flags for gradual rollout?

---

## ✏️ Design Alternatives to Consider

### Alternative A: Simpler System (Stamina Only)

**Change:** Remove Durability from stamina calculations

**Pros:**
- Simpler to understand
- Fewer stats to balance
- Easier to tune

**Cons:**
- Less strategic depth
- Durability remains unused
- Binary endurance profiles (high/low stamina only)

**Recommendation:** Keep Durability integration (more interesting)

---

### Alternative B: Stronger Penalties

**Change:** Increase max penalty from 9% to 15-20%

**Pros:**
- More dramatic late-race changes
- Makes stamina more important
- Clearer difference between horses

**Cons:**
- Can feel frustrating to players
- Risk of stamina becoming mandatory
- Larger collapses might seem unrealistic

**Recommendation:** Start mild (9%), tune up if needed after testing

---

### Alternative C: Non-Linear Depletion

**Change:** Use quadratic or exponential depletion rate (accelerates over time)

**Pros:**
- More realistic fatigue model
- Creates dramatic late-race moments
- Natural "wall" effect in marathons

**Cons:**
- Harder to balance
- Less predictable
- More complex formulas

**Recommendation:** Start linear, consider non-linear in future iteration

---

## 📝 Open Questions Summary

### Critical (Need Answers Before Implementation)

1. **Stamina/Durability Synergy**
   - Is the two-stat approach acceptable?
   - Should Durability have other effects too?

2. **Penalty Severity**
   - Is 9% max penalty sufficient?
   - Should curve be steeper below 25% stamina?

3. **Distance Scaling**
   - Are distance categories correct?
   - Should 10f have more impact?

4. **LegType Patterns**
   - Do multipliers make intuitive sense?
   - Are ranges balanced (0.80x to 1.40x)?

### Nice to Clarify (Can Decide During Implementation)

5. **Stamina Regeneration**
   - Add in MVP or wait for v2?

6. **ModifierContext Change**
   - Is adding RaceRunHorse OK?

7. **Balance Targets**
   - Should Stamina correlation be stronger?

8. **Alternative Approaches**
   - Any of the alternatives preferred?

---

## ✅ Approval Checklist

Please review and confirm:

- [ ] Core design approach (Stamina + Durability) approved
- [ ] Mild penalty severity (9% max) approved
- [ ] Progressive distance scaling approved
- [ ] LegType stamina patterns approved
- [ ] Pace-based depletion approved
- [ ] Modifier pipeline placement approved
- [ ] Balance targets acceptable
- [ ] Testing strategy sufficient
- [ ] No major design changes needed

**OR**

- [ ] Request design changes (specify below)

---

## 💭 Reviewer Notes / Change Requests

**Your feedback here:**

```
[Space for reviewer to add notes, questions, or requested changes]
```

---

## Next Steps After Approval

1. ✅ Begin Phase 1: Write stamina depletion core logic tests (RED)
2. Implement formulas (GREEN)
3. Refactor (REFACTOR)
4. Proceed through phases 2-6
5. Run full balance validation
6. Update documentation

---

**Document Status:** Awaiting Review
**Expected Review Time:** 15-30 minutes
**Implementation Time (if approved):** 6-8 hours (6 TDD phases)

---

**End of Design Review**
