---
phase: 08-ufo-asteroid-collision
plan: 01
subsystem: collision-system
tags: [ecs, collision, ufo, asteroid, tdd]
dependency_graph:
  requires: []
  provides: [ufo-asteroid-collision-handling]
  affects: [EcsCollisionHandlerSystem, CollisionHandlerTests]
tech_stack:
  added: []
  patterns: [IsAsteroid/IsUfoAny helpers, symmetric collision pair handling]
key_files:
  created: []
  modified:
    - Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs
    - Assets/Tests/EditMode/ECS/CollisionHandlerTests.cs
decisions:
  - "IsAsteroid + IsUfoAny helpers for readability, placed after Ship+Enemy block"
metrics:
  duration: 2min
  completed: "2026-04-03T22:35:00Z"
---

# Phase 08 Plan 01: UFO+Asteroid Collision Handling Summary

Обработка столкновений Asteroid+UFO в EcsCollisionHandlerSystem с 4 TDD-тестами -- оба entity получают DeadTag, очки не начисляются.

## Tasks Completed

| # | Task | Commit | Key Changes |
|---|------|--------|-------------|
| 1 | Регрессионные тесты UFO+Asteroid коллизий | e0996c5 | 4 новых теста в CollisionHandlerTests |
| 2 | Реализация UFO+Asteroid коллизии в ProcessCollision | 37d9bc3 | IsAsteroid, IsUfoAny хелперы + обработка в ProcessCollision |

## Changes Made

### EcsCollisionHandlerSystem.cs
- Добавлен хелпер `IsAsteroid(ref EntityManager, Entity)` -- проверка AsteroidTag
- Добавлен хелпер `IsUfoAny(ref EntityManager, Entity)` -- проверка UfoBigTag или UfoTag
- В `ProcessCollision` добавлены два блока после Ship+Enemy: Asteroid+UFO (оба порядка entityA/entityB)
- Оба entity получают DeadTag, очки не начисляются (столкновение без участия игрока)

### CollisionHandlerTests.cs
- `AsteroidHitsUfo_BothGetDeadTag` -- Asteroid + Ufo, оба получают DeadTag
- `AsteroidHitsUfoBig_BothGetDeadTag` -- Asteroid + UfoBig, оба получают DeadTag
- `AsteroidHitsUfo_ScoreNotChanged` -- Score остается 0 при столкновении без игрока
- `AsteroidHitsUfo_ReversedOrder_BothGetDeadTag` -- обратный порядок entityA/entityB

## Deviations from Plan

None -- план выполнен точно как написано.

## Decisions Made

1. **IsAsteroid/IsUfoAny размещение:** Хелперы добавлены после IsEnemy для группировки связанных проверок.

## Known Stubs

None.

## Verification

- 4 новых теста покрывают Asteroid+Ufo, Asteroid+UfoBig, отсутствие очков, обратный порядок entities
- Существующие 6 тестов не затронуты (PlayerBullet+Asteroid, PlayerBullet+UfoBig, EnemyBullet+Ship, Ship+Asteroid, NoCollisionEvents, PlayerBullet+Asteroid score)
- grep "IsAsteroid" и "IsUfoAny" подтверждают наличие хелперов

## Self-Check: PASSED
