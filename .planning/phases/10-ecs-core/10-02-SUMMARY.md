---
phase: 10-ecs-core
plan: 02
subsystem: ecs-rocket-guidance
tags: [ecs, guidance, homing, tdd]
dependency_graph:
  requires: [10-01]
  provides: [EcsRocketGuidanceSystem]
  affects: [EcsMoveSystem-ordering]
tech_stack:
  added: []
  patterns: [SystemBase, cross-product-rotation, distancesq-nearest-search]
key_files:
  created:
    - Assets/Scripts/ECS/Systems/EcsRocketGuidanceSystem.cs
    - Assets/Tests/EditMode/ECS/RocketGuidanceSystemTests.cs
  modified: []
decisions:
  - SystemBase (не ISystem) для managed-вызовов EntityManager.Exists/HasComponent
  - internal static RotateTowards для тестируемости cross-product логики
metrics:
  duration: 101s
  completed: 2026-04-05T19:40:01Z
  tasks: 2
  files: 2
---

# Phase 10 Plan 02: EcsRocketGuidanceSystem -- система наведения ракеты Summary

Система наведения ракеты через seek с ограниченным turn rate: cross-product для направления поворота, distancesq для поиска ближайшего врага среди AsteroidTag/UfoBigTag/UfoTag, переключение при DeadTag или уничтожении цели.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | RED -- failing-тесты наведения | 4e2cac0 | RocketGuidanceSystemTests.cs, EcsRocketGuidanceSystem.cs (заглушка) |
| 2 | GREEN -- реализация EcsRocketGuidanceSystem | 376c05d | EcsRocketGuidanceSystem.cs |

## Implementation Details

### EcsRocketGuidanceSystem

- `partial class EcsRocketGuidanceSystem : SystemBase` с `[UpdateAfter(typeof(EcsMoveSystem))]`
- OnUpdate: итерация по `RefRW<MoveData>, RefRW<RocketTargetData>` с `.WithAll<RocketTag>().WithNone<DeadTag>()`
- Валидация цели: `EntityManager.Exists(target)` + `!HasComponent<DeadTag>(target)`
- FindClosestEnemy: 3 foreach по AsteroidTag, UfoBigTag, UfoTag (все с WithNone<DeadTag>), минимум distancesq
- RotateTowards: cross-product для знака, acos для угла, clamp maxAngle = turnRate * deltaTime, normalizesafe

### Тесты (9 штук)

1. NoEnemies_DirectionUnchanged
2. SingleEnemy_TurnsTowardsEnemy
3. TurnRate_LimitsRotationPerFrame
4. MultipleEnemies_TargetsClosest
5. TargetWithDeadTag_Retargets
6. AllEnemiesDead_FliesStraight
7. RocketWithDeadTag_NotProcessed
8. AlreadyFacingTarget_DirectionUnchanged
9. TargetEntityDestroyed_Retargets

## Decisions Made

1. **SystemBase вместо ISystem** -- managed-вызовы EntityManager.Exists и HasComponent не совместимы с Burst/ISystem struct
2. **internal static RotateTowards** -- доступен для юнит-тестов при необходимости, статический т.к. не зависит от состояния системы

## Deviations from Plan

None -- план выполнен точно как написан.

## Known Stubs

None.

## Self-Check: PASSED

- All 2 created files exist on disk
- All 2 task commits found in git history (4e2cac0, 376c05d)
