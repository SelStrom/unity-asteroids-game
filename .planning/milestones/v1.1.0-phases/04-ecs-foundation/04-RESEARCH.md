# Phase 4: ECS Foundation - Research

**Researched:** 2026-04-02
**Domain:** Unity DOTS ECS (com.unity.entities) -- IComponentData, ISystem, Burst, EditMode TDD
**Confidence:** HIGH

## Summary

Phase 4 создает параллельный ECS-слой рядом с существующим MonoBehaviour-кодом. Ключевые задачи: установка `com.unity.entities` 1.4.x, определение IComponentData-структур для всех сущностей, реализация 8 игровых систем на ISystem, создание EntityFactory, и покрытие всего EditMode-тестами. Burst-компиляция применяется к чистым системам (Move, Rotate, Thrust).

Проект уже работает на Unity 6.3 (6000.3.12f1). Пакеты `com.unity.burst` 1.8.28, `com.unity.collections` 2.6.5, `com.unity.mathematics` 1.3.3 уже установлены как транзитивные зависимости -- это совместимо с `com.unity.entities` 1.4.x. ECSTestsFixture из пакета Entities предоставляет готовую инфраструктуру для тестирования (World, EntityManager, Setup/TearDown).

**Primary recommendation:** Установить `com.unity.entities` 1.4.5, использовать `ISystem` + `SystemAPI.Query<RefRW<T>>` для итерации, `[BurstCompile]` на методах OnUpdate для чистых систем, и ECSTestsFixture как базу для всех тестов.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Использовать **ISystem** (unmanaged) для всех систем -- это современный Unity DOTS API, совместимый с Burst-компиляцией
- **D-02:** Burst-компиляция обязательна для чистых систем: MoveSystem, RotateSystem, ThrustSystem (требования ECS-04/05/06)
- **D-03:** Системы с managed-зависимостями (ShootToSystem, MoveToSystem, GunSystem, LaserSystem, CollisionHandler) реализуются как ISystem без BurstCompile -- managed access через SystemAPI
- **D-04:** 1:1 маппинг существующих компонентов на IComponentData: MoveComponent -> MoveData, ThrustComponent -> ThrustData, RotateComponent -> RotateData, GunComponent -> GunData, LaserComponent -> LaserData, ShootToComponent -> ShootToData, MoveToComponent -> MoveToData, LifeTimeComponent -> LifeTimeData
- **D-05:** Суффикс `Data` для IComponentData (вместо `Component`) -- избежать конфликта имён с существующими MonoBehaviour-компонентами
- **D-06:** Tag-компоненты для типов сущностей: ShipTag, AsteroidTag, BulletTag, UfoTag, UfoBigTag -- для фильтрации в запросах
- **D-07:** Компонент AgeData для астероидов (int Age) -- используется для логики дробления
- **D-08:** Позиция корабля доступна через **singleton component** `ShipPosition` -- `SystemAPI.GetSingleton<ShipPosition>()` в ShootToSystem и MoveToSystem
- **D-09:** ShipPosition обновляется MoveSystem после перемещения корабля (порядок систем сохраняется как в оригинале)
- **D-10:** ECS-код размещается в **`Assets/Scripts/ECS/`** с подкаталогами `Components/`, `Systems/`, `Authoring/`
- **D-11:** Отдельный asmdef `AsteroidsECS` с зависимостью на `Unity.Entities`, `Unity.Burst`, `Unity.Mathematics`, `Unity.Collections`
- **D-12:** Тесты ECS в `Assets/Tests/EditMode/ECS/` с ссылкой на `AsteroidsECS` assembly
- **D-13:** CollisionHandler реализуется как ISystem, принимающий коллизионные события через managed buffer/component -- конкретный механизм передачи данных из Physics2D определяется в Phase 5 (Bridge Layer)
- **D-14:** В Phase 4 CollisionHandler тестируется с ручным добавлением коллизионных данных в ECS World (mock-подход)
- **D-15:** EntityFactory -- статический класс или utility, создающий entities через EntityManager с правильным набором компонентов для каждого типа сущности
- **D-16:** Маппинг компонентов по типам сущностей сохраняется из оригинала: Ship -> Move+Rotate+Gun+Laser+Thrust, Asteroid -> Move+LifeTime(age), Bullet -> Move+LifeTime, UfoBig -> Move+Gun+ShootTo, Ufo -> Move+Gun+ShootTo+MoveTo

### Claude's Discretion
- Конкретная структура IComponentData полей (float2 vs float для позиций, использование Unity.Mathematics)
- Способ реализации системного порядка (UpdateBefore/UpdateAfter атрибуты vs SystemGroup)
- Детали EntityFactory API (методы, параметры конфигурации)
- Подход к тестированию: создание World в SetUp, уничтожение в TearDown, helper-методы для entity creation

