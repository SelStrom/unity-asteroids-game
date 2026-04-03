---
phase: 06-legacy-cleanup
plan: 02
subsystem: application
tags: [refactoring, ecs, entity-type, legacy-removal, shoot-events, input-bridge]

requires:
  - phase: 06-legacy-cleanup
    plan: 01
    provides: Standalone ActionScheduler и gameArea, Model.Update() не вызывается
  - phase: 05-bridge-layer
    provides: ECS bridge systems (ObservableBridgeSystem, DeadEntityCleanupSystem, CollisionBridge)
provides:
  - EntitiesCatalog без ModelFactory с EntityType enum tracking
  - Application.cs с полной ECS инициализацией (singletons, bridges, callbacks)
  - Game.cs с ECS-only input и ShipPositionData для спавна
  - GameScreen.cs без legacy Model/ShipModel зависимостей
  - ShootEventProcessorSystem для обработки GunShootEvent/LaserShootEvent
  - ObservableBridgeSystem без Model зависимости
affects: [06-03, 06-04]

tech-stack:
  added: []
  patterns: [entity-type-enum, ecs-input-bridge, shoot-event-processor, ecs-singleton-init]

key-files:
  created:
    - Assets/Scripts/Bridge/ShootEventProcessorSystem.cs
  modified:
    - Assets/Scripts/Application/EntitiesCatalog.cs
    - Assets/Scripts/Application/Application.cs
    - Assets/Scripts/Application/Game.cs
    - Assets/Scripts/Application/Screens/GameScreen.cs
    - Assets/Scripts/Bridge/ObservableBridgeSystem.cs

key-decisions:
  - "EntityType enum вместо TryFindModel для определения типа entity в OnDeadEntity"
  - "Player input пишется напрямую в ECS components (RotateData, ThrustData, GunData, LaserData) через EntityManager"
  - "ShootEventProcessorSystem как managed SystemBase bridge -- читает GunShootEvent/LaserShootEvent и создает bullets/laser VFX"
  - "Score читается из ECS ScoreData через Game.GetCurrentScore(), не через Model.Score"
  - "ECS singletons (GameAreaData, ScoreData, event buffers) инициализируются в Application.Start()"

patterns-established:
  - "ECS singleton initialization: Application.Start() создает все необходимые singletons перед началом игры"
  - "EntityType tracking: EntitiesCatalog хранит GameObject -> EntityType маппинг для type dispatch без legacy-моделей"
  - "ECS input bridge: PlayerInput callbacks пишут напрямую в ECS components через EntityManager"

requirements-completed: [LC-02, LC-03, LC-05]

duration: 4min
completed: 2026-04-03
---

# Phase 06 Plan 02: Удаление legacy-моделей из Application/Game/GameScreen Summary

**EntitiesCatalog с EntityType enum, ECS-only input/spawn/score, ShootEventProcessorSystem для обработки стрельбы**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-03T13:26:37Z
- **Completed:** 2026-04-03T13:31:00Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- EntitiesCatalog полностью рефакторен: ModelFactory удален, EntityType enum для tracking, Create* методы создают только ECS entities
- Application.cs инициализирует ECS мир (GameAreaData, ScoreData, event buffers, CollisionBridge) и обрабатывает OnDeadEntity через EntityType
- Game.cs работает без _shipModel/_model: input через ECS components, спавн через ShipPositionData, score через ScoreData
- GameScreen.cs читает Score напрямую из Game.GetCurrentScore() (ECS ScoreData)
- Создан ShootEventProcessorSystem bridge для обработки GunShootEvent -> CreateBullet и LaserShootEvent -> raycast + DeadTag

## Task Commits

Each task was committed atomically:

1. **Task 1: EntitiesCatalog -- убрать ModelFactory, добавить EntityType tracking** - `e17198a` (refactor)
2. **Task 2: Application/Game/GameScreen -- убрать legacy-модели, ECS-only data path** - `aa062e2` (refactor)

