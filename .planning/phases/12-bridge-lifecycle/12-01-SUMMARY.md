---
phase: 12-bridge-lifecycle
plan: 01
subsystem: ecs-bridge-sync
tags: [ecs, sync, rocket, gameobjectsync, buffer-event]
dependency_graph:
  requires: [RocketTag, MoveData, GameObjectRef]
  provides: [RocketShootEvent, GameObjectSyncSystem-rocket-branch]
  affects: [GameObjectSyncSystem.cs, GameObjectSyncSystemTests.cs]
tech_stack:
  added: []
  patterns: [IBufferElementData-event, WithAll/WithNone-query-branching]
key_files:
  created:
    - Assets/Scripts/ECS/Components/RocketShootEvent.cs
  modified:
    - Assets/Scripts/ECS/Systems/GameObjectSyncSystem.cs
    - Assets/Tests/EditMode/ECS/GameObjectSyncSystemTests.cs
key_decisions:
  - "Ракета использует MoveData.Direction для rotation, а не отдельный RotateData -- ракета не вращается интерактивно, а летит по направлению"
  - "RocketShootEvent без IsPlayer -- ракеты только у игрока"
metrics:
  duration: 62s
  completed: 2026-04-05T20:32:45Z
  tasks_completed: 2
  tasks_total: 2
  files_created: 1
  files_modified: 2
---

# Phase 12 Plan 01: ECS-инфраструктура визуальной синхронизации ракеты

Третья ветка GameObjectSyncSystem для rotation ракеты по MoveData.Direction и компонент RocketShootEvent для событийного спавна визуала.

## Completed Tasks

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Третья ветка GameObjectSyncSystem для RocketTag (TDD) | 8951ef0 (RED), 470184a (GREEN) | GameObjectSyncSystem.cs, GameObjectSyncSystemTests.cs |
| 2 | RocketShootEvent компонент | 7c319ca | RocketShootEvent.cs |

## Key Changes

### GameObjectSyncSystem -- 3 ветки синхронизации
1. **MoveData + RotateData + GameObjectRef** -- корабль, UFO (позиция + rotation из RotateData)
2. **MoveData + GameObjectRef, без RotateData и без RocketTag** -- астероиды, пули (только позиция)
3. **MoveData + RocketTag + GameObjectRef, без RotateData** -- ракеты (позиция + rotation из MoveData.Direction через `math.atan2`)

### RocketShootEvent
IBufferElementData с полями ShooterEntity, Position, Direction. Следует паттерну GunShootEvent/LaserShootEvent. Готов для использования в плане 12-02.

## TDD Cycles

### Task 1: GameObjectSyncSystem rocket branch
- **RED:** 4 новых теста добавлены -- SyncsPositionAndRotationFromDirection_ForRocketEntity, RocketEntity_DoesNotAffectPositionOnlyBranch, RocketRotation_ZeroDegrees_ForRightDirection, RocketRotation_180Degrees_ForLeftDirection
- **GREEN:** Третья ветка с `WithAll<RocketTag>().WithNone<RotateData>()`, вторая ветка дополнена `WithNone<RocketTag>()`

## Deviations from Plan

None -- план выполнен точно как написан.

## Self-Check: PASSED