### Deferred Ideas (OUT OF SCOPE)
- **Исправление багов** (wrapping formula, UFO kill, division by zero в ShootTo/MoveTo) -- отложено на отдельный milestone (QUAL-01)
- **Рефакторинг хардкодов** (скорость пули 20, MoveToComponent.Every = 3f, максимальная скорость осколков 10f) -- не часть миграции 1:1
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ECS-01 | Пакеты com.unity.entities и com.unity.burst установлены и совместимы с Unity 6.3 | com.unity.entities 1.4.5, burst 1.8.28 уже есть, mathematics 1.3.3 уже есть |
| ECS-02 | IComponentData определены для всех игровых сущностей | 8 Data-компонентов + 5 Tag + GameAreaData + ShipPositionData -- маппинг из оригинала |
| ECS-03 | EntityFactory создает entities с правильными компонентами | Статический класс с методами CreateShip/Asteroid/Bullet/UfoBig/Ufo |
| ECS-04 | ThrustSystem перенесена на ISystem с Burst | ISystem + [BurstCompile] на OnUpdate, SystemAPI.Query с RefRW |
| ECS-05 | RotateSystem перенесена на ISystem с Burst | math.mul(quaternion.EulerZXY) вместо Quaternion.Euler |
| ECS-06 | MoveSystem перенесена на ISystem с Burst + toroidal wrap | PlaceWithinGameArea как static math, GameAreaData singleton |
| ECS-07 | GunSystem перенесена на ISystem | ISystem без Burst (Action callback -- managed) |
| ECS-08 | LaserSystem перенесена на ISystem | ISystem без Burst (ObservableValue в Bridge Phase 5) |
| ECS-09 | ShootToSystem перенесена на ISystem | ShipPositionData singleton для позиции корабля |
| ECS-10 | MoveToSystem перенесена на ISystem | ShipPositionData singleton + ShipSpeedData |
| ECS-11 | CollisionHandler перенесен на ISystem | Mock-компонент CollisionEventData для Phase 4, реальные данные в Phase 5 |
| TST-01 | EditMode тесты для всех ECS компонентов | ECSTestsFixture + EntityManager.CreateEntity + AddComponentData |
| TST-02 | EditMode тесты для ThrustSystem | Тест тяги, торможения, maxSpeed clamp |
| TST-03 | EditMode тесты для MoveSystem | Тест перемещения, тороидального wrapping |
| TST-04 | EditMode тесты для RotateSystem | Тест поворота по/против часовой, нулевого direction |
| TST-05 | EditMode тесты для GunSystem | Тест стрельбы, перезарядки, лимита |
| TST-06 | EditMode тесты для LaserSystem | Тест зарядов, cooldown, инкрементальной перезарядки |
| TST-07 | EditMode тесты для ShootToSystem | Тест упреждения с mock ShipPosition singleton |
| TST-08 | EditMode тесты для MoveToSystem | Тест движения к цели с mock ShipPosition |
| TST-09 | EditMode тесты для CollisionHandler | Тест с ручными CollisionEventData |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Документация и комментарии на русском
- **Фигурные скобки:** Обязательны в if/else/for/while даже для однострочников
- **Без однострочников:** Никогда не использовать однострочные конструкции
- **C# 9.0:** LangVersion 9.0 в проекте
- **Unsafe код запрещён:** `AllowUnsafeBlocks=False`
- **Namespace:** `SelStrom.Asteroids` (основной), ECS-код должен следовать конвенции проекта
- **GSD Workflow:** Все изменения через GSD-команды

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.entities | 1.4.5 | ECS-фреймворк (IComponentData, ISystem, SystemAPI, World) | Официальный DOTS-пакет Unity, graduated из experimental в Unity 6 |
| com.unity.burst | 1.8.28 | Burst-компилятор для ISystem | Уже установлен как транзитивная зависимость |
| com.unity.mathematics | 1.3.3 | float2, float3, quaternion, math для Burst-совместимого кода | Уже установлен, требуется для Burst-совместимых вычислений |
| com.unity.collections | 2.6.5 | NativeArray, NativeList для ECS | Уже установлен как транзитивная зависимость |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| com.unity.test-framework | 1.6.0 | NUnit для EditMode-тестов | Уже установлен, используется для TST-01..TST-09 |
| Unity.Entities.Tests (internal) | -- | ECSTestsFixture base class | В тестах для World lifecycle management |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ISystem (unmanaged) | SystemBase (managed) | SystemBase проще, но не совместим с Burst; ISystem -- современный стандарт (D-01) |
| SystemAPI.Query | IJobEntity / IJobChunk | Jobs нужны для multi-threading, Query проще для main-thread single-entity-at-a-time; для Phase 4 Query достаточен |
| ECSTestsFixture | Ручное создание World | Fixture автоматизирует Setup/TearDown, проверку consistency; ручное -- подвержено утечкам |

**Installation:**
```
Добавить в Packages/manifest.json:
"com.unity.entities": "1.4.5"
```

**Version verification:** com.unity.entities 1.4.5 -- последний released (verified via needle-mirror/com.unity.entities package.json). Требует Unity 2022.3+, burst 1.8.27+ (у нас 1.8.28), collections 2.6.5+ (у нас 2.6.5), mathematics 1.3.2+ (у нас 1.3.3). Полная совместимость.

## Architecture Patterns

