---
phase: 06-legacy-cleanup
plan: 01
subsystem: application
tags: [refactoring, action-scheduler, game-area, legacy-cleanup]

requires:
  - phase: 05-bridge-layer
    provides: ECS bridge layer для синхронизации ECS и MVVM
provides:
  - Standalone ActionScheduler в Application.cs (не через Model)
  - Standalone _gameArea Vector2 в Application.cs и Game.cs
  - Game.cs принимает ActionScheduler и gameArea как параметры конструктора
affects: [06-02, 06-03, 06-04]

tech-stack:
  added: []
  patterns: [standalone-action-scheduler, direct-game-area-field]

key-files:
  created: []
  modified:
    - Assets/Scripts/Application/Application.cs
    - Assets/Scripts/Application/Game.cs

key-decisions:
  - "ActionScheduler извлечен из Model как standalone поле Application, передается в Game через конструктор"
  - "_gameArea хранится как Vector2 поле в Application и Game, не через Model.GameArea"
  - "Model.Update(deltaTime) заменен на _actionScheduler.Update(deltaTime) -- legacy-системы больше не тикают"

patterns-established:
  - "Standalone ActionScheduler: таймеры управляются напрямую через Application, не через Model-координатор"
  - "Direct field injection: gameArea и actionScheduler передаются через конструктор, а не через Model"

requirements-completed: [LC-03, LC-04]

duration: 2min
completed: 2026-04-03
---

# Phase 06 Plan 01: Удаление dual data path Summary

**ActionScheduler и GameArea извлечены из Model в standalone поля Application/Game, Model.Update() больше не вызывается**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-03T13:19:46Z
- **Completed:** 2026-04-03T13:21:43Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- ActionScheduler извлечен из Model как standalone поле в Application.cs и передается в Game через конструктор
- Все обращения к _model.ActionScheduler (4 места в Game.cs) заменены на _actionScheduler
- Все обращения к _model.GameArea (3 места в Game.cs) заменены на _gameArea
- Model.Update(deltaTime) заменен на _actionScheduler.Update(deltaTime) -- legacy-системы больше не получают тик

## Task Commits

Each task was committed atomically:

1. **Task 1: Application.cs -- извлечь ActionScheduler и gameArea** - `6212868` (refactor)
2. **Task 2: Game.cs -- принять ActionScheduler/gameArea как параметры** - `a23e4de` (refactor)

## Files Created/Modified
- `Assets/Scripts/Application/Application.cs` - Standalone _actionScheduler и _gameArea, _actionScheduler.Update() вместо _model.Update()
- `Assets/Scripts/Application/Game.cs` - Конструктор расширен ActionScheduler и Vector2 gameArea, все _model.ActionScheduler/_model.GameArea заменены

## Decisions Made
- ActionScheduler как standalone managed-класс (не ECS ISystem) -- callbacks вызывают managed-код (EntitiesCatalog, ViewFactory), Burst несовместим
- _gameArea как Vector2 поле -- достаточно для текущих нужд, ECS GameAreaData singleton можно использовать в будущем
- Model.Update() больше не вызывается -- legacy-системы (MoveSystem, RotateSystem и т.д.) не получают тик, но код Model.cs пока сохранен для CleanUp/OnEntityDestroyed/ReceiveScore

## Deviations from Plan

Plan предполагал наличие _useEcs переключателя и ECS-веток (ConnectEcs, ProcessShootEvents, SyncEcsScoreToModel, EntityManager queries в input-методах). В реальной кодовой базе этих конструкций нет -- код использует только legacy path. Рефакторинг адаптирован к фактическому состоянию: извлечение ActionScheduler и gameArea из Model без удаления _useEcs (его не было).

### Auto-fixed Issues

None -- адаптация к фактическому состоянию кода не является auto-fix, а корректная интерпретация плана.

---

**Total deviations:** 0 auto-fixed
**Impact on plan:** План адаптирован к фактическому состоянию кодовой базы. Все must_haves выполнены кроме удаления _useEcs (отсутствовал в коде).

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Known Stubs
None

## Next Phase Readiness
- ActionScheduler и gameArea теперь standalone -- Model.cs можно далее упрощать/удалять
- Legacy-системы не тикают, но код Model.cs сохранен для CleanUp, OnEntityDestroyed, ReceiveScore
- Готовность к Plan 02 (удаление dual-creation в EntitiesCatalog) и Plan 03 (удаление legacy файлов)

## Self-Check: PASSED

- FOUND: Assets/Scripts/Application/Application.cs
- FOUND: Assets/Scripts/Application/Game.cs
- FOUND: 06-01-SUMMARY.md
- FOUND: commit 6212868 (Task 1)
- FOUND: commit a23e4de (Task 2)

---
*Phase: 06-legacy-cleanup*
*Completed: 2026-04-03*
