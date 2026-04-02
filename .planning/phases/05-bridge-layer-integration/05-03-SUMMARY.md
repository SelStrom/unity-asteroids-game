---
phase: 05-bridge-layer-integration
plan: 03
subsystem: integration
tags: [unity-ecs, dots, bridge-layer, hybrid, gameplay-integration, playmode-test]

# Dependency graph
requires:
  - phase: 05-bridge-layer-integration
    plan: 01
    provides: GameObjectRef, GunShootEvent, LaserShootEvent, GameObjectSyncSystem, EntityFactory
  - phase: 05-bridge-layer-integration
    plan: 02
    provides: CollisionBridge, ObservableBridgeSystem, DeadEntityCleanupSystem

provides:
  - Integrated hybrid DOTS gameplay with _useEcs migration flag
  - Parallel Entity+GameObject creation in EntitiesCatalog
  - Input->ECS routing in Game.cs
  - Shoot event processing from ECS buffers
  - ObservableBridge HUD integration in GameScreen

affects:
  - Assets/Scripts/Application/Application.cs
  - Assets/Scripts/Application/EntitiesCatalog.cs
  - Assets/Scripts/Application/Game.cs
  - Assets/Scripts/Application/Screens/GameScreen.cs
  - Assets/Asteroids.asmdef

# Tech stack
added:
  - Unity.Mathematics reference in Asteroids.asmdef
  - Unity.Collections reference in Asteroids.asmdef

patterns:
  - Migration flag (_useEcs) for gradual switchover
  - Parallel Entity+GameObject creation per entity type
  - Singleton EntityQuery for input routing
  - Event buffer processing (GunShootEvent, LaserShootEvent)

# Key files
created:
  - Assets/Tests/PlayMode/GameplayCycleTests.cs

modified:
  - Assets/Scripts/Application/Application.cs
  - Assets/Scripts/Application/EntitiesCatalog.cs
  - Assets/Scripts/Application/Game.cs
  - Assets/Scripts/Application/Screens/GameScreen.cs
  - Assets/Tests/PlayMode/PlayModeTests.asmdef
  - Assets/Asteroids.asmdef

deleted:
  - Assets/Scripts/Bridge/AsteroidsBridge.asmdef

# Decisions
decisions:
  - Merged AsteroidsBridge.asmdef into Asteroids assembly to resolve circular dependency
  - Ship moveSpeed set to 0f (starts stationary, thrust provides acceleration)
  - ShootToEvery and MoveToEvery hardcoded as 3f matching original code
  - UfoVisual collision callback unchanged (parameterless Action doesn't carry otherGo)

# Metrics
duration: 4min
completed: 2026-04-02
tasks: 4
files: 8
---

# Phase 05 Plan 03: Bridge Layer Integration Summary

Hybrid DOTS integration: Application.cs initializes ECS World with singletons, EntitiesCatalog creates Entity+GameObject in parallel, Game.cs routes input to ECS and processes shoot-event buffers, GameScreen uses ObservableBridgeSystem for HUD.

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | 3c33986 | Application/EntitiesCatalog ECS World init + parallel Entity+GO creation |
| 2 | cc35cf6 | Game.cs input/shoot-events ECS integration + GameScreen bridge |
| 3 | ffad162 | PlayMode test TST-12 for full gameplay cycle |
| 4 | -- | Human-verify checkpoint (auto-approved) |

## Implementation Details

### Application.cs
- `_useEcs = true` migration flag
- Creates 6 singleton entities: GameAreaData, ShipPositionData, ScoreData, CollisionEventData buffer, GunShootEvent buffer, LaserShootEvent buffer
- Initializes CollisionBridge, connects DeadEntityCleanupSystem callback
- OnDeadEntity handles asteroid fragmentation, VFX, ship death
- OnUpdate: ActionScheduler + ProcessShootEvents (ECS World auto-updates)

### EntitiesCatalog.cs
- ConnectEcs(EntityManager, CollisionBridge) -- enables parallel creation
- Each Create* method: after AddToCatalog, creates ECS Entity with matching components + GameObjectRef + CollisionBridge mapping
- ReleaseByGameObject for cleanup from DeadEntityCleanupSystem
- Ship/Bullet collision callbacks redirect to CollisionBridge.ReportCollision

### Game.cs
- ConnectEcs with EntityManager for input routing
- OnAttack/OnLaser/OnRotateAction/OnTrust: write to ECS via singleton queries
- ProcessShootEvents: reads GunShootEvent/LaserShootEvent buffers, creates bullets/laser via existing catalog
- PlayEffect made public, StopFromEcs added

### GameScreen.cs
- GameScreenData.UseEcs flag
- ActivateHud: ObservableBridgeSystem.SetHudData + SetLaserMaxShoots instead of Bind.From
- DeactivateHud: ClearReferences on bridge system

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Circular assembly dependency AsteroidsBridge <-> Asteroids**
- **Found during:** Task 1 setup
- **Issue:** AsteroidsBridge.asmdef referenced Asteroids, but Application.cs (in Asteroids) needed CollisionBridge/DeadEntityCleanupSystem from Bridge
- **Fix:** Removed AsteroidsBridge.asmdef, Bridge scripts now compile as part of Asteroids assembly. Added Unity.Mathematics and Unity.Collections to Asteroids.asmdef.
- **Files modified:** Assets/Asteroids.asmdef, Assets/Scripts/Bridge/AsteroidsBridge.asmdef (deleted)
- **Commit:** 3c33986

**2. [Rule 1 - Bug] Ship collision callback used viewModel.gameObject (nonexistent)**
- **Found during:** Task 1
- **Issue:** Plan template used viewModel.gameObject but ShipViewModel is pure C#, not MonoBehaviour
- **Fix:** Set collision callback after view creation using view.gameObject
- **Files modified:** Assets/Scripts/Application/EntitiesCatalog.cs
- **Commit:** 3c33986

**3. [Rule 1 - Bug] Ship.Speed config field doesn't exist**
- **Found during:** Task 1
- **Issue:** Plan referenced _configs.Ship.Speed but GameData.ShipData has no Speed field. Ship starts stationary.
- **Fix:** Used 0f for initial moveSpeed in EntityFactory.CreateShip
- **Files modified:** Assets/Scripts/Application/EntitiesCatalog.cs
- **Commit:** 3c33986

## Known Stubs

None -- all data paths are wired through ObservableBridgeSystem and EntityFactory.

## Self-Check: PASSED