### Recommended Project Structure
```
Assets/Scripts/ECS/
├── Components/
│   ├── MoveData.cs           # IComponentData: float2 Position, float Speed, float2 Direction
│   ├── ThrustData.cs         # IComponentData: float UnitsPerSecond, float MaxSpeed, bool IsActive
│   ├── RotateData.cs         # IComponentData: float TargetDirection, float2 Rotation
│   ├── GunData.cs            # IComponentData: int MaxShoots, float ReloadDurationSec, int CurrentShoots, float ReloadRemaining, bool Shooting, float2 Direction, float2 ShootPosition
│   ├── LaserData.cs          # IComponentData: аналогично GunData + UpdateDurationSec
│   ├── ShootToData.cs        # IComponentData: float Every, float ReadyRemaining (Ship через singleton)
│   ├── MoveToData.cs         # IComponentData: float Every, float ReadyRemaining (Ship через singleton)
│   ├── LifeTimeData.cs       # IComponentData: float TimeRemaining
│   ├── AgeData.cs            # IComponentData: int Age
│   ├── GameAreaData.cs       # IComponentData (singleton): float2 Size
│   ├── ShipPositionData.cs   # IComponentData (singleton): float2 Position, float Speed, float2 Direction
│   └── Tags/
│       ├── ShipTag.cs        # IComponentData (tag, zero-size)
│       ├── AsteroidTag.cs
│       ├── BulletTag.cs
│       ├── UfoTag.cs
│       ├── UfoBigTag.cs
│       ├── PlayerBulletTag.cs
│       └── EnemyBulletTag.cs
├── Systems/
│   ├── RotateSystem.cs       # ISystem + [BurstCompile]
│   ├── ThrustSystem.cs       # ISystem + [BurstCompile]
│   ├── MoveSystem.cs         # ISystem + [BurstCompile]
│   ├── LifeTimeSystem.cs     # ISystem (Burst-safe, но простой)
│   ├── GunSystem.cs          # ISystem (без Burst -- OnShooting callback)
│   ├── LaserSystem.cs        # ISystem (без Burst -- OnShooting callback)
│   ├── ShootToSystem.cs      # ISystem (без Burst -- singleton read)
│   ├── MoveToSystem.cs       # ISystem (без Burst -- singleton read)
│   └── CollisionHandlerSystem.cs  # ISystem (mock collision data)
├── EntityFactory.cs           # Создание entities с правильными archetypes
└── AsteroidsECS.asmdef       # Assembly definition

Assets/Tests/EditMode/ECS/
├── ComponentTests.cs          # TST-01: тесты создания/дефолтов компонентов
├── ThrustSystemTests.cs       # TST-02
├── MoveSystemTests.cs         # TST-03
├── RotateSystemTests.cs       # TST-04
├── GunSystemTests.cs          # TST-05
├── LaserSystemTests.cs        # TST-06
├── ShootToSystemTests.cs      # TST-07
├── MoveToSystemTests.cs       # TST-08
├── CollisionHandlerTests.cs   # TST-09
├── EntityFactoryTests.cs      # ECS-03
└── EcsEditModeTests.asmdef    # Отдельный asmdef для ECS-тестов
```

### Pattern 1: IComponentData как unmanaged struct
**What:** Все компоненты -- `struct : IComponentData` с plain полями (`float`, `float2`, `int`, `bool`). Нет managed types (string, class, Action, ObservableValue).
**When to use:** Всегда для ECS-компонентов.
**Example:**
```csharp
// Source: Unity Entities 1.4.x API
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct MoveData : IComponentData
    {
        public float2 Position;
        public float Speed;
        public float2 Direction;
    }

    // Tag -- zero-size component
    public struct ShipTag : IComponentData { }
}
```

### Pattern 2: ISystem с BurstCompile
**What:** Unmanaged-система с `[BurstCompile]` на методе `OnUpdate`. Итерация через `SystemAPI.Query<RefRW<T>>`.
**When to use:** Для чистых систем без managed-зависимостей (Move, Rotate, Thrust).
**Example:**
```csharp
// Source: Unity Entities 1.x official docs -- ISystem overview
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [BurstCompile]
    public partial struct EcsMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameArea = SystemAPI.GetSingleton<GameAreaData>().Size;

            foreach (var move in SystemAPI.Query<RefRW<MoveData>>())
            {
                var position = move.ValueRO.Position
                    + move.ValueRO.Direction * (move.ValueRO.Speed * SystemAPI.Time.DeltaTime);

                PlaceWithinGameArea(ref position.x, gameArea.x);
                PlaceWithinGameArea(ref position.y, gameArea.y);

                move.ValueRW.Position = position;
            }
        }

        // 1:1 портирование оригинальной формулы (включая известный баг)
        private static void PlaceWithinGameArea(ref float position, float side)
        {
            if (position > side / 2)
            {
                position = -side + position;
            }

            if (position < -side / 2)
            {
                position = side - position;
            }
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameAreaData>();
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
```

