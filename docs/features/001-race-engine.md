# Race Engine

**Feature Number:** 001

**Status:** Pending

## Actors & Goals

| Actor | Goal |
|-------|------|
| **Player** | Enter a horse in a race against similarly-experienced competitors and observe the outcome via play-by-play to earn prize money |
| **System (Matchmaker)** | Select 7-11 CPU horses with similar race counts to fill the field (8-12 total) |
| **Admin** | View and manage race definitions, tracks, and historical results |

## Observable Behaviors

| # | Behavior |
|---|----------|
| **B1** | Player selects a race and horse; System finds 7-11 CPU horses with similar race counts and creates a field of 8-12 competitors |
| **B2** | System assigns each horse to a lane (1-8+) and generates a random track condition for the race |
| **B3** | System simulates the race tick-by-tick, calculating each horse's distance based on speed, stats, leg type, lane, surface, and condition modifiers |
| **B4** | Stamina depletes each tick based on effort and conditions; low stamina reduces horse speed progressively |
| **B5** | System generates play-by-play notes for each tick describing notable events (lead changes, overtakes, closing gaps, surges, fading) |
| **B6** | System determines final placements when all horses cross the finish line and records finishing order |
| **B7** | System distributes purse money to top finishers (1st, 2nd, 3rd) and updates player's balance |
| **B8** | System increments each horse's race count and checks against career limits (max 8 races/year, 3-year career) |
| **B9** | Player views the race result showing final standings, play-by-play recap, and earnings |
| **B10** | System evaluates injury chance for each horse post-race based on durability and race stress |

## Sizing Assessment

**Outcome:** Needs Decomposition

**Reason:** The following behaviors were identified as deferrable to later slices:
- B5 (Play-by-play commentary) - Races can run without narrative initially
- B8 (Career limits) - Tracking race counts isn't essential for a race to complete
- B10 (Injury chance) - Injuries are a consequence, not core to race simulation

## Sub-Features

| # | Sub-Feature | Description | Depends On | Status |
|---|-------------|-------------|------------|--------|
| 1 | Core Race Simulation | Field assembly, lane/condition assignment, tick-based movement with stamina, finish order determination | - | Implemented |
| 2 | Purse Distribution | Calculate and distribute prize money to top finishers, update player balance | 1 | Pending |
| 3 | Play-by-Play Commentary | Generate narrative notes for each tick describing race events | 1 | Pending |
| 4 | Career Tracking | Increment race counts, enforce yearly and career limits | 1 | Pending |
| 5 | Post-Race Injury | Evaluate and apply injury chance based on durability and race stress | 1 | Pending |

## Sub-Feature Documents

- [002-core-race-simulation](002-core-race-simulation.md) - Implemented

## Related Decisions

*Decisions will be linked here as they are made.*
