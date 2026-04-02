# Testing

**Дата анализа:** 2026-04-02

## Test Framework

В проекте установлен `com.unity.test-framework` версии **1.1.33** (транзитивно через `com.unity.collections`).
`com.unity.ext.nunit` 1.0.6 — NUnit для Unity (транзитивно).

**Тестовое покрытие: 0%** — тесты полностью отсутствуют. Нет папок `Tests/`, `.asmdef` для тестов, ни одного `*Test*.cs`.

---

## Обнаруженные баги и edge cases

### BUG-1 (Critical): UFO не убивается пулями игрока
**Файл:** `Game.cs:140-143`
```csharp
if (_catalog.TryFindModel<UfoBigModel>(col.gameObject, out var ufoModel))
{
    _model.ReceiveScore(ufoModel);
    // ⚠ Kill(ufoModel) отсутствует!
}
```
Очки начисляются, но `Kill()` не вызывается. UFO остаётся в игре после попадания.

**Тестовый сценарий:** Убедиться, что UFO удаляется при попадании пули.

### BUG-2 (Critical): Division by zero в ShootToSystem
**Файл:** `ShootToSystem.cs:16-17`
```csharp
var time = (ship.Move.Position.Value - node.Move.Position.Value).magnitude
           / (20 - ship.Move.Speed.Value);
```
Если `ship.Move.Speed.Value == 20` → деление на ноль. Также, если скорость > 20, time отрицательный → UFO целится «назад».

**Хардкод 20** должен быть `configs.Bullet.Speed`.

### BUG-3 (Critical): Division by zero в MoveToSystem
**Файл:** `MoveToSystem.cs:18-19`
```csharp
var time = (ship.Move.Position.Value - node.Move.Position.Value).magnitude
           / (node.Move.Speed.Value - ship.Move.Speed.Value);
```
Если скорость UFO == скорость корабля → деление на ноль.

### BUG-4 (Medium): Неверная формула тороидального wrapping
**Файл:** `Model.cs:160,165`
```csharp
if (position > side / 2)
    position = -side + position;  // Для правого края — может остаться за пределами если position >> side
if (position < -side / 2)
    position = side - position;   // ⚠ Инвертирует знак! position=-6, side=10 → 10-(-6) = 16
```
Корректные формулы: `position -= side` и `position += side`.

### BUG-5 (Medium): GetRandomUfoPosition — division by zero
**Файл:** `GameUtils.cs:17-18`
```csharp
var verticalDistance = shipPosition.y - position.y;
var allowedDistance = verticalDistance - spawnAllowedRadius;
if (allowedDistance < 0)
    position.y += verticalDistance / Math.Abs(verticalDistance) * allowedDistance;
```
Если `verticalDistance == 0` → `0 / 0` = `NaN`.

### BUG-6 (Low): GetRandomAsteroidPosition сдвигает К кораблю
**Файл:** `GameUtils.cs:30-33`
```csharp
var allowedDistance = distance.magnitude - spawnAllowedRadius;
if (allowedDistance < 0)
    position += distance.normalized * allowedDistance;
```
`distance = shipPosition - position`, `allowedDistance < 0`. `distance.normalized` указывает от позиции к кораблю, умножение на отрицательное `allowedDistance` сдвигает **к кораблю** (хотя должно отодвигать).

### EDGE-1 (Medium): ActionScheduler — добавление во время итерации
**Файл:** `ActionScheduler.cs:28`
```csharp
//TODO theoretically it can be added during update
```
`SpawnNewEnemy` вызывает `ScheduleAction` внутри `entry.Action()` (строка 56). Обратный цикл и swap-with-last частично защищают, но поведение зависит от порядка обработки.

### EDGE-2 (Low): ThrustSystem — потеря направления при 180°
**Файл:** `ThrustSystem.cs:12-16`
```csharp
var velocity = node.Move.Direction * node.Move.Speed.Value + node.Rotate.Rotation.Value * acceleration;
node.Move.Direction = velocity.normalized;
```
Если `direction * speed` и `rotation * acceleration` коллинеарны и противоположны с равной magnitude → `velocity = (0,0)` → `normalized = (0,0)` → потеря направления.

---

## Классификация тестируемости

### Tier 1: Pure C# — тестируются без Unity (EditMode)

