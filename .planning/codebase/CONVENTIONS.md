# Coding Conventions

**Дата анализа:** 2026-04-02

## Naming Patterns

### Классы и интерфейсы
- Классы -- `PascalCase`: `ShipModel`, `GunSystem`, `GameObjectPool`, `EntitiesCatalog`
- Интерфейсы -- `PascalCase` с префиксом `I`: `IGameEntityModel`, `IModelSystem`, `IEntityView`, `IGroupVisitor`
- Абстрактные классы -- `Abstract` + суть: `AbstractScreen`, `AbstractViewModel`, `AbstractWidgetView<T>`
- Enum -- `PascalCase`: `State`, значения тоже `PascalCase`: `Default`, `Game`, `EndGame`

### Методы
- `PascalCase` для публичных, защищённых и приватных методов: `AddEntity`, `Update`, `CleanUp`, `PlaceWithinGameArea`, `SpawnAsteroid`
- Методы-обработчики событий -- префикс `On` + существительное/глагол: `OnUpdate`, `OnAttack`, `OnShipCollided`, `OnEntityDestroyed`, `OnRotationChanged`, `OnCurrentShootsChanged`
- Корутины -- префикс `Run` для запускающих обёрток, суффикс `Routine` для IEnumerator-методов: `RunInitialize`, `FetchAndShowLeaderboardRoutine`, `SubmitAndShowLeaderboardRoutine`

### Поля и свойства
- Приватные поля -- префикс `_` + `camelCase`: `_killed`, `_model`, `_gameObjectPool`, `_typeToSystem`
- Публичные `readonly` поля компонентов -- `PascalCase` без префикса: `Position`, `Speed`, `IsActive`, `CurrentShoots`, `Rotation`
- Свойства -- `PascalCase`: `Score`, `GameArea`, `ActionScheduler`, `IsSuccess`, `Data`
- `[SerializeField]` поля -- `_` + `camelCase`: `_configs`, `_hudVisual`, `_spriteRenderer`, `_collider`
- Константы -- `PascalCase`: `PlayerNameKey`, `MinSpeed`, `DegreePerSecond`

### Параметры и локальные переменные
- `camelCase`: `deltaTime`, `shipPosition`, `prefabId`, `gameObject`, `orthographicSize`

### Дженерики
- Один символ `T` или `T` + описание: `TSystem`, `TModel`, `TNode`, `TComponent`, `TView`, `TData`

---

## File Organization

### Структура директорий
```
Assets/Scripts/
├── Model/
│   ├── Components/     -- data-классы компонентов (MoveComponent, GunComponent и т.д.)
│   ├── Entities/       -- модели сущностей (ShipModel, AsteroidModel, BulletModel, UfoBigModel)
│   ├── Systems/        -- системы обработки (MoveSystem, GunSystem, LaserSystem и т.д.)
│   ├── Model.cs        -- центральный класс игровой модели, хранит коллекции сущностей и систем
│   └── ActionScheduler.cs -- планировщик отложенных действий по таймеру
├── View/
│   ├── Base/           -- BaseVisual, IEntityView
│   ├── Components/     -- переиспользуемые UI-компоненты (GuiText)
│   ├── Bindings/       -- расширения для data bindings (BindingToExtensions)
│   ├── *Visual.cs      -- визуальные представления сущностей
│   └── *ViewModel.cs   -- вместе с Visual в одном файле
├── Application/
│   ├── Leaderboard/    -- сервис таблицы лидеров, прокси-интерфейсы и Unity-реализации
│   ├── Screens/        -- экраны (GameScreen, TitleScreen, AbstractScreen)
│   ├── Game.cs         -- игровая логика: спавн, столкновения, дробление, ввод
│   ├── Application.cs  -- оркестратор: камера, GameArea, создание каталога и Game
│   ├── ApplicationEntry.cs -- MonoBehaviour-обёртка, точка входа Unity
│   ├── EntitiesCatalog.cs  -- реестр сущностей, фабрики, двусторонний маппинг model<->view
│   ├── ModelFactory.cs     -- фабрика моделей с регистрацией в Model
│   └── ViewFactory.cs      -- фабрика визуалов с пулом объектов
├── Configs/            -- ScriptableObject конфиги (GameData, AsteroidData, UfoData, GunData)
├── Input/              -- PlayerInput, PlayerActions (включая сгенерированный код)
│   └── Generated/      -- авто-сгенерированные файлы Input System
└── Utils/              -- GameObjectPool, GameUtils, CoroutineResult
```

