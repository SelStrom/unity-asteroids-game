---
phase: 04-ecs-foundation
plan: 03
subsystem: ecs
tags: [unity-entities, dots, ecs-systems, gun, laser, weapon-systems]

# Dependency graph
requires:
  - phase: 04-ecs-foundation
    plan: 01
    provides: GunData, LaserData IComponentData structs, AsteroidsEcsTestFixture
provides:
  - EcsGunSystem with full reload logic (CurrentShoots = MaxShoots)
  - EcsLaserSystem with incremental reload logic (CurrentShoots += 1)
  - 13 EditMode tests (6 gun + 7 laser)
affects: [04-04]

# Tech tracking
tech-stack:
  added: []
  patterns: [ISystem without BurstCompile for managed-callback systems, SystemAPI.Query RefRW iteration]

key-files:
  created:
    - Assets/Scripts/ECS/Systems/EcsGunSystem.cs
    - Assets/Scripts/ECS/Systems/EcsLaserSystem.cs
    - Assets/Tests/EditMode/ECS/GunSystemTests.cs
    - Assets/Tests/EditMode/ECS/LaserSystemTests.cs
  modified: []

key-decisions:
  - "UpdateAfter/UpdateBefore attributes commented out -- referenced systems (EcsLifeTimeSystem, EcsShootToSystem) not yet created by parallel plans"
  - "No BurstCompile on either system per D-03 (managed callbacks in Phase 5)"
  - "OnShooting callback omitted -- managed, deferred to Phase 5 Bridge Layer"

patterns-established:
  - "ISystem weapon systems use SystemAPI.Query<RefRW<T>>() for component iteration"
  - "Shooting flag reset to false at end of every OnUpdate (fire-and-forget pattern)"
  - "TDD RED-GREEN for each system: failing tests first, then implementation"

requirements-completed: [ECS-07, ECS-08, TST-05, TST-06]

# Metrics
duration: 3min
completed: 2026-04-02
---

# Phase 04 Plan 03: Weapon Systems (Gun + Laser) Summary

**EcsGunSystem (full reload) and EcsLaserSystem (incremental reload) ported 1:1 from MonoBehaviour originals, 13 EditMode TDD tests covering reload, shooting, ammo limits, and Shooting reset**

## Performance

- **Duration:** 3 min
- **Started:** 2026-04-02T21:52:04Z
- **Completed:** 2026-04-02T21:56:00Z
- **Tasks:** 2
- **Files created:** 4

## Accomplishments
- Ported GunSystem to EcsGunSystem (ISystem, no Burst) with full reload logic: CurrentShoots = MaxShoots when ReloadRemaining reaches zero
- Ported LaserSystem to EcsLaserSystem (ISystem, no Burst) with incremental reload: CurrentShoots += 1 per reload cycle
- Both systems reset Shooting flag to false after every update
- OnShooting managed callbacks deferred to Phase 5 Bridge Layer
- 6 gun tests + 7 laser tests = 13 total, all following TDD RED-GREEN workflow

## Task Commits

Each task was committed atomically:

1. **Task 1 RED: GunSystem failing tests** - `9493b54` (test)
2. **Task 1 GREEN: EcsGunSystem implementation** - `07dbab9` (feat)
3. **Task 2 RED: LaserSystem failing tests** - `6fbc82a` (test)
4. **Task 2 GREEN: EcsLaserSystem implementation** - `473ad07` (feat)

## Files Created
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` - Gun system with full reload, shooting, Shooting reset
- `Assets/Scripts/ECS/Systems/EcsLaserSystem.cs` - Laser system with incremental reload, shooting, Shooting reset
- `Assets/Tests/EditMode/ECS/GunSystemTests.cs` - 6 tests: reload timing, full reload trigger, no-reload-when-full, shooting decrement, no-shoot-when-empty, Shooting reset
- `Assets/Tests/EditMode/ECS/LaserSystemTests.cs` - 7 tests: reload timing, incremental reload, no-reload-when-full, shooting decrement, no-shoot-when-empty, Shooting reset, 2-cycle incremental verification

## Decisions Made
- UpdateAfter/UpdateBefore ordering attributes commented out because referenced systems (EcsLifeTimeSystem, EcsShootToSystem) are created by parallel agent plans and don't exist yet
- Both systems implemented without [BurstCompile] per D-03 (managed callbacks will be added in Phase 5)
- OnShooting callback omitted -- managed delegate, will be wired in Phase 5 Bridge Layer

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] UpdateAfter/UpdateBefore attributes reference non-existent types**
- **Found during:** Task 1 and Task 2
- **Issue:** Plan specifies [UpdateAfter(typeof(EcsLifeTimeSystem))] and [UpdateBefore(typeof(EcsLaserSystem/EcsShootToSystem))] but these types are created by parallel plans (04-02, 04-04) not yet merged
- **Fix:** Commented out the ordering attributes with notes to add them when referenced systems are available
- **Files modified:** EcsGunSystem.cs, EcsLaserSystem.cs
- **Commits:** 07dbab9, 473ad07

## Issues Encountered
None beyond the deviation above.

## Known Stubs
None -- both systems are fully functional 1:1 ports of the original logic (minus managed callbacks deferred to Phase 5 by design).

## User Setup Required
None.

## Next Phase Readiness
- Both weapon systems ready for integration with EntityFactory (Plan 04)
- UpdateAfter/UpdateBefore attributes should be uncommented when EcsLifeTimeSystem and EcsShootToSystem are merged
- OnShooting callbacks to be wired in Phase 5 Bridge Layer

## Self-Check: PASSED
