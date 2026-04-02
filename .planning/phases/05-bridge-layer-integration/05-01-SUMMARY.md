---
phase: 05-bridge-layer-integration
plan: 01
subsystem: ecs
tags: [unity-ecs, dots, bridge-layer, icleanupcomponent, ibufferelementdata, systembase, tdd]

# Dependency graph
requires:
  - phase: 04-ecs-components-systems
    provides: ECS компоненты (MoveData, RotateData, GunData, LaserData, LifeTimeData, DeadTag, ShipTag) и системы (EcsGunSystem, EcsLaserSystem, EcsLifeTimeSystem)
provides:
  - GameObjectRef (ICleanupComponentData) для связки Entity с Transform/GameObject
  - GunShootEvent и LaserShootEvent (IBufferElementData) для событий стрельбы
  - GameObjectSyncSystem для синхронизации ECS Position/Rotation в Transform
  - EcsDeadByLifeTimeSystem для добавления DeadTag при TimeRemaining <= 0
  - Assembly references Asteroids -> AsteroidsECS + Unity.Entities
  - TDD-тесты EcsGunSystem (TST-05) и EcsLaserSystem (TST-06)
affects: [05-02, 05-03, bridge-systems, entity-lifecycle]

# Tech tracking
tech-stack:
  added: []
  patterns: [managed-cleanup-component, singleton-event-buffer, presentation-sync-system]

key-files:
  created:
    - Assets/Scripts/ECS/Components/GameObjectRef.cs
    - Assets/Scripts/ECS/Components/GunShootEvent.cs
    - Assets/Scripts/ECS/Components/LaserShootEvent.cs
    - Assets/Scripts/ECS/Systems/EcsDeadByLifeTimeSystem.cs
    - Assets/Scripts/ECS/Systems/GameObjectSyncSystem.cs
    - Assets/Tests/EditMode/ECS/EcsGunSystemTests.cs
    - Assets/Tests/EditMode/ECS/EcsLaserSystemTests.cs
    - Assets/Tests/EditMode/ECS/GameObjectSyncSystemTests.cs
  modified:
    - Assets/Asteroids.asmdef
    - Assets/Scripts/ECS/Systems/EcsGunSystem.cs
    - Assets/Scripts/ECS/Systems/EcsLaserSystem.cs
    - Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef
    - Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs

key-decisions:
  - "GameObjectRef как ICleanupComponentData (managed class) для доступа к Transform/GameObject"
  - "Singleton DynamicBuffer для shoot-events вместо per-entity буферов"
  - "GameObjectSyncSystem в PresentationSystemGroup для выполнения после всех Simulation-систем"
  - "EcsDeadByLifeTimeSystem как managed SystemBase из-за structural change (AddComponent)"

patterns-established:
  - "Managed cleanup component: class : ICleanupComponentData для хранения ссылок на Unity-объекты"
  - "Singleton event buffer: DynamicBuffer<TEvent> на singleton entity для межсистемной коммуникации"
  - "SystemAPI.GetSingletonBuffer<T>() + RequireForUpdate<T>() в OnCreate для event-driven систем"
  - "AddComponentObject для managed компонентов в тестах вместо AddComponentData"
  - "World.CreateSystemManaged<T>() для тестирования SystemBase систем"

requirements-completed: [BRG-01, BRG-02, ECS-07, ECS-08, TST-05, TST-06]

# Metrics
duration: 3min
completed: 2026-04-02
---

# Phase 05 Plan 01: Bridge Layer Foundation Summary

**Bridge-компоненты GameObjectRef/GunShootEvent/LaserShootEvent, GameObjectSyncSystem для ECS->Transform, DeadByLifeTimeSystem и TDD-тесты Gun/Laser систем**

## Performance

- **Duration:** 3 min
- **Started:** 2026-04-02T22:40:12Z
- **Completed:** 2026-04-02T22:43:13Z
- **Tasks:** 2
- **Files modified:** 13

