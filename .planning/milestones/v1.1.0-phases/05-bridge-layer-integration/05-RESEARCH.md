# Phase 5: Bridge Layer + Integration - Research

**Researched:** 2026-04-03
**Domain:** Unity DOTS Hybrid (ECS + GameObjects), Bridge Layer, MVVM integration
**Confidence:** HIGH

## Summary

Phase 5 соединяет ECS-слой (Phase 4) с существующими GameObjects и MVVM-привязками. Ключевые задачи: managed-компонент GameObjectRef для связи Entity-GameObject, синхронизация позиции/ротации из ECS в Transform, проброс Physics2D коллизий в ECS CollisionEventData буфер, трансляция ECS-данных в ObservableValue/ReactiveValue для HUD, синхронизация жизненного цикла (DeadTag -> Release GameObject).

EcsGunSystem и EcsLaserSystem уже реализованы в Phase 4 (код и тесты присутствуют), но содержат TODO-комментарии для callback'ов стрельбы ("Phase 5 Bridge Layer"). Основная работа Phase 5 -- создание bridge-систем и переключение Game.cs/EntitiesCatalog/Application с Model-слоя на ECS World. Также обнаружен пробел: EcsLifeTimeSystem не добавляет DeadTag при TimeRemaining==0, что нужно исправить для корректной работы cleanup-цикла.

**Primary recommendation:** Создать bridge-системы (GameObjectSyncSystem, CollisionBridge, ObservableBridgeSystem, DeadEntityCleanupSystem) как managed ISystem, расширить EntitiesCatalog для параллельного создания Entity+GameObject, переключить Application.OnUpdate с Model.Update на World.Update, и добавить миграционный флаг для отключения старого Model-слоя.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Managed component `GameObjectRef` (ICleanupComponentData) хранит ссылку на Transform привязанного GameObject
- **D-02:** Обратный маппинг `Dictionary<GameObject, Entity>` поддерживается в managed-коде для O(1) lookup при коллизиях
- **D-03:** GameObjectRef добавляется в EntityFactory при создании entity
- **D-04:** `GameObjectSyncSystem` (managed ISystem) синхронизирует позицию и ротацию из MoveData/RotateData в Transform каждый кадр
- **D-05:** Синхронизация без change filter -- количество entities мало (~20-50)
- **D-06:** Порядок: ECS-системы -> GameObjectSyncSystem -> рендер Unity
- **D-07:** MonoBehaviour-визуалы сохраняют `OnCollisionEnter2D` -- Physics2D остается на GameObjects
- **D-08:** `CollisionBridge` вызывается из OnCollisionEnter2D, разрешает Entity через обратный маппинг, записывает CollisionEventData
- **D-09:** EcsCollisionHandlerSystem обрабатывает CollisionEventData буфер без изменений
- **D-10:** `ObservableBridgeSystem` (managed ISystem) пушит ECS-данные в ReactiveValue/ObservableValue каждый кадр
- **D-11:** Бридж покрывает: Score, Gun/Laser->HUD, Move->HUD, Rotate->HUD, Thrust->ShipViewModel
- **D-12:** Бридж заменяет EventBindingContext привязки из EntitiesCatalog
- **D-13:** EntitiesCatalog остается оркестратором -- создает и Entity, и GameObject
- **D-14:** DeadTag триггерит cleanup: DeadEntityCleanupSystem вызывает EntitiesCatalog.Release, уничтожает Entity
- **D-15:** Game.cs сохраняется с минимальными изменениями -- переключается с Model.Update на ECS World
- **D-16:** ActionScheduler остается в managed-коде
- **D-17:** Поэтапная замена: Bridge параллельно со старым Model, затем Model отключается
- **D-18:** Старый Model-слой не удаляется -- отключается через флаг
- **D-19:** GunSystem (ECS-07) и LaserSystem (ECS-08) -- завершить в Phase 5 перед интеграцией
- **D-20:** TST-05 и TST-06 реализуются в Phase 5

### Claude's Discretion
- Конкретная реализация обратного маппинга (static dictionary, singleton managed component, или часть EntitiesCatalog)
- Порядок инициализации ECS World относительно ApplicationEntry.Awake
- Детали миграционного флага (bool в Game.cs, ScriptableObject, или define)
- Подход к PlayMode-тестам (TST-12): сценарии, длительность, assertions

