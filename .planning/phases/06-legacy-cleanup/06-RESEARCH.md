# Phase 6: Legacy Cleanup - Research

**Researched:** 2026-04-03
**Domain:** Unity C# -- удаление legacy MonoBehaviour-слоя, рефакторинг к единому ECS data path
**Confidence:** HIGH

## Summary

Phase 6 завершает миграцию на гибридный DOTS, удаляя весь legacy MonoBehaviour-слой (Model/Systems, Model/Entities, Model/Components), переключатель `_useEcs`, dual-creation паттерн и Model.cs как координатор legacy-систем. ActionScheduler необходимо перенести на ECS или сохранить как standalone managed-класс.

Основная сложность не в удалении файлов, а в refactoring зависимостей: Game.cs, Application.cs, EntitiesCatalog.cs и GameScreen.cs интенсивно используют legacy-модели (ShipModel, AsteroidModel, UfoBigModel) для передачи данных, callbacks коллизий, и доступа к score/gameArea. Все эти зависимости должны быть переведены на прямой доступ к ECS-данным или на минимальные структуры-обертки.

**Primary recommendation:** Удалять послойно -- сначала убрать `_useEcs` и все `else`-ветки (legacy code paths), затем упростить EntitiesCatalog (убрать dual-creation), затем удалить legacy-файлы, и наконец заменить Model.cs на минимальный контейнер или ECS singletons.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CLN-01 | Все legacy-системы (MoveSystem, RotateSystem, ThrustSystem, GunSystem, LaserSystem, ShootToSystem, MoveToSystem, LifeTimeSystem, BaseModelSystem) удалены | 9 файлов в Assets/Scripts/Model/Systems/, все dormant при _useEcs=true, безопасно удаляемы после CLN-03 |
| CLN-02 | Legacy-модели (ShipModel, AsteroidModel, BulletModel, UfoModel, UfoBigModel) и компоненты (Model/Components/) удалены | 5 моделей + IGameEntityModel + IGroupVisitor, 9 компонентов; EntitiesCatalog и Game.cs зависят от них для callbacks/данных |
| CLN-03 | Переключатель _useEcs и dual-creation паттерн удалены -- единый ECS data path | 3 файла: Application.cs, Game.cs, EntitiesCatalog.cs содержат if(_useEcs)/else ветки |
| CLN-04 | ActionScheduler перенесен на ECS (ISystem) или заменен ECS-аналогом | ActionScheduler используется для спавна врагов и таймера лазерного VFX; 5 мест вызова |
| CLN-05 | Model.cs упрощен или удален -- score/state хранятся только в ECS singletons | Model.Score читается GameScreen.ShowEndGame(); GameArea используется в Game.cs для спавна/raycast; ActionScheduler -- в Game.cs |
| CLN-06 | Все существующие тесты проходят зеленым, новые тесты покрывают измененный code path | EcsBridgeRegressionTests используют ShipModel/AsteroidModel/Model -- нужен рефакторинг тестов |
| CLN-07 | Игра воспроизводит весь геймплей 1:1 без legacy-слоя | Ручная верификация: корабль, стрельба, лазер, астероиды, НЛО, дробление, лидерборд |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Все ответы и документация на русском
- **Фигурные скобки:** Обязательны в if/else/for/while даже для одной строки
- **Без однострочников:** Никогда не использовать однострочники
- **Без switch expressions:** Запрещены (anti-pattern в проекте)
- **Без тернарных операторов:** Запрещены
- **Порядок миграции:** Unity 6.3 -> URP -> DOTS -- каждый шаг на стабильной базе
- **Функциональная эквивалентность:** Геймплей 1:1 после каждого этапа
- **Тестирование:** При исправлении бага обязателен регрессионный тест
- **Naming:** PascalCase для методов, _camelCase для приватных полей, namespace SelStrom.Asteroids

## Architecture Patterns

### Текущее состояние: Dual Data Path

```
PlayerInput -> Game.cs -> if(_useEcs) { ECS EntityManager } else { legacy ShipModel }
                       -> EntitiesCatalog -> ModelFactory + EntityFactory (dual-creation)
                       -> Model.ActionScheduler (используется в обоих путях)
Model.Update()          -- НЕ вызывается в ECS-режиме
ECS World.Update()      -- автоматически обновляет все ISystem
```