### Один класс -- один файл
Правило соблюдается повсеместно. Исключения:
- ViewModel и Visual одной сущности объединены в один файл (например, `Assets/Scripts/View/ShipVisual.cs` содержит `ShipViewModel` и `ShipVisual`).
- `UfoModel` и `UfoBigModel` объединены в `Assets/Scripts/Model/Entities/UfoBigModel.cs` (наследование `UfoModel : UfoBigModel`).

### Namespace
- Основной namespace: `SelStrom.Asteroids`
- Компоненты модели: `namespace Model.Components` (`Assets/Scripts/Model/Components/`)
- Конфиги: `namespace SelStrom.Asteroids.Configs` (`Assets/Scripts/Configs/`)
- Биндинги: `namespace SelStrom.Asteroids.Bindings` (`Assets/Scripts/View/Bindings/`)
- Папки `Model`, `View`, `Application` пропущены в namespace через `Asteroids.csproj.DotSettings` (`NamespaceFoldersToSkip`).
- **Внимание:** `Model.Components` -- это НЕ `SelStrom.Asteroids.Model.Components`, а просто `Model.Components`. Файлы в этом namespace используют `using SelStrom.Asteroids;` для доступа к `IGameEntityModel` и пр.

---

## Code Style

### Фигурные скобки
Открывающая скобка для классов, методов и свойств -- на **новой строке** (Allman).
Управляющие конструкции (`if`, `for`, `foreach`, `while`, `switch`) -- `.editorconfig` указывает `csharp_new_line_before_open_brace = control_blocks,local_functions,methods`, т.е. **на новой строке**. Фактически в коде скобки ставятся на новой строке повсеместно.

**Исключение:** в `ActionScheduler.cs:44` цикл `for` использует K&R стиль (скобка на той же строке). Это единственное отклонение.

Тело `case` в `switch` без фигурных скобок. Всегда используются фигурные скобки даже для однострочных `if`.

```csharp
// Стандартный стиль -- скобка на новой строке:
if (_newEntities.Any())
{
    _entities.UnionWith(_newEntities);
}

foreach (var system in _systems)
{
    system.Update(deltaTime);
}
```

### Отступы
4 пробела (настройка `.editorconfig`).

### default-инициализация [SerializeField]
SerializeField-поля MonoBehaviour инициализируются значением `default`:
```csharp
[SerializeField] private GameData _configs = default;
[SerializeField] private SpriteRenderer _spriteRenderer = default;
```

---

## Patterns to Follow

### MVVM (через пакет `com.shtl.mvvm`)

Каждая сущность имеет три слоя:
- **Model** -- pure C# данные: `ShipModel`, `AsteroidModel`, `BulletModel`, `UfoBigModel`, `UfoModel`
- **ViewModel** -- `AbstractViewModel` с `ReactiveValue<T>` полями: `ShipViewModel`, `AsteroidViewModel`, `BulletViewModel`, `UfoViewModel`, `HudData`, `ScoreViewModel`, `LeaderboardEntryViewModel`
- **View** -- `AbstractWidgetView<TViewModel>` MonoBehaviour: `ShipVisual`, `AsteroidVisual`, `BulletVisual`, `UfoVisual`, `HudVisual`, `ScoreVisual`, `LeaderboardEntryVisual`

**Связывание Model -> ViewModel** через `EventBindingContext` в `EntitiesCatalog`:
```csharp
// Assets/Scripts/Application/EntitiesCatalog.cs:61-65
var bindings = new EventBindingContext();
bindings.From(model.Move.Position).To(viewModel.Position);
bindings.From(model.Rotate.Rotation).To(viewModel.Rotation);
bindings.From(model.Thrust.IsActive).To(viewModel.Sprite,
    (bool isThrust, ReactiveValue<Sprite> sprite) =>
        sprite.Value = isThrust ? _configs.Ship.ThrustSprite : _configs.Ship.MainSprite);
bindings.InvokeAll();
```

