# Enhancement: Realistic Horse Speed Calculation for Traffic Response

**Current State:** `CalculateHorseSpeed()` in RaceService returns a constant (`RaceModifierConfig.AverageBaseSpeed`) for all horses, regardless of their Speed stat or current race conditions.

**Location:** [TripleDerby.Core/Services/RaceService.cs](../../TripleDerby.Core/Services/RaceService.cs:742-747) (or OvertakingManager after Feature 010 extraction)

**Why This Matters:** Traffic response currently assumes all horses move at the same speed, which makes slower horses as difficult to pass as faster ones. Using actual speed would create more realistic racing dynamics.

---

## Prompt to Use When Ready

```
Enhance the CalculateHorseSpeed method in OvertakingManager (or RaceService if not yet extracted) to use actual horse speed based on stats and modifiers instead of a constant.

Current implementation:
- Returns RaceModifierConfig.AverageBaseSpeed (constant) for all horses
- Ignores horse Speed stat and race modifiers
- Located at: TripleDerby.Core/Services/RaceService.cs:742-747

Requirements:
1. Calculate realistic speed based on horse's Speed stat using the same modifier pipeline as UpdateHorsePosition
2. Consider environmental modifiers (surface, condition)
3. Account for phase modifiers if applicable
4. Include stamina effects on current speed
5. Maintain or improve current traffic response behavior
6. Write comprehensive tests to verify:
   - Faster horses have higher calculated speeds
   - Traffic capping works correctly with variable speeds
   - Edge cases (exhausted horses, extreme stats) handled properly

Expected behavior:
- Horse with Speed=80 calculates faster than Speed=50
- Exhausted horse (stamina=0) calculates slower speed
- Traffic caps adapt to actual leader speed, not average

Implementation notes:
- Reuse existing speedModifierCalculator and staminaCalculator
- May need to pass additional context (tick, totalTicks, raceRun)
- Consider caching calculated speeds per tick to avoid redundant calculations
- Update XML documentation to reflect new behavior

Acceptance criteria:
- All existing RaceService tests continue to pass
- New tests verify speed calculation accuracy
- Traffic response creates more realistic passing dynamics
- Performance remains acceptable (no significant slowdown)
```

---

## Technical Considerations

### Option 1: Full Modifier Pipeline (Most Accurate)
```csharp
private double CalculateHorseSpeed(
    RaceRunHorse horse,
    short currentTick,
    short totalTicks,
    RaceRun raceRun)
{
    var baseSpeed = RaceModifierConfig.AverageBaseSpeed;

    var context = new ModifierContext(
        CurrentTick: currentTick,
        TotalTicks: totalTicks,
        Horse: horse.Horse,
        RaceCondition: raceRun.ConditionId,
        RaceSurface: raceRun.Race.SurfaceId,
        RaceFurlongs: raceRun.Race.Furlongs
    );

    // Apply stat modifiers
    baseSpeed *= speedModifierCalculator.CalculateStatModifiers(context);

    // Apply environmental modifiers
    baseSpeed *= speedModifierCalculator.CalculateEnvironmentalModifiers(context);

    // Apply phase modifiers
    baseSpeed *= speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);

    // Apply stamina modifier
    baseSpeed *= speedModifierCalculator.CalculateStaminaModifier(horse);

    return baseSpeed;
}
```

**Pros:**
- Most accurate representation of actual speed
- Accounts for all factors affecting horse performance

**Cons:**
- More expensive (multiple calculations per traffic check)
- Requires additional parameters (tick, totalTicks, raceRun)
- May need caching to avoid performance impact

