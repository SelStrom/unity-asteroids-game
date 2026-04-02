# Architecture Research: Гибридный DOTS для Asteroids

**Domain:** Unity 2D аркада, миграция на гибридный DOTS (Entities для логики, GameObjects для визуала/UI)
**Researched:** 2026-04-02
**Confidence:** MEDIUM

## Ключевое решение: "Логика в Entities, рендеринг на GameObjects"

Проект Asteroids — 2D-игра с ~5 типами сущностей и ~50-200 активными объектами одновременно. Полный DOTS-рендеринг (Entities Graphics) **не поддерживает SpriteRenderer** — пакет `com.unity.entities.graphics` работает только с 3D мешами через `RenderMeshUtility`. Пакет `com.unity.2d.entities` остался в preview (0.32) и не совместим с Unity 6. Поэтому гибридный подход — единственный жизнеспособный вариант для 2D-игры на DOTS.

**Пакеты для Unity 6.3:**
- `com.unity.entities` 1.4.x (1.4.5 — последний на февраль 2026)
- `com.unity.physics` — для 3D-физики DOTS (нам НЕ нужен, см. раздел "Физика")
- `com.unity.entities.graphics` 1.4.x — нам НЕ нужен (нет 2D-поддержки)

**Confidence:** MEDIUM — версии пакетов подтверждены через GitHub-зеркало needle-mirror, но не через официальную документацию Unity 6.3.

## Архитектура системы

### Обзор слоёв

```
┌─────────────────────────────────────────────────────────────────┐
│  Unity MonoBehaviour Layer (сохраняется)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────┐    │
│  │ApplicationEntry│  │  UI Screens  │  │  PlayerInput       │    │
│  │ (точка входа) │  │ (MVVM, uGUI) │  │  (Input System)    │    │
│  └──────┬───────┘  └──────┬───────┘  └────────┬───────────┘    │
├─────────┼──────────────────┼───────────────────┼────────────────┤
│  Bridge Layer (НОВЫЙ)                                           │
│  ┌──────┴──────────────────┴───────────────────┴───────────┐    │
│  │              GameObjectSyncSystem (SystemBase)           │    │
│  │  Entity → читает Position/Rotation → пишет в Transform   │    │
│  │  Managed component GameObjectRef хранит ссылку на GO     │    │
│  └─────────────────────────┬───────────────────────────────┘    │
│  ┌─────────────────────────┴───────────────────────────────┐    │
│  │              EntityViewRegistry (чистый C#)              │    │
│  │  Entity ↔ GameObject, создание/уничтожение GO из пула    │    │
│  └─────────────────────────┬───────────────────────────────┘    │
├─────────────────────────────┼───────────────────────────────────┤
│  ECS World (НОВЫЙ — заменяет Model Layer)                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │IComponentData│  │  SystemBase  │  │  Entity     │             │
│  │ (компоненты) │  │  (системы)  │  │  (сущности) │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
│                                                                 │
│  Системы (порядок обновления через SystemGroup):                │
│  RotateSystem → ThrustSystem → MoveSystem → LifeTimeSystem     │
│  → GunSystem → LaserSystem → ShootToSystem → MoveToSystem      │
│  → [GameObjectSyncSystem] → [CollisionCheckSystem]              │
├─────────────────────────────────────────────────────────────────┤
│  Config Layer (сохраняется)                                     │
│  ScriptableObject ассеты → BlobAsset или прямое чтение          │
└─────────────────────────────────────────────────────────────────┘
```

### Границы компонентов

| Компонент | Ответственность | Мир |
|-----------|----------------|-----|
| **ECS World (Entities)** | Вся игровая логика: перемещение, вращение, тяга, стрельба, ИИ НЛО, время жизни пуль, тороидальная обёртка | Entities |
| **GameObjectSyncSystem** | Однонаправленная синхронизация: Entity → GameObject Transform | Bridge |
| **EntityViewRegistry** | Маппинг Entity ↔ GameObject, управление пулом GO, создание/уничтожение визуалов | Bridge |
| **Visual/ViewModel (MVVM)** | SpriteRenderer, UI-биндинги через shtl-mvvm, коллизии (Physics2D), эффекты частиц | GameObjects |
| **UI Screens** | HUD, Title, Result, Leaderboard — полностью на GameObjects + uGUI + shtl-mvvm | GameObjects |
| **PlayerInput** | Ввод игрока, трансляция в ECS-компоненты через managed system | GameObjects → Entities |