### Pattern 3: ISystem без Burst (managed access)
**What:** ISystem без `[BurstCompile]` для систем, которым нужен доступ к managed-данным (callback events) или singleton-компоненты.
**When to use:** GunSystem, LaserSystem, ShootToSystem, MoveToSystem, CollisionHandler.
**Example:**
```csharp
// Source: Unity Entities docs -- SystemAPI singleton
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public partial struct EcsShootToSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var shipPos = SystemAPI.GetSingleton<ShipPositionData>();

            foreach (var (move, gun, shootTo)
                in SystemAPI.Query<RefRO<MoveData>, RefRW<GunData>, RefRW<ShootToData>>())
            {
                if (gun.ValueRO.CurrentShoots <= 0)
                {
                    continue;
                }

                // Предиктивное прицеливание: 1:1 из оригинала
                var time = math.length(shipPos.Position - move.ValueRO.Position)
                           / (20f - shipPos.Speed);

                var pendingPosition = shipPos.Position
                    + (shipPos.Direction * shipPos.Speed) * time;
                var direction = math.normalizesafe(pendingPosition - move.ValueRO.Position);

                gun.ValueRW.Shooting = true;
                gun.ValueRW.Direction = direction;
                gun.ValueRW.ShootPosition = move.ValueRO.Position;
            }
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShipPositionData>();
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
```

### Pattern 4: Singleton Component для глобальных данных
**What:** IComponentData на единственном entity для GameArea и ShipPosition.
**When to use:** Данные, нужные нескольким системам (GameArea для MoveSystem, ShipPosition для AI-систем).
**Example:**
```csharp
public struct GameAreaData : IComponentData
{
    public float2 Size;
}

public struct ShipPositionData : IComponentData
{
    public float2 Position;
    public float Speed;
    public float2 Direction;
}

// Создание singleton entity:
var entity = entityManager.CreateEntity();
entityManager.AddComponentData(entity, new GameAreaData { Size = new float2(width, height) });
```

### Pattern 5: System Ordering через UpdateBefore/UpdateAfter
**What:** Порядок обновления систем через атрибуты `[UpdateBefore(typeof(...))]` / `[UpdateAfter(typeof(...))]`.
**When to use:** Сохранение оригинального порядка: Rotate -> Thrust -> Move -> LifeTime -> Gun -> Laser -> ShootTo -> MoveTo.