| Класс | Что тестировать | Сложность |
|-------|-----------------|-----------|
| `ThrustSystem` | Физика ускорения, торможения, edge case с 180° | Низкая |
| `RotateSystem` | Кватернионное вращение, 90°/сек | Низкая |
| `MoveSystem` | Перемещение, ⚠ wrapping (нужен `Model` для GameArea) | Средняя |
| `GunSystem` | Перезарядка (batch), стрельба, cooldown | Низкая |
| `LaserSystem` | Инкрементальная перезарядка, стрельба | Низкая |
| `LifeTimeSystem` | Обратный отсчёт | Низкая |
| `ShootToSystem` | Предиктивное прицеливание, div-by-zero | Низкая |
| `MoveToSystem` | Перехват, div-by-zero, интервал | Низкая |
| `ActionScheduler` | Отложенные действия, swap-with-last, добавление во время итерации | Средняя |
| `Model.PlaceWithinGameArea` | Тороидальная обёртка, граничные случаи | Низкая |
| `GameUtils` | Позиционирование спауна, div-by-zero | Низкая |
| `CoroutineResult<T>` | Тривиальный data class | Тривиальная |
| Entity models | `SetData`, `IsDead`, `Kill` | Низкая |

### Tier 2: Нужен мок или минимальный setup

| Класс | Зависимость | Что мокать |
|-------|-------------|-----------|
| `Model` (целиком) | Собственные системы | Ничего — но большой integration test |
| `LeaderboardService` | `IAuthProxy`, `ILeaderboardProxy`, `MonoBehaviour` | Интерфейсы + coroutine host |
| `EntitiesCatalog` | `ModelFactory`, `ViewFactory`, `GameData` | Конфиг + фабрики |

### Tier 3: PlayMode (Unity runtime)

| Класс | Причина |
|-------|---------|
| `Game` | Зависит от `EntitiesCatalog`, `PlayerInput`, `GameScreen` |
| `GameScreen` | UI, `MonoBehaviour`, корутины |
| Все Visuals | `MonoBehaviour`, `SpriteRenderer`, Physics |
| `GameObjectPool` | `Object.Instantiate`, `Transform` |

---

## Таблица приоритетов тестирования

| # | Компонент | Приоритет | Тип | Обоснование |
|---|-----------|-----------|-----|-------------|
| 1 | `Model.PlaceWithinGameArea` | P0 | Unit | Активный баг в wrapping формуле |
| 2 | `ShootToSystem` | P0 | Unit | Division by zero при speed=20 |
| 3 | `MoveToSystem` | P0 | Unit | Division by zero при равных скоростях |
| 4 | `GameUtils` | P0 | Unit | Division by zero + астероид спаунится к кораблю |
| 5 | `ThrustSystem` | P1 | Unit | Основная физика, edge case 180° |
| 6 | `GunSystem` | P1 | Unit | Стрельба и перезарядка — ядро геймплея |
| 7 | `LaserSystem` | P1 | Unit | Инкрементальная перезарядка |
| 8 | `ActionScheduler` | P1 | Unit | Отложенные действия, swap-with-last |
| 9 | `RotateSystem` | P2 | Unit | Простая система, мало edge cases |
| 10 | `MoveSystem` | P2 | Unit | Зависит от PlaceWithinGameArea |
| 11 | `LifeTimeSystem` | P2 | Unit | Тривиальный таймер |
| 12 | `Model` (integration) | P2 | Integration | Цикл Update, создание/удаление |
| 13 | `LeaderboardService` | P3 | Unit+Mock | Интерфейсы позволяют мокать |
| 14 | Entity Models | P3 | Unit | Data classes, мало логики |

---

## Примеры тестов

### Тест 1: PlaceWithinGameArea — обнаружение бага wrapping

```csharp
[Test]
public void PlaceWithinGameArea_PositionBeyondLeftEdge_WrapsToRightSide()
{
    float position = -6f;
    float side = 10f;  // Границы: [-5, 5]
    Model.PlaceWithinGameArea(ref position, side);

    // Ожидаемое: position ≈ 4 (wrap через левый край)
    // Фактическое (баг): position = 10 - (-6) = 16 (за правым краем)
    Assert.That(position, Is.InRange(-5f, 5f), "Position should wrap within game area");
}
```

### Тест 2: ShootToSystem — division by zero

```csharp
[Test]
public void ShootToSystem_ShipSpeedEquals20_NoDivisionByZero()
{
    var shipMove = new MoveComponent();
    shipMove.Position.Value = new Vector2(5, 5);
    shipMove.Speed.Value = 20f;  // == хардкод скорости пули
    shipMove.Direction = Vector2.right;

    var ship = new ShipModel();
    // Нужен reflection или internal access для установки Move

    var gun = new GunComponent { CurrentShoots = 1, MaxShoots = 1 };
    var shootTo = new ShootToComponent { Ship = ship };
    var move = new MoveComponent();
    move.Position.Value = Vector2.zero;

    var system = new ShootToSystem();
    system.Add(someModel, (move, gun, shootTo));

    // Не должен бросать исключение
    Assert.DoesNotThrow(() => system.Update(0.016f));
}
```