### Deferred Ideas (OUT OF SCOPE)
- Полный переход на DOTS Physics -- Physics2D на GameObjects достаточен
- Entities Graphics для рендеринга -- не поддерживает SpriteRenderer и WebGL
- Удаление старого Model-слоя -- отключается флагом, полное удаление в будущем
- Оптимизация sync с change filter -- при ~20-50 entities не нужна
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| BRG-01 | Managed component GameObjectRef связывает Entity с GameObject/Transform | ICleanupComponentData с полем Transform, добавляется в EntityFactory. Паттерн стандартный для Unity DOTS hybrid |
| BRG-02 | GameObjectSyncSystem синхронизирует позицию/ротацию из ECS в Transform каждый кадр | Managed ISystem, запрос по (MoveData, RotateData, GameObjectRef), конвертация float2->Vector3 + float2->Quaternion |
| BRG-03 | CollisionBridge передает результаты Physics2D коллизий в ECS World | Utility-класс с доступом к EntityManager, вызывается из OnCollisionEnter2D визуалов, записывает в DynamicBuffer<CollisionEventData> |
| BRG-04 | ObservableBridgeSystem транслирует ECS-данные в ObservableValue для shtl-mvvm UI | Managed ISystem, читает singleton ScoreData + ShipTag entities, пушит в ReactiveValue на HudData и ShipViewModel |
| BRG-05 | Жизненный цикл Entity-GameObject синхронизирован (создание, уничтожение) | EntitiesCatalog расширяется для параллельного создания, DeadEntityCleanupSystem обнаруживает DeadTag и вызывает Release |
| BRG-06 | Игра запускается в Editor и воспроизводит весь геймплей 1:1 | Application переключает Update с Model на World, Game.cs минимально изменяется, миграционный флаг |
| TST-10 | EditMode тесты для Bridge Layer (синхронизация позиций, жизненный цикл) | Расширение AsteroidsEcsTestFixture для managed-систем, тесты на GameObjectSyncSystem и DeadEntityCleanupSystem |
| TST-12 | PlayMode тесты для полного игрового цикла (старт -> игра -> конец) | PlayMode тест с загрузкой сцены, симуляцией ввода через InputSystem, проверкой Score и GameScreen.State |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Документация и комментарии на русском
- **Фигурные скобки:** Всегда `{}` в if/else/for/while, даже для одной строки
- **Без однострочников:** Никогда не использовать однострочные конструкции
- **C# 9.0**, .NET Standard 2.1
- **Unsafe-код запрещен** (`AllowUnsafeBlocks=False`)
- **Naming:** PascalCase для классов/методов, _camelCase для приватных полей, I-prefix для интерфейсов
- **Namespace:** `SelStrom.Asteroids` (основной), `SelStrom.Asteroids.ECS` (ECS-слой)
- **MVVM паттерн:** Model(ObservableValue) -> ViewModel(ReactiveValue) -> View(AbstractWidgetView)
- **GSD Workflow:** Изменения через GSD-команды

## Standard Stack

### Core (уже в проекте)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.entities | 1.4.5 | ECS framework (World, EntityManager, ISystem) | Уже установлен в Phase 4 |
| com.unity.burst | 1.8.19 | Burst-компиляция ECS-систем | Уже установлен, используется Burst-совместимыми системами |
| com.unity.mathematics | 1.2.6 | Математика (float2, math.*) | Уже установлен, используется ECS-компонентами |
| com.shtl.mvvm | git#v1.1.0 | MVVM привязки (ObservableValue, ReactiveValue) | Уже установлен, используется UI-слоем |

### Supporting (Phase 5 специфичное)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Unity Test Framework | 1.1.33 | NUnit для EditMode/PlayMode тестов | TST-10, TST-12 |
| com.unity.inputsystem | 1.19.0 | Симуляция ввода в PlayMode тестах | TST-12 (InputTestFixture) |

### Installation
Новые пакеты не требуются -- все зависимости уже в проекте.

## Architecture Patterns