**Рекомендация (Claude's Discretion):** Использовать `[UpdateBefore]`/`[UpdateAfter]` напрямую между системами -- проще и нагляднее, чем создавать кастомный SystemGroup для 8 систем. Все системы по умолчанию обновляются в SimulationSystemGroup.

```csharp
// Порядок: Rotate -> Thrust -> Move -> LifeTime -> Gun -> Laser -> ShootTo -> MoveTo
[UpdateBefore(typeof(EcsThrustSystem))]
public partial struct EcsRotateSystem : ISystem { ... }

[UpdateBefore(typeof(EcsMoveSystem))]
[UpdateAfter(typeof(EcsRotateSystem))]
public partial struct EcsThrustSystem : ISystem { ... }

[UpdateBefore(typeof(EcsLifeTimeSystem))]
[UpdateAfter(typeof(EcsThrustSystem))]
public partial struct EcsMoveSystem : ISystem { ... }

// ... и так далее по цепочке
```

### Pattern 6: ECS Test Fixture
**What:** Наследование от `ECSTestsFixture` для автоматического управления World lifecycle.
**When to use:** Все EditMode тесты для ECS.

**Рекомендация (Claude's Discretion):** Создать `AsteroidsEcsTestFixture` -- обёртку над `ECSTestsFixture` с helper-методами: CreateShipEntity(), CreateAsteroidEntity(), SetupGameArea(), UpdateSystem<T>().

```csharp
using Unity.Entities;
using Unity.Entities.Tests;
using NUnit.Framework;
using Unity.Mathematics;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class AsteroidsEcsTestFixture : ECSTestsFixture
    {
        protected Entity CreateShipEntity(float2 position = default, float speed = 0f)
        {
            var entity = m_Manager.CreateEntity(
                typeof(ShipTag),
                typeof(MoveData),
                typeof(RotateData),
                typeof(ThrustData),
                typeof(GunData),
                typeof(LaserData)
            );
            m_Manager.SetComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = new float2(1, 0)
            });
            return entity;
        }

        protected Entity CreateGameAreaSingleton(float2 size)
        {
            var entity = m_Manager.CreateEntity(typeof(GameAreaData));
            m_Manager.SetComponentData(entity, new GameAreaData { Size = size });
            return entity;
        }

        protected Entity CreateShipPositionSingleton(float2 position, float speed, float2 direction)
        {
            var entity = m_Manager.CreateEntity(typeof(ShipPositionData));
            m_Manager.SetComponentData(entity, new ShipPositionData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            return entity;
        }

        protected void UpdateSystem<T>() where T : unmanaged, ISystem
        {
            var handle = World.GetExistingSystem<T>();
            handle.Update(World.Unmanaged);
        }

        protected SystemHandle CreateAndGetSystem<T>() where T : unmanaged, ISystem
        {
            return World.CreateSystem<T>();
        }
    }
}
```

### Anti-Patterns to Avoid
- **Managed types в IComponentData:** Нельзя использовать string, class, Action, ObservableValue. Только blittable-типы (float, int, bool, float2, float3, quaternion). Action/OnShooting callback-и заменяются на `bool Shooting` флаг -- Bridge Layer (Phase 5) читает флаг и вызывает managed callback.
- **BurstCompile на системах с managed access:** ShootToSystem/MoveToSystem используют singleton -- в текущей реализации это допустимо с Burst, но GunSystem/LaserSystem имеют логику callback-ов, которая потребует managed доступа в Phase 5. Не ставить BurstCompile заранее.
- **Quaternion в Burst-коде:** Unity.Mathematics использует `quaternion` (lowercase), не `UnityEngine.Quaternion`. Для `Quaternion.Euler` замена: `quaternion.EulerZXY(0, 0, radians)`.
- **Vector2/Vector3 в IComponentData:** Использовать `float2`/`float3` из Unity.Mathematics вместо `UnityEngine.Vector2`/`Vector3`.
- **Тесты без Setup/TearDown World:** Утечка Entity World между тестами. Всегда наследовать от ECSTestsFixture.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| World lifecycle в тестах | Ручное создание/уничтожение World + EntityManager | `ECSTestsFixture` (из Unity.Entities.Tests) | Автоматический cleanup, consistency check, job completion |
| Entity archetype queries | Ручная итерация по EntityManager | `SystemAPI.Query<RefRW<T>>` | Source-generated, cached EntityQuery, auto-dependency management |
| Component data access | `EntityManager.GetComponentData` в системах | `SystemAPI.GetSingleton<T>`, `RefRW<T>.ValueRW` | SystemAPI -- source-generated, better performance, auto-dependency |
| Math для Burst | `UnityEngine.Mathf`, `Vector2`, `Quaternion` | `Unity.Mathematics.math`, `float2`, `quaternion` | Burst-совместимые типы, SIMD-оптимизация |
| Ordering attributes | Ручная регистрация порядка | `[UpdateBefore]`/`[UpdateAfter]` атрибуты | Декларативно, проверяется компилятором |

**Key insight:** Unity.Entities предоставляет source-generators, которые заменяют ручной boilerplate. `SystemAPI.Query` генерирует оптимальный код итерации. `ECSTestsFixture` решает сложную проблему lifecycle management. Не пытаться воспроизводить эту инфраструктуру вручную.

## Common Pitfalls

### Pitfall 1: Partial struct keyword для ISystem
**What goes wrong:** Компилятор выдает ошибку, если ISystem struct не объявлен как `partial`.
**Why it happens:** Unity source generators генерируют дополнительный код для ISystem, требуя partial declaration.
**How to avoid:** Всегда объявлять `public partial struct MySystem : ISystem`.
**Warning signs:** Ошибка компиляции "ISystem types must be declared partial".

### Pitfall 2: Unity.Mathematics vs UnityEngine типы в Burst-коде
**What goes wrong:** Burst не может компилировать код с `UnityEngine.Vector2`, `Quaternion`, `Mathf`.
**Why it happens:** UnityEngine типы -- managed/не-blittable, Burst работает только с unmanaged типами.
**How to avoid:** Использовать `float2`, `float3`, `quaternion`, `math.min`, `math.max`, `math.normalizesafe`.
**Warning signs:** Burst compilation error mentioning managed types.

### Pitfall 3: Quaternion.Euler -> quaternion.EulerZXY (радианы vs градусы)
**What goes wrong:** `quaternion.EulerZXY` принимает **радианы**, а `Quaternion.Euler` принимает **градусы**.
**Why it happens:** Unity.Mathematics следует математической конвенции (радианы), UnityEngine -- gamedev-конвенции (градусы).
**How to avoid:** Конвертировать через `math.radians()`: `quaternion.EulerZXY(0, 0, math.radians(angle))`.
**Warning signs:** Поворот слишком быстрый/медленный в ~57 раз (180/pi).

### Pitfall 4: RequireForUpdate для singleton-зависимостей
**What goes wrong:** Система выполняет OnUpdate, когда singleton entity ещё не создан. `GetSingleton<T>()` бросает exception.
**Why it happens:** По умолчанию системы обновляются каждый кадр, даже если нет matching entities.
**How to avoid:** В OnCreate вызвать `state.RequireForUpdate<T>()` для каждого singleton-компонента.
**Warning signs:** InvalidOperationException "No singleton of type X found".

### Pitfall 5: ECSTestsFixture в отдельном internal assembly
**What goes wrong:** `Unity.Entities.Tests.ECSTestsFixture` не найден -- class missing.
**Why it happens:** ECSTestsFixture доступен через `testables` в manifest.json. Нужно добавить `"com.unity.entities"` в секцию `testables`.
**How to avoid:** Добавить в Packages/manifest.json: `"testables": ["com.unity.entities", "com.unity.test-framework"]`. В asmdef тестов добавить reference на `Unity.Entities.Tests`.
**Warning signs:** Compilation error "The type or namespace name 'ECSTestsFixture' could not be found".

### Pitfall 6: bool в IComponentData
**What goes wrong:** `bool` в unmanaged struct занимает 1 байт, но alignment может вызвать неожиданное поведение.
**Why it happens:** C# bool -- 1 byte, но в struct alignment может добавить padding.
**How to avoid:** Использовать `bool` -- это допустимо в IComponentData (blittable). Просто помнить, что `default(bool) = false`.
**Warning signs:** Нет -- это скорее предупреждение для code review.

### Pitfall 7: Division by zero в ShootToSystem/MoveToSystem (1:1 bug)
**What goes wrong:** Когда ship.Speed == 20 (ShootTo) или ufo.Speed == ship.Speed (MoveTo), деление на ноль.
**Why it happens:** Оригинальный код содержит этот баг. Мы портируем 1:1.
**How to avoid:** НЕ исправлять -- это deferred item (QUAL-01). Тесты должны обходить деление на ноль.
**Warning signs:** NaN в позициях, бесконечный direction.

### Pitfall 8: SystemAPI не работает в static методах
**What goes wrong:** `SystemAPI.Query` и `SystemAPI.GetSingleton` вызывают ошибку в static context.
**Why it happens:** Source generators работают только в instance-методах ISystem.
**How to avoid:** Вся логика с SystemAPI -- в OnUpdate/OnCreate. Helper-методы (PlaceWithinGameArea) -- как обычные static pure functions без SystemAPI.
**Warning signs:** Compilation error от source generator.

## Code Examples

### Component Definition: ThrustData
```csharp
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct ThrustData : IComponentData
    {
        public const float MinSpeed = 0.0f;

        public float UnitsPerSecond;
        public float MaxSpeed;
        public bool IsActive;
    }
}
```

### Component Definition: RotateData
```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct RotateData : IComponentData
    {
        public const float DegreePerSecond = 90f;

        public float TargetDirection;
        public float2 Rotation; // Единичный вектор направления, default = (1, 0)
    }
}
```

### Burst-compiled ThrustSystem
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [BurstCompile]
    [UpdateBefore(typeof(EcsMoveSystem))]
    [UpdateAfter(typeof(EcsRotateSystem))]
    public partial struct EcsThrustSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (thrust, move, rotate)
                in SystemAPI.Query<RefRO<ThrustData>, RefRW<MoveData>, RefRO<RotateData>>())
            {
                if (thrust.ValueRO.IsActive)
                {
                    var acceleration = thrust.ValueRO.UnitsPerSecond * deltaTime;
                    var velocity = move.ValueRO.Direction * move.ValueRO.Speed
                        + rotate.ValueRO.Rotation * acceleration;

                    move.ValueRW.Direction = math.normalizesafe(velocity);
                    move.ValueRW.Speed = math.min(math.length(velocity), thrust.ValueRO.MaxSpeed);
                }
                else
                {
                    move.ValueRW.Speed = math.max(
                        move.ValueRO.Speed - thrust.ValueRO.UnitsPerSecond / 2f * deltaTime,
                        ThrustData.MinSpeed);
                }
            }
        }

        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
    }
}
```

### RotateSystem с quaternion вместо Quaternion
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [BurstCompile]
    [UpdateBefore(typeof(EcsThrustSystem))]
    public partial struct EcsRotateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var rotate in SystemAPI.Query<RefRW<RotateData>>())
            {
                if (rotate.ValueRO.TargetDirection == 0)
                {
                    continue;
                }

                var angle = math.radians(RotateData.DegreePerSecond * deltaTime * rotate.ValueRO.TargetDirection);
                var cos = math.cos(angle);
                var sin = math.sin(angle);
                var current = rotate.ValueRO.Rotation;

                // 2D rotation: (x*cos - y*sin, x*sin + y*cos)
                rotate.ValueRW.Rotation = new float2(
                    current.x * cos - current.y * sin,
                    current.x * sin + current.y * cos
                );
            }
        }

        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
    }
}
```