### Целевое состояние: Single ECS Data Path

```
PlayerInput -> Game.cs -> EntityManager (напрямую, без _useEcs)
                       -> EntitiesCatalog -> EntityFactory + ViewFactory (без ModelFactory)
                       -> ActionScheduler (standalone или ECS ISystem)
ECS World.Update()      -- единственный source of truth для логики
ObservableBridgeSystem  -- синхронизация ECS -> MVVM UI
```

### Pattern: Что убирается

| Компонент | Текущая роль | После cleanup |
|-----------|-------------|---------------|
| `_useEcs` flag | Выбор data path | Удален -- только ECS |
| `ModelFactory` | Создает legacy-модели | Удален |
| `Model._typeToSystem` | Реестр legacy-систем | Удален |
| `Model.Update()` | Тик legacy-систем | Удален |
| `Model.AddEntity()` | Регистрация в legacy | Удален |
| `Model.OnEntityDestroyed` | Callback при смерти | Удален (DeadEntityCleanupSystem) |
| `Model.ReceiveScore()` | Подсчет очков legacy | Удален (ScoreData singleton) |
| `Model.CleanUp()` | Очистка legacy-систем | Упрощен (только reset ActionScheduler + score) или удален |
| `Model.Score` | Промежуточное хранилище | Читается из ScoreData singleton напрямую |
| `Model.GameArea` | Размер игровой области | Читается из GameAreaData singleton или хранится отдельно |
| `Model.ActionScheduler` | Таймер спавна + VFX | Standalone класс или ECS managed system |
| `IGameEntityModel` | Интерфейс legacy-моделей | Удален |
| `IGroupVisitor` | Visitor-паттерн для ECS-like dispatch | Удален |
| `IModelSystem` | Интерфейс legacy-систем | Удален |
| `GroupCreator` | Регистрация компонентов в системах | Удален |

### Pattern: Что остается и рефакторится

| Компонент | Почему остается | Что меняется |
|-----------|----------------|-------------|
| `EntitiesCatalog` | Нужен для маппинга Entity <-> GameObject | Убрать ModelFactory, dual-creation; оставить EntityFactory + ViewFactory + маппинг |
| `Game.cs` | Координатор геймплея | Убрать все `else` ветки, заменить model-доступы на ECS queries |
| `Application.cs` | Точка входа | Убрать `_useEcs`, `_model.Update()`, legacy-ветки |
| `GameScreen.cs` | UI координатор | `_data.Model.Score` -> прямое чтение из ScoreData singleton |
| `ActionScheduler` | Управление таймерами | Standalone класс (не привязан к Model) |
| `ObservableBridgeSystem` | Синхронизация ECS -> MVVM | Убрать `_model` reference, score синхронизация через отдельный механизм |

### Рекомендуемая структура после cleanup