### Recommended Project Structure (новые файлы Phase 5)
```
Assets/Scripts/ECS/
  Components/
    GameObjectRef.cs           # ICleanupComponentData с Transform
    GunShootEvent.cs           # Tag/Buffer для callback стрельбы пушки
    LaserShootEvent.cs         # Tag/Buffer для callback лазера
  Systems/
    GameObjectSyncSystem.cs    # Managed ISystem: ECS Position/Rotation -> Transform
    ObservableBridgeSystem.cs  # Managed ISystem: ECS data -> ReactiveValue (HUD)
    DeadEntityCleanupSystem.cs # Managed ISystem: DeadTag -> Release GameObject
  Bridge/
    CollisionBridge.cs         # Utility: OnCollisionEnter2D -> CollisionEventData

Assets/Scripts/Application/
  EntitiesCatalog.cs           # Расширяется: создание Entity параллельно с GameObject
  Application.cs               # Расширяется: инициализация ECS World, переключение Update
  Game.cs                      # Расширяется: ECS-интеграция стрельбы, input -> ECS

Assets/Tests/EditMode/ECS/
  GameObjectSyncSystemTests.cs
  DeadEntityCleanupSystemTests.cs
  CollisionBridgeTests.cs
  ObservableBridgeSystemTests.cs

Assets/Tests/PlayMode/
  GameplayCycleTests.cs        # TST-12: полный игровой цикл
```

### Pattern 1: GameObjectRef -- Managed Companion Component
**What:** `ICleanupComponentData` (class, не struct) хранит managed-ссылку на Transform. Используется для синхронизации ECS->GameObject.
**When to use:** Для каждого entity, имеющего визуальное представление (Ship, Asteroid, Bullet, Ufo).
**Example:**
```csharp
// GameObjectRef.cs
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids.ECS
{
    public class GameObjectRef : ICleanupComponentData
    {
        public Transform Transform;
        public GameObject GameObject;
    }
}
```
**Confidence:** HIGH -- ICleanupComponentData стандартный паттерн Unity DOTS для managed-ссылок. В Unity Entities 1.x `ICleanupComponentData` может быть class для хранения managed-объектов.

**IMPORTANT:** В Unity Entities 1.x managed components реализуются через `IComponentData` class (не struct). `ICleanupComponentData` гарантирует, что компонент не удаляется при DestroyEntity -- это позволяет cleanup-системе обработать его. Однако, для простого хранения managed-ссылки можно использовать обычный `class IComponentData`. Cleanup-семантика нужна только если entity уничтожается до того, как cleanup-система успела обработать его.

### Pattern 2: Managed ISystem для Bridge-систем
**What:** Bridge-системы используют `partial struct ISystem` но без `[BurstCompile]`, так как работают с managed-типами (Transform, ReactiveValue).
**When to use:** GameObjectSyncSystem, ObservableBridgeSystem, DeadEntityCleanupSystem.
**Example:**
```csharp
// Managed ISystem -- нет [BurstCompile], доступ к managed GameObjectRef
[UpdateAfter(typeof(EcsMoveSystem))]
public partial struct GameObjectSyncSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (move, rotate, goRef) in
                 SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, GameObjectRef>())
        {
            var pos = move.ValueRO.Position;
            goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);
            var rot = rotate.ValueRO.Rotation;
            var angle = Mathf.Atan2(rot.y, rot.x) * Mathf.Rad2Deg;
            goRef.Transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
```
**Confidence:** HIGH -- managed ISystem без Burst -- стандартный паттерн Unity DOTS hybrid.

### Pattern 3: CollisionBridge -- Proxy из MonoBehaviour в ECS
**What:** Static/singleton utility, вызываемый из OnCollisionEnter2D визуалов. Разрешает GameObject -> Entity через обратный маппинг и записывает CollisionEventData в DynamicBuffer singleton.
**When to use:** В OnCollisionEnter2D каждого визуала вместо текущих callback'ов.
**Example:**
```csharp
// CollisionBridge.cs
namespace SelStrom.Asteroids.ECS
{
    public class CollisionBridge
    {
        private readonly Dictionary<GameObject, Entity> _goToEntity;
        private readonly EntityManager _entityManager;
        private Entity _collisionBufferEntity;

        public void RegisterMapping(GameObject go, Entity entity)
        {
            _goToEntity[go] = entity;
        }

        public void ReportCollision(GameObject selfGo, GameObject otherGo)
        {
            if (_goToEntity.TryGetValue(selfGo, out var selfEntity) &&
                _goToEntity.TryGetValue(otherGo, out var otherEntity))
            {
                var buffer = _entityManager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
                buffer.Add(new CollisionEventData
                {
                    EntityA = selfEntity,
                    EntityB = otherEntity
                });
            }
        }
    }
}
```
**Confidence:** HIGH -- паттерн прост и использует уже существующий CollisionEventData buffer.