### EntityFactory
```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public static class EntityFactory
    {
        public static Entity CreateShip(EntityManager em,
            float2 position, float moveSpeed, float thrustAcceleration,
            float thrustMaxSpeed, int gunMaxShoots, float gunReloadSec,
            int laserMaxShoots, float laserReloadSec)
        {
            var entity = em.CreateEntity(
                typeof(ShipTag),
                typeof(MoveData),
                typeof(RotateData),
                typeof(ThrustData),
                typeof(GunData),
                typeof(LaserData)
            );

            em.SetComponentData(entity, new MoveData
            {
                Position = position,
                Speed = moveSpeed,
                Direction = new float2(1, 0)
            });
            em.SetComponentData(entity, new RotateData
            {
                Rotation = new float2(1, 0)
            });
            em.SetComponentData(entity, new ThrustData
            {
                UnitsPerSecond = thrustAcceleration,
                MaxSpeed = thrustMaxSpeed
            });
            em.SetComponentData(entity, new GunData
            {
                MaxShoots = gunMaxShoots,
                ReloadDurationSec = gunReloadSec,
                CurrentShoots = gunMaxShoots,
                ReloadRemaining = gunReloadSec
            });
            em.SetComponentData(entity, new LaserData
            {
                MaxShoots = laserMaxShoots,
                UpdateDurationSec = laserReloadSec,
                CurrentShoots = laserMaxShoots,
                ReloadRemaining = laserReloadSec
            });

            return entity;
        }

        public static Entity CreateAsteroid(EntityManager em,
            float2 position, float speed, float2 direction, int age)
        {
            var entity = em.CreateEntity(
                typeof(AsteroidTag),
                typeof(MoveData),
                typeof(AgeData)
            );

            em.SetComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            em.SetComponentData(entity, new AgeData { Age = age });

            return entity;
        }

        // Аналогично: CreateBullet, CreateUfoBig, CreateUfo
    }
}
```

