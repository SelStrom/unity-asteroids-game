---
phase: 09-ecs-tech-debt-cleanup
plan: 01
subsystem: ecs
tags: [unity-dots, system-ordering, icomponentdata, marker-component]

requires:
  - phase: 04-ecs-foundation
    provides: ECS systems (Gun, Laser, ShootTo, MoveTo) and ShootToData component
  - phase: 05-bridge-layer
    provides: EcsShipPositionUpdateSystem for ordering dependency
provides:
  - Deterministic ordering for EcsGunSystem, EcsLaserSystem, EcsShootToSystem, EcsMoveToSystem
  - ShootToData as clean marker component (no vestigial fields)
affects: [09-02, 09-03]

tech-stack:
  added: []
  patterns: [UpdateAfter/UpdateBefore attributes for ECS system ordering]

key-files:
  created: []
  modified:
    - Assets/Scripts/ECS/Systems/EcsGunSystem.cs
    - Assets/Scripts/ECS/Systems/EcsLaserSystem.cs
    - Assets/Scripts/ECS/Systems/EcsShootToSystem.cs
    - Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs
    - Assets/Scripts/ECS/Components/ShootToData.cs
    - Assets/Scripts/ECS/EntityFactory.cs
    - Assets/Scripts/Application/EntitiesCatalog.cs
    - Assets/Tests/EditMode/ECS/ShootToSystemTests.cs
    - Assets/Tests/EditMode/ECS/ComponentTests.cs
    - Assets/Tests/EditMode/ECS/EntityFactoryTests.cs

key-decisions:
  - "Ordering attributes added only to 4 systems that depend on ShipPositionData -- other systems already have correct ordering"

patterns-established:
  - "ECS system ordering via [UpdateAfter]/[UpdateBefore] attributes for deterministic execution"

requirements-completed: [TD-01, TD-02, TD-03]

duration: 2min
completed: 2026-04-04
---

# Phase 09 Plan 01: System Ordering and ShootToData Cleanup Summary

**Ordering-атрибуты для 4 ECS-систем (Gun, Laser, ShootTo, MoveTo) и очистка ShootToData до маркерного компонента**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-04T00:46:51Z
- **Completed:** 2026-04-04T00:49:06Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments
- EcsGunSystem, EcsLaserSystem получили ordering-атрибуты (Gun после ShipPosition, перед Laser; Laser после Gun)
- EcsShootToSystem, EcsMoveToSystem получили [UpdateAfter(EcsShipPositionUpdateSystem)]
- ShootToData очищен от vestigial полей Every/ReadyRemaining -- теперь пустой маркерный компонент
- EntityFactory.CreateUfoBig/CreateUfo упрощены -- параметр shootToEvery удалён
- Все тесты обновлены для соответствия новой сигнатуре

## Task Commits

Each task was committed atomically:

1. **Task 1: Ordering-атрибуты к 4 системам** - `11d631e` (feat)
2. **Task 2: Очистка ShootToData и обновление factory/тестов** - `b89eae6` (refactor)

## Files Created/Modified
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` - [UpdateAfter(EcsShipPositionUpdateSystem)], [UpdateBefore(EcsLaserSystem)]
- `Assets/Scripts/ECS/Systems/EcsLaserSystem.cs` - [UpdateAfter(EcsGunSystem)]
- `Assets/Scripts/ECS/Systems/EcsShootToSystem.cs` - [UpdateAfter(EcsShipPositionUpdateSystem)]
- `Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs` - [UpdateAfter(EcsShipPositionUpdateSystem)]
- `Assets/Scripts/ECS/Components/ShootToData.cs` - Empty marker component (fields removed)
- `Assets/Scripts/ECS/EntityFactory.cs` - Removed shootToEvery param from CreateUfoBig/CreateUfo
- `Assets/Scripts/Application/EntitiesCatalog.cs` - Updated calls to match new factory signatures
- `Assets/Tests/EditMode/ECS/ShootToSystemTests.cs` - Updated ShootToData init to default
- `Assets/Tests/EditMode/ECS/ComponentTests.cs` - Updated test to check HasComponent only
- `Assets/Tests/EditMode/ECS/EntityFactoryTests.cs` - Removed shootToEvery args

## Decisions Made
None - followed plan as specified

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Known Stubs
None

## Next Phase Readiness
- TD-01, TD-02, TD-03 закрыты
- Готово для Plan 02 (EntitiesCatalog.CreateX refactoring) и Plan 03 (дальнейшая очистка)

---
*Phase: 09-ecs-tech-debt-cleanup*
*Completed: 2026-04-04*

## Self-Check: PASSED
- All 10 modified files exist on disk
- Commit 11d631e found in git log
- Commit b89eae6 found in git log
