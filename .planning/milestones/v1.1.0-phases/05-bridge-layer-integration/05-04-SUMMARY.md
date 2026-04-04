---
phase: 05-bridge-layer-integration
plan: 04
subsystem: ecs-bridge
tags: [ecs, dots, dead-tag, laser, vfx, unity-entities]

requires:
  - phase: 05-03
    provides: "ECS bridge layer with DeadEntityCleanupSystem, CollisionBridge, TryGetEntity"
provides:
  - "Laser kill path via DeadTag instead of Kill(model) in ECS mode"
  - "Active laser VFX tracking and cleanup on player death"
  - "Regression tests for laser kill and VFX cleanup bugs"
affects: [05-bridge-layer-integration]

tech-stack:
  added: []
  patterns:
    - "_activeLaserVfx list pattern for tracking active VFX outside ActionScheduler"

key-files:
  created: []
  modified:
    - Assets/Scripts/Application/Game.cs
    - Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs

key-decisions:
  - "DeadTag вместо Kill(model) для лазера в ECS-режиме -- единый путь уничтожения через DeadEntityCleanupSystem"
  - "_activeLaserVfx как дополнительный трекинг VFX вне ActionScheduler для корректной очистки при Stop()"

patterns-established:
  - "VFX tracking: _activeLaserVfx + Stop() cleanup before ResetSchedule()"

requirements-completed: [BRG-04, BRG-06]

duration: 1min
completed: 2026-04-03
---

# Phase 05 Plan 04: Laser Kill DeadTag + VFX Cleanup Summary

**Лазер убивает через DeadTag в ECS-режиме; активные VFX лазера освобождаются при смерти корабля**

## Performance

- **Duration:** 1 min
- **Started:** 2026-04-03T12:05:25Z
- **Completed:** 2026-04-03T12:07:00Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments
- ProcessShootEvents laser path добавляет DeadTag к ECS entity вместо вызова Kill(model) -- корректное уничтожение через DeadEntityCleanupSystem с позицией из Transform
- _activeLaserVfx отслеживает активные лазерные VFX; Stop() освобождает их перед ResetSchedule()
- Два регрессионных теста добавлены: LaserKill_InEcsMode и LaserVfx_ActiveList

## Task Commits

Each task was committed atomically (TDD):

1. **Task 1 RED: Regression tests** - `861b4ec` (test)
2. **Task 1 GREEN: Laser kill + VFX cleanup fix** - `1079d1c` (fix)

## Files Created/Modified
- `Assets/Scripts/Application/Game.cs` - Фикс laser kill path (DeadTag) + _activeLaserVfx трекинг + Stop() cleanup
- `Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs` - 2 новых регрессионных теста

## Decisions Made
- DeadTag вместо Kill(model) для лазера -- единый путь уничтожения; DeadEntityCleanupSystem читает позицию из Transform, а не из модели
- _activeLaserVfx как параллельный трекинг к ActionScheduler -- необходим, т.к. ResetSchedule() стирает запланированные Release

## Deviations from Plan

None -- plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Known Stubs
None

## Next Phase Readiness
- Laser kill и VFX cleanup баги закрыты
- UAT gap 1 и 2 решены; остается gap 3 (score sync) в отдельном плане

## Self-Check: PASSED

---
*Phase: 05-bridge-layer-integration*
*Completed: 2026-04-03*