### EditMode Test Example
```csharp
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    [TestFixture]
    public class MoveSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _moveSystem;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _moveSystem = CreateAndGetSystem<EcsMoveSystem>();
            CreateGameAreaSingleton(new float2(20f, 15f));
        }

        [Test]
        public void Update_EntityMovesInDirection_PositionUpdated()
        {
            // Arrange
            var entity = m_Manager.CreateEntity(typeof(MoveData));
            m_Manager.SetComponentData(entity, new MoveData
            {
                Position = float2.zero,
                Speed = 10f,
                Direction = new float2(1, 0)
            });

            // Act -- один tick с deltaTime = 0.1
            // Примечание: для контроля deltaTime в тестах используется World.PushTime
            World.PushTime(new TimeData(0.1, 0.1));
            _moveSystem.Update(World.Unmanaged);
            World.PopTime();

            // Assert
            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(1f, move.Position.x, 0.001f);
            Assert.AreEqual(0f, move.Position.y, 0.001f);
        }

        [Test]
        public void Update_PositionBeyondRightEdge_WrapsToLeft()
        {
            // GameArea = 20x15, half = 10x7.5
            var entity = m_Manager.CreateEntity(typeof(MoveData));
            m_Manager.SetComponentData(entity, new MoveData
            {
                Position = new float2(9.5f, 0f),
                Speed = 10f,
                Direction = new float2(1, 0)
            });

            World.PushTime(new TimeData(0.1, 0.1));
            _moveSystem.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            // 9.5 + 1.0 = 10.5 > 10 (half), wrap: -20 + 10.5 = -9.5
            Assert.AreEqual(-9.5f, move.Position.x, 0.001f);
        }
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| SystemBase (managed class) | ISystem (unmanaged struct) | Entities 1.0 (2023) | Burst-совместимость, лучшая производительность |
| Entities.ForEach lambda | SystemAPI.Query foreach | Entities 1.0 (2023) | Entities.ForEach deprecated, Query -- замена |
| [BurstCompile] на struct | [BurstCompile] на отдельных методах | Entities 1.x | Гранулярный контроль Burst per-method |
| ComponentSystemGroup + manual order | [UpdateBefore]/[UpdateAfter] | Entities 1.0 | Декларативный ordering |
| EntityQuery manual creation | Source-generated queries через SystemAPI | Entities 1.0 | Меньше boilerplate, auto-caching |

**Deprecated/outdated:**
- `Entities.ForEach`: Deprecated в Entities 1.0+. Использовать `SystemAPI.Query` или `IJobEntity`.
- `SystemBase`: Менее производителен, чем ISystem. Всё ещё поддерживается, но ISystem -- рекомендуемый путь.
- `[GenerateAuthoringComponent]`: Удален. Использовать `Baker<T>` для authoring.

## Open Questions

1. **World.PushTime / PopTime для контроля deltaTime в тестах**
   - What we know: ECSTestsFixture предоставляет World, но контроль deltaTime в тестах не очевиден
   - What's unclear: Точный API для PushTime/PopTime -- может зависеть от версии Entities
   - Recommendation: В первом тесте проверить наличие PushTime. Альтернатива: вручную устанавливать deltaTime через `state.WorldUnmanaged.Time = new TimeData(elapsed, delta)`

2. **ECSTestsFixture internals -- доступ из внешнего test assembly**
   - What we know: Нужно добавить `"com.unity.entities"` в `testables` в manifest.json и reference на `Unity.Entities.Tests` в asmdef
   - What's unclear: Возможные ограничения InternalsVisibleTo
   - Recommendation: Если ECSTestsFixture недоступен, создать собственный fixture с `new World("Test")` в SetUp и `World.Dispose()` в TearDown

3. **GunSystem/LaserSystem -- callback OnShooting в ECS**
   - What we know: В Phase 4 callback-и не нужны (Bridge Layer в Phase 5). В ECS используется только `bool Shooting` флаг.
   - What's unclear: Как именно Bridge Layer будет читать Shooting и вызывать managed Action
   - Recommendation: В Phase 4 GunData/LaserData имеют `bool Shooting` + `float2 Direction` + `float2 ShootPosition`. Тесты проверяют логику reload/shoot через эти поля.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | NUnit (com.unity.test-framework 1.6.0) |
| Config file | Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef (Wave 0) |
| Quick run command | `Unity -runTests -testPlatform EditMode -testFilter "SelStrom.Asteroids.Tests.EditMode.ECS"` |
| Full suite command | `Unity -runTests -testPlatform EditMode` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TST-01 | Компоненты создаются с дефолтами | unit | `Unity -runTests -testFilter "ComponentTests"` | Wave 0 |
| TST-02 | ThrustSystem: тяга, торможение | unit | `Unity -runTests -testFilter "ThrustSystemTests"` | Wave 0 |
| TST-03 | MoveSystem: перемещение, wrapping | unit | `Unity -runTests -testFilter "MoveSystemTests"` | Wave 0 |
| TST-04 | RotateSystem: поворот | unit | `Unity -runTests -testFilter "RotateSystemTests"` | Wave 0 |
| TST-05 | GunSystem: стрельба, перезарядка | unit | `Unity -runTests -testFilter "GunSystemTests"` | Wave 0 |
| TST-06 | LaserSystem: заряды, cooldown | unit | `Unity -runTests -testFilter "LaserSystemTests"` | Wave 0 |
| TST-07 | ShootToSystem: упреждение | unit | `Unity -runTests -testFilter "ShootToSystemTests"` | Wave 0 |
| TST-08 | MoveToSystem: движение к цели | unit | `Unity -runTests -testFilter "MoveToSystemTests"` | Wave 0 |
| TST-09 | CollisionHandler: пары столкновений | unit | `Unity -runTests -testFilter "CollisionHandlerTests"` | Wave 0 |
| ECS-02 | Все IComponentData определены | unit | `Unity -runTests -testFilter "ComponentTests"` | Wave 0 |
| ECS-03 | EntityFactory создает корректные entities | unit | `Unity -runTests -testFilter "EntityFactoryTests"` | Wave 0 |
| ECS-01 | Пакеты установлены, проект компилируется | compilation | `Unity -buildTarget StandaloneOSX -quit` | N/A |

### Sampling Rate
- **Per task commit:** Запуск тестов через Unity MCP или CLI для затронутых систем
- **Per wave merge:** Полный EditMode suite
- **Phase gate:** Все EditMode-тесты зеленые перед `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef` -- asmdef для ECS-тестов с reference на AsteroidsECS и Unity.Entities.Tests
- [ ] `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` -- базовый fixture с helper-методами
- [ ] `Assets/Scripts/ECS/AsteroidsECS.asmdef` -- asmdef для ECS production-кода
- [ ] `Packages/manifest.json` -- добавление `com.unity.entities: "1.4.5"` и `testables: ["com.unity.entities"]`

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity Editor | All | Y | 6000.3.12f1 | -- |
| com.unity.burst | ECS-04/05/06 (Burst) | Y | 1.8.28 | -- |
| com.unity.collections | ECS components | Y | 2.6.5 | -- |
| com.unity.mathematics | ECS components, systems | Y | 1.3.3 | -- |
| com.unity.entities | ECS-01..ECS-11 | N (to install) | 1.4.5 target | -- |
| com.unity.test-framework | TST-01..TST-09 | Y | 1.6.0 | -- |

**Missing dependencies with no fallback:**
- `com.unity.entities` 1.4.5 -- must be installed via Packages/manifest.json (ECS-01)

**Missing dependencies with fallback:**
- None

## Sources

### Primary (HIGH confidence)
- [Unity Entities 1.4.5 package.json](https://github.com/needle-mirror/com.unity.entities/blob/master/package.json) -- версия, зависимости, совместимость
- [ISystem overview (Entities 1.0)](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-isystem.html) -- API ISystem, BurstCompile pattern
- [SystemAPI.Query (Entities 1.0)](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-systemapi-query.html) -- итерация компонентов
- [Singleton components (Entities 1.0)](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/components-singleton.html) -- GetSingleton/SetSingleton
- [System groups / ordering (Entities 1.0)](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-update-order.html) -- UpdateBefore/UpdateAfter
- [ECSTestsFixture source](https://github.com/needle-mirror/com.unity.entities/blob/master/Unity.Entities.Tests/ECSTestsFixture.cs) -- test fixture API

### Secondary (MEDIUM confidence)
- [ECS Unit Testing Best Practices](https://gamedev.center/unit-testing-made-easy-unity-ecs-best-practices/) -- тестовые паттерны, CustomEcsTestsFixture
- [ECS Testing DeepWiki](https://deepwiki.com/needle-mirror/com.unity.entities/6.3-testing-with-ecs) -- обзор тестовой инфраструктуры
- [ECS Development Status March 2025](https://discussions.unity.com/t/ecs-development-status-milestones-march-2025/1615810) -- текущий статус Entities

### Tertiary (LOW confidence)
- World.PushTime/PopTime API для контроля deltaTime в тестах -- нужна практическая проверка при первом запуске тестов

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- com.unity.entities 1.4.5 verified через package.json, зависимости уже установлены
- Architecture: HIGH -- ISystem + SystemAPI.Query + BurstCompile -- стандартный паттерн Unity DOTS, документирован в official docs
- Pitfalls: HIGH -- quaternion radians, partial struct, ECSTestsFixture testables -- все из official docs и community experience
- Testing: MEDIUM -- ECSTestsFixture доступен, но PushTime/PopTime API для deltaTime нужна проверка

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (stable API, Entities 1.4.x -- released version)