```
Assets/Scripts/
  Application/
    Application.cs          -- без _useEcs, без Model.Update(), ActionScheduler standalone
    Game.cs                 -- без legacy code paths, без Model.ReceiveScore()
    EntitiesCatalog.cs      -- без ModelFactory, без dual-creation
    ModelFactory.cs         -- УДАЛЕН
    Screens/GameScreen.cs   -- score из ECS singleton
  Model/
    ActionScheduler.cs      -- СОХРАНЕН (standalone managed class)
    Model.cs                -- УДАЛЕН (или минимальная обертка для GameArea/Score)
    Systems/                -- УДАЛЕН (вся директория)
    Entities/               -- УДАЛЕН (вся директория)
    Components/             -- УДАЛЕН (вся директория)
  Bridge/
    (без изменений)
  ECS/
    (без изменений)
  View/
    (без изменений -- MVVM слой остается)
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Score хранение после удаления Model | Новый ScoreManager класс | ScoreData singleton (уже есть) + прямой query | Дублирование ECS singleton |
| GameArea доступ | Новый GameAreaManager | GameAreaData singleton (уже есть) + прямой query | Дублирование ECS singleton |
| Таймер спавна врагов | Новая ECS spawn system с NativeTimer | Существующий ActionScheduler как standalone | ActionScheduler проверен, managed callbacks вызывают EntitiesCatalog (managed), нет смысла переписывать на unmanaged |

**Key insight:** ActionScheduler не нуждается в полной замене на ECS ISystem -- его callbacks вызывают managed-код (EntitiesCatalog.CreateAsteroid, ViewFactory.Release), который не может работать из Burst/unmanaged контекста. Переместить ActionScheduler из Model.cs в standalone класс -- минимально инвазивное решение.

## Common Pitfalls

### Pitfall 1: Удаление моделей до удаления зависимостей
**What goes wrong:** Компиляция ломается, потому что Game.cs, EntitiesCatalog.cs, GameScreen.cs ссылаются на ShipModel, AsteroidModel, etc.
**Why it happens:** Модели используются не только в legacy code paths, но и в ECS-ветках (для передачи данных при создании entity, для callbacks коллизий, для asteroid splitting).
**How to avoid:** Сначала рефакторить потребителей (убрать зависимость от моделей), потом удалять файлы моделей.
**Warning signs:** Ошибки компиляции CS0246 (type or namespace could not be found).

### Pitfall 2: ActionScheduler callbacks после удаления Model
**What goes wrong:** `_model.ActionScheduler.ScheduleAction(...)` вызывается в 5 местах Game.cs -- если удалить Model, потеряется доступ к ActionScheduler.
**Why it happens:** ActionScheduler -- property Model.cs, а не standalone класс.
**How to avoid:** Сначала извлечь ActionScheduler из Model в отдельный параметр Game/Application, затем удалять Model.
**Warning signs:** NullReferenceException при спавне врагов или лазерном VFX.

### Pitfall 3: GameScreen.ShowEndGame() читает Model.Score
**What goes wrong:** После удаления Model, GameScreen не может прочитать score для отображения и отправки в лидерборд.
**Why it happens:** `_data.Model.Score` используется в 3 местах GameScreen.cs (строки 161, 178, 206).
**How to avoid:** Заменить на прямое чтение ScoreData singleton через EntityManager query.
**Warning signs:** Score = 0 на экране EndGame.

### Pitfall 4: Model.CleanUp() при рестарте
**What goes wrong:** Model.CleanUp() вызывает OnEntityDestroyed для всех entity в _newEntities -- EntitiesCatalog.Release() уничтожает ECS entity и возвращает GameObject в пул. Без этого механизма entity утекают.
**Why it happens:** В ECS-режиме entities попадают в _newEntities (через ModelFactory), но Update() не вызывается, поэтому они остаются в _newEntities.
**How to avoid:** Рефакторить Restart() -- использовать EntitiesCatalog.CleanUp() напрямую для уничтожения ECS entity и возврата GameObjects в пул, без прохода через Model.
**Warning signs:** Регрессионный тест ModelCleanUp_InvokesOnEntityDestroyed_ForNewEntities падает.

### Pitfall 5: Регрессионные тесты используют legacy-модели
**What goes wrong:** EcsBridgeRegressionTests.cs создает `new ShipModel()`, `new AsteroidModel()`, `new Model()` -- эти тесты сломаются при удалении legacy-классов.
**Why it happens:** Тесты были написаны для верификации bridge-слоя, когда legacy-модели еще существовали.
**How to avoid:** Рефакторить тесты: заменить конструкцию legacy-моделей на прямую работу с ECS entity. Тесты, которые тестировали чисто legacy-поведение (ModelCleanUp_*), заменить тестами нового lifecycle.
**Warning signs:** Тесты не компилируются.

### Pitfall 6: EntitiesCatalog.TryFindModel<T> в Application.OnDeadEntity
**What goes wrong:** OnDeadEntity() использует `TryFindModel<AsteroidModel>`, `TryFindModel<ShipModel>`, `TryFindModel<UfoBigModel>` для определения типа мертвой entity (дробление астероидов, VFX взрыва, конец игры).
**Why it happens:** Тип entity определяется через legacy model, а не через ECS tag.
**How to avoid:** Заменить на query ECS tags (AsteroidTag, ShipTag, UfoBigTag) через _gameObjectToEntity маппинг.
**Warning signs:** Астероиды не дробятся, корабль не завершает игру при гибели.

### Pitfall 7: GunComponent/LaserComponent callbacks в legacy code
**What goes wrong:** `model.Gun.OnShooting` и `model.Laser.OnShooting` -- legacy callbacks, которые не используются в ECS-режиме (стрельба идет через GunShootEvent/LaserShootEvent буферы).
**Why it happens:** EntitiesCatalog.CreateShip() назначает callbacks на legacy model даже в ECS-режиме.
**How to avoid:** Убрать назначение callbacks при удалении legacy models. ECS event buffers уже полностью обрабатывают стрельбу.
**Warning signs:** Двойная стрельба или отсутствие стрельбы.

## Dependency Analysis

### Файлы к удалению (9 legacy systems)

| File | Lines | Dependencies Outside |
|------|-------|---------------------|
| `Model/Systems/BaseModelSystem.cs` | ~30 | Model.cs (IModelSystem) |
| `Model/Systems/MoveSystem.cs` | ~30 | Model.PlaceWithinGameArea |
| `Model/Systems/RotateSystem.cs` | ~20 | -- |
| `Model/Systems/ThrustSystem.cs` | ~30 | -- |
| `Model/Systems/GunSystem.cs` | ~30 | -- |
| `Model/Systems/LaserSystem.cs` | ~30 | -- |
| `Model/Systems/ShootToSystem.cs` | ~30 | -- |
| `Model/Systems/MoveToSystem.cs` | ~20 | -- |
| `Model/Systems/LifeTimeSystem.cs` | ~20 | -- |

### Файлы к удалению (5 legacy models + 2 interfaces)

| File | Used By (outside Model/) |
|------|------------------------|
| `ShipModel.cs` | Game.cs (спавн, input, callbacks), EntitiesCatalog.cs (создание, параметры), Application.cs (OnDeadEntity), GameScreen.cs (UseEcs=false HUD) |
| `AsteroidModel.cs` | Game.cs (Kill/splitting), EntitiesCatalog.cs (создание), Application.cs (OnDeadEntity, Age/Speed) |
| `BulletModel.cs` | Game.cs (Kill), EntitiesCatalog.cs (создание) |
| `UfoBigModel.cs` + `UfoModel.cs` | Game.cs (callbacks), EntitiesCatalog.cs (создание), Application.cs (OnDeadEntity) |
| `IGameEntityModel.cs` | Model.cs, EntitiesCatalog.cs, Game.cs, ModelFactory.cs |
| `IGroupVisitor.cs` | Model.cs (GroupCreator) |

### Файлы к удалению (9 legacy components)

| File | Used By (outside Model/) |
|------|------------------------|
| `GunComponent.cs` | Game.cs (OnEnemyGunShooting, OnUserGunShooting callbacks) |
| `LaserComponent.cs` | Game.cs (OnUserLaserShooting callback) |
| Остальные 7 | Только внутри Model/ |

### Файлы к удалению (вспомогательные)

| File | Notes |
|------|-------|
| `ModelFactory.cs` | Только создает legacy модели + AddEntity |

### Ключевые замены при рефакторинге

| Текущий код | Замена |
|-------------|--------|
| `_model.ActionScheduler.ScheduleAction(...)` | `_actionScheduler.ScheduleAction(...)` (standalone) |
| `_model.ActionScheduler.ResetSchedule()` | `_actionScheduler.ResetSchedule()` |
| `_model.ActionScheduler.Update(deltaTime)` | `_actionScheduler.Update(deltaTime)` |
| `_model.GameArea` | `_gameArea` (Vector2, сохраняется в Application/Game) |
| `_model.GameArea.magnitude` | `_gameArea.magnitude` |
| `_model.Score` | `EntityManager.GetComponentData<ScoreData>(scoreEntity).Value` |
| `_model.SetScore(val)` | Не нужен -- score только в ScoreData |
| `_model.ReceiveScore(model)` | Не нужен -- EcsCollisionHandlerSystem начисляет score |
| `_model.CleanUp()` | `_catalog.CleanUp()` + `_actionScheduler.ResetSchedule()` |
| `_model.Update(deltaTime)` | Удален (ECS World обновляется автоматически) |
| `_model.OnEntityDestroyed += handler` | Удален (DeadEntityCleanupSystem) |
| `ModelFactory(_model)` | Удален |
| `_catalog.TryFindModel<AsteroidModel>(go)` | `_catalog.TryGetEntity(go) + em.HasComponent<AsteroidTag>(entity)` |
| `asteroidModel.Age` | `em.GetComponentData<AgeData>(entity).Age` |
| `asteroidModel.Move.Speed.Value` | `em.GetComponentData<MoveData>(entity).Speed` |
| `_shipModel.Move.Position.Value` | `em.GetComponentData<MoveData>(shipEntity).Position` (через ShipPositionData singleton) |
| `_data.Model.Score` (GameScreen) | `EntityManager.GetComponentData<ScoreData>(entity).Value` |
| `_data.UseEcs` (GameScreenData) | Удален |
| `_data.ShipModel` (GameScreenData) | Удален |

## Code Examples

### Замена TryFindModel на ECS tag check

```csharp
// БЫЛО:
if (_catalog.TryFindModel<AsteroidModel>(go, out var asteroidModel))
{
    _game.PlayEffect(_configs.VfxBlowPrefab, position);
    var age = asteroidModel.Age - 1;
    if (age > 0)
    {
        var speed = Math.Min(asteroidModel.Move.Speed.Value * 2, 10f);
        _catalog.CreateAsteroid(age, position, speed);
        _catalog.CreateAsteroid(age, position, speed);
    }
}