### Pattern 4: ObservableBridge -- ECS -> MVVM
**What:** Managed ISystem, который после всех ECS-систем читает компоненты корабля и singleton ScoreData и пушит значения в ReactiveValue на HudData/ShipViewModel.
**When to use:** Для всех UI-данных, которые раньше приходили из Model-компонентов через EventBindingContext.
**Key insight:** В текущей реализации GameScreen.ActivateHud() подписывается на `shipModel.Move.Position`, `shipModel.Laser.CurrentShoots` и т.д. В ECS-режиме эти данные приходят из ECS-компонентов. ObservableBridge читает ECS и пушит в те же ReactiveValue, которые используются HudVisual.

### Pattern 5: Shooting Callback через DynamicBuffer Event
**What:** EcsGunSystem и EcsLaserSystem содержат TODO для стрельбы. Вместо managed-callback'а, после выстрела (CurrentShoots--) система добавляет tag-компонент или записывает event в DynamicBuffer. Bridge-система в managed-коде обрабатывает эти events и создает пули/лазерные эффекты через EntitiesCatalog.
**Example:**
```csharp
// GunShootEvent.cs
public struct GunShootEvent : IBufferElementData
{
    public Entity ShooterEntity;
    public float2 Position;
    public float2 Direction;
    public bool IsPlayer;
}
```
**Confidence:** HIGH -- DynamicBuffer events стандартный паттерн для ECS -> managed-код коммуникации.

### Pattern 6: Миграционный флаг
**What:** bool поле `_useEcs` в Application.cs или Game.cs, определяющее какой Update-путь использовать.
**When to use:** Для параллельного запуска старого Model и нового ECS во время отладки.
**Recommendation:** `private bool _useEcs = true;` в Application.cs. В OnUpdate: если `_useEcs` -- `World.DefaultGameObjectInjectionWorld.Update()`, иначе -- `_model.Update(deltaTime)`.

### Anti-Patterns to Avoid
- **Не использовать `[BurstCompile]` на системах с managed-доступом:** GameObjectSyncSystem, ObservableBridgeSystem, DeadEntityCleanupSystem работают с Transform, ReactiveValue -- managed-типы несовместимы с Burst.
- **Не дублировать логику коллизий:** CollisionBridge только проксирует Physics2D -> ECS buffer. Вся логика обработки остается в EcsCollisionHandlerSystem.
- **Не модифицировать ECS-системы Phase 4:** Rotate, Thrust, Move, ShootTo, MoveTo -- работают. Только GunSystem и LaserSystem дополняются event-буферами.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Managed-ссылки в ECS | Свой словарь Entity->Transform | ICleanupComponentData class | Стандартный Unity DOTS паттерн, автоматический cleanup при DestroyEntity |
| ECS -> managed events | Callback'и из ISystem | DynamicBuffer<Event> | Чистое разделение Burst/managed, один проход за кадр |
| Physics2D в ECS | DOTS Physics | Physics2D + CollisionBridge proxy | DOTS Physics 2D не production-ready |
| Реактивные привязки | Свой observer | shtl-mvvm (ObservableValue/ReactiveValue) | Уже используется, проверен |

## Common Pitfalls

### Pitfall 1: Managed ISystem запрос managed-компонентов
**What goes wrong:** `SystemAPI.Query<ManagedComponent>()` работает иначе чем struct-компоненты -- нет RefRO/RefRW wrapper.
**Why it happens:** Managed IComponentData (class) возвращается по ссылке напрямую, без RefRO/RefRW.
**How to avoid:** Для managed-компонентов в Query: `SystemAPI.Query<MoveData, GameObjectRef>()` -- GameObjectRef возвращается как сам объект, не RefRO<GameObjectRef>.
**Warning signs:** Компиляционная ошибка при попытке `RefRO<GameObjectRef>`.