### Option 2: Simplified Stat-Based (Good Compromise)
```csharp
private static double CalculateHorseSpeed(RaceRunHorse horse)
{
    var baseSpeed = RaceModifierConfig.AverageBaseSpeed;

    // Apply Speed stat modifier only
    var speedModifier = 1.0 + ((horse.Horse.Speed - 50) * RaceModifierConfig.SpeedModifierPerPoint);
    baseSpeed *= speedModifier;

    // Apply stamina penalty if low
    if (horse.CurrentStamina < horse.InitialStamina * 0.5)
    {
        var staminaPercent = horse.CurrentStamina / horse.InitialStamina;
        var staminaPenalty = 1.0 - (RaceModifierConfig.MaxStaminaSpeedPenalty * (1.0 - staminaPercent));
        baseSpeed *= staminaPenalty;
    }

    return baseSpeed;
}
```

**Pros:**
- Simple, efficient calculation
- No additional parameters needed
- Captures most important factors (Speed stat + stamina)

**Cons:**
- Ignores environmental/phase modifiers
- Less accurate than full pipeline

### Option 3: Cache Calculated Speeds Per Tick
```csharp
// Add to ApplyTrafficEffects or higher level
private Dictionary<Guid, double> _tickSpeedCache = new();

private double CalculateHorseSpeed(
    RaceRunHorse horse,
    short currentTick,
    Dictionary<Guid, double> speedCache)
{
    if (speedCache.TryGetValue(horse.HorseId, out var cachedSpeed))
        return cachedSpeed;

    // Calculate using full pipeline (Option 1)
    var calculatedSpeed = /* full calculation */;

    speedCache[horse.HorseId] = calculatedSpeed;
    return calculatedSpeed;
}
```

**Pros:**
- Best accuracy with acceptable performance
- Cache cleared each tick, so always current

**Cons:**
- More complex state management
- Cache needs to be passed around or stored in service

---

## Recommended Approach

**Start with Option 2 (Simplified Stat-Based):**
1. Low risk, easy to test
2. Significant improvement over constant speed
3. Can enhance to Option 1 or 3 later if needed
4. Maintains backward compatibility with signature

**Future enhancement to Option 1 if:**
- Performance testing shows it's acceptable
- Gameplay feedback indicates more realism needed
- Full modifier pipeline proves necessary for balance

---

## Testing Strategy

**Unit Tests:**
```csharp
[Fact]
public void CalculateHorseSpeed_FastHorse_ReturnsHigherSpeed()
{
    var fastHorse = CreateHorse(speed: 80);
    var slowHorse = CreateHorse(speed: 40);

    var fastSpeed = CalculateHorseSpeed(fastHorse);
    var slowSpeed = CalculateHorseSpeed(slowHorse);

    Assert.True(fastSpeed > slowSpeed);
}

[Fact]
public void CalculateHorseSpeed_LowStamina_ReducesSpeed()
{
    var horse = CreateHorse(speed: 50, stamina: 50);
    var raceRunHorse = CreateRaceRunHorse(horse);

    raceRunHorse.InitialStamina = 50;
    raceRunHorse.CurrentStamina = 10; // 20% remaining

    var speed = CalculateHorseSpeed(raceRunHorse);

    Assert.True(speed < RaceModifierConfig.AverageBaseSpeed);
}
```

**Integration Tests:**
```csharp
[Fact]
public async Task TrafficResponse_FasterHorseBlocked_CappedToLeaderSpeed()
{
    // Setup: Fast horse (Speed=80) behind slow horse (Speed=40)
    // Verify: Fast horse's speed capped relative to slow leader's speed
}
```

**Balance Validation:**
- Run 1000 races with varying Speed stats
- Verify faster horses pass slower horses more frequently
- Ensure traffic doesn't become too easy/hard to navigate
- Compare race times before/after enhancement

---

## Related Files

After Feature 010 extraction, this will likely be in:
- `TripleDerby.Core/Racing/OvertakingManager.cs` (extracted location)
- Called by `ApplyTrafficEffects()` method
- May need to update interface `IOvertakingManager`

---

## Status

- **Priority:** Medium (quality of life improvement)
- **Difficulty:** Easy (Option 2) to Medium (Option 1)
- **Estimated Effort:** 2-4 hours including tests
- **Dependencies:** None (can be done anytime after Feature 010)