// СТАЛО:
if (_catalog.TryGetEntity(go, out var entity) && _entityManager.HasComponent<AsteroidTag>(entity))
{
    _game.PlayEffect(_configs.VfxBlowPrefab, position);
    var ageData = _entityManager.GetComponentData<AgeData>(entity);
    var moveData = _entityManager.GetComponentData<MoveData>(entity);
    var age = ageData.Age - 1;
    if (age > 0)
    {
        var speed = Math.Min(moveData.Speed * 2, 10f);
        _catalog.CreateAsteroid(age, position, speed);
        _catalog.CreateAsteroid(age, position, speed);
    }
}
```

### Замена Score доступа в GameScreen

```csharp
// БЫЛО:
scoreVm.Score.Value = $"score: {_data.Model.Score}";

// СТАЛО -- через score int из GameScreenData или callback:
scoreVm.Score.Value = $"score: {_data.Score}";

// Где Score берется из ScoreData singleton при вызове ShowEndGame
```

### ActionScheduler как standalone

```csharp
// БЫЛО (Application.cs):
_model = new Model { GameArea = new Vector2(sceneWidth, sceneHeight) };
// ...
_model.ActionScheduler.Update(deltaTime);

// СТАЛО:
_actionScheduler = new ActionScheduler();
_gameArea = new Vector2(sceneWidth, sceneHeight);
// ...
_actionScheduler.Update(deltaTime);
```

### EntitiesCatalog.CreateShip без ModelFactory

```csharp
// БЫЛО:
var model = _modelFactory.Get<ShipModel>();
model.SetData(_configs.Ship);
// ... настройка model
var entity = EntityFactory.CreateShip(_entityManager, ...);