## Рекомендуемая структура проекта

```
Assets/Scripts/
├── Application/                    # Слой приложения (сохраняется с рефактором)
│   ├── ApplicationEntry.cs         # MonoBehaviour, создаёт World + Application
│   ├── Application.cs              # Корневой объект, владеет EntityViewRegistry
│   ├── Game.cs                     # Управление игровым процессом (рефактор)
│   └── Screens/                    # UI-экраны (без изменений)
│
├── ECS/                            # НОВЫЙ — всё что Entities
│   ├── Components/                 # IComponentData структуры
│   │   ├── MoveData.cs             # float2 Position, float2 Direction, float Speed
│   │   ├── RotateData.cs           # float2 Rotation, int TargetDirection
│   │   ├── ThrustData.cs           # bool IsActive, float UnitsPerSecond, float MaxSpeed
│   │   ├── GunData.cs              # int MaxShoots, float ReloadDuration, ...
│   │   ├── LaserData.cs            # аналогично GunData + Observable bridge
│   │   ├── LifeTimeData.cs         # float TimeRemaining
│   │   ├── ShootToData.cs          # Entity ShipTarget
│   │   ├── MoveToData.cs           # Entity ShipTarget, float Every
│   │   └── Tags/                   # Тег-компоненты
│   │       ├── ShipTag.cs          # IComponentData struct (пустой)
│   │       ├── AsteroidTag.cs      # + int Age
│   │       ├── BulletTag.cs        # + bool IsPlayerBullet
│   │       ├── UfoTag.cs
│   │       └── DeadTag.cs          # IComponentData + IEnableableComponent
│   │
│   ├── Systems/                    # SystemBase / ISystem
│   │   ├── RotateSystem.cs
│   │   ├── ThrustSystem.cs
│   │   ├── MoveSystem.cs           # включает тороидальную обёртку
│   │   ├── LifeTimeSystem.cs
│   │   ├── GunSystem.cs
│   │   ├── LaserSystem.cs
│   │   ├── ShootToSystem.cs
│   │   ├── MoveToSystem.cs
│   │   └── DeadCleanupSystem.cs    # обработка DeadTag → уничтожение entity
│   │
│   ├── Archetypes/                 # Определения архетипов (фабрики entity)
│   │   └── EntityFactory.cs        # Создание entity с нужным набором компонентов
│   │
│   └── Bridge/                     # Мост ECS ↔ GameObject
│       ├── GameObjectRef.cs        # ICleanupComponentData class (managed)
│       ├── GameObjectSyncSystem.cs # Entity Position/Rotation → GO Transform
│       ├── EntityViewRegistry.cs   # Entity ↔ GO маппинг + пул
│       ├── InputBridgeSystem.cs    # PlayerInput → ECS компоненты
│       └── ObservableBridge.cs     # ECS данные → ObservableValue для MVVM
│
├── View/                           # Визуалы (сохраняются с минимальным рефактором)
│   ├── ShipVisual.cs               # SpriteRenderer + collision relay
│   ├── AsteroidVisual.cs
│   ├── BulletVisual.cs
│   ├── UfoVisual.cs
│   └── EffectVisual.cs
│
├── Configs/                        # ScriptableObject (сохраняются)
└── Utils/                          # Утилиты (сохраняются)
```

### Обоснование структуры

