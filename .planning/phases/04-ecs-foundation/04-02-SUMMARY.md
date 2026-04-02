---
phase: 04-ecs-foundation
plan: 02
subsystem: ecs
tags: [unity-entities, dots, burst, ecs-systems, entity-factory]

# Dependency graph
requires:
  - phase: 04-ecs-foundation
    plan: 01
    provides: ECS components, tags, test fixture, AsteroidsECS assembly
provides:
  - EntityFactory with 5 creation methods (CreateShip, CreateAsteroid, CreateBullet, CreateUfoBig, CreateUfo)
  - ScoreValue on Asteroid, UfoBig, Ufo entities for collision scoring
  - EcsRotateSystem with Burst (90 deg/sec 2D rotation)
  - EcsThrustSystem with Burst (acceleration/deceleration with MaxSpeed clamp)
  - EcsMoveSystem with Burst (toroidal wrapping via PlaceWithinGameArea)
  - EcsShipPositionUpdateSystem (singleton update from ShipTag entity)
  - EcsLifeTimeSystem with Burst (TimeRemaining decrement)
  - System execution order Rotate -> Thrust -> Move -> ShipPositionUpdate -> LifeTime
  - 21 EditMode tests covering factory and systems
affects: [04-03, 04-04]

# Tech stack
added: []
patterns:
  - "[BurstCompile] on ISystem struct + OnUpdate for Burst-compatible systems"
  - "SystemAPI.Query<RefRW<T>> for component iteration in ISystem"
  - "SystemAPI.GetSingleton/SetSingleton for shared data access"
  - "Static EntityFactory pattern for entity creation with consistent archetypes"
  - "UpdateBefore/UpdateAfter attributes for deterministic system ordering"

# Key files
created:
  - Assets/Scripts/ECS/EntityFactory.cs
  - Assets/Scripts/ECS/Systems/EcsRotateSystem.cs
  - Assets/Scripts/ECS/Systems/EcsThrustSystem.cs
  - Assets/Scripts/ECS/Systems/EcsMoveSystem.cs
  - Assets/Scripts/ECS/Systems/EcsShipPositionUpdateSystem.cs
  - Assets/Scripts/ECS/Systems/EcsLifeTimeSystem.cs
  - Assets/Tests/EditMode/ECS/EntityFactoryTests.cs
  - Assets/Tests/EditMode/ECS/RotateSystemTests.cs
  - Assets/Tests/EditMode/ECS/ThrustSystemTests.cs
  - Assets/Tests/EditMode/ECS/MoveSystemTests.cs
modified: []

# Decisions
key-decisions:
  - "2D rotation via math.sin/cos instead of Quaternion for Burst compatibility"
  - "ShipPositionData update separated into own non-Burst system to keep MoveSystem fully Burst-compatible"
  - "PlaceWithinGameArea preserves original wrapping logic 1:1 (including known edge-case bug)"

# Metrics
duration: 4min
completed: "2026-04-02T21:55:34Z"
tasks_completed: 2
tasks_total: 2
files_created: 10
files_modified: 0
---

# Phase 04 Plan 02: EntityFactory + Core Systems Summary

EntityFactory (5 методов создания entities с корректными archetypes включая ScoreValue) и 5 ECS-систем: 3 с Burst (Rotate, Thrust, Move) + ShipPositionUpdate (singleton) + LifeTime, цепочка Rotate->Thrust->Move->ShipPositionUpdate->LifeTime, 21 тест.

## What Was Done

### Task 1: EntityFactory и тесты (TDD)
- Создан `EntityFactory` -- статический класс с 5 методами: `CreateShip`, `CreateAsteroid`, `CreateBullet`, `CreateUfoBig`, `CreateUfo`
- Каждый метод создает entity с полным набором компонентов согласно маппингу из оригинальной архитектуры
- `ScoreValue` добавляется к Asteroid, UfoBig, Ufo (для подсчета очков в CollisionHandler)
- Ship и Bullet НЕ получают ScoreValue
- 10 тестов покрывают все типы сущностей, проверяют наличие компонентов и начальные значения

### Task 2: Burst-системы и тесты (TDD)
- **EcsRotateSystem** [BurstCompile]: 2D rotation через sin/cos с math.radians(90 * dt * direction)
- **EcsThrustSystem** [BurstCompile]: ускорение (velocity = direction*speed + rotation*acceleration, normalizesafe, clamp MaxSpeed), замедление (speed -= unitsPerSecond/2 * dt, clamp MinSpeed)
- **EcsMoveSystem** [BurstCompile]: position += direction * speed * dt, toroidal wrapping через PlaceWithinGameArea с GameAreaData singleton
- **EcsShipPositionUpdateSystem** (без Burst): обновляет ShipPositionData singleton из MoveData entity с ShipTag
- **EcsLifeTimeSystem** [BurstCompile]: TimeRemaining = max(TimeRemaining - dt, 0)
- 11 тестов: 3 rotate (CCW/CW/zero), 4 thrust (accel/clamp/decel/minSpeed), 4 move (position/wrapRight/wrapLeft/singletonUpdate)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed redundant using directives**
- **Found during:** Task 2 refactor
- **Issue:** System files had `using SelStrom.Asteroids.ECS;` while already being in that namespace
- **Fix:** Removed self-referencing using directives from all 5 system files
- **Files modified:** All 5 system .cs files
- **Commit:** ebedbb1

## Commits

| Commit | Type | Description |
|--------|------|-------------|
| 9079373 | test | add failing tests for EntityFactory |
| 1a88b9d | feat | implement EntityFactory with 5 entity creation methods |
| 2942d0b | test | add failing tests for Rotate, Thrust, Move systems |
| b180f52 | feat | implement 5 ECS systems (3 Burst + ShipPositionUpdate + LifeTime) |
| ebedbb1 | refactor | remove redundant using directives in ECS systems |

## Known Stubs

None -- all systems implement complete logic, all factory methods wire full component data.

## Self-Check: PASSED

- All 10 created files verified on disk
- All 5 commits verified in git log