**Связывание ViewModel -> View** через `Bind.From(...).To(...)` в `OnConnected()`:
```csharp
// Assets/Scripts/View/ShipVisual.cs:22-24
Bind.From(ViewModel.Position).To(transform);
ViewModel.Rotation.Connect(OnRotationChanged);
ViewModel.Sprite.Connect(sprite => _spriteRenderer.sprite = sprite);
```

**Кастомный binding extension** для Vector2 -> Transform.position:
```csharp
// Assets/Scripts/View/Bindings/BindingToExtensions.cs:8-15
public static void To(this BindFrom<ReactiveValue<Vector2>> from, Transform target) =>
    from.Source.Connect(value =>
    {
        var position = target.position;
        position.x = value.x;
        position.y = value.y;
        target.position = position;
    });
```
Этот extension сохраняет z-координату Transform, заменяя только x и y.

### Паттерн Connect/Dispose
Классы НЕ используют конструкторы для Unity-зависимостей. Два подхода:
1. **Конструкторы** -- только для pure C# зависимостей (например, `Game`, `LeaderboardService`, `ModelFactory`, `ViewFactory`).
2. **`Connect(...)`** -- для зависимостей от Unity-объектов или для отложенной инициализации (например, `Application.Connect()`, `EntitiesCatalog.Connect()`, `GameObjectPool.Connect()`).

Очистка: `Dispose()` / `CleanUp()` для освобождения ресурсов.

### Паттерн null-сброс при Dispose
В методах `Dispose()` поля явно обнуляются:
```csharp
// Assets/Scripts/Application/Application.cs:83-96
private void Dispose()
{
    _catalog.Dispose();
    _gameObjectPool.Dispose();
    _catalog = null;
    _gameObjectPool = null;
    _appComponent = null;
    // ...
}
```

### Event подписка/отписка
Всегда симметричная: подписка в `Start()` / `Connect()`, отписка в `Stop()` / `Quit()`. Пример:
```csharp
// Assets/Scripts/Application/Game.cs:43-46 (подписка)
_playerInput.OnAttackAction += OnAttack;
_playerInput.OnRotateAction += OnRotateAction;
_playerInput.OnTrustAction += OnTrust;
_playerInput.OnLaserAction += OnLaser;

// Assets/Scripts/Application/Game.cs:61-64 (отписка)
_playerInput.OnAttackAction -= OnAttack;
_playerInput.OnRotateAction -= OnRotateAction;
_playerInput.OnTrustAction -= OnTrust;
_playerInput.OnLaserAction -= OnLaser;
```

### Action-делегаты как публичные поля
В компонентах `GunComponent`, `LaserComponent` Action-поля объявлены как **публичные поля** (не свойства, не события):
```csharp
// Assets/Scripts/Model/Components/GunComponent.cs:8
public Action<GunComponent> OnShooting;
```

### ECS-подобные системы

**BaseModelSystem<TNode>** (`Assets/Scripts/Model/Systems/BaseModelSystem.cs`) -- базовый класс всех систем:
- Хранит `Dictionary<IGameEntityModel, TNode>` для маппинга entity -> node
- `Add(model, node)` / `Remove(model)` для регистрации
- `Update(deltaTime)` итерирует все nodes и вызывает `UpdateNode(node, deltaTime)`
- `TNode` может быть как одиночный компонент, так и **ValueTuple** нескольких компонентов

**Примеры TNode:**
- Одиночный: `BaseModelSystem<MoveComponent>`, `BaseModelSystem<GunComponent>`, `BaseModelSystem<LaserComponent>`
- Tuple: `BaseModelSystem<(ThrustComponent, MoveComponent, RotateComponent)>`, `BaseModelSystem<(MoveComponent, GunComponent, ShootToComponent)>`, `BaseModelSystem<(MoveComponent, MoveToComponent)>`