### Тест 3: ThrustSystem — физика ускорения

```csharp
[Test]
public void ThrustSystem_WhenActive_AcceleratesInRotationDirection()
{
    var thrust = new ThrustComponent { UnitsPerSecond = 10f, MaxSpeed = 20f };
    thrust.IsActive.Value = true;
    var move = new MoveComponent();
    move.Direction = Vector2.right;
    move.Speed.Value = 0f;
    var rotate = new RotateComponent();
    // Rotation по умолчанию = Vector2.right

    var system = new ThrustSystem();
    system.Add(someModel, (thrust, move, rotate));
    system.Update(1.0f);  // 1 секунда

    Assert.AreEqual(Vector2.right, move.Direction);
    Assert.That(move.Speed.Value, Is.EqualTo(10f).Within(0.01f));
}

[Test]
public void ThrustSystem_WhenInactive_Decelerates()
{
    var thrust = new ThrustComponent { UnitsPerSecond = 10f, MaxSpeed = 20f };
    thrust.IsActive.Value = false;
    var move = new MoveComponent();
    move.Speed.Value = 10f;
    var rotate = new RotateComponent();

    var system = new ThrustSystem();
    system.Add(someModel, (thrust, move, rotate));
    system.Update(1.0f);

    // Торможение = UnitsPerSecond / 2 = 5/сек
    Assert.That(move.Speed.Value, Is.EqualTo(5f).Within(0.01f));
}
```

### Тест 4: GunSystem — batch перезарядка

```csharp
[Test]
public void GunSystem_Reload_RestoresAllShotsAtOnce()
{
    var gun = new GunComponent
    {
        MaxShoots = 3,
        CurrentShoots = 0,
        ReloadDurationSec = 1.0f,
        ReloadRemaining = 1.0f
    };

    var system = new GunSystem();
    system.Add(someModel, gun);

    // Прошла 1 секунда — перезарядка
    system.Update(1.0f);

    Assert.AreEqual(3, gun.CurrentShoots, "All shots restored at once");
}
```

### Тест 5: ActionScheduler — swap-with-last

```csharp
[Test]
public void ActionScheduler_ExecutesActionAndRemoves()
{
    var scheduler = new ActionScheduler();
    var executed = false;
    scheduler.ScheduleAction(() => executed = true, 1.0f);

    scheduler.Update(0.5f);
    Assert.IsFalse(executed, "Not yet");

    scheduler.Update(0.6f);  // Суммарно 1.1 секунды
    Assert.IsTrue(executed);
}

[Test]
public void ActionScheduler_ActionCanRescheduleItself()
{
    var scheduler = new ActionScheduler();
    var count = 0;
    void Reschedule()
    {
        count++;
        if (count < 3)
        {
            scheduler.ScheduleAction(Reschedule, 1.0f);
        }
    }
    scheduler.ScheduleAction(Reschedule, 1.0f);

    scheduler.Update(1.1f);
    Assert.AreEqual(1, count);

    scheduler.Update(1.1f);
    Assert.AreEqual(2, count);
}
```

---

## Инфраструктура для тестов

### Создание тестовой сборки

```
Assets/
└── Tests/
    ├── EditMode/
    │   ├── EditModeTests.asmdef    # References: Asteroids, nunit
    │   ├── Systems/
    │   │   ├── ThrustSystemTests.cs
    │   │   ├── ShootToSystemTests.cs
    │   │   └── ...
    │   ├── ModelTests.cs
    │   └── ActionSchedulerTests.cs
    └── PlayMode/
        ├── PlayModeTests.asmdef    # References: Asteroids, nunit
        └── GameIntegrationTests.cs
```

### Минимальный .asmdef для EditMode

```json
{
    "name": "EditModeTests",
    "rootNamespace": "SelStrom.Asteroids.Tests",
    "references": ["Asteroids"],
    "includePlatforms": ["Editor"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "optionalUnityReferences": ["TestAssemblies"]
}
```

### Барьер: namespace Model.Components

Компоненты (`GunComponent`, `MoveComponent`, etc.) находятся в `Model.Components`, а не `SelStrom.Asteroids`. Тестовая сборка должна ссылаться на правильный namespace или использовать `InternalsVisibleTo`.
