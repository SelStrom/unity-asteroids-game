---
phase: 06-legacy-cleanup
plan: 03
subsystem: architecture
tags: [legacy-removal, ecs, cleanup, model-deletion]

requires:
  - phase: 06-02
    provides: "Все legacy-consumer'ы рефакторены, код не ссылается на legacy-модели"
provides:
  - "Полное удаление legacy-слоя: 9 систем, 6 моделей/интерфейсов, 9 компонентов"
  - "Удаление Model.cs и ModelFactory.cs"
  - "Тесты без зависимостей от legacy-типов"
affects: [06-04-validation]

tech-stack:
  added: []
  patterns: ["ECS-only data path"]

key-files:
  created: []
  modified:
    - Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs
    - Assets/Tests/EditMode/Upgrade/PackageCompatibilityTests.cs

key-decisions:
  - "ObservableBridgeSystem уже был очищен от _model в 06-02 -- дополнительных изменений не потребовалось"

patterns-established:
  - "ECS-only: проект больше не содержит legacy Model/Component/System слоя"

requirements-completed: [LC-01, LC-02, LC-05, LC-06]

duration: 3min
completed: 2026-04-03
---

# Phase 06 Plan 03: Legacy Cleanup Summary

**Удалены 27 legacy-файлов (Systems, Entities, Components, Model.cs, ModelFactory.cs), тесты переведены на ECS-only API**

## Performance

- **Duration:** 3 min
- **Started:** 2026-04-03T13:37:24Z
- **Completed:** 2026-04-03T13:40:30Z
- **Tasks:** 2
- **Files modified:** 57 (55 deleted + 2 refactored)

## Accomplishments
- Удалены все 9 legacy-систем (BaseModelSystem, MoveSystem, RotateSystem, ThrustSystem, GunSystem, LaserSystem, ShootToSystem, MoveToSystem, LifeTimeSystem)
- Удалены все 6 legacy-моделей и интерфейсов (ShipModel, AsteroidModel, BulletModel, UfoBigModel, IGameEntityModel, IGroupVisitor)
- Удалены все 9 legacy-компонентов (GunComponent, LaserComponent, IModelComponent, LifeTimeComponent, MoveComponent, MoveToComponent, RotateComponent, ShootToComponent, ThrustComponent)
- Удалены Model.cs и ModelFactory.cs
- В Assets/Scripts/Model/ остался только ActionScheduler.cs (standalone)
- 4 теста рефакторены с legacy на ECS-only API

## Task Commits

1. **Task 1: Удалить legacy-файлы** - `f59fd5d` (chore)
2. **Task 2: Рефакторить тесты** - `9b86a00` (refactor)
3. **Deviation fix: PackageCompatibilityTests** - `6e863c5` (fix)

## Files Created/Modified
- 55 файлов удалены (Systems/, Entities/, Components/, Model.cs, ModelFactory.cs + .meta)
- `Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs` - 4 теста заменены на ECS-only версии
- `Assets/Tests/EditMode/Upgrade/PackageCompatibilityTests.cs` - MoveComponent заменен на MoveData

## Decisions Made
- ObservableBridgeSystem не требовал изменений -- уже очищен от _model в 06-02

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] PackageCompatibilityTests ссылался на удаленный MoveComponent**
- **Found during:** Task 2 (финальная верификация)
- **Issue:** Тест PackageCompatibilityTests.CoreGameTypesExist() использовал `using Model.Components` и `typeof(MoveComponent)` -- оба удалены
- **Fix:** Заменил `using Model.Components` на `using SelStrom.Asteroids.ECS`, `MoveComponent` на `MoveData`
- **Files modified:** Assets/Tests/EditMode/Upgrade/PackageCompatibilityTests.cs
- **Verification:** grep подтверждает 0 ссылок на legacy namespace
- **Committed in:** 6e863c5

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Необходимый фикс для компилируемости тестов после удаления legacy-типов.

## Issues Encountered
None

## User Setup Required
None

## Next Phase Readiness
- Все legacy-файлы удалены, проект содержит только ECS data path
- Готов к 06-04 (финальная валидация)

---
*Phase: 06-legacy-cleanup*
*Completed: 2026-04-03*