- **ECS/** выделен в отдельную папку, потому что это полная замена текущего `Model/`. Чистое разделение упрощает параллельную разработку и тестирование.
- **Bridge/** внутри ECS, потому что bridge-системы — это SystemBase, работающие в ECS World, но обращающиеся к managed-объектам.
- **View/** сохраняется без изменений — визуалы продолжают быть MonoBehaviour с ViewModel-биндингами.
- **Application/** рефакторится минимально: `EntitiesCatalog` заменяется на `EntityViewRegistry` + `EntityFactory`.

## Архитектурные паттерны

### Паттерн 1: Managed Component как ссылка на GameObject (GameObjectRef)

**Что:** Entity хранит ссылку на свой GameObject через managed IComponentData (class, не struct). Это позволяет ECS-системам знать, какой GO соответствует какому entity.

**Когда:** Для каждой игровой сущности, которой нужен визуал (Ship, Asteroid, Bullet, UFO).

**Компромиссы:**
- (+) Простая однозначная связь Entity ↔ GO
- (+) Lifecycle через ICleanupComponentData — при уничтожении entity система автоматически возвращает GO в пул
- (-) Managed компоненты не совместимы с Burst и Jobs — синхронизация только в main thread
- (-) Не даёт перформанс-выигрыша DOTS для рендеринга

**Пример:**
```csharp
// Managed component — хранит ссылку на GameObject
public class GameObjectRef : IComponentData, IDisposable, ICloneable
{
    public GameObject Value;
    public IEntityView View;

    public void Dispose()
    {
        // Возврат в пул при уничтожении entity
    }

    public object Clone()
    {
        return new GameObjectRef { Value = Value, View = View };
    }
}

// Cleanup component — для обнаружения уничтожения entity
public struct GameObjectCleanup : ICleanupComponentData
{
    // Пустой — сам факт наличия говорит "нужно вернуть GO в пул"
}
```

### Паттерн 2: Однонаправленная синхронизация Entity → Transform

**Что:** Отдельная SystemBase на каждом кадре читает Position/Rotation из ECS-компонентов и записывает в Transform привязанных GameObjects.

**Когда:** После всех систем игровой логики, перед рендерингом.

**Компромиссы:**
- (+) Данные остаются авторитетными в ECS — единый источник истины
- (+) Визуалы "тупые", только отображают
- (-) Main thread только, нельзя Burst
- (-) При 200 сущностях это ~200 Transform.position записей в кадр — приемлемо

**Пример:**
```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class GameObjectSyncSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Main thread — managed component требует этого
        foreach (var (moveData, goRef) in
            SystemAPI.Query<RefRO<MoveData>, GameObjectRef>())
        {
            if (goRef.Value != null)
            {
                var pos = moveData.ValueRO.Position;
                goRef.Value.transform.position = new Vector3(pos.x, pos.y, 0f);
            }
        }

        // Отдельный запрос для сущностей с вращением
        foreach (var (rotateData, goRef) in
            SystemAPI.Query<RefRO<RotateData>, GameObjectRef>())
        {
            if (goRef.Value != null)
            {
                var dir = rotateData.ValueRO.Rotation;
                float angle = math.degrees(math.atan2(dir.y, dir.x));
                goRef.Value.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}
```

### Паттерн 3: Observable Bridge для MVVM

**Что:** Специальная managed system читает ECS-данные, которые должны отображаться в UI (лазерные заряды, скорость, координаты для HUD), и пишет их в `ObservableValue<T>` из shtl-mvvm.

**Когда:** Только для данных, видимых в UI: координаты корабля в HUD, заряды лазера, угол поворота.

**Компромиссы:**
- (+) UI-слой не знает про ECS — shtl-mvvm биндинги работают как раньше
- (+) Минимальное изменение существующих экранов
- (-) Дополнительный managed system на main thread
- (-) Нужно аккуратно управлять lifecycle ObservableValue при создании/уничтожении entity

**Пример:**
```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(GameObjectSyncSystem))]
public partial class ObservableBridgeSystem : SystemBase
{
    // Регистрируется из Application layer при создании корабля
    public ObservableValue<int> LaserCharges;
    public ObservableValue<float> LaserReloadRemaining;

    protected override void OnUpdate()
    {
        foreach (var (laser, tag) in
            SystemAPI.Query<RefRO<LaserData>, RefRO<ShipTag>>())
        {
            if (LaserCharges != null)
            {
                LaserCharges.Value = laser.ValueRO.CurrentShoots;
            }
            if (LaserReloadRemaining != null)
            {
                LaserReloadRemaining.Value = laser.ValueRO.ReloadRemaining;
            }
        }
    }
}
```

### Паттерн 4: ECS-физика НЕ используется — остаётся Physics2D

**Что:** Коллизии обрабатываются через Unity Physics2D на GameObjects (как сейчас). `CollisionHandler` на Visual-объектах relay-ит событие столкновения в managed ECS-систему, которая ставит `DeadTag` на соответствующие entity.

**Почему:** Unity Physics (DOTS) — 3D только. Официального DOTS Physics 2D не существует. Сторонний плагин `UnityPhysics2DPlugin` поддерживает только Box/Circle/Capsule коллайдеры и находится в экспериментальном состоянии. При 50-200 объектах Physics2D на GameObjects работает адекватно.

**Компромиссы:**
- (+) Нулевой риск — существующая физика работает
- (+) Визуалы уже имеют коллайдеры и relay-коллбэки
- (-) Позиция коллайдера зависит от Transform, который обновляется из ECS → задержка в 1 кадр
- (-) Нельзя использовать Burst для проверки коллизий

**Confidence:** HIGH — отсутствие DOTS Physics 2D подтверждено множественными источниками и обсуждениями на форуме Unity.

## Поток данных

### Основной игровой цикл

```
[Input (PlayerInput MonoBehaviour)]
    │
    ↓ (InputBridgeSystem читает Input и пишет в ECS-компоненты)
[SimulationSystemGroup]
    │
    ├─→ RotateSystem (TargetDirection → Rotation)
    ├─→ ThrustSystem (IsActive + Rotation → Direction + Speed)
    ├─→ MoveSystem (Direction + Speed → Position + ToroidalWrap)
    ├─→ LifeTimeSystem (TimeRemaining -= dt)
    ├─→ GunSystem (Shooting + Reload → OnShooting callback)
    ├─→ LaserSystem (аналогично GunSystem)
    ├─→ ShootToSystem (предиктивное прицеливание → Gun.Shooting)
    └─→ MoveToSystem (перехват корабля → Direction)
    │
    ↓
[PresentationSystemGroup]
    │
    ├─→ GameObjectSyncSystem (Position/Rotation → Transform)
    ├─→ ObservableBridgeSystem (LaserCharges → ObservableValue → UI)
    └─→ SpriteSwitchBridge (ThrustIsActive → ViewModel.Sprite)
    │
    ↓
[Physics2D на GameObjects (Unity внутренний цикл)]
    │
    ↓ (OnCollisionEnter2D / OnTriggerEnter2D)
[CollisionBridgeSystem (managed, ставит DeadTag на entity)]
    │
    ↓
[DeadCleanupSystem (следующий кадр)]
    ├─→ Удаляет entity с DeadTag
    ├─→ GameObjectCleanup → возвращает GO в пул
    └─→ Событие для Game.cs (дробление астероидов, подсчёт очков)
```

### Создание сущности (Spawn Flow)

```
Game.cs вызывает EntityFactory.CreateAsteroid(...)
    │
    ├─→ EntityManager.CreateEntity(asteroidArchetype)
    ├─→ EntityManager.SetComponentData(entity, new MoveData{...})
    ├─→ EntityManager.SetComponentData(entity, new AsteroidTag{Age=3})
    ├─→ EntityViewRegistry создаёт GO из пула
    ├─→ EntityManager.AddComponentData(entity, new GameObjectRef{Value=go})
    └─→ ViewModel биндинги настраиваются через ObservableBridge
```

### Уничтожение сущности (Death Flow)

```
Physics2D коллизия → Visual.OnCollision → CollisionBridge
    │
    ├─→ Находит Entity по GameObject через EntityViewRegistry
    ├─→ EntityManager.SetComponentEnabled<DeadTag>(entity, true)
    │
    ↓ (следующий кадр, DeadCleanupSystem)
    ├─→ Для астероидов: читает AsteroidTag.Age, создаёт 2 осколка
    ├─→ EntityManager.DestroyEntity(entity)
    ├─→ GameObjectCleanup срабатывает → EntityViewRegistry.Release(go)
    └─→ Событие Score → ObservableBridge → UI
```

## Маппинг существующих систем на ECS

| Текущая система | ECS-эквивалент | Изменения |
|-----------------|----------------|-----------|
| `Model.cs` (центральный класс) | `World.DefaultGameObjectInjectionWorld` | Заменяется полностью; порядок систем через `[UpdateBefore/After]` |
| `BaseModelSystem<TNode>` с `Dictionary<IGameEntityModel, TNode>` | `SystemBase` / `ISystem` с `SystemAPI.Query<>()` | Архетипы вместо ручных словарей |
| `IGameEntityModel` + Visitor | Tag-компоненты (`ShipTag`, `AsteroidTag`, ...) | Архетип определяет набор компонентов |
| `MoveComponent` (class с ObservableValue) | `MoveData : IComponentData` (struct, blittable) | Observable убран, bridge sync вместо |
| `EntitiesCatalog` (Model↔View маппинг) | `EntityViewRegistry` + `GameObjectRef` managed component | Реестр упрощается, cleanup автоматический |
| `ModelFactory` / `ViewFactory` | `EntityFactory` + существующий `GameObjectPool` | EntityFactory создаёт Entity + GO |
| `ActionScheduler` | `SystemAPI.Time.ElapsedTime` + компонент-таймер | Встроенное время ECS |
| `GameObjectPool` | Сохраняется для GameObject-визуалов | Без изменений |

## Порядок сборки (Build Order)

Зависимости определяют порядок миграции:

### Фаза 1: Фундамент ECS (блокирует всё остальное)
1. Подключить `com.unity.entities` 1.4.x
2. Создать `IComponentData` структуры (MoveData, RotateData, ThrustData, ...) — прямая конвертация из текущих `*Component.cs`
3. Создать архетипы (наборы компонентов для каждого типа сущности)
4. Создать `EntityFactory` — замена `ModelFactory`

**Зависимости:** Нет (чистый новый код)

### Фаза 2: ECS-системы (зависит от Фазы 1)
5. Портировать 8 систем из `BaseModelSystem<T>` в `ISystem` / `SystemBase`
6. Настроить `[UpdateInGroup]` и `[UpdateBefore/After]` для порядка обновления
7. Верифицировать: системы работают изолированно (можно тестировать без GameObjects)

**Зависимости:** Фаза 1 (компоненты должны существовать)

### Фаза 3: Bridge Layer (зависит от Фаз 1-2)
8. Реализовать `GameObjectRef` (managed component) + `GameObjectCleanup`
9. Реализовать `EntityViewRegistry` (маппинг Entity ↔ GO)
10. Реализовать `GameObjectSyncSystem` (Position/Rotation → Transform)
11. Реализовать `InputBridgeSystem` (PlayerInput → ECS)
12. Реализовать `ObservableBridgeSystem` (ECS → ObservableValue для MVVM)

**Зависимости:** Фазы 1 + 2 (нужны компоненты и работающие системы)

### Фаза 4: Интеграция (зависит от Фаз 1-3)
13. Рефакторить `Game.cs` — заменить вызовы `EntitiesCatalog` на `EntityFactory` + `EntityViewRegistry`
14. Подключить Physics2D коллизии через `CollisionBridgeSystem`
15. Рефакторить `Application.cs` — убрать `Model.Update()`, World обновляется автоматически
16. Верифицировать: полный игровой цикл работает

**Зависимости:** Все предыдущие фазы

### Фаза 5: Оптимизация (опционально)
17. Конвертировать `SystemBase` → `ISystem` + `Burst` для чистых систем (Move, Rotate, Thrust, LifeTime)
18. Профилирование и оптимизация

**Зависимости:** Фаза 4 (работающая игра)

## Решение по рендерингу: URP 2D + SpriteRenderer на GameObjects

### Что меняется при миграции на URP

1. **Render Pipeline Asset:** Создать URP Asset с 2D Renderer
2. **Материалы:** Конвертировать через Window > Rendering > Render Pipeline Converter
3. **Спрайты:** URP автоматически назначает `Sprite-Lit-Default` материал — спрайты работают из коробки
4. **Шейдеры:** Если есть кастомные — конвертировать на URP-совместимые (в текущем проекте кастомных нет)
5. **Particle System (эффекты взрывов):** URP поддерживает стандартный ParticleSystem, но материал нужно обновить

### Что НЕ меняется

- SpriteRenderer остаётся на GameObjects (не затронут миграцией на DOTS)
- Ортографическая камера (size 22.5) — работает в URP 2D идентично
- Sorting Layers / Order in Layer — работают идентично
- Physics2D — не зависит от Render Pipeline

**Confidence:** HIGH — URP 2D с SpriteRenderer — хорошо задокументированная, стабильная конфигурация.

## Анти-паттерны

### Анти-паттерн 1: Companion GameObjects вместо ручного bridge

**Что делают:** Используют встроенный механизм companion GameObjects из Entities Graphics, надеясь что он подхватит SpriteRenderer.

**Почему плохо:** Entities Graphics companion components поддерживают Light, ReflectionProbe, TextMesh, ParticleSystem, VisualEffect — но НЕ SpriteRenderer. Неподдерживаемые компоненты **стриппятся** при конвертации. Transform синхронизация работает только в одном направлении (Entity → GO) и только для поддерживаемых компонентов.

**Вместо этого:** Ручной `GameObjectRef` managed component + `GameObjectSyncSystem`. Полный контроль, работает с любыми MonoBehaviour.

### Анти-паттерн 2: Дублирование данных в ECS и GameObjects

**Что делают:** Хранят Position и в ECS-компоненте, и в MonoBehaviour-компоненте, синхронизируя туда-сюда.

**Почему плохо:** Два источника истины → рассинхронизация, баги, сложная отладка.

**Вместо этого:** Единственный источник истины — ECS. Transform GO обновляется из ECS. Если Physics2D нуждается в позиции (для коллайдеров), он читает Transform, который уже обновлён из ECS.

### Анти-паттерн 3: Попытка запихнуть ObservableValue в IComponentData

**Что делают:** Пытаются сделать ECS-компоненты реактивными (Observable), как в текущей модели.

**Почему плохо:** `IComponentData` должны быть blittable struct. `ObservableValue<T>` — managed класс с подписками. Они фундаментально несовместимы.

**Вместо этого:** Bridge-система (`ObservableBridgeSystem`) как промежуточный слой: читает struct из ECS, пишет в managed ObservableValue. Реактивность — только на стороне GameObjects/UI.

### Анти-паттерн 4: SubScene/Baker для runtime-спауна

**Что делают:** Пытаются использовать Baking (SubScene) для создания сущностей в runtime.

**Почему плохо:** Baking происходит только в Editor, не в runtime. SubScene — для статического контента.

**Вместо этого:** `EntityManager.CreateEntity(archetype)` + ручная установка компонентов. Для prototype-based спауна: один entity-прототип в SubScene → `EntityManager.Instantiate(prototype)` в runtime.

## Интеграционные точки

### Внешние сервисы

| Сервис | Паттерн интеграции | Примечания |
|--------|-------------------|------------|
| UGS Leaderboard | Остаётся на MonoBehaviour (корутины) | Не затронут миграцией на DOTS |
| UGS Auth | Остаётся на MonoBehaviour | Не затронут |

### Внутренние границы

| Граница | Коммуникация | Направление | Примечания |
|---------|-------------|-------------|------------|
| ECS Systems ↔ GameObjects | GameObjectSyncSystem | Entity → GO (однонаправл.) | Main thread, каждый кадр |
| ECS ↔ MVVM/UI | ObservableBridgeSystem | Entity → ObservableValue | Только для UI-видимых данных |
| PlayerInput ↔ ECS | InputBridgeSystem | GO → Entity | Записывает в Singleton entity |
| Physics2D ↔ ECS | CollisionBridgeSystem | GO → Entity | Через EntityViewRegistry lookup |
| Game.cs ↔ ECS | EntityFactory + прямые вызовы EM | Bidirectional | Game.cs управляет спауном/смертью |

## Масштабирование

| Сценарий | Подход |
|----------|--------|
| Текущий (~50-200 объектов) | Гибридный DOTS адекватен, main thread sync не является bottleneck |
| ~1000 объектов | Burst-компиляция чистых систем (Move, Rotate) даст ускорение; bridge останется bottleneck |
| ~10000+ объектов | Нужен полный DOTS-рендеринг (NSprites или кастомный) вместо GameObjects; не в scope |

Для Asteroids ~50-200 объектов — это потолок геймплея. Масштабирование не является проблемой.

## Источники

- [Entities Graphics 1.4 — Companion Components](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.4/manual/companion-components.html) — HIGH confidence
- [Upgrading from Built-in RP to URP (Unity 6)](https://docs.unity3d.com/6000.2/Documentation/Manual/urp/upgrading-from-birp.html) — HIGH confidence
- [Runtime Entity Creation (Entities Graphics 1.0)](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/runtime-entity-creation.html) — MEDIUM confidence
- [needle-mirror/com.unity.entities releases](https://github.com/needle-mirror/com.unity.entities/releases) — версии 1.4.5, 1.3.15 подтверждены
- [Unity 6.3 What's New](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html) — HIGH confidence
- [ECS Development Status — December 2025](https://discussions.unity.com/t/ecs-development-status-december-2025/1699284) — MEDIUM confidence (форум, не документация)
- [Any 2D physics solutions with DOTS?](https://discussions.unity.com/t/any-2d-physics-solutions-with-dots/1654080) — подтверждает отсутствие DOTS Physics 2D
- [URP 2D Renderer Setup](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/Setup.html) — HIGH confidence
- [URP Sprite-Lit-Default](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/PrepShader.html) — HIGH confidence

---
*Architecture research for: Asteroids — гибридный DOTS на Unity 6.3*
*Researched: 2026-04-02*