### Pitfall 2: Порядок систем -- GameObjectSyncSystem после всех ECS-систем
**What goes wrong:** Transform обновляется до того, как ECS-системы обновили позицию -- визуал отстает на 1 кадр.
**Why it happens:** Неправильный UpdateAfter/UpdateBefore.
**How to avoid:** `[UpdateAfter(typeof(EcsMoveSystem))]` и `[UpdateAfter(typeof(EcsRotateSystem))]` на GameObjectSyncSystem. Лучше -- явная SystemGroup.
**Warning signs:** Визуальный "дрожание" объектов.

### Pitfall 3: DeadTag на entities без визуала (CollisionEventData singleton, GameArea singleton)
**What goes wrong:** DeadEntityCleanupSystem находит DeadTag на entities, которые не имеют GameObjectRef.
**Why it happens:** Singleton entities (ScoreData, GameAreaData, CollisionEventData buffer) не имеют GameObjectRef.
**How to avoid:** Запрос `WithAll<DeadTag, GameObjectRef>()` или проверка HasComponent<GameObjectRef>.
**Warning signs:** NullReferenceException при cleanup.

### Pitfall 4: EcsLifeTimeSystem не добавляет DeadTag
**What goes wrong:** Пули с TimeRemaining==0 не уничтожаются, потому что LifeTimeSystem только уменьшает таймер, не помечает как мертвые.
**Why it happens:** В Phase 4 EcsLifeTimeSystem реализован минимально -- только декремент, без DeadTag.
**How to avoid:** Добавить в EcsLifeTimeSystem: если TimeRemaining <= 0, добавить DeadTag (или создать отдельную систему).
**Warning signs:** Пули накапливаются бесконечно.

### Pitfall 5: Дублирование коллизий -- OnCollisionEnter2D вызывается для обоих участников
**What goes wrong:** Physics2D вызывает OnCollisionEnter2D на обоих GameObject'ах -- CollisionBridge запишет два события для одной коллизии.
**Why it happens:** Unity Physics2D поведение -- оба объекта получают callback.
**How to avoid:** CollisionBridge проверяет, что пара (A, B) и (B, A) не дублируется. Или: только определенные визуалы (Ship, Bullet, Ufo) вызывают CollisionBridge.ReportCollision, а остальные игнорируют. В текущем коде: ShipVisual, BulletVisual, UfoVisual имеют OnCollisionEnter2D, а AsteroidVisual -- нет. Это значит коллизии обрабатываются только со стороны "активных" объектов.
**Warning signs:** Двойной score, двойной Kill.

### Pitfall 6: asmdef зависимости -- AsteroidsECS не ссылается на Asteroids
**What goes wrong:** Bridge-системы в AsteroidsECS не могут обращаться к классам из Asteroids assembly (EntitiesCatalog, ReactiveValue, ShipViewModel).
**Why it happens:** `AsteroidsECS.asmdef` ссылается только на Unity.Entities, Unity.Burst, Unity.Mathematics, Unity.Collections -- нет ссылки на Asteroids или Shtl.Mvvm.
**How to avoid:** Два варианта: (1) Bridge-системы размещаются в Asteroids assembly; (2) Добавить ссылки в AsteroidsECS.asmdef. **Рекомендация:** Bridge-системы в Asteroids assembly (или новый AsteroidsBridge.asmdef), так как они managed и нуждаются в shtl-mvvm.
**Warning signs:** Компиляционные ошибки при обращении к EntitiesCatalog или ReactiveValue из ECS assembly.

### Pitfall 7: World.Update() vs ручной Update
**What goes wrong:** `World.DefaultGameObjectInjectionWorld.Update()` обновляет ВСЕ системы в World, включая встроенные Unity-системы.
**Why it happens:** В Unity Entities 1.x DefaultWorld содержит множество встроенных систем.
**How to avoid:** Использовать DefaultGameObjectInjectionWorld и добавлять bridge-системы в SimulationSystemGroup. НЕ создавать отдельный World -- это усложнит интеграцию с managed-кодом. Unity вызывает Update на DefaultWorld автоматически -- **не нужно** вызывать World.Update() вручную из Application.OnUpdate.
**Warning signs:** Двойной Update ECS-систем (один от Unity, один из Application.OnUpdate).

