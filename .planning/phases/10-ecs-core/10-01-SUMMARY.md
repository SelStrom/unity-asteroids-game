---
phase: 10-ecs-core
plan: 01
subsystem: ecs
tags: [unity-ecs, IComponentData, EntityFactory, rocket, homing]

requires:
  - phase: 09-ecs-core
    provides: "EntityFactory, AsteroidsEcsTestFixture, existing IComponentData patterns"
provides:
  - "RocketTag IComponentData marker"
  - "RocketTargetData (Entity Target + TurnRateDegPerSec)"
  - "RocketAmmoData (MaxAmmo, CurrentAmmo, ReloadDurationSec, ReloadRemaining)"
  - "EntityFactory.CreateRocket method"
  - "EntityFactory.CreateShip with RocketAmmoData"
  - "CreateRocketEntity test helper"
affects: [10-02-guidance-system, 10-03-ammo-system, 11-collision, 13-launch]

tech-stack:
  added: []
  patterns: ["RocketTargetData with Entity reference for homing target"]

key-files:
  created:
    - Assets/Scripts/ECS/Components/Tags/RocketTag.cs
    - Assets/Scripts/ECS/Components/RocketTargetData.cs
    - Assets/Scripts/ECS/Components/RocketAmmoData.cs
  modified:
    - Assets/Scripts/ECS/EntityFactory.cs
    - Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs
    - Assets/Tests/EditMode/ECS/EntityFactoryTests.cs
    - Assets/Scripts/Application/EntitiesCatalog.cs

key-decisions:
  - "RocketTargetData.Target = Entity.Null at creation -- target assigned by homing system (Plan 02)"
  - "RocketAmmoData follows LaserData pattern (MaxAmmo/CurrentAmmo/ReloadDurationSec/ReloadRemaining)"
  - "Temporary hardcoded rocket defaults (maxAmmo=3, reloadSec=5) in EntitiesCatalog until config phase"

patterns-established:
  - "Rocket entity composition: RocketTag + MoveData + LifeTimeData + RocketTargetData"
  - "Ammo data on ship entity: RocketAmmoData alongside GunData and LaserData"

requirements-completed: [ROCK-04, ROCK-05]

duration: 2min
completed: 2026-04-05
---

# Phase 10 Plan 01: ECS Rocket Components Summary

**3 IComponentData structs (RocketTag, RocketTargetData, RocketAmmoData) + EntityFactory.CreateRocket + расширенный CreateShip с боезапасом ракет**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-05T19:34:15Z
- **Completed:** 2026-04-05T19:36:18Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Created 3 new IComponentData structs following existing project patterns (BulletTag, GunData, LaserData)
- Extended EntityFactory with CreateRocket (6 params) and CreateShip (+2 rocket params)
- Added CreateRocketEntity test helper and 2 new factory tests
- All existing CreateShip test calls updated with new rocket parameters

## Task Commits

Each task was committed atomically:

1. **Task 1: Создать ECS-компоненты ракеты** - `b1b1491` (feat)
2. **Task 2: Расширить EntityFactory и тестовую fixture** - `0c8d113` (feat)

## Files Created/Modified
- `Assets/Scripts/ECS/Components/Tags/RocketTag.cs` - IComponentData маркер ракеты
- `Assets/Scripts/ECS/Components/RocketTargetData.cs` - Entity Target + TurnRateDegPerSec для наведения
- `Assets/Scripts/ECS/Components/RocketAmmoData.cs` - Боезапас ракет (MaxAmmo, CurrentAmmo, ReloadDurationSec, ReloadRemaining)
- `Assets/Scripts/ECS/EntityFactory.cs` - CreateRocket + расширенный CreateShip
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` - CreateRocketEntity + расширенный CreateShipEntity
- `Assets/Tests/EditMode/ECS/EntityFactoryTests.cs` - 2 новых теста + обновлённые вызовы CreateShip
- `Assets/Scripts/Application/EntitiesCatalog.cs` - Обновлён вызов CreateShip с временными значениями ракет

## Decisions Made
- RocketTargetData.Target = Entity.Null при создании -- цель назначается системой наведения (Plan 02)
- RocketAmmoData следует паттерну LaserData (Max/Current/ReloadDuration/ReloadRemaining) без Shooting/Direction/ShootPosition
- Временные хардкод-значения (maxAmmo=3, reloadSec=5) в EntitiesCatalog до появления конфига ракет

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Updated EntitiesCatalog.CreateShip call with new required parameters**
- **Found during:** Task 2 (EntityFactory extension)
- **Issue:** EntityFactory.CreateShip получил 2 новых обязательных параметра (rocketMaxAmmo, rocketReloadSec), но вызов в EntitiesCatalog.cs не был обновлён -- код не скомпилировался бы
- **Fix:** Добавлены временные значения rocketMaxAmmo: 3, rocketReloadSec: 5f в вызов CreateShip
- **Files modified:** Assets/Scripts/Application/EntitiesCatalog.cs
- **Verification:** grep подтвердил наличие новых параметров
- **Committed in:** 0c8d113 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Auto-fix necessary for compilation. Temporary hardcoded values will be replaced when rocket config is added.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- RocketTag, RocketTargetData, RocketAmmoData ready for Plan 02 (guidance system) and Plan 03 (ammo system)
- CreateRocketEntity helper ready for system tests
- EntityFactory.CreateRocket ready for spawn logic (Phase 13)
- EntitiesCatalog needs rocket config values when ScriptableObject config is added

---
*Phase: 10-ecs-core*
*Completed: 2026-04-05*