**Порядок регистрации систем определяет порядок обновления** (`Assets/Scripts/Model/Model.cs:75-83`):
1. `RotateSystem` -- поворот
2. `ThrustSystem` -- ускорение (изменение direction и speed)
3. `MoveSystem` -- передвижение (применение direction * speed * dt)
4. `LifeTimeSystem` -- уменьшение оставшегося времени жизни
5. `GunSystem` -- перезарядка и стрельба
6. `LaserSystem` -- перезарядка и стрельба лазером
7. `ShootToSystem` -- AI: выбор направления стрельбы (UFO)
8. `MoveToSystem` -- AI: выбор направления движения (маленький UFO)

### Visitor-паттерн для dispatch сущностей
`IGroupVisitor` (`Assets/Scripts/Model/Entities/IGroupVisitor.cs`) с перегрузками `Visit(XxxModel)`.
Каждая модель реализует `AcceptWith(IGroupVisitor visitor)`.
`GroupCreator` (вложенный класс в `Model.cs:10-53`) использует visitor для регистрации сущностей в нужных системах.

### Object Pool
`GameObjectPool` (`Assets/Scripts/Utils/GameObjectPool.cs`) -- пул переиспользования GameObject:
- Ключ пула -- `prefab.GetInstanceID().ToString()` (строковый ID инстанса префаба)
- `Stack<GameObject>` для каждого типа префаба
- При `Get`: если есть в стеке -- Pop + SetParent + SetActive(true); иначе -- `Object.Instantiate`
- При `Release`: SetActive(false) + SetParent(poolContainer) + Push в стек
- `_gameObjectToPrefabId` -- обратный маппинг для Release
- При Release незнакомого объекта -- `throw new Exception`

### Proxy-интерфейсы для внешних сервисов
`IAuthProxy` (`Assets/Scripts/Application/Leaderboard/IAuthProxy.cs`), `ILeaderboardProxy` (`Assets/Scripts/Application/Leaderboard/ILeaderboardProxy.cs`).
Реализации: `UnityAuthProxy` (Unity Authentication Service), `UnityLeaderboardProxy` (Unity Leaderboards Service).

### CoroutineResult для async-операций
Вместо async/await -- корутины с передачей `CoroutineResult` / `CoroutineResult<T>`:
```csharp
// Assets/Scripts/Utils/CoroutineResult.cs
public class CoroutineResult
{
    public Exception Error { get; set; }
    public bool IsSuccess => Error == null;
}
public class CoroutineResult<T> : CoroutineResult
{
    public T Value { get; set; }
}
```
Паттерн использования:
```csharp
var result = new CoroutineResult<List<LeaderboardEntry>>();
yield return _leaderboardService.GetTopScores(result);
if (!result.IsSuccess) { /* обработка ошибки */ }
```

### Конфиги через ScriptableObject
Все игровые параметры -- в `GameData` (`Assets/Scripts/Configs/GameData.cs`):
- Вложенные `[Serializable] struct` для группировки: `BulletData`, `ShipData`, `LaserData`
- Отдельные ScriptableObject для данных сущностей: `AsteroidData`, `UfoData`, `GunData` -- наследуют `BaseGameEntityData` (содержит `Score`)
- `[Space]` и `[Header]` для группировки в инспекторе

### Struct для immutable data
`LeaderboardEntry` (`Assets/Scripts/Application/Leaderboard/LeaderboardEntry.cs`) -- `readonly struct` с `readonly` полями.
`GameScreenData` (`Assets/Scripts/Application/Screens/GameScreen.cs:11-16`) -- обычный struct для передачи данных между слоями.

---

## Algorithm Patterns

### Движение с телепортацией через границу (toroidal wrapping)
`Model.PlaceWithinGameArea` (`Assets/Scripts/Model/Model.cs:156-167`):
```csharp
public static void PlaceWithinGameArea(ref float position, float side)
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
```
Применяется к каждой оси отдельно в `MoveSystem.UpdateNode`. Координатное пространство центрировано: `-side/2..+side/2`.

### Движение: velocity = direction * speed
`MoveSystem` (`Assets/Scripts/Model/Systems/MoveSystem.cs:14-19`):
```csharp
var position = oldPosition + node.Direction * (node.Speed.Value * deltaTime);
```
Direction -- нормализованный вектор, Speed -- скалярная скорость. Скорость и направление хранятся раздельно.