### Pitfall 8: Астероиды без RotateData
**What goes wrong:** GameObjectSyncSystem запрашивает (MoveData, RotateData, GameObjectRef), но астероиды и пули не имеют RotateData.
**Why it happens:** В EntityFactory астероиды и пули создаются без RotateData.
**How to avoid:** Два отдельных запроса: (1) с RotateData для корабля/UFO, (2) только с MoveData для остальных. Или: добавить RotateData всем entities с дефолтным значением. **Рекомендация:** Два запроса -- проще и чище.

## Code Examples

### GameObjectRef Component
```csharp
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids.ECS
{
    // Managed component -- class, не struct
    // ICleanupComponentData гарантирует сохранение при DestroyEntity
    // для обработки cleanup-системой
    public class GameObjectRef : ICleanupComponentData
    {
        public Transform Transform;
        public GameObject GameObject;
    }
}
```

### EntityFactory расширение (добавление GameObjectRef)
```csharp
// В EntitiesCatalog после создания entity через EntityFactory:
var entity = EntityFactory.CreateShip(em, ...);
em.AddComponentObject(entity, new GameObjectRef
{
    Transform = view.transform,
    GameObject = view.gameObject
});
```

### GameObjectSyncSystem
```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids.ECS
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GameObjectSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Entities с MoveData + RotateData + GameObjectRef (корабль, UFO)
            foreach (var (move, rotate, goRef) in
                     SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, GameObjectRef>())
            {
                var pos = move.ValueRO.Position;
                goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);

                var rot = rotate.ValueRO.Rotation;
                var angle = Mathf.Atan2(rot.y, rot.x) * Mathf.Rad2Deg;
                goRef.Transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Entities с MoveData + GameObjectRef, НО без RotateData (астероиды, пули)
            foreach (var (move, goRef) in
                     SystemAPI.Query<RefRO<MoveData>, GameObjectRef>()
                         .WithNone<RotateData>())
            {
                var pos = move.ValueRO.Position;
                goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);
            }
        }
    }
}
```
**Note:** Используется `SystemBase` вместо `ISystem` для удобства работы с managed-компонентами. `SystemBase` позволяет прямой доступ к managed IComponentData через SystemAPI.Query.

### DeadEntityCleanupSystem
```csharp
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DeadEntityCleanupSystem : SystemBase
{
    private EntitiesCatalog _catalog;

    public void SetCatalog(EntitiesCatalog catalog)
    {
        _catalog = catalog;
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (goRef, entity) in
                 SystemAPI.Query<GameObjectRef>()
                     .WithAll<DeadTag>()
                     .WithEntityAccess())
        {
            // Делегируем Release в EntitiesCatalog для возврата в пул
            _catalog.ReleaseByGameObject(goRef.GameObject);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
```

