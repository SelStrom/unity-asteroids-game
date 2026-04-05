# Phase 10: ECS Core -- данные и логика ракет - Research

**Researched:** 2026-04-05
**Domain:** Unity ECS (Entities 1.x) -- компоненты, системы, юнит-тесты для самонаводящейся ракеты
**Confidence:** HIGH

## Summary

Фаза добавляет ECS-entity ракеты с логикой наведения (seek с ограниченным turn rate), самоуничтожения по таймеру, и инкрементальной перезарядкой боезапаса на Ship entity. Визуал исключён (Phase 12).

Проект уже имеет зрелую ECS-инфраструктуру: 8 IComponentData-структур, 10+ систем (ISystem и SystemBase), EntityFactory для создания entity, развитый тестовый фреймворк (AsteroidsEcsTestFixture с хелперами). Все новые компоненты и системы должны точно следовать существующим паттернам.

**Primary recommendation:** Создать 3 новых компонента (RocketTag, RocketTargetData, RocketAmmoData), 2 новые системы (EcsRocketGuidanceSystem, EcsRocketAmmoSystem), расширить EntityFactory двумя методами (CreateRocket, расширение CreateShip). Все покрыть EditMode-тестами по паттерну EcsGunSystemTests/EcsLaserSystemTests.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Простой seek с ограниченным turn rate -- каждый кадр Direction ракеты поворачивается к позиции цели на фиксированную дельту (turnRate * deltaTime), создавая дугообразную траекторию
- **D-02:** Turn rate задаётся через компонент (градусы/сек), аналогично RotateData.TargetDirection -- но вращение автоматическое к цели, а не по вводу игрока
- **D-03:** Ракета летит прямо в текущем Direction с постоянной скоростью (MoveData), наведение влияет только на Direction
- **D-04:** Ближайший враг по евклидову расстоянию (без учёта тороидального wrap) -- итерация по всем entity с AsteroidTag, UfoBigTag, UfoTag
- **D-05:** При уничтожении цели (DeadTag на target) -- немедленный пересчёт на следующего ближайшего врага
- **D-06:** Если врагов нет -- ракета летит прямо в текущем Direction
- **D-07:** Переиспользовать существующие: MoveData (позиция, скорость, направление), LifeTimeData (время жизни)
- **D-08:** Новый тег: RocketTag (IComponentData маркер, аналогично BulletTag)
- **D-09:** Новый компонент: RocketTargetData -- хранит Entity цели (Entity.Null если цели нет)
- **D-10:** Новый компонент: RocketAmmoData -- на Ship entity: текущий боезапас, макс. боезапас, таймер перезарядки, длительность перезарядки
- **D-11:** Инкрементальная перезарядка (как LaserSystem) -- одна ракета за период перезарядки
- **D-12:** RocketAmmoData живёт на Ship entity (аналогично GunData/LaserData)
- **D-13:** Перезарядка работает когда CurrentAmmo < MaxAmmo: таймер уменьшается, при достижении 0 -- CurrentAmmo += 1, таймер сбрасывается

### Claude's Discretion
- Конкретная формула поворота Direction к цели (math.atan2 или cross-product подход -- оба Burst-совместимы)
- Порядок новой системы (EcsRocketGuidanceSystem) в update chain
- Нужна ли отдельная система для перезарядки боезапаса или встроить в существующую EcsGunSystem