### Ускорение корабля (thrust)
`ThrustSystem` (`Assets/Scripts/Model/Systems/ThrustSystem.cs:9-20`):
- **При ускорении**: суммирует текущий velocity (`direction * speed`) с вектором ускорения (`rotation * acceleration`), нормализует, clamp до `MaxSpeed`:
  ```csharp
  var velocity = node.Move.Direction * node.Move.Speed.Value + node.Rotate.Rotation.Value * acceleration;
  node.Move.Direction = velocity.normalized;
  node.Move.Speed.Value = Math.Min(velocity.magnitude, node.Thrust.MaxSpeed);
  ```
- **При отпускании газа**: линейное замедление со скоростью `UnitsPerSecond / 2`, до `MinSpeed` (0.0):
  ```csharp
  node.Move.Speed.Value = Math.Max(node.Move.Speed.Value - node.Thrust.UnitsPerSecond / 2 * deltaTime, ThrustComponent.MinSpeed);
  ```

### Поворот корабля (rotate)
`RotateSystem` (`Assets/Scripts/Model/Systems/RotateSystem.cs:9-17`):
- Константная скорость: `DegreePerSecond = 90` градусов/сек
- Поворот через `Quaternion.Euler(0, 0, angle) * currentRotation`:
  ```csharp
  node.Rotation.Value = Quaternion.Euler(0, 0, RotateComponent.DegreePerSecond * deltaTime * node.TargetDirection) * node.Rotation.Value;
  ```
- `TargetDirection` -- float, приходит от Input System как ось: -1 (вправо, по часовой), 0 (нет поворота), +1 (влево, против часовой). Инвертировано через `1DAxis(minValue=1,maxValue=-1)` в InputActions.
- `Rotation` хранится как `Vector2` (не как угол), начальное значение `Vector2.right`.

### Перезарядка оружия (gun / laser)
`GunSystem` (`Assets/Scripts/Model/Systems/GunSystem.cs:7-25`) и `LaserSystem` аналогичны:
1. Если `CurrentShoots < MaxShoots` -- уменьшается `ReloadRemaining` на `deltaTime`
2. Когда `ReloadRemaining <= 0` -- полная перезарядка: `CurrentShoots = MaxShoots`, `ReloadRemaining = ReloadDurationSec`
3. Если `Shooting && CurrentShoots > 0` -- один выстрел за кадр: `CurrentShoots--`, вызов `OnShooting`
4. `Shooting` сбрасывается в `false` каждый кадр

**Отличие лазера от пушки:** `LaserComponent` использует `ObservableValue<int>` для `CurrentShoots` и `ObservableValue<float>` для `ReloadRemaining` (для привязки к HUD), а перезарядка восстанавливает по одному заряду за цикл (`CurrentShoots += 1`), а не полностью.

### Стрельба пули
`ShootPoint` корабля (`Assets/Scripts/Model/Entities/ShipModel.cs:17`):
```csharp
public Vector2 ShootPoint => Move.Position.Value + Rotate.Rotation.Value;
```
Пуля создаётся с позицией = ShootPoint, направлением = Rotation корабля, скоростью из конфига.

### Лазер (raycast)
`Game.OnUserLaserShooting` (`Assets/Scripts/Application/Game.cs:210-238`):
1. Создаёт визуальный LineRenderer из пула, позиционирует по кораблю
2. Угол LineRenderer вычисляется: `Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg`
3. LineRenderer уничтожается через `ActionScheduler` по таймеру `BeamEffectLifetimeSec`
4. `Physics2D.RaycastNonAlloc` с буфером на 30 хитов, маска `"Asteroid", "Enemy"`
5. Дистанция raycast -- `gameArea.magnitude` (диагональ экрана)
6. Все поражённые сущности получают урон и очки