### Обработка стрельбы через GunShootEvent buffer
```csharp
// Компонент-событие
public struct GunShootEvent : IBufferElementData
{
    public Entity ShooterEntity;
    public float2 Position;
    public float2 Direction;
    public bool IsPlayer;
}

// В EcsGunSystem -- после выстрела:
// gun.ValueRW.CurrentShoots--;
// Добавить event в singleton buffer (managed bridge прочитает)

// ShootingBridgeSystem (managed) -- обрабатывает events, создает пули
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Model.Update(dt) из Application | World автоматически обновляет системы | Phase 5 | Application больше не вызывает Update -- Unity Entities Runtime обновляет DefaultWorld |
| EventBindingContext From(model.X).To(vm.Y) | ObservableBridgeSystem пушит ECS данные в ReactiveValue | Phase 5 | Источник данных меняется с Model-компонентов на ECS-компоненты |
| Game.Kill(model) -> model.Kill() -> Model cleanup | DeadTag -> DeadEntityCleanupSystem -> EntitiesCatalog.Release | Phase 5 | Lifecycle управляется через ECS tags |
| OnCollisionEnter2D -> callback в Game.cs | OnCollisionEnter2D -> CollisionBridge -> CollisionEventData -> EcsCollisionHandlerSystem | Phase 5 | Коллизии проходят через ECS |

## Critical Discovery: ECS-07/ECS-08 Already Implemented

Анализ кода показал, что `EcsGunSystem.cs` и `EcsLaserSystem.cs` **уже реализованы** с полной логикой перезарядки и стрельбы. Тесты `GunSystemTests.cs` и `LaserSystemTests.cs` также присутствуют с полным покрытием (6 и 7 тестов соответственно).

Единственное отсутствующее -- callback'и стрельбы (строки с комментарием "OnShooting callback -- Phase 5 Bridge Layer"). Это значит:
- **ECS-07/ECS-08 НЕ нужно "завершать"** -- они работают
- **TST-05/TST-06 УЖЕ реализованы** -- тесты покрывают все сценарии
- **Нужно только добавить GunShootEvent/LaserShootEvent** в эти системы для bridge-интеграции

## Critical Discovery: LifeTimeSystem Gap

`EcsLifeTimeSystem` уменьшает `TimeRemaining` до 0, но **не добавляет DeadTag**. Это значит пули с истекшим временем жизни не будут уничтожены. Нужно:
- Либо добавить `DeadTag` в `EcsLifeTimeSystem` когда `TimeRemaining <= 0`
- Либо создать отдельную систему `EcsDeadTagLifeTimeSystem` для этого

**Рекомендация:** Добавить прямо в EcsLifeTimeSystem (но это сломает Burst-компиляцию, так как AddComponent -- structural change). Альтернатива: отдельная managed-система после LifeTimeSystem.

## Critical Discovery: Assembly Dependencies

`AsteroidsECS.asmdef` не ссылается на `Asteroids` assembly и `Shtl.Mvvm`. Bridge-системы нуждаются в доступе к:
- `EntitiesCatalog` (Asteroids assembly)
- `ReactiveValue`, `ObservableValue` (Shtl.Mvvm assembly)
- `ShipViewModel`, `HudData` (Asteroids assembly)

**Рекомендация:** Разместить bridge-системы в **Asteroids assembly** (`Assets/Scripts/`), добавив ссылку на `AsteroidsECS` в `Asteroids.asmdef`. Это проще, чем создавать новый asmdef или добавлять обратные ссылки.

**Текущие ссылки Asteroids.asmdef:**
```json
"references": [
    "GUID:75469ad4d38634e559750d17036d5f7c",  // InputSystem
    "GUID:14ffe7d22523436e8735b8ec4e4dab0c",  // ???
    "GUID:6055be8ebefd69e48b49212b09b47b2f",  // TMP
    "Shtl.Mvvm",
    "Unity.Services.Core",
    "Unity.Services.Authentication",
    "Unity.Services.Leaderboards"
]
```
Нужно добавить: `"AsteroidsECS"` и `"Unity.Entities"`.

## Open Questions

1. **SystemBase vs ISystem для managed-систем**
   - What we know: ISystem (struct) стандартнее в Unity Entities 1.x, но для managed-компонентов SystemBase (class) удобнее
   - What's unclear: Какой подход предпочтительнее для hybrid -- оба работают
   - Recommendation: SystemBase для bridge-систем, ISystem с Burst для чистых ECS-систем

2. **World.Update() -- автоматический vs ручной**
   - What we know: Unity автоматически обновляет DefaultGameObjectInjectionWorld каждый кадр
   - What's unclear: Нужно ли отключать автоматический Update и вызывать вручную из Application.OnUpdate для контроля порядка
   - Recommendation: Оставить автоматический Update, убрать `_model.Update(deltaTime)` из Application.OnUpdate когда `_useEcs=true`. ECS-системы обновляются автоматически.

3. **PlayMode тесты (TST-12) -- глубина проверок**
   - What we know: Нужен тест полного цикла (старт -> игра -> конец)
   - What's unclear: Как детально проверять (только State transitions, или также Score, Entity count)
   - Recommendation: Минимальный тест: (1) загрузка сцены, (2) проверка начального состояния, (3) симуляция Input (Space для стрельбы), (4) ожидание N кадров, (5) проверка что GameScreen.State меняется. Полный цикл: имитировать столкновение корабля и проверить EndGame.

4. **Дробление астероидов в ECS-режиме**
   - What we know: При уничтожении астероида с Age>1 создаются 2 новых астероида
   - What's unclear: Кто создает новые астероиды -- DeadEntityCleanupSystem или Game.cs?
   - Recommendation: DeadEntityCleanupSystem обнаруживает DeadTag+AsteroidTag с AgeData.Age>1, и вызывает EntitiesCatalog для создания осколков. Или: отдельная AsteroidSplitSystem.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef`, `Assets/Tests/PlayMode/PlayModeTests.asmdef` |