## Accomplishments
- Bridge-компоненты: GameObjectRef (managed ICleanupComponentData), GunShootEvent и LaserShootEvent (IBufferElementData)
- EcsGunSystem и EcsLaserSystem расширены singleton event buffer записью при выстреле
- GameObjectSyncSystem синхронизирует ECS позицию/ротацию в Transform (PresentationSystemGroup)
- EcsDeadByLifeTimeSystem добавляет DeadTag entities с истекшим LifeTime
- 18 TDD-тестов: 7 для EcsGunSystem (TST-05), 6 для EcsLaserSystem (TST-06), 5 для GameObjectSyncSystem
- Assembly references настроены для bridge-слоя (Asteroids -> AsteroidsECS + Unity.Entities)

## Task Commits

Each task was committed atomically:

1. **Task 1: Bridge-компоненты, assembly setup, Gun/Laser/LifeTime расширения и TDD-тесты** - `4dd8d2b` (feat)
2. **Task 2: GameObjectSyncSystem и EditMode тесты** - `dc24968` (feat)

## Files Created/Modified
- `Assets/Scripts/ECS/Components/GameObjectRef.cs` - Managed ICleanupComponentData с Transform и GameObject
- `Assets/Scripts/ECS/Components/GunShootEvent.cs` - IBufferElementData для событий стрельбы пушки
- `Assets/Scripts/ECS/Components/LaserShootEvent.cs` - IBufferElementData для событий лазера
- `Assets/Scripts/ECS/Systems/EcsDeadByLifeTimeSystem.cs` - Добавление DeadTag при TimeRemaining <= 0
- `Assets/Scripts/ECS/Systems/GameObjectSyncSystem.cs` - Синхронизация ECS позиции/ротации в Transform
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` - Расширен shoot-event записью и RequireForUpdate
- `Assets/Scripts/ECS/Systems/EcsLaserSystem.cs` - Расширен shoot-event записью и RequireForUpdate
- `Assets/Asteroids.asmdef` - Добавлены ссылки AsteroidsECS и Unity.Entities
- `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef` - Добавлены ссылки Asteroids и Shtl.Mvvm
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` - Хелперы CreateGunShootEventSingleton/CreateLaserShootEventSingleton
- `Assets/Tests/EditMode/ECS/EcsGunSystemTests.cs` - 7 TDD-тестов для EcsGunSystem (TST-05)
- `Assets/Tests/EditMode/ECS/EcsLaserSystemTests.cs` - 6 TDD-тестов для EcsLaserSystem (TST-06)
- `Assets/Tests/EditMode/ECS/GameObjectSyncSystemTests.cs` - 5 EditMode тестов для GameObjectSyncSystem

## Decisions Made
- GameObjectRef реализован как managed class : ICleanupComponentData (per D-01) для хранения ссылок на Unity-объекты
- Singleton DynamicBuffer используется для shoot-events (per plan D-17, без Burst для систем с managed доступом)
- GameObjectSyncSystem размещен в PresentationSystemGroup (per D-06) для гарантии выполнения после Simulation
- EcsDeadByLifeTimeSystem как managed SystemBase из-за structural change AddComponent через EntityCommandBuffer

## Deviations from Plan

None - план выполнен точно как написан.

## Issues Encountered
None

## User Setup Required
None - настройка внешних сервисов не требуется.

## Known Stubs
None - все компоненты и системы полностью реализованы.

## Next Phase Readiness
- Bridge-компоненты готовы для использования в планах 05-02 и 05-03
- GameObjectSyncSystem готов к интеграции с entity lifecycle системами
- Shoot-event буферы готовы для SpawnBulletBridgeSystem и LaserRaycastBridgeSystem

## Self-Check: PASSED

All 8 created files verified. Both task commits (4dd8d2b, dc24968) confirmed in git log.

---
*Phase: 05-bridge-layer-integration*
*Completed: 2026-04-02*
