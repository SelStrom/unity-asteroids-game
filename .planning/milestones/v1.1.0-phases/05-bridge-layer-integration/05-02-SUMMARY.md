---
phase: 05-bridge-layer-integration
plan: 02
subsystem: bridge
tags: [ecs, dots, collision-bridge, observable-bridge, cleanup-system, tdd]

requires:
  - phase: 04-ecs-foundation
    provides: ECS components (MoveData, RotateData, ThrustData, LaserData, DeadTag, ShipTag, CollisionEventData)
provides:
  - CollisionBridge utility for Physics2D->ECS collision proxying
  - ObservableBridgeSystem for ECS->MVVM HUD data synchronization
  - DeadEntityCleanupSystem for dead entity lifecycle management
  - GameObjectRef ICleanupComponentData for Entity-GameObject binding
  - AsteroidsBridge.asmdef assembly for bridge layer
affects: [05-03-integration, game-screen, entities-catalog]

tech-stack:
  added: [AsteroidsBridge assembly]
  patterns: [managed SystemBase for bridge, Dictionary<GameObject,Entity> mapping, ECB cleanup pattern]

key-files:
  created:
    - Assets/Scripts/Bridge/CollisionBridge.cs
    - Assets/Scripts/Bridge/ObservableBridgeSystem.cs
    - Assets/Scripts/Bridge/DeadEntityCleanupSystem.cs
    - Assets/Scripts/Bridge/AsteroidsBridge.asmdef
    - Assets/Scripts/ECS/Components/GameObjectRef.cs
    - Assets/Tests/EditMode/ECS/CollisionBridgeTests.cs
    - Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs
    - Assets/Tests/EditMode/ECS/DeadEntityCleanupSystemTests.cs
  modified:
    - Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef

key-decisions:
  - "AsteroidsBridge.asmdef: separate assembly referencing both Asteroids and AsteroidsECS to avoid circular dependencies"
  - "GameObjectRef as ICleanupComponentData in AsteroidsECS namespace for proper lifecycle management"
  - "ObservableBridgeSystem uses SystemBase (managed) for access to ReactiveValue types"

patterns-established:
  - "Bridge pattern: managed SystemBase classes bridge ECS data to MVVM ReactiveValues"
  - "ECB cleanup: RemoveComponent<GameObjectRef> before DestroyEntity for ICleanupComponentData"

requirements-completed: [BRG-03, BRG-04, BRG-05, TST-10]

duration: 4min
completed: 2026-04-03
---

# Phase 5 Plan 2: Bridge Systems Summary

**CollisionBridge, ObservableBridgeSystem и DeadEntityCleanupSystem -- три bridge-компонента для связи ECS с GameObjects/MVVM, с 16 EditMode TDD-тестами**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-02T22:40:12Z
- **Completed:** 2026-04-02T22:44:36Z
- **Tasks:** 2/2
- **Files modified:** 9

## Accomplishments

### Task 1: CollisionBridge utility и тесты

CollisionBridge -- utility-класс для проксирования Physics2D коллизий из MonoBehaviour в ECS CollisionEventData буфер.

- `Dictionary<GameObject, Entity>` маппинг для O(1) разрешения Entity по GameObject
- `RegisterMapping/UnregisterMapping` для управления маппингом
- `ReportCollision(selfGo, otherGo)` добавляет CollisionEventData в singleton buffer
- 5 EditMode тестов: оба зарегистрированы, self/other не зарегистрирован, отмена регистрации, множественные коллизии

### Task 2: ObservableBridgeSystem, DeadEntityCleanupSystem и тесты

ObservableBridgeSystem -- managed SystemBase для синхронизации ECS-данных в MVVM ReactiveValue:
- Координаты, скорость, ротация, лазер -> HudData
- Позиция, ротация, спрайт тяги -> ShipViewModel
- Формат строк 1:1 с GameScreen.ActivateHud()
- 7 EditMode тестов

DeadEntityCleanupSystem -- cleanup-система для entities с DeadTag:
- Entity с DeadTag + GameObjectRef -> callback для Release + уничтожение Entity
- Entity с DeadTag без GameObjectRef -> уничтожение Entity
- ECB pattern: RemoveComponent<GameObjectRef> перед DestroyEntity (ICleanupComponentData)
- 4 EditMode тестов

## Commits

| Commit | Type | Description |
|--------|------|-------------|
| 1cae1d1 | feat | CollisionBridge with TDD tests, GameObjectRef, AsteroidsBridge.asmdef |
| 1e2a4aa | feat | ObservableBridgeSystem and DeadEntityCleanupSystem with TDD tests |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Created AsteroidsBridge.asmdef assembly**
- **Found during:** Task 1
- **Issue:** Bridge classes need access to both Asteroids (HudData, ShipViewModel) and AsteroidsECS (CollisionEventData, ShipTag) assemblies. Placing files in Assets/Scripts/Bridge/ under Asteroids.asmdef would not have access to ECS types.
- **Fix:** Created separate AsteroidsBridge.asmdef referencing both assemblies
- **Files created:** Assets/Scripts/Bridge/AsteroidsBridge.asmdef

**2. [Rule 3 - Blocking] Created GameObjectRef component**
- **Found during:** Task 1
- **Issue:** DeadEntityCleanupSystem requires GameObjectRef (ICleanupComponentData) which is specified in plan 05-01 but not yet available (parallel execution)
- **Fix:** Created GameObjectRef in Assets/Scripts/ECS/Components/ (same location as 05-01 plan specifies for easy merge)
- **Files created:** Assets/Scripts/ECS/Components/GameObjectRef.cs

**3. [Rule 3 - Blocking] Updated EcsEditModeTests.asmdef references**
- **Found during:** Task 1
- **Issue:** Test assembly needed references to AsteroidsBridge, Asteroids, and Shtl.Mvvm for bridge tests
- **Fix:** Added AsteroidsBridge, Asteroids, Shtl.Mvvm to EcsEditModeTests.asmdef references
- **Files modified:** Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef

## Known Stubs

None -- all bridge systems are fully wired to ECS components and MVVM types.

## Self-Check: PASSED