| Quick run command | Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All |
| Full suite command | Unity Editor -> Window -> General -> Test Runner -> Run All (EditMode + PlayMode) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | File Exists? |
|--------|----------|-----------|-------------|
| BRG-01 | GameObjectRef связывает Entity с Transform | unit (EditMode) | Wave 0 |
| BRG-02 | GameObjectSyncSystem обновляет Transform из MoveData/RotateData | unit (EditMode) | Wave 0 |
| BRG-03 | CollisionBridge записывает Physics2D коллизии в CollisionEventData buffer | unit (EditMode) | Wave 0 |
| BRG-04 | ObservableBridgeSystem пушит ECS данные в ReactiveValue | unit (EditMode) | Wave 0 |
| BRG-05 | DeadTag -> Release GameObject, DestroyEntity | unit (EditMode) | Wave 0 |
| BRG-06 | Полный геймплей 1:1 | PlayMode (manual verify) | Wave 0 |
| TST-10 | EditMode тесты Bridge Layer | unit (EditMode) | Wave 0 (это и есть тесты для BRG-01..05) |
| TST-12 | PlayMode тест полного цикла | integration (PlayMode) | Wave 0 |

### Sampling Rate
- **Per task commit:** Run ECS EditMode tests
- **Per wave merge:** Run all EditMode + PlayMode tests
- **Phase gate:** Full suite green + manual gameplay verification

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/ECS/GameObjectSyncSystemTests.cs` -- BRG-01, BRG-02
- [ ] `Assets/Tests/EditMode/ECS/CollisionBridgeTests.cs` -- BRG-03
- [ ] `Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs` -- BRG-04
- [ ] `Assets/Tests/EditMode/ECS/DeadEntityCleanupSystemTests.cs` -- BRG-05
- [ ] `Assets/Tests/PlayMode/GameplayCycleTests.cs` -- TST-12
- [ ] `PlayModeTests.asmdef` -- добавить ссылку на `AsteroidsECS` и `Unity.Entities`
- [ ] `EcsEditModeTests.asmdef` -- добавить ссылку на `Asteroids` и `Shtl.Mvvm` для bridge-тестов (или создать отдельный BridgeEditModeTests.asmdef)

## Sources

### Primary (HIGH confidence)
- Исходный код проекта -- EntityFactory.cs, EntitiesCatalog.cs, Game.cs, Application.cs, все ECS-системы и компоненты
- Исходный код тестов -- GunSystemTests.cs, LaserSystemTests.cs (подтверждение что ECS-07/ECS-08 реализованы)
- AsteroidsECS.asmdef, Asteroids.asmdef (зависимости assembly)
- Unity Entities 1.4.5 (установлен в проекте)

### Secondary (MEDIUM confidence)
- Unity DOTS hybrid паттерны (ICleanupComponentData, managed ISystem, SystemBase для managed) -- основаны на документации Unity Entities 1.x
- DynamicBuffer для ECS events -- стандартный паттерн Unity DOTS

### Tertiary (LOW confidence)
- Точный API для Query managed-компонентов в SystemBase vs ISystem -- рекомендуется проверить при реализации

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все пакеты уже установлены и проверены в Phase 4
- Architecture: HIGH -- решения зафиксированы в CONTEXT.md, паттерны стандартные для Unity DOTS hybrid
- Pitfalls: HIGH -- обнаружены конкретные проблемы в коде (LifeTimeSystem, asmdef, дубли коллизий)
- Code examples: MEDIUM -- API managed-компонентов может иметь нюансы при реализации

**Research date:** 2026-04-03
**Valid until:** 2026-05-03 (стабильный стек, Unity Entities 1.4.x)
