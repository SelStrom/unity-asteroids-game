# Architecture

**Дата анализа:** 2026-04-02

## Общий дизайн

Проект реализует аркадную игру «Астероиды» на Unity (C#). Архитектура строится на **кастомном ECS-подобном подходе в связке с MVVM** для отображения.

Ключевые принципы:
- Игровая логика полностью отделена от Unity: модели и системы — чистые C#-классы без `MonoBehaviour`.
- Единственная точка входа в Unity-стек — `ApplicationEntry` (MonoBehaviour). Все остальные объекты создаются вручную через `new`.
- Связывание модель↔вью реализовано через реактивные значения (`ObservableValue`, `ReactiveValue`) из библиотеки `Shtl.Mvvm`.
- Конфигурация хранится в ScriptableObject-ассетах.

## Архитектурные слои

```
┌─────────────────────────────────────────────────────┐
│  Unity MonoBehaviour (ApplicationEntry)              │
│  ─ OnUpdate/OnPause/OnResume → C#-события           │
├─────────────────────────────────────────────────────┤
│  Application Layer                                   │
│  ─ Application: владеет Pool, Catalog, Model, Game   │
│  ─ Game: игровой процесс, спаун, коллизии            │
│  ─ EntitiesCatalog: фабрика+реестр Model↔View        │
│  ─ Screens: TitleScreen, GameScreen (MVVM)           │
├─────────────────────────────────────────────────────┤
│  Model Layer (ECS-подобное ядро)                     │
│  ─ Model: реестр сущностей + ordered systems          │
│  ─ Components: MoveComponent, ThrustComponent, ...    │
│  ─ Systems: MoveSystem, ThrustSystem, GunSystem, ...  │
│  ─ Entities: ShipModel, AsteroidModel, UfoBigModel, . │
│  ─ ActionScheduler: отложенные действия               │
├─────────────────────────────────────────────────────┤
│  View Layer (MVVM)                                   │
│  ─ ViewModels: ShipViewModel, AsteroidViewModel, ...  │
│  ─ Visuals: ShipVisual, AsteroidVisual, ... (MonoB.)  │
│  ─ Bindings: ObservableValue → ReactiveValue → UI     │
├─────────────────────────────────────────────────────┤
│  Config Layer                                        │
│  ─ GameData (ScriptableObject) → ассеты              │
└─────────────────────────────────────────────────────┘
```

## Основные системы

### 1. Слой приложения (`Assets/Scripts/Application/`)

**`ApplicationEntry`** (`ApplicationEntry.cs`)
- Единственный `MonoBehaviour` в игровом коде (не считая визуалов).
- Реализует `IApplicationComponent`: пробрасывает `Update`, `OnPause`, `OnResume` как C#-события.
- Создаёт `Application`, `LeaderboardService`, `GameScreen`, `TitleScreen` в `Awake`.
- `[SerializeField] GameData _configs` — точка вброса конфига из сцены.

**`Application`** (`Application.cs`)
- Корневой объект приложения (чистый C#).
- Владеет `GameObjectPool`, `EntitiesCatalog`, `Model`, `Game`, экранами.
- Вычисляет GameArea из камеры: `sceneWidth = camera.aspect * orthographicSize * 2`.
- Подписывается на `IApplicationComponent.OnUpdate` и вызывает `Model.Update(deltaTime)` каждый кадр.

**`Game`** (`Game.cs`)
- Управляет игровым процессом: старт, стоп, рестарт.
- Спаунит корабль, астероиды, UFO через `EntitiesCatalog`.
- Обрабатывает коллизии, уничтожение сущностей, логику разбивания астероидов на части.
- Расписывает таймер появления новых врагов через `ActionScheduler`.

**`EntitiesCatalog`** (`EntitiesCatalog.cs`)
- Фабрика и реестр всех игровых сущностей.
- Хранит двунаправленные словари: модель↔вью, `GameObject`↔модель, модель↔bindings.
- При создании каждой сущности создаёт модель (`ModelFactory`), вью (`ViewFactory`) и `EventBindingContext` с привязками реактивных значений.
- При уничтожении (`Release`) снимает привязки и возвращает объекты в пул.

### 2. Модельный слой — ECS-ядро (`Assets/Scripts/Model/`)

**`Model`** (`Model.cs`) — центральный класс, владеет всеми сущностями и системами.

**Регистрация систем (порядок важен!):**
```
RotateSystem → ThrustSystem → MoveSystem → LifeTimeSystem → GunSystem → LaserSystem → ShootToSystem → MoveToSystem
```
Порядок гарантирует, что rotation обновится до thrust, thrust до move, и т.д.

**Visitor-паттерн для регистрации компонентов:** `Model.GroupCreator` реализует `IGroupVisitor` и привязывает компоненты модели к нужным системам через `AcceptWith`. Каждый тип сущности регистрируется в своём наборе систем:
- `ShipModel` → MoveSystem, RotateSystem, GunSystem, LaserSystem, ThrustSystem
- `AsteroidModel` → MoveSystem
- `BulletModel` → MoveSystem, LifeTimeSystem
- `UfoBigModel` → MoveSystem, GunSystem, ShootToSystem
- `UfoModel` → MoveSystem, GunSystem, ShootToSystem, MoveToSystem

**Цикл обновления** (`Model.Update`, `Model.cs:124-154`):
1. `ActionScheduler.Update(deltaTime)` — отложенные действия
2. Новые сущности из `_newEntities` переносятся в `_entities`, регистрируются через Visitor
3. Все системы обновляются по порядку: `foreach (var system in _systems) system.Update(deltaTime)`
4. Мёртвые сущности (`IsDead()`) удаляются из систем, вызывается `OnEntityDestroyed`
5. `_entities.RemoveWhere(x => x.IsDead())`

### 3. Системы компонентов — подробно

Все системы наследуют `BaseModelSystem<TNode>`, хранят `Dictionary<IGameEntityModel, TNode>` и итерируют по `.Values`.

#### ThrustSystem (`ThrustSystem.cs`) — физика ускорения корабля

**Алгоритм (строка 9-22):**

Когда тяга активна (`IsActive == true`):
1. Вычисляет ускорение: `acceleration = UnitsPerSecond * deltaTime`
2. Строит вектор скорости: `velocity = текущее_направление × текущая_скорость + вектор_поворота × ускорение`
3. Новое направление = `velocity.normalized`
4. Новая скорость = `min(velocity.magnitude, MaxSpeed)`

Когда тяга неактивна:
1. Скорость уменьшается: `speed -= UnitsPerSecond / 2 * deltaTime` (торможение вдвое медленнее ускорения)
2. Ограничение снизу: `max(speed, 0)`

**Особенность:** направление и скорость хранятся раздельно в `MoveComponent`. Тяга складывает вектор текущей скорости и вектор ускорения в направлении поворота, что даёт плавные повороты в стиле классических Asteroids.

**Edge case:** при развороте на 180° и равных magnitude текущей скорости и ускорения результирующий `velocity` может стать нулевым вектором — `normalized` от нуля даст `(0,0)`, и направление потеряется.

#### RotateSystem (`RotateSystem.cs`) — вращение корабля

**Алгоритм (строка 8-18):**
1. Если `TargetDirection == 0` — ничего не делать (нет ввода)
2. Создать кватернион вращения на `90° × deltaTime × direction` вокруг оси Z
3. Умножить на текущий `Rotation.Value` (Vector2) — кватернион вращает 2D-вектор

**Скорость вращения:** фиксированная, 90°/сек (`RotateComponent.DegreePerSecond = 90`).

#### MoveSystem (`MoveSystem.cs`) — перемещение + тороидальный экран

**Алгоритм (строка 14-21):**
1. `newPosition = oldPosition + direction × speed × deltaTime`
2. `PlaceWithinGameArea(ref position.x, gameArea.x)`
3. `PlaceWithinGameArea(ref position.y, gameArea.y)`

**Тороидальная обёртка** (`Model.PlaceWithinGameArea`, `Model.cs:156-167`):
```csharp
if (position > side / 2)
    position = -side + position;  // ⚠ баг: должно быть position - side
if (position < -side / 2)
    position = side - position;   // ⚠ баг: должно быть position + side
```

**Известный баг:** формула для выхода за левую/нижнюю границу (`position = side - position`) инвертирует знак позиции вместо корректного wraparound. При `position = -6` и `side = 10` результат = `10 - (-6) = 16`, что находится далеко за правой границей. Корректная формула: `position = position + side`.

#### GunSystem (`GunSystem.cs`) — механика перезарядки и стрельбы

**Алгоритм (строка 7-26):**
1. Если `CurrentShoots < MaxShoots` — идёт перезарядка:
   - `ReloadRemaining -= deltaTime`
   - Когда `ReloadRemaining <= 0`: сбросить таймер, установить `CurrentShoots = MaxShoots`
   - **Особенность:** перезарядка восстанавливает ВСЕ выстрелы сразу, а не по одному
2. Если `Shooting && CurrentShoots > 0`:
   - `CurrentShoots--`
   - Вызвать `OnShooting` callback
3. Сбросить `Shooting = false` (событийная модель: один кадр — один выстрел)

#### LaserSystem (`LaserSystem.cs`) — лазер с инкрементальной перезарядкой

**Алгоритм (строка 7-26):**
Аналогичен GunSystem, но:
- `CurrentShoots` и `ReloadRemaining` — `ObservableValue<>` (реактивные, связаны с HUD)
- Перезарядка восстанавливает **по одному** выстрелу за период (`CurrentShoots += 1`)
- Стрельба тратит **по одному** (`CurrentShoots -= 1`)

#### ShootToSystem (`ShootToSystem.cs`) — предиктивное прицеливание UFO

**Алгоритм предсказания позиции игрока (строка 7-25):**
1. Если `CurrentShoots <= 0` — нет патронов, не стрелять
2. Вычислить расстояние до корабля: `distance = (shipPos - ufoPos).magnitude`
3. Вычислить время подлёта пули: `time = distance / (20 - shipSpeed)`
   - **20** — хардкод скорости пули (должна быть `configs.Bullet.Speed`)
   - ⚠ **Division by zero**: если `shipSpeed == 20`, будет деление на ноль
4. Предсказать будущую позицию корабля: `pendingPos = shipPos + shipDirection × shipSpeed × time`
5. Направить пулю: `direction = (pendingPos - ufoPos).normalized`
6. Установить `Gun.Shooting = true` (GunSystem обработает на этом же кадре)

**Проблема алгоритма:** при `shipSpeed > 20` время становится отрицательным, и UFO будет целиться в обратную сторону от корабля.

#### MoveToSystem (`MoveToSystem.cs`) — перехват корабля малым UFO

**Алгоритм перехвата (строка 7-23):**
1. Таймер `ReadyRemaining` уменьшается каждый кадр
2. Когда `ReadyRemaining <= 0` (каждые 3 сек для UFO):
   - Сбросить таймер
   - `time = distanceToShip / (ufoSpeed - shipSpeed)`
   - ⚠ **Division by zero**: если скорости равны
   - Предсказать позицию корабля: `pendingPos = shipPos + shipDir × shipSpeed × time`
   - Направить UFO к предсказанной точке

**Ключевое отличие от ShootToSystem:** делит на разницу скоростей объектов (UFO vs корабль), а не на разницу скорости пули и корабля. Формула рассчитывает время перехвата при сближении.

#### LifeTimeSystem (`LifeTimeSystem.cs`) — время жизни пуль

Простой таймер: `TimeRemaining = max(TimeRemaining - deltaTime, 0)`. Когда достигает 0, `BulletModel.IsDead()` возвращает `true`.

### 4. ActionScheduler — отложенные действия

**Алгоритм** (`ActionScheduler.cs:31-57`):
1. `_nextUpdateDuration` — оптимизация: не итерировать список, пока не пришло время ближайшего действия
2. `_secondsSinceLastUpdate` — аккумулятор времени с последней обработки
3. При обработке (обратный цикл `for (i = Count-1; i >= 0; i--)`):
   - Вычитает накопленное время из Duration каждого entry
   - Если Duration > 0 — обновляет `_nextUpdateDuration`
   - Если Duration ≤ 0 — **swap-with-last** удаление (O(1) вместо O(n)):
     ```
     entries[i] = entries[last]; entries.RemoveAt(last);
     ```
   - Вызывает `entry.Action()` ПОСЛЕ удаления из списка
4. Сбрасывает `_secondsSinceLastUpdate = 0`

**Проблема:** действие может добавить новые entry через `ScheduleAction` во время итерации (TODO в коде, строка 28). SpawnNewEnemy делает именно это — reschedule себя. Swap-with-last + обратный цикл частично защищает, но теоретически race condition возможен.

### 5. Управление жизненным циклом сущностей

**Создание:**
1. `EntitiesCatalog.CreateXxx()` → `ModelFactory.Get<T>()` → `new T()` + `Model.AddEntity()`
2. Модель попадает в `_newEntities`
3. На следующем `Model.Update()` переносится в `_entities` и регистрируется через Visitor

**Уничтожение** (`Game.Kill()`, `Game.cs:170-196`):
1. `entityModel.Kill()` → устанавливает `_killed = true`
2. Для астероидов — алгоритм дробления:
   - `age = asteroidAge - 1`
   - Если `age > 0`: создать **2** новых астероида меньшего размера
   - Скорость осколков: `min(originalSpeed × 2, 10f)` — удвоение с потолком 10
3. Для любой сущности — `PlayEffect` (взрыв из пула)
4. В `Model.Update()`: `IsDead()` → удаление из систем → `OnEntityDestroyed` → `EntitiesCatalog.Release`

**Иерархия размеров астероидов:** 3 (big) → 2 (medium) → 1 (small) → уничтожен

### 6. Коллизии и подсчёт очков

**Пуля игрока vs астероид** (`Game.OnUserBulletCollided`, `Game.cs:128-144`):
1. Kill пули
2. Отключить коллайдер цели (`col.otherCollider.enabled = false`)
3. Найти модель по `GameObject` через `EntitiesCatalog.TryFindModel<T>`
4. `ReceiveScore` — добавить очки (Score = Score + entity.Data.Score)
5. Kill астероида (запускает дробление)

**Пуля игрока vs UFO** (`Game.cs:140-143`):
- ⚠ **Баг:** `ReceiveScore` вызывается, но `Kill` для UFO **НЕ вызывается**. UFO остаётся бессмертным после попадания пули.

**Лазер** (`Game.OnUserLaserShooting`, `Game.cs:210-238`):
1. Создать визуальный эффект (LineRenderer из пула)
2. Установить позицию и поворот по направлению корабля
3. Запланировать удаление эффекта через `ActionScheduler`
4. `Physics2D.RaycastNonAlloc` — лучевой кастинг по слоям "Asteroid" и "Enemy"
5. Для каждого попадания: `ReceiveScore` + `Kill`
6. Дальность: `GameArea.magnitude` (диагональ игровой области)

**Столкновение корабля** (`Game.OnShipCollided`, `Game.cs:122-126`):
- Kill корабля + Stop (конец игры)
- Не различает тип столкновения (астероид/UFO/пуля)

### 7. Спаун врагов

**Периодический спаун** (`Game.SpawnNewEnemy`, `Game.cs:77-93`):
1. `Random.Range(0, 3)` — равновероятный выбор: астероид, UFO малый, UFO большой
2. Рекурсивная перепланировка через `ActionScheduler` с фиксированным интервалом `SpawnNewEnemyDurationSec`

**Позиционирование астероидов** (`GameUtils.GetRandomAsteroidPosition`, `GameUtils.cs:24-37`):
1. Случайная позиция в пределах GameArea
2. Вычисление расстояния до корабля
3. Если расстояние < `SpawnAllowedRadius` — сдвиг **к кораблю** (баг: `position += distance.normalized * allowedDistance`, где `allowedDistance < 0`)

**Позиционирование UFO** (`GameUtils.GetRandomUfoPosition`, `GameUtils.cs:9-22`):
1. Всегда `x = 0` (после вычитания gameArea*0.5 → левый край экрана)
2. Случайный `y` в пределах gameArea
3. Проверка расстояния по вертикали до корабля
4. ⚠ Division by zero при `verticalDistance == 0`

**Направление UFO при спауне:**
- Малый UFO: `Random.insideUnitCircle.normalized` — в любую сторону
- Большой UFO: `(Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized` — преимущественно горизонтально (y-компонента подавлена в 10×)

### 8. Машина состояний игры

```
TitleScreen (кнопка Start)
    ↓
Game.Start() → создание корабля, спаун врагов, подключение ввода
    ↓
[игровой цикл: Model.Update каждый кадр]
    ↓
Game.Stop() → при гибели корабля → отключение ввода, сброс ActionScheduler
    ↓
GameScreen.EndGame → показ очков, лидерборд
    ↓
Game.Restart() → Model.CleanUp() → Game.Start() (новый цикл)
```

### 9. MVVM-связывание

**Паттерн создания entity с биндингами** (`EntitiesCatalog.CreateShip`, `EntitiesCatalog.cs:45-73`):
1. Создать модель через `ModelFactory`
2. Создать `ViewModel` (чистый C#)
3. Создать `EventBindingContext` (управляет подписками)
4. Связать: `bindings.From(model.Move.Position).To(viewModel.Position)` — ObservableValue → ReactiveValue
5. `bindings.InvokeAll()` — выполнить начальную синхронизацию
6. Создать View из пула, подключить ViewModel
7. Сохранить в каталог

**Специальные привязки:**
- Thrust sprite: `From(model.Thrust.IsActive).To(viewModel.Sprite, ...)` — переключение спрайта по булевому значению через лямбда-адаптер
- Rotation: View сам конвертирует Vector2 → угол через `Atan2` и устанавливает `Quaternion.Euler`
- Position: расширение `BindingToExtensions.To(transform)` — копирует x,y из Vector2 в Transform.position

### 10. Leaderboard — лучший результат

**Алгоритм** (`GameScreen.SubmitAndShowLeaderboardRoutine`, `GameScreen.cs:225-289`):
1. Загрузить текущий лучший результат игрока: `GetPlayerScore`
2. `bestScore = max(serverScore ?? 0, currentScore)`
3. Отправить `bestScore` (а не текущий) — гарантирует что на сервере всегда максимум
4. Загрузить топ-10
5. Загрузить персональный ранг повторно (мог измениться после submit)
6. Защита от stale coroutine: `if (_score.ViewModel != viewModel) yield break` — если пользователь рестартнул игру, ранее запущенная корутина прекращается
