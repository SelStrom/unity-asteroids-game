---
phase: 06-legacy-cleanup
verified: 2026-04-03T14:30:00Z
status: human_needed
score: 6/7 must-haves verified
gaps: []
human_verification:
  - test: "Запустить игру в Unity Editor и проверить полный геймплей 1:1"
    expected: "Корабль управляется, астероиды/НЛО спавнятся, стрельба/лазер работают, Score обновляется, EndGame показывает правильный Score, рестарт работает"
    why_human: "Gameplay verification требует запуска Unity Editor -- невозможно автоматизировать без среды. 06-04 SUMMARY указывает auto-approved, ручная проверка не была выполнена."
---

# Phase 6: Legacy Cleanup Verification Report

**Phase Goal:** Полное удаление legacy MonoBehaviour-слоя (Model/Systems, Model/Entities, Model/Components), переключателя _useEcs, dual-creation паттерна. ActionScheduler как standalone managed-класс. Единый ECS data path без дублирования.
**Verified:** 2026-04-03T14:30:00Z
**Status:** human_needed
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Все legacy-системы (MoveSystem, RotateSystem, ThrustSystem, GunSystem, LaserSystem, ShootToSystem, MoveToSystem, LifeTimeSystem, BaseModelSystem) удалены | VERIFIED | `Assets/Scripts/Model/Systems/` не существует. grep по всему Assets/ не находит ни одного legacy-имени. |
| 2 | Legacy-модели (ShipModel, AsteroidModel, BulletModel, UfoModel, UfoBigModel) и компоненты (Model/Components/) удалены | VERIFIED | `Assets/Scripts/Model/Entities/` и `Assets/Scripts/Model/Components/` не существуют. grep подтверждает 0 ссылок. |
| 3 | Переключатель _useEcs и dual-creation паттерн удалены -- единый ECS data path | VERIFIED | grep `_useEcs` по Assets/Scripts/ -- 0 результатов. Application.cs, Game.cs, EntitiesCatalog.cs используют только EntityManager/EntityFactory. 06-01 SUMMARY отмечает, что `_useEcs` не существовал в кодовой базе на момент начала Phase 6 (уже был удален в Phase 5). |
| 4 | ActionScheduler выделен из Model как standalone managed-класс | VERIFIED | `Assets/Scripts/Model/ActionScheduler.cs` -- standalone класс (67 строк, нет зависимости от Model). `Application.cs:15` содержит `private ActionScheduler _actionScheduler`. `Game.cs:15` содержит `private readonly ActionScheduler _actionScheduler`. Передается через конструктор Game. |
| 5 | Model.cs удален -- score/state хранятся только в ECS singletons | VERIFIED | `Assets/Scripts/Model/Model.cs` не существует. `Assets/Scripts/Application/ModelFactory.cs` не существует. Score читается из ScoreData через `Game.ReadScoreFromEcs()` (строка 76-83) и `GameScreen.ReadScoreFromEcs()` (строка 112-126). |
| 6 | Все существующие тесты проходят зеленым, новые тесты покрывают измененный code path | VERIFIED | 06-04 SUMMARY подтверждает 142 теста (135 EditMode + 7 PlayMode), 0 failures. `EcsBridgeRegressionTests.cs` содержит 11 тестов (4 новых ECS-only замены + 7 regression). Тесты не содержат ссылок на legacy-типы. |
| 7 | Игра воспроизводит весь геймплей 1:1 без legacy-слоя | UNCERTAIN | 06-04 SUMMARY указывает "Auto-approved human-verify checkpoint (auto_advance mode)". Ручная верификация геймплея НЕ была выполнена. Требуется человек. |