### Deferred Ideas (OUT OF SCOPE)
Нет -- обсуждение не выходило за рамки фазы.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ROCK-02 | Ракета автоматически наводится на ближайшего врага с ограниченным turn rate | EcsRocketGuidanceSystem: итерация по врагам, seek-алгоритм с cross-product rotation, MoveData.Direction обновление |
| ROCK-03 | Ракета переключает цель при уничтожении текущей | EcsRocketGuidanceSystem: проверка EntityManager.Exists + DeadTag на RocketTargetData.Target, пересчёт ближайшего |
| ROCK-04 | Ракета имеет ограниченное время жизни (LifeTimeData) | Переиспользование существующих EcsLifeTimeSystem + EcsDeadByLifeTimeSystem -- ноль новой логики |
| ROCK-05 | Боезапас ракет ограничен конфигурируемым количеством | RocketAmmoData.CurrentAmmo / MaxAmmo на Ship entity, уменьшение при запуске |
| ROCK-06 | Ракеты респавнятся по таймеру (инкрементальная перезарядка) | EcsRocketAmmoSystem: паттерн LaserSystem (CurrentAmmo += 1 за период) |
| TEST-01 | Юнит-тесты на ECS-компоненты и системы ракет (EditMode) | AsteroidsEcsTestFixture + паттерн PushTime/PopTime, хелперы для создания entity |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Вся документация и комментарии на русском
- **Фигурные скобки:** Всегда использовать `{}` даже для однострочных `if/for/while`
- **Namespace:** `SelStrom.Asteroids.ECS` для компонентов и систем
- **Тесты:** При исправлении бага -- обязателен регрессионный тест
- **C# 9.0**, .NET Standard 2.1, unsafe-код запрещён
- **Именование:** PascalCase для классов/методов/свойств, `_camelCase` для приватных полей
- **ECS паттерн:** IComponentData struct для данных, partial struct : ISystem для систем
- **float2** вместо Vector2 (Unity.Mathematics для Burst)
- **Без switch expressions и тернарных операторов**

## Standard Stack

### Core (уже установлено)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.entities | 1.x | ECS framework | Основа архитектуры проекта [VERIFIED: csproj/asmdef] |
| com.unity.mathematics | 1.2.6 | float2, math.* | Burst-совместимая математика [VERIFIED: packages-lock.json] |
| com.unity.burst | 1.8.19 | Burst compiler | Высокопроизводительные системы [VERIFIED: packages-lock.json] |
| com.unity.collections | 1.2.4 | NativeArray и др. | Allocator.Temp для ECB [VERIFIED: packages-lock.json] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| com.unity.test-framework | 1.1.33 | NUnit для Unity | EditMode тесты ECS-систем [VERIFIED: manifest.json] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| cross-product rotation | math.atan2 rotation | cross-product проще (один вызов math.cross вместо 2x atan2), обе Burst-совместимы |
| Отдельная EcsRocketAmmoSystem | Встроить в EcsGunSystem | Отдельная система -- чище separation of concerns, EcsGunSystem не знает про ракеты |

## Architecture Patterns

### Новые файлы
```
Assets/Scripts/ECS/
├── Components/
│   ├── Tags/
│   │   └── RocketTag.cs         # IComponentData маркер
│   ├── RocketTargetData.cs      # Entity цели
│   └── RocketAmmoData.cs        # Боезапас на Ship
├── Systems/
│   ├── EcsRocketGuidanceSystem.cs  # Наведение + выбор цели
│   └── EcsRocketAmmoSystem.cs      # Перезарядка боезапаса
└── EntityFactory.cs                # +CreateRocket(), расширение CreateShip()

Assets/Tests/EditMode/ECS/
├── RocketGuidanceSystemTests.cs
├── RocketAmmoSystemTests.cs
└── (обновление AsteroidsEcsTestFixture.cs -- хелперы)
```

### Pattern 1: IComponentData -- data struct
**What:** Чистая struct без логики, только поля данных
**When to use:** Все новые компоненты ракеты
**Example:**
```csharp
// Source: Assets/Scripts/ECS/Components/LifeTimeData.cs (существующий паттерн)
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketTag : IComponentData { }
}
```

```csharp
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketTargetData : IComponentData
    {
        public Entity Target;
    }
}
```

```csharp
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketAmmoData : IComponentData
    {
        public int MaxAmmo;
        public float ReloadDurationSec;
        public int CurrentAmmo;
        public float ReloadRemaining;
    }
}
```

### Pattern 2: ISystem -- Burst-совместимая система (guidance)
**What:** partial struct : ISystem с итерацией через SystemAPI.Query
**When to use:** EcsRocketGuidanceSystem -- наведение ракеты
**Ключевое решение:** Система НЕ может быть BurstCompile, т.к. требуется EntityManager.Exists() и EntityManager.HasComponent<>() для проверки цели, а также итерация по enemy query для поиска ближайшего -- это managed вызовы [VERIFIED: STATE.md: "SystemBase без BurstCompile для homing (managed EntityQuery)"]