## Files Created/Modified
- `Assets/Scripts/Application/EntitiesCatalog.cs` - EntityType enum, TryGetEntityType, ReleaseAllGameEntities, без ModelFactory/IGameEntityModel
- `Assets/Scripts/Application/Application.cs` - ECS initialization, OnDeadEntity через EntityType + ECS queries
- `Assets/Scripts/Application/Game.cs` - ECS input bridge, GetShipPosition через ShipPositionData, StopGame/Restart через ECS
- `Assets/Scripts/Application/Screens/GameScreen.cs` - GameScreenData без Model/ShipModel/UseEcs, Score из Game.GetCurrentScore()
- `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` - Удалена Model зависимость (SetModel, Score sync)
- `Assets/Scripts/Bridge/ShootEventProcessorSystem.cs` - Bridge: GunShootEvent -> CreateBullet, LaserShootEvent -> raycast + score + DeadTag

## Decisions Made
- Player input пишется напрямую в ECS components вместо legacy-модели -- EntityManager.GetComponentData/SetComponentData для ShipTag entity
- ShootEventProcessorSystem создан как managed SystemBase (не Burst) -- обращается к EntitiesCatalog.CreateBullet и ViewFactory для VFX
- ECS singletons инициализируются в Application.Start() программно (нет Authoring/SubScene)
- Score передается через Game.GetCurrentScore() который читает ScoreData перед Stop()
- ObservableBridgeSystem больше не синхронизирует Score в Model -- Score читается из ECS напрямую

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Создан ShootEventProcessorSystem**
- **Found during:** Task 2 (Game.cs refactoring)
- **Issue:** План указывал "ECS event buffers уже обрабатывают" стрельбу, но bridge для GunShootEvent/LaserShootEvent не существовал
- **Fix:** Создан ShootEventProcessorSystem в Bridge/ -- читает event buffers, создает пули через catalog, делает laser raycast
- **Files modified:** Assets/Scripts/Bridge/ShootEventProcessorSystem.cs
- **Committed in:** aa062e2 (Task 2 commit)

**2. [Rule 2 - Missing Critical] ECS World initialization**
- **Found during:** Task 2 (Application.cs refactoring)
- **Issue:** ECS singletons (GameAreaData, ScoreData, event buffers) не создавались нигде -- ECS системы не работали (RequireForUpdate)
- **Fix:** Добавлена InitializeEcsSingletons() в Application.Start() -- создает все необходимые singletons
- **Files modified:** Assets/Scripts/Application/Application.cs
- **Committed in:** aa062e2 (Task 2 commit)

**3. [Rule 2 - Missing Critical] ECS Input bridge**
- **Found during:** Task 2 (Game.cs refactoring)
- **Issue:** Не было механизма передачи player input в ECS components (RotateData, ThrustData, GunData, LaserData)
- **Fix:** OnRotateAction/OnTrust/OnAttack/OnLaser пишут напрямую через EntityManager в ECS components на ShipTag entity
- **Files modified:** Assets/Scripts/Application/Game.cs
- **Committed in:** aa062e2 (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (3 missing critical functionality)
**Impact on plan:** Все auto-fix необходимы для работоспособности -- без них ECS системы не получали данные. Архитектурный scope не расширен.

## Issues Encountered
- ECS singletons (GameAreaData, ScoreData) не создавались в существующей кодовой базе -- RequireForUpdate блокировал ECS системы. Решено программной инициализацией в Application.Start().

## User Setup Required
None - no external service configuration required.

## Known Stubs
None

## Next Phase Readiness
- Application/ полностью свободен от legacy-моделей (ShipModel, AsteroidModel, BulletModel, UfoBigModel)
- ModelFactory.cs и Model.cs остаются в кодовой базе, но не используются Application/ -- готовы к удалению в Plan 03
- Все Create*/Spawn*/Input callbacks работают через ECS

---
*Phase: 06-legacy-cleanup*
*Completed: 2026-04-03*