**Score:** 6/7 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/Application/Application.cs` | Standalone ActionScheduler, ECS init, OnDeadEntity через EntityType | VERIFIED | 279 строк, нет legacy-ссылок, InitializeEcsSingletons(), OnDeadEntity с EntityType dispatch |
| `Assets/Scripts/Application/Game.cs` | ECS-only input, ShipPositionData, StopGame | VERIFIED | 277 строк, нет _shipModel/_model, ECS input bridge, GetShipPosition via ShipPositionData singleton |
| `Assets/Scripts/Application/EntitiesCatalog.cs` | EntityType enum, без ModelFactory | VERIFIED | 301 строка, EntityType enum, TryGetEntityType, TryGetEntity, ReleaseAllGameEntities, без legacy-типов |
| `Assets/Scripts/Application/Screens/GameScreen.cs` | Score из ECS, без Model/ShipModel | VERIFIED | 319 строк, GameScreenData содержит только Game, ReadScoreFromEcs(), нет legacy-ссылок |
| `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` | Без _model ссылки | VERIFIED | 93 строки, нет _model, нет SetModel, нет using Model |
| `Assets/Scripts/Bridge/ShootEventProcessorSystem.cs` | GunShootEvent/LaserShootEvent bridge | VERIFIED | 137 строк, managed SystemBase, обрабатывает GunShootEvent -> CreateBullet и LaserShootEvent -> raycast + DeadTag |
| `Assets/Scripts/Model/ActionScheduler.cs` | Standalone managed-класс | VERIFIED | 67 строк, нет зависимости от Model, ScheduleAction/Update/ResetSchedule |
| `Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs` | Тесты без legacy-моделей | VERIFIED | 440 строк, 11 тестов, все используют только ECS API |
| `Assets/Scripts/Model/Systems/` | Удалена | VERIFIED | Директория не существует |
| `Assets/Scripts/Model/Entities/` | Удалена | VERIFIED | Директория не существует |
| `Assets/Scripts/Model/Components/` | Удалена | VERIFIED | Директория не существует |
| `Assets/Scripts/Model/Model.cs` | Удален | VERIFIED | Файл не существует |
| `Assets/Scripts/Application/ModelFactory.cs` | Удален | VERIFIED | Файл не существует |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| Application.cs | Game.cs | ActionScheduler и gameArea через конструктор | WIRED | `new Game(_catalog, _actionScheduler, _gameArea, _configs, _playerInput, _gameScreen, _entityManager)` (строка 87) |
| Application.cs | OnDeadEntity | TryGetEntityType dispatch | WIRED | `_catalog.TryGetEntityType(go, out var entityType)` (строка 176), switch по EntityType.Asteroid/Ship/UfoBig/Ufo |
| Game.cs | ShipPositionData | EntityManager query | WIRED | `GetShipPosition()` (строка 150-160) использует `CreateEntityQuery(typeof(ShipPositionData))`, вызывается из SpawnNewEnemy |
| Game.cs | ScoreData | EntityManager query | WIRED | `ReadScoreFromEcs()` (строка 76-83) читает ScoreData singleton |
| GameScreen.cs | ScoreData | Game.GetCurrentScore() | WIRED | `ShowEndGame()` вызывает `_data.Game.GetCurrentScore()` (строка 130), который читает ScoreData |
| ObservableBridgeSystem | ShipTag query | SystemAPI.Query | WIRED | Строка 51-53 итерирует MoveData/RotateData/ThrustData/LaserData с WithAll<ShipTag> |
| ShootEventProcessorSystem | EntitiesCatalog | SetDependencies | WIRED | Application.Start() вызывает `shootProcessor.SetDependencies(_catalog, _configs, _actionScheduler, _gameArea)` (строка 84) |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|-------------------|--------|
| Game.cs | _currentScore | ScoreData ECS singleton | EntityManager.GetComponentData<ScoreData> | FLOWING |
| Game.cs | shipPosition | ShipPositionData ECS singleton | EntityManager query | FLOWING |
| GameScreen.cs | score | Game.GetCurrentScore() -> ScoreData | ECS singleton query | FLOWING |
| ObservableBridgeSystem | HudData, ShipViewModel | SystemAPI.Query<MoveData,RotateData,ThrustData,LaserData> | ECS query per frame | FLOWING |
| ShootEventProcessorSystem | GunShootEvent/LaserShootEvent | DynamicBuffer | ECS event buffers, filled by EcsGunSystem/EcsLaserSystem | FLOWING |

### Behavioral Spot-Checks

Step 7b: SKIPPED (Unity проект -- требует запуска Unity Editor для тестирования, нет CLI entry point)

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-----------|-------------|--------|----------|
| LC-01 | 06-03 | Удаление legacy-систем | SATISFIED | 9 legacy-систем удалены, директория Systems/ не существует |
| LC-02 | 06-02, 06-03 | Удаление legacy-моделей | SATISFIED | 6 моделей/интерфейсов удалены, директория Entities/ не существует |
| LC-03 | 06-01, 06-02 | Удаление _useEcs и dual-creation | SATISFIED | 0 ссылок на _useEcs, единый ECS path |
| LC-04 | 06-01 | ActionScheduler standalone | SATISFIED | ActionScheduler.cs standalone, передается через конструктор |
| LC-05 | 06-02, 06-03 | Удаление Model.cs | SATISFIED | Model.cs и ModelFactory.cs удалены, Score из ECS singletons |
| LC-06 | 06-03, 06-04 | Тесты проходят зеленым | SATISFIED | 142 теста (135 EditMode + 7 PlayMode), 0 failures |
| LC-07 | 06-04 | Геймплей 1:1 | NEEDS HUMAN | auto-approved в 06-04, ручная проверка не выполнена |

**Note:** Requirements LC-01 through LC-07 referenced in ROADMAP.md but not defined in REQUIREMENTS.md. Coverage assessment based on ROADMAP Success Criteria.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| Assets/Scripts/Model/ActionScheduler.cs | 28 | TODO comment "theoretically it can be added during update" | Info | Пре-существующий TODO, не введен в Phase 6 |

Нет blocker или warning anti-patterns. Код чистый от placeholder/stub паттернов.

### Human Verification Required

### 1. Полная верификация геймплея 1:1

**Test:** Открыть проект в Unity Editor, запустить Play Mode:
- Корабль появляется, управление WASD/Space/Q работает
- Астероиды спавнятся и двигаются
- Пули стреляются и уничтожают астероиды (дробление работает)
- Лазер стреляется и уничтожает объекты
- НЛО (маленький и большой) спавнятся и стреляют
- Score отображается в HUD и увеличивается при уничтожении врагов
- При гибели корабля появляется EndGame экран с правильным Score
- Рестарт работает корректно (Space на EndGame экране)
- Тороидальный wrap работает (объекты телепортируются через края)
**Expected:** Все пункты работают идентично предыдущей версии
**Why human:** Unity Editor runtime verification -- невозможно запустить Play Mode программно из CLI. 06-04 plan содержал human-verify checkpoint, но он был auto-approved.

### Gaps Summary

Автоматическая верификация подтверждает полное удаление legacy-слоя:
- 27 legacy-файлов удалены (9 систем, 6 моделей/интерфейсов, 9 компонентов, Model.cs, ModelFactory.cs)
- 0 ссылок на legacy-типы в кодовой базе
- ActionScheduler -- standalone managed-класс
- Score/state хранятся только в ECS singletons
- 142 теста зеленые (135 EditMode + 7 PlayMode)
- Все key links WIRED, все data flows FLOWING

Единственный непроверенный пункт -- ручная верификация геймплея (Success Criterion 7). Human-verify checkpoint в 06-04 был auto-approved без фактической проверки.

---

_Verified: 2026-04-03T14:30:00Z_
_Verifier: Claude (gsd-verifier)_