```csharp
// Source: паттерн из EcsShootToSystem.cs + EcsRotateSystem.cs
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsMoveSystem))]
    [UpdateBefore(typeof(EcsLifeTimeSystem))]
    public partial class EcsRocketGuidanceSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (move, target, entity) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RocketTargetData>>()
                         .WithAll<RocketTag>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                // 1. Проверить валидность цели
                if (target.ValueRO.Target != Entity.Null)
                {
                    if (!EntityManager.Exists(target.ValueRO.Target) ||
                        EntityManager.HasComponent<DeadTag>(target.ValueRO.Target))
                    {
                        target.ValueRW.Target = Entity.Null;
                    }
                }

                // 2. Если цели нет -- найти ближайшего врага
                if (target.ValueRO.Target == Entity.Null)
                {
                    target.ValueRW.Target = FindClosestEnemy(move.ValueRO.Position);
                }

                // 3. Если цель есть -- повернуть Direction к цели
                if (target.ValueRO.Target != Entity.Null)
                {
                    var targetPos = EntityManager.GetComponentData<MoveData>(target.ValueRO.Target).Position;
                    var toTarget = math.normalizesafe(targetPos - move.ValueRO.Position);
                    move.ValueRW.Direction = RotateTowards(
                        move.ValueRO.Direction, toTarget, RocketTargetData.TurnRateDegPerSec, deltaTime);
                }
                // Если цели нет -- ракета летит прямо (Direction не меняется)
            }
        }
    }
}
```

