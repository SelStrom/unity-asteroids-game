# Architecture

**Дата анализа:** 2026-03-26

## Общий дизайн

Проект реализует аркадную игру «Астероиды» на Unity (C#). Архитектура строится на **кастомном ECS-подобном подходе в связке с MVVM** для отображения.

Ключевые принципы:
- Игровая логика полностью отделена от Unity: модели и системы — чистые C#-классы без `MonoBehaviour`.
- Единственная точка входа в Unity-стек — `ApplicationEntry` (MonoBehaviour). Все остальные объекты создаются вручную через `new`.
- Связывание модель↔вью реализовано через реактивные значения (`ObservableValue`, `ReactiveValue`) из библиотеки `Shtl.Mvvm`.
- Конфигурация хранится в ScriptableObject-ассетах.

## Основные системы

### 1. Слой приложения (`Assets/Scripts/Application/`)

**`ApplicationEntry`** (`Application/ApplicationEntry.cs`)
- Единственный `MonoBehaviour` в игровом коде (не считая визуалов).
- Реализует `IApplicationComponent`: пробрасывает `Update`, `OnPause`, `OnResume` как C#-события.
- Создаёт `Application`, `LeaderboardService`, `GameScreen`, `TitleScreen` в `Awake`.
- Поле `[SerializeField] GameData _configs` — точка вброса конфига из сцены.

**`Application`** (`Application/Application.cs`)
- Корневой объект приложения (чистый C#).
- Владеет `GameObjectPool`, `EntitiesCatalog`, `Model`, `Game`, экранами.
- Подписывается на `IApplicationComponent.OnUpdate` и вызывает `Model.Update(deltaTime)` каждый кадр.

**`Game`** (`Application/Game.cs`)
- Управляет игровым процессом: старт, стоп, рестарт.
- Спаунит корабль, астероиды, UFO через `EntitiesCatalog`.
- Обрабатывает коллизии, уничтожение сущностей, логику разбивания астероидов на части.
- Расписывает таймер появления новых врагов через `ActionScheduler`.

**`EntitiesCatalog`** (`Application/EntitiesCatalog.cs`)
- Фабрика и реестр всех игровых сущностей.
- Хранит двунаправленные словари: модель↔вью, `GameObject`↔модель.
- При создании каждой сущности создаёт модель (`ModelFactory`), вью (`ViewFactory`) и `EventBindingContext` с привязками реактивных значений.
- При уничтожении (`Release`) снимает привязки и возвращает объекты в пул.

**`ModelFactory`** (`Application/ModelFactory.cs`)
- Создаёт экземпляр модели через `new TModel()` и регистрирует его в `Model` (`Model.AddEntity`).
- Задел под пул моделей (TODO в коде).

**`ViewFactory`** (`Application/ViewFactory.cs`)
- Обёртка над `GameObjectPool`: выдаёт `GameObject` из пула по prefab, возвращает обратно при `Release`.

### 2. Модельный слой — ECS-ядро (`Assets/Scripts/Model/`)

**`Model`** (`Model/Model.cs`)
- Реестр всех систем и сущностей.
- Регистрирует системы в конструкторе в строгом порядке: `RotateSystem`, `ThrustSystem`, `MoveSystem`, `LifeTimeSystem`, `GunSystem`, `LaserSystem`, `ShootToSystem`, `MoveToSystem`.
- В `Update(deltaTime)` каждый кадр:
  1. Обновляет `ActionScheduler`.
  2. Регистрирует новые сущности в системах через Visitor (`IGroupVisitor`).
  3. Итерирует все системы (`system.Update`).
  4. Вызывает `OnEntityDestroyed` для мёртвых сущностей и удаляет их.
- Хранит игровое поле (`GameArea: Vector2`) и счёт (`Score`).

**Сущности** (`Model/Entities/`)
- `IGameEntityModel` — интерфейс: `IsDead()`, `Kill()`, `AcceptWith(IGroupVisitor)`.
- Конкретные сущности: `ShipModel`, `AsteroidModel`, `BulletModel`, `UfoBigModel`, `UfoModel`.
- Каждая сущность содержит компоненты как поля-объекты (composition), не наследование.
- `UfoModel` наследует `UfoBigModel`, добавляя `MoveToComponent` (движение к кораблю).

**Компоненты** (`Model/Components/`)
- `MoveComponent` — позиция (`ObservableValue<Vector2>`), скорость (`ObservableValue<float>`), направление.
- `RotateComponent` — направление вращения, цель.
- `ThrustComponent` — флаг тяги (`ObservableValue<bool>`), параметры ускорения.
- `GunComponent` — состояние обычного оружия, коллбэк `OnShooting`.
- `LaserComponent` — состояние лазера, реактивные значения для HUD.
- `LifeTimeComponent`, `MoveToComponent`, `ShootToComponent`.

**Системы** (`Model/Systems/`)
- Базовый класс `BaseModelSystem<TNode>`: хранит `Dictionary<IGameEntityModel, TNode>`, итерирует в `Update`.
- Каждая система получает только нужные ей компоненты (или кортеж компонентов) в качестве `TNode`.
- Например, `ThrustSystem` принимает кортеж `(ThrustComponent, MoveComponent, RotateComponent)`.

**`ActionScheduler`** (`Model/ActionScheduler.cs`)
- Планировщик отложенных действий (без корутин, тикается вручную в `Model.Update`).
- Использует оптимизацию: отслеживает ближайший срабатывающий таймаут.

### 3. Слой представления — MVVM (`Assets/Scripts/View/`)

Библиотека `Shtl.Mvvm` предоставляет:
- `AbstractViewModel` — базовый класс для ViewModel.
- `AbstractWidgetView<TViewModel>` — базовый MonoBehaviour-вью, хранит `ViewModel` и `EventBindingContext Bind`.
- `ReactiveValue<T>`, `ObservableValue<T>` — реактивные обёртки для подписки.
- `EventBindingContext` — контейнер биндингов, позволяет разом снять все подписки через `CleanUp()`.

**Паттерн для каждой игровой сущности:**
1. ViewModel: `ShipViewModel`, `AsteroidViewModel`, `BulletViewModel`, `UfoViewModel` — набор `ReactiveValue<T>`.
2. Visual (MonoBehaviour): `ShipVisual`, `AsteroidVisual`, `BulletVisual`, `UfoVisual` — подписывается на ViewModel в `OnConnected()`.
3. Соединение через `view.Connect(viewModel)` в `EntitiesCatalog`.

**GUI:**
- `HudVisual` / `HudData` — данные корабля в реальном времени (координаты, скорость, лазер).
- `ScoreVisual` / `ScoreViewModel` — экран окончания игры с таблицей лидеров.
- `TitleScreenView` / `TitleScreenViewModel` — стартовый экран.

**Биндинги:**
- `BindingToExtensions` (`View/Bindings/BindingToExtensions.cs`) — расширение: `From(ReactiveValue<Vector2>).To(Transform)` обновляет позицию объекта напрямую.

### 4. Экраны (`Application/Screens/`)

- `AbstractScreen` — базовый класс, владеет `EventBindingContext`, предоставляет `CleanUp()`.
- `TitleScreen` — связывает кнопку старта с запуском `Game.Start()`.
- `GameScreen` — управляет состоянием `State.Game` / `State.EndGame`, биндит HUD к модели корабля, запускает загрузку таблицы лидеров.

### 5. Лидерборд (`Application/Leaderboard/`)

- `LeaderboardService` — сервисный класс, координирует авторизацию и отправку/получение очков через Unity Services.
- Интерфейсы `IAuthProxy`, `ILeaderboardProxy` — абстрагируют Unity Gaming Services.
- Реализации `UnityAuthProxy`, `UnityLeaderboardProxy` — вызывают Unity Authentication и Unity Leaderboards SDK.
- Все асинхронные операции реализованы через `IEnumerator`-корутины с `CoroutineResult<T>`.

### 6. Ввод (`Assets/Scripts/Input/`)

- `PlayerInput` — чистый C#-класс, оборачивает Unity Input System (generated `PlayerActions`).
- Публикует события: `OnAttackAction`, `OnRotateAction`, `OnTrustAction`, `OnLaserAction`, `OnBackAction`.
- `Game` подписывается на события ввода и записывает намерения в компоненты модели.

### 7. Утилиты (`Assets/Scripts/Utils/`)

- `GameObjectPool` — пул `GameObject` по prefab-id (Stack-based).
- `GameUtils` — вычисление случайных позиций спауна вне радиуса корабля.
- `CoroutineResult<T>` — типизированный контейнер результата для корутин-цепочек.

## Поток данных

### Игровой цикл (кадр)

```
Unity Update()
  → IApplicationComponent.OnUpdate event
    → Application.OnUpdate(deltaTime)
      → Model.Update(deltaTime)
          1. ActionScheduler.Update         — таймеры (спаун, перезарядка)
          2. регистрация новых сущностей в системах (Visitor)
          3. foreach system: system.Update   — физика, оружие, AI
          4. foreach dead entity:
               → OnEntityDestroyed event
                 → Game.OnEntityDestroyed
                   → EntitiesCatalog.Release
                     → EventBindingContext.CleanUp  — снять биндинги
                     → ViewFactory.Release          — вернуть в пул
                     → ModelFactory.Release         — (задел под пул)
```

### Ввод игрока → модель

```
Unity Input System
  → PlayerInput (C# events)
    → Game.OnAttack / OnRotate / OnTrust / OnLaser
      → ShipModel.Gun.Shooting = true   (и т.п.)
        → GunSystem.UpdateNode обнаруживает флаг
          → Gun.OnShooting callback
            → Game.OnUserGunShooting
              → EntitiesCatalog.CreateBullet
```

### Изменение модели → обновление вью

```
MoveSystem.UpdateNode
  → MoveComponent.Position.Value = newPos   (ObservableValue)
    → ReactiveValue<Vector2> в ViewModel (через EventBindingContext)
      → Transform.position обновляется (BindingToExtensions)
```

### Коллизия → уничтожение сущности

```
Unity OnCollisionEnter2D (на Visual MonoBehaviour)
  → ViewModel.OnCollision.Value?.Invoke(col)
    → Game.OnUserBulletCollided / OnShipCollided
      → entityModel.Kill()   → IsDead() = true
        → Model.Update следующего кадра обнаружит мёртвую сущность
          → OnEntityDestroyed → Release
```

## Паттерны проектирования

| Паттерн | Где используется |
|---------|-----------------|
| **ECS (упрощённый)** | `Model` + `BaseModelSystem<TNode>` + компоненты в сущностях |
| **Visitor** | `IGroupVisitor` / `IGameEntityModel.AcceptWith` — регистрация сущностей в системах |
| **MVVM** | `AbstractViewModel` + `AbstractWidgetView<T>` + `ReactiveValue<T>` для всех визуалов |
| **Observer / Reactive** | `ObservableValue<T>`, `ReactiveValue<T>`, `EventBindingContext` |
| **Object Pool** | `GameObjectPool` для всех игровых GameObject |
| **ScriptableObject Data** | `GameData`, `AsteroidData`, `UfoData`, `BaseGameEntityData` |
| **Proxy / Interface** | `IAuthProxy`, `ILeaderboardProxy`, `IApplicationComponent` |
| **Factory** | `ModelFactory`, `ViewFactory`, `EntitiesCatalog.CreateXxx` |
| **Coroutine Result** | `CoroutineResult<T>` — аналог Promise для Unity-корутин |

## Ключевые абстракции

**`IGameEntityModel`** (`Model/Entities/IGameEntityModel.cs`)
- Контракт: `IsDead()`, `Kill()`, `AcceptWith(IGroupVisitor)`.
- Реализуют: `ShipModel`, `AsteroidModel`, `BulletModel`, `UfoBigModel`, `UfoModel`.

**`IModelSystem`** (`Model/Systems/BaseModelSystem.cs`)
- Контракт: `Update(float)`, `Remove(IGameEntityModel)`, `CleanUp()`.
- Все системы наследуют `BaseModelSystem<TNode>`.

**`IEntityView`** (`View/Base/IEntityView.cs`)
- Контракт для вью-компонентов, необходим `GameObjectPool` для идентификации.

**`IApplicationComponent`** (`Application/IApplicationComponent.cs`)
- Мост между Unity `MonoBehaviour` и чистым C#-кодом приложения.
- Изолирует `Application` от прямой зависимости на Unity.

**`AbstractScreen`** (`Application/Screens/AbstractScreen.cs`)
- Базовый класс экранов с управлением жизненным циклом биндингов.

## Обработка ошибок

- Нет глобального обработчика исключений.
- Лидерборд: ошибки логируются через `Debug.LogError`, UI переходит в fallback-состояние (форма ввода имени).
- `GameObjectPool.Release` бросает `Exception` при попытке вернуть незарегистрированный объект.
- Корутинные операции передают ошибку через `CoroutineResult.Error` (паттерн без исключений).

---

*Анализ архитектуры: 2026-03-26*