// СТАЛО:
var entity = EntityFactory.CreateShip(_entityManager, ...);
// ViewModel и bindings -- напрямую из ECS данных через ObservableBridgeSystem
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Legacy Model + ECS dual-creation | ECS-only creation | Phase 6 | Удаление ~35 файлов, упрощение EntitiesCatalog |
| Model.Score -> GameScreen | ScoreData singleton -> GameScreen | Phase 6 | Устранение промежуточного хранилища |
| _useEcs conditional paths | Single ECS path | Phase 6 | Устранение ~40 строк if/else |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef`, `Assets/Tests/PlayMode/PlayModeTests.asmdef` |
| Quick run command | `Unity -batchmode -runTests -testPlatform EditMode -testFilter "SelStrom.Asteroids.Tests" -quit` |
| Full suite command | `Unity -batchmode -runTests -testPlatform EditMode -quit && Unity -batchmode -runTests -testPlatform PlayMode -quit` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CLN-01 | Legacy systems removed, no compilation errors | build | Compile check via Unity | N/A |
| CLN-02 | Legacy models removed, no compilation errors | build | Compile check via Unity | N/A |
| CLN-03 | _useEcs removed, single code path | build + unit | Grep + ECS tests | Exists (ECS system tests) |
| CLN-04 | ActionScheduler works standalone | unit | EditMode tests | Wave 0: new test file |
| CLN-05 | Score/state from ECS singletons only | unit | EditMode tests | Wave 0: update EcsBridgeRegressionTests |
| CLN-06 | All existing tests pass green | full suite | EditMode + PlayMode | Exists (refactor needed) |
| CLN-07 | Gameplay 1:1 without legacy | manual + PlayMode | PlayMode cycle test | Exists (GameplayCycleTests.cs) |

### Sampling Rate
- **Per task commit:** EditMode tests for changed code
- **Per wave merge:** Full EditMode suite
- **Phase gate:** Full EditMode + PlayMode suite green, manual gameplay verification

### Wave 0 Gaps
- [ ] `EcsBridgeRegressionTests.cs` -- рефакторинг: тесты ModelCleanUp_* и ScoreData_IsSynced_ToModelScore_ViaObservableBridge используют Model/ShipModel/AsteroidModel
- [ ] Новый тест для standalone ActionScheduler (или существующий, если он есть вне Model)
- [ ] Новый тест для EntitiesCatalog lifecycle без ModelFactory

## Open Questions

1. **Нужно ли полностью удалять Model.cs или оставить минимальную обертку?**
   - What we know: Model.cs используется как контейнер для Score, GameArea, ActionScheduler и OnEntityDestroyed. В ECS-режиме Score и GameArea уже есть как singletons, ActionScheduler можно выделить.
   - What's unclear: GameScreen.cs передает `_data.Model` -- рефакторинг может затронуть множество мест.
   - Recommendation: Полностью удалить Model.cs. Score читать из ScoreData singleton, GameArea хранить как поле Application/Game, ActionScheduler -- standalone. Это чище, чем оставлять пустую обертку.

2. **Как определять тип entity в OnDeadEntity без legacy моделей?**
   - What we know: Текущий код использует TryFindModel<AsteroidModel/ShipModel/UfoBigModel>. Entity уже уничтожена в ECB.Playback к моменту вызова callback.
   - What's unclear: DeadEntityCleanupSystem уничтожает entity ДО callback -- EntityManager.HasComponent<AsteroidTag>(entity) может не работать.
   - Recommendation: Сохранять тип entity (tag) в промежуточной структуре перед уничтожением. Или расширить callback: `Action<GameObject, EntityType>` где EntityType -- enum. Или хранить маппинг gameObject -> entityType в EntitiesCatalog.

3. **Нужен ли рефакторинг спавна врагов?**
   - What we know: SpawnNewEnemy() использует `_shipModel.Move.Position.Value` для safe spawn. В ECS-режиме есть ShipPositionData singleton.
   - Recommendation: Читать позицию корабля из ShipPositionData singleton через EntityManager query.

## Sources

### Primary (HIGH confidence)
- Прямой анализ кодовой базы: Application.cs, Game.cs, EntitiesCatalog.cs, Model.cs, GameScreen.cs
- Прямой анализ ECS-слоя: все файлы в Assets/Scripts/ECS/ и Assets/Scripts/Bridge/
- Прямой анализ тестов: Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs

### Secondary (MEDIUM confidence)
- Unity Entities lifecycle patterns (from Phase 4/5 implementation experience in this project)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- нет новых библиотек, только рефакторинг существующего кода
- Architecture: HIGH -- полностью основано на анализе текущей кодовой базы
- Pitfalls: HIGH -- каждый pitfall подтвержден конкретными строками кода

**Research date:** 2026-04-03
**Valid until:** 2026-05-03 (стабильная кодовая база, без внешних зависимостей)