**Рекомендация по формуле поворота (Claude's Discretion):** Использовать cross-product подход. Он проще и надёжнее:

```csharp
// cross-product подход для поворота 2D-вектора к цели
private static float2 RotateTowards(float2 current, float2 target, float turnRateDeg, float deltaTime)
{
    var maxAngle = math.radians(turnRateDeg * deltaTime);

    // cross product в 2D = current.x * target.y - current.y * target.x
    // > 0 -- цель слева (поворот против часовой), < 0 -- справа (по часовой)
    var cross = current.x * target.y - current.y * target.x;

    // dot product для определения угла между векторами
    var dot = math.dot(current, target);
    var angle = math.acos(math.clamp(dot, -1f, 1f));

    if (angle <= maxAngle)
    {
        return target;
    }

    var rotAngle = math.sign(cross) * maxAngle;
    var cos = math.cos(rotAngle);
    var sin = math.sin(rotAngle);
    return new float2(
        current.x * cos - current.y * sin,
        current.x * sin + current.y * cos
    );
}
```

### Pattern 3: Поиск ближайшего врага
**What:** Итерация по всем entity с AsteroidTag, UfoBigTag, UfoTag без DeadTag
**Limitation:** Нет единого "EnemyTag" -- нужно 3 отдельных query или один with any-of [ASSUMED]

```csharp
private Entity FindClosestEnemy(float2 rocketPosition)
{
    var closestEntity = Entity.Null;
    var closestDistSq = float.MaxValue;

    // Итерация по астероидам
    foreach (var (move, entity) in
             SystemAPI.Query<RefRO<MoveData>>()
                 .WithAll<AsteroidTag>()
                 .WithNone<DeadTag>()
                 .WithEntityAccess())
    {
        var distSq = math.distancesq(rocketPosition, move.ValueRO.Position);
        if (distSq < closestDistSq)
        {
            closestDistSq = distSq;
            closestEntity = entity;
        }
    }

    // Аналогично для UfoBigTag и UfoTag
    // ...

    return closestEntity;
}
```

**Альтернатива:** Можно использовать `EntityQueryBuilder.WithAny<AsteroidTag, UfoBigTag, UfoTag>()` для единого запроса. В Unity Entities 1.x SystemAPI.Query не поддерживает WithAny из struct ISystem, но из SystemBase можно создать EntityQuery с WithAny через GetEntityQuery/EntityQueryBuilder. [ASSUMED]

### Pattern 4: Инкрементальная перезарядка (EcsRocketAmmoSystem)
**What:** Точная копия паттерна LaserSystem: CurrentAmmo += 1 за период
**Source:** `Assets/Scripts/ECS/Systems/EcsLaserSystem.cs`

```csharp
// По паттерну EcsLaserSystem -- инкрементальная перезарядка
[UpdateAfter(typeof(EcsLaserSystem))]
public partial struct EcsRocketAmmoSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var ammo in SystemAPI.Query<RefRW<RocketAmmoData>>())
        {
            if (ammo.ValueRO.CurrentAmmo < ammo.ValueRO.MaxAmmo)
            {
                ammo.ValueRW.ReloadRemaining -= deltaTime;
                if (ammo.ValueRO.ReloadRemaining <= 0)
                {
                    ammo.ValueRW.ReloadRemaining = ammo.ValueRO.ReloadDurationSec;
                    ammo.ValueRW.CurrentAmmo += 1;
                }
            }
        }
    }
}
```

**Рекомендация (Claude's Discretion):** Отдельная система EcsRocketAmmoSystem, а не встраивание в EcsGunSystem. Причины:
1. EcsGunSystem работает с GunData, у ракет другая структура данных (RocketAmmoData)
2. Single responsibility -- каждая система обрабатывает одну концепцию
3. Паттерн проекта: EcsGunSystem и EcsLaserSystem -- отдельные системы, хотя обе про перезарядку

### Pattern 5: EntityFactory -- создание ракеты
**Source:** `Assets/Scripts/ECS/EntityFactory.cs` -- паттерн CreateBullet()

```csharp
public static Entity CreateRocket(
    EntityManager em,
    float2 position,
    float speed,
    float2 direction,
    float lifeTime,
    float turnRateDegPerSec)
{
    var entity = em.CreateEntity();
    em.AddComponentData(entity, new RocketTag());
    em.AddComponentData(entity, new MoveData
    {
        Position = position,
        Speed = speed,
        Direction = direction
    });
    em.AddComponentData(entity, new LifeTimeData
    {
        TimeRemaining = lifeTime
    });
    em.AddComponentData(entity, new RocketTargetData
    {
        Target = Entity.Null
    });
    return entity;
}
```

### Pattern 6: Расширение CreateShip -- добавление RocketAmmoData

```csharp
// В существующем EntityFactory.CreateShip добавить:
em.AddComponentData(entity, new RocketAmmoData
{
    MaxAmmo = rocketMaxAmmo,
    ReloadDurationSec = rocketReloadSec,
    CurrentAmmo = rocketMaxAmmo,
    ReloadRemaining = rocketReloadSec
});
```

### Порядок систем в update chain (Claude's Discretion)

Текущий порядок:
```
EcsRotateSystem
  -> EcsThrustSystem
    -> EcsMoveSystem
      -> EcsShipPositionUpdateSystem
        -> EcsLifeTimeSystem
          -> EcsDeadByLifeTimeSystem
        -> EcsGunSystem
          -> EcsLaserSystem
        -> EcsShootToSystem
        -> EcsMoveToSystem
```

**Рекомендация:** EcsRocketGuidanceSystem после EcsMoveSystem (позиции уже обновлены), перед EcsLifeTimeSystem:
```
EcsMoveSystem
  -> EcsRocketGuidanceSystem   (обновляет Direction ракеты, нужна актуальная Position врагов)
    -> EcsShipPositionUpdateSystem
      -> EcsLifeTimeSystem
        -> EcsDeadByLifeTimeSystem
      -> EcsGunSystem
        -> EcsLaserSystem
          -> EcsRocketAmmoSystem  (после лазера, аналогичный паттерн)
```

### Anti-Patterns to Avoid
- **BurstCompile на GuidanceSystem:** SystemBase с managed-вызовами (EntityManager.Exists, HasComponent) не совместим с Burst [VERIFIED: STATE.md]
- **Тороидальное наведение:** Исключено из scope (REQUIREMENTS.md Out of Scope) -- не пытаться вычислять shortest path через wrap
- **Магические числа:** Turn rate, скорость, время жизни -- через параметры EntityFactory, не хардкод в системе (в отличие от ShootToSystem.cs:17 где speed=20 захардкожена)
- **Switch expression / ternary:** Запрещены по CLAUDE.md

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Время жизни ракеты | Свой таймер в GuidanceSystem | LifeTimeData + EcsLifeTimeSystem + EcsDeadByLifeTimeSystem | Уже работает для пуль, автоматически добавит DeadTag |
| Движение ракеты | Свою логику position += dir * speed * dt | MoveData + EcsMoveSystem | Включает тороидальную телепортацию бесплатно |
| Уничтожение entity | Свою логику Destroy | DeadTag + DeadEntityCleanupSystem | Единый pipeline уничтожения |
| 2D-вращение вектора | Quaternion/Matrix | math.cos/math.sin + ручная формула | Burst-совместимо, паттерн из EcsRotateSystem |

## Common Pitfalls

### Pitfall 1: Entity.Null vs несуществующая entity
**What goes wrong:** RocketTargetData.Target хранит Entity, которая уже была уничтожена DeadEntityCleanupSystem. EntityManager.GetComponentData на несуществующей entity -- exception.
**Why it happens:** DeadEntityCleanupSystem уничтожает entity в LateSimulationSystemGroup, но GuidanceSystem работает в SimulationSystemGroup. В том же кадре entity ещё жива, но имеет DeadTag.
**How to avoid:** Проверять ДВУМЯ условиями: `EntityManager.Exists(target) && !EntityManager.HasComponent<DeadTag>(target)`.
**Warning signs:** NullReferenceException или ArgumentException при GetComponentData.

### Pitfall 2: Нормализация нулевого вектора
**What goes wrong:** `math.normalizesafe(targetPos - rocketPos)` возвращает float2.zero если ракета и цель на одной точке.
**Why it happens:** При столкновении позиции совпадают.
**How to avoid:** Использовать `math.normalizesafe` (уже возвращает default вместо NaN). Если результат float2.zero -- не менять Direction.
**Warning signs:** Ракета теряет направление (Direction = 0,0), перестаёт двигаться.

### Pitfall 3: Перезарядка при полном боезапасе
**What goes wrong:** Таймер перезарядки уменьшается даже когда CurrentAmmo == MaxAmmo.
**Why it happens:** Забыли условие `if (CurrentAmmo < MaxAmmo)`.
**How to avoid:** Точно скопировать паттерн LaserSystem: `if (currentShoots < maxShoots) { ... }`.
**Warning signs:** Первая перезарядка после расходования происходит мгновенно.

### Pitfall 4: WithNone<DeadTag> в query для целей
**What goes wrong:** Ракета наводится на уже "мёртвого" врага.
**Why it happens:** DeadTag добавлен, но entity ещё не уничтожена (ждёт LateSimulationSystemGroup).
**How to avoid:** `.WithNone<DeadTag>()` в каждом enemy query.
**Warning signs:** Ракета преследует уже взорвавшегося врага.

### Pitfall 5: Тест GuidanceSystem требует managed SystemBase
**What goes wrong:** `CreateAndGetSystem<T>()` в тестовой fixture работает только для `unmanaged ISystem`, не для `SystemBase`.
**Why it happens:** AsteroidsEcsTestFixture.CreateAndGetSystem использует `World.Unmanaged.GetUnsafeSystemRef` -- это только для ISystem struct.
**How to avoid:** Для SystemBase использовать `World.CreateSystemManaged<T>()` и `systemHandle.Update(World.Unmanaged)` или `system.Update()`.
**Warning signs:** InvalidCastException или компиляционная ошибка при попытке создать SystemBase через CreateAndGetSystem.

## Code Examples

### Полный тест -- наведение поворачивает Direction к цели
```csharp
// Source: паттерн из EcsGunSystemTests.cs
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketGuidanceSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // SystemBase создаётся через CreateSystemManaged
            var system = World.CreateSystemManaged<EcsRocketGuidanceSystem>();
            _systemHandle = World.GetExistingSystem<EcsRocketGuidanceSystem>();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Guidance_TurnsDirection_TowardsClosestEnemy()
        {
            // Ракета в центре, летит вправо
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 5f,
                direction: new float2(1f, 0f),
                lifeTime: 5f
            );

            // Враг сверху
            CreateAsteroidEntity(
                position: new float2(0f, 10f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3
            );

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            // Direction должен повернуться вверх (y > 0)
            Assert.Greater(move.Direction.y, 0f);
        }
    }
}
```

### Хелпер в AsteroidsEcsTestFixture
```csharp
protected Entity CreateRocketEntity(
    float2 position, float speed, float2 direction, float lifeTime)
{
    var entity = m_Manager.CreateEntity();
    m_Manager.AddComponentData(entity, new RocketTag());
    m_Manager.AddComponentData(entity, new MoveData
    {
        Position = position,
        Speed = speed,
        Direction = direction
    });
    m_Manager.AddComponentData(entity, new LifeTimeData
    {
        TimeRemaining = lifeTime
    });
    m_Manager.AddComponentData(entity, new RocketTargetData
    {
        Target = Entity.Null
    });
    return entity;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ComponentSystemGroup + JobComponentSystem | SystemBase / ISystem | Entities 1.0 | Все новые системы -- ISystem (struct) или SystemBase (class) |
| EntityQuery + Entities.ForEach | SystemAPI.Query<> | Entities 1.0 | Проект полностью на SystemAPI.Query |
| GetSingleton<T>() | SystemAPI.GetSingleton<T>() | Entities 1.0 | Используется повсеместно |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | SystemAPI.Query из SystemBase поддерживает WithAny<T1,T2,T3> для единого enemy query | Architecture Patterns, Pattern 3 | Придётся использовать 3 отдельных foreach (работает, но verbose); не блокирует реализацию |
| A2 | World.CreateSystemManaged<T>() -- правильный API для создания SystemBase в тестах | Common Pitfalls, Pitfall 5 | Нужно найти альтернативный API; если нет -- переделать на ISystem + EntityQuery вместо SystemBase |
| A3 | Turn rate 180-270 grad/sec создаёт хороший баланс между маневренностью и дугообразной траекторией | Architecture Patterns | Значение конфигурируемое -- легко подстроить через параметры EntityFactory |

## Open Questions

1. **Где хранить TurnRateDegPerSec?**
   - Что мы знаем: D-02 говорит "через компонент". Два варианта: поле в RocketTargetData или отдельный компонент RocketGuidanceData
   - Что неясно: константа в компоненте (как RotateData.DegreePerSecond = 90f) или поле инстанса
   - Рекомендация: Поле инстанса в RocketTargetData (TurnRateDegPerSec), передаётся через EntityFactory.CreateRocket -- позволяет разный turn rate для разных ракет в будущем (ROCK-07)

2. **EntityQuery WithAny для поиска врагов**
   - Что мы знаем: нужно итерировать по AsteroidTag, UfoBigTag, UfoTag одновременно
   - Что неясно: поддерживает ли SystemAPI.Query<>.WithAny<> в текущей версии Entities
   - Рекомендация: Если нет -- 3 отдельных foreach. Производительность не критична (десятки entity, не тысячи)

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | Assets/Tests/EditMode/EditModeTests.asmdef |
| Quick run command | Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All |
| Full suite command | Unity Editor -> Test Runner -> Run All (EditMode + PlayMode) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ROCK-02 | Наведение поворачивает Direction к ближайшему врагу | unit | Test Runner -> RocketGuidanceSystemTests | Wave 0 |
| ROCK-02 | Turn rate ограничивает скорость поворота за кадр | unit | Test Runner -> RocketGuidanceSystemTests | Wave 0 |
| ROCK-03 | Переключение цели при DeadTag на текущей | unit | Test Runner -> RocketGuidanceSystemTests | Wave 0 |
| ROCK-03 | Переключение цели при Destroy текущей entity | unit | Test Runner -> RocketGuidanceSystemTests | Wave 0 |
| ROCK-04 | LifeTimeData уменьшается -> DeadTag | unit | Покрыто существующими тестами EcsLifeTimeSystem | Existing |
| ROCK-05 | CurrentAmmo уменьшается при запуске | unit | Test Runner -> RocketAmmoSystemTests | Wave 0 |
| ROCK-05 | Запуск невозможен при CurrentAmmo == 0 | unit | Test Runner -> RocketAmmoSystemTests | Wave 0 |
| ROCK-06 | Перезарядка: CurrentAmmo += 1 за период | unit | Test Runner -> RocketAmmoSystemTests | Wave 0 |
| ROCK-06 | Перезарядка не превышает MaxAmmo | unit | Test Runner -> RocketAmmoSystemTests | Wave 0 |
| TEST-01 | Все ECS-компоненты и системы покрыты тестами | meta | Все вышеперечисленные | Wave 0 |

### Sampling Rate
- **Per task commit:** Unity Test Runner -> EditMode -> Run All
- **Per wave merge:** Unity Test Runner -> Run All (EditMode + PlayMode)
- **Phase gate:** Full suite green before /gsd-verify-work

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/ECS/RocketGuidanceSystemTests.cs` -- ROCK-02, ROCK-03
- [ ] `Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs` -- ROCK-05, ROCK-06
- [ ] Обновление `AsteroidsEcsTestFixture.cs` -- хелпер CreateRocketEntity

## Security Domain

> Не применимо. Фаза -- чистая игровая логика (ECS-компоненты и системы), нет сетевого взаимодействия, пользовательского ввода строк, аутентификации или хранения данных. Вся логика работает локально в Unity.

## Sources

### Primary (HIGH confidence)
- `Assets/Scripts/ECS/Components/MoveData.cs` -- структура данных движения
- `Assets/Scripts/ECS/Components/LifeTimeData.cs` -- структура времени жизни
- `Assets/Scripts/ECS/Components/GunData.cs` -- паттерн боезапаса
- `Assets/Scripts/ECS/Components/LaserData.cs` -- паттерн инкрементальной перезарядки
- `Assets/Scripts/ECS/Components/Tags/DeadTag.cs` -- маркер уничтожения
- `Assets/Scripts/ECS/Systems/EcsGunSystem.cs` -- паттерн ISystem с перезарядкой
- `Assets/Scripts/ECS/Systems/EcsLaserSystem.cs` -- паттерн инкрементальной перезарядки (CurrentShoots += 1)
- `Assets/Scripts/ECS/Systems/EcsRotateSystem.cs` -- паттерн 2D-вращения через cos/sin
- `Assets/Scripts/ECS/Systems/EcsMoveSystem.cs` -- паттерн BurstCompile ISystem
- `Assets/Scripts/ECS/Systems/EcsShootToSystem.cs` -- паттерн доступа к ShipPositionData
- `Assets/Scripts/ECS/Systems/EcsDeadByLifeTimeSystem.cs` -- паттерн SystemBase с ECB
- `Assets/Scripts/Bridge/DeadEntityCleanupSystem.cs` -- LateSimulationSystemGroup cleanup
- `Assets/Scripts/ECS/EntityFactory.cs` -- паттерн создания entity
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` -- тестовая fixture
- `Assets/Tests/EditMode/ECS/EcsGunSystemTests.cs` -- паттерн тестирования ISystem
- `Assets/Tests/EditMode/ECS/EcsLaserSystemTests.cs` -- паттерн тестирования перезарядки

### Secondary (MEDIUM confidence)
- `.planning/STATE.md` -- решение про SystemBase без BurstCompile для homing

### Tertiary (LOW confidence)
- Нет

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все пакеты уже установлены и используются в проекте
- Architecture: HIGH -- все паттерны выведены из существующего кода проекта
- Pitfalls: HIGH -- выведены из анализа existing systems и их взаимодействия
- Формула наведения: MEDIUM -- cross-product подход стандартен для 2D, но точная имплементация в контексте Unity Entities не проверена через внешние источники

**Research date:** 2026-04-05
**Valid until:** 2026-05-05 (стабильный стек, все зависимости locked)