### Предиктивное прицеливание AI (lead targeting)
`ShootToSystem` (`Assets/Scripts/Model/Systems/ShootToSystem.cs:9-24`) и `MoveToSystem` (`Assets/Scripts/Model/Systems/MoveToSystem.cs:7-23`):
```csharp
// Время до перехвата: расстояние / относительная скорость
var time = (ship.Move.Position.Value - node.Move.Position.Value).magnitude
           / (bulletSpeed - ship.Move.Speed.Value);
// Предсказанная позиция корабля через time секунд
var pendingPosition = ship.Move.Position.Value + (ship.Move.Direction * ship.Move.Speed.Value) * time;
// Направление стрельбы/движения
var direction = (pendingPosition - node.Move.Position.Value).normalized;
```
- `ShootToSystem` использует жёстко закодированную скорость пули `20` в формуле `(20 - ship.Move.Speed.Value)`
- `MoveToSystem` использует разницу скоростей UFO и корабля
- `MoveToSystem` перезаходит каждые `Every` секунд (3 сек для маленького UFO)
- `ShootToSystem` стреляет каждый кадр при наличии патронов (без cooldown кроме GunSystem reload)

### Дробление астероидов
`Game.Kill` (`Assets/Scripts/Application/Game.cs:170-196`):
- Астероид имеет `Age` (3 = большой, 2 = средний, 1 = маленький)
- При уничтожении: `age = asteroidModel.Age - 1`
- Если `age > 0` -- создаются **2 новых астероида** меньшего размера на той же позиции
- Скорость осколков: `Math.Min(asteroidModel.Move.Speed.Value * 2, 10f)` -- удвоенная скорость родителя, но не более 10
- Направление осколков: `Random.insideUnitCircle` (случайное)

### Спавн врагов
`Game.SpawnNewEnemy` (`Assets/Scripts/Application/Game.cs:77-94`):
- Периодический спавн через `ActionScheduler.ScheduleAction` с интервалом `SpawnNewEnemyDurationSec`
- Рекурсивное перепланирование: после каждого спавна ставится следующий
- Равновероятный выбор типа: `Random.Range(0, 3)` -- астероид / маленький UFO / большой UFO

### Спавн позиций (safe spawn)
`GameUtils.GetRandomAsteroidPosition` (`Assets/Scripts/Utils/GameUtils.cs:24-37`):
- Случайная позиция в пределах GameArea
- Если слишком близко к кораблю (< `SpawnAllowedRadius`) -- сдвигает позицию **от** корабля на недостающее расстояние

`GameUtils.GetRandomUfoPosition` (`Assets/Scripts/Utils/GameUtils.cs:9-22`):
- Спавн на **левом краю** экрана (`x = 0 - gameArea.x * 0.5`)
- Случайная y-позиция
- Если вертикальное расстояние до корабля < `SpawnAllowedRadius` -- сдвигает по вертикали

### Большой UFO: горизонтальное движение
`Game.SpawnBigUfo` (`Assets/Scripts/Application/Game.cs:103-108`):
```csharp
(Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized
```
Направление для большого UFO -- почти горизонтальное (y-компонента подавлена в 10 раз).

### Маленький UFO: преследование
Маленький UFO (`UfoModel`) имеет дополнительный компонент `MoveToComponent` и систему `MoveToSystem`, которая каждые 3 секунды пересчитывает направление движения к предсказанной позиции корабля.

### Время жизни пуль
`BulletModel.IsDead()` (`Assets/Scripts/Model/Entities/BulletModel.cs:27`):
```csharp
public bool IsDead() => LifeTime.TimeRemaining <= 0 || _killed;
```
Двойное условие: и по таймеру (`LifeTimeSystem` уменьшает `TimeRemaining`), и по явному Kill (при столкновении).

### Расчёт GameArea из камеры
`Application.Start` (`Assets/Scripts/Application/Application.cs:37-40`):
```csharp
var orthographicSize = mainCamera!.orthographicSize;
var sceneWidth = mainCamera.aspect * orthographicSize * 2;
var sceneHeight = orthographicSize * 2;
```
GameArea определяется по ортографической камере при старте.

### ActionScheduler: отложенное выполнение
`ActionScheduler` (`Assets/Scripts/Model/ActionScheduler.cs`):
- Оптимизация: `_nextUpdateDuration` -- минимальная задержка до ближайшего действия; если время не пришло, `Update` возвращается рано
- Удаление по swap-with-last: при исполнении действия -- меняется местами с последним элементом, удаляется последний (O(1) удаление из List)
- `_secondsSinceLastUpdate` накапливается между обновлениями, при обработке сбрасывается в 0
- Действия, добавленные через `ScheduleAction`, учитывают уже прошедшее время: `nextUpdate = durationSec + _secondsSinceLastUpdate`

### Leaderboard: best-score submit
`GameScreen.SubmitAndShowLeaderboardRoutine` (`Assets/Scripts/Application/Screens/GameScreen.cs:225-289`):
- Перед отправкой запрашивает текущий результат игрока с сервера
- Отправляет `Math.Max(serverBestScore, currentScore)` -- только если текущий результат лучше
- Проверка stale ViewModel: `if (_score.ViewModel != viewModel) yield break;` -- защита от race condition при быстром рестарте

### Collision handling: отключение коллайдера
`Game.OnUserBulletCollided` (`Assets/Scripts/Application/Game.cs:128-144`):
```csharp
col.otherCollider.enabled = false;
```
При столкновении пули с противником немедленно отключается коллайдер противника, чтобы предотвратить повторные столкновения до момента удаления (Kill + IsDead -> Remove в следующем кадре).

### VFX: эффект взрыва через пул
`EffectVisual` (`Assets/Scripts/View/EffectVisual.cs`) наследует `BaseVisual<Action<EffectVisual>>`:
- В `OnConnected()` запускает ParticleSystem
- По событию `OnParticleSystemStopped` вызывает callback, который возвращает эффект в пул

---

## Documentation

### Комментарии
XML-документация (`/// <summary>`) **не используется** (за исключением автосгенерированного `PlayerActions.cs`).
Используются только `//` комментарии:
1. TODO-пометки с автором: `// TODO @a.shatalov: model pool` (`Assets/Scripts/Application/ModelFactory.cs:14`)
2. TODO без автора: `// TODO @a.shatalov: refactor` (`Assets/Scripts/Application/Game.cs:28`)
3. Маркировка пустых методов: `//empty` (`Assets/Scripts/View/Base/BaseVisual.cs:11`)

### Атрибуты Unity
- `[SerializeField]` -- для инспекторных полей MonoBehaviour
- `[Space]` и `[Header]` -- для группировки в инспекторе (`Assets/Scripts/Configs/GameData.cs`, `Assets/Scripts/View/ScoreVisual.cs`)
- `[CreateAssetMenu]` -- для ScriptableObject конфигов
- `[PublicAPI]` (JetBrains) -- для подавления предупреждений о неиспользуемых приватных методах (см. `PlayerInput.cs:35,42,48,54,59,64`)

### Логирование
`Debug.Log` / `Debug.LogError` с префиксом `[ClassName]`:
```csharp
Debug.LogError($"[LeaderboardService] Initialization failed: {result.Error}");
Debug.Log($"[LeaderboardService] Signed in. PlayerId: {_auth.PlayerId}");
Debug.Log("Scene size: " + sceneWidth + " x " + sceneHeight);
```

---

## Anti-Patterns to Avoid

### Жёстко закодированные магические числа
- `ShootToSystem.cs:17`: скорость пули `20` -- жёстко в формуле, не из конфига
- `MoveToComponent`: `Every = 3f` задаётся в `EntitiesCatalog.cs:149`, не в конфиге
- `Game.cs:185`: максимальная скорость осколков `10f` -- жёстко в коде
- `Game.cs:115`: начальный размер астероида `3` -- жёстко в коде
- `LaserSystem/GunSystem`: буфер raycast `30` -- жёстко в `Game.cs:220`

### switch expressions
Используются в одном месте (`EntitiesCatalog.CreateAsteroid:96-102`):
```csharp
var data = size switch {
    3 => _configs.AsteroidBig,
    2 => _configs.AsteroidMedium,
    1 => _configs.AsteroidSmall,
    var _ => throw new InvalidDataException()
};
```

### Ternary / null-coalescing
Используются: `?:`, `?.`, `??`, `??=` там, где уместны.
