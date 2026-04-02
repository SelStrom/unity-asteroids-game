# Conventions

**Дата анализа:** 2026-03-26

## Naming

### Классы и интерфейсы
- Классы — `PascalCase`: `ShipModel`, `GunSystem`, `GameObjectPool`, `EntitiesCatalog`
- Интерфейсы — `PascalCase` с префиксом `I`: `IGameEntityModel`, `IModelSystem`, `IEntityView`, `IGroupVisitor`
- Абстрактные классы — `Abstract` + суть: `AbstractScreen`, `AbstractViewModel`, `AbstractWidgetView<T>`
- Enum — `PascalCase`: `State`, значения тоже `PascalCase`: `Default`, `Game`, `EndGame`

### Методы
- `PascalCase` для публичных и защищённых методов: `AddEntity`, `Update`, `CleanUp`, `PlaceWithinGameArea`
- `PascalCase` для приватных методов: `OnRotationChanged`, `SpawnAsteroid`, `FetchAndShowLeaderboard`
- Методы-обработчики событий — префикс `On` + существительное: `OnUpdate`, `OnAttack`, `OnShipCollided`, `OnEntityDestroyed`
- Корутины — `Run` или `Routine` суффикс: `RunInitialize`, `FetchAndShowLeaderboardRoutine`, `SubmitAndShowLeaderboardRoutine`

### Поля и свойства
- Приватные поля — префикс `_` + `camelCase`: `_killed`, `_model`, `_gameObjectPool`, `_typeToSystem`
- Публичные `readonly` поля компонентов — `PascalCase` без префикса: `Position`, `Speed`, `IsActive`, `CurrentShoots`
- Свойства — `PascalCase`: `Score`, `GameArea`, `ActionScheduler`, `IsSuccess`
- `[SerializeField]` поля — `_` + `camelCase`: `_configs`, `_hudVisual`, `_spriteRenderer`
- Константы — `PascalCase`: `PlayerNameKey`, `MinSpeed`

### Параметры и локальные переменные
- `camelCase`: `deltaTime`, `shipPosition`, `prefabId`, `gameObject`

### Дженерики
- Один символ `T` или `T` + описание: `TSystem`, `TModel`, `TNode`, `TComponent`, `TView`, `TData`

---

## File Organization

### Структура директорий
```
Assets/Scripts/
├── Model/
│   ├── Components/     — data-классы компонентов (MoveComponent, GunComponent и т.д.)
│   ├── Entities/       — модели сущностей (ShipModel, AsteroidModel, BulletModel и т.д.)
│   ├── Systems/        — системы обработки (MoveSystem, GunSystem, LaserSystem и т.д.)
│   ├── Model.cs        — центральный класс игровой модели
│   └── ActionScheduler.cs
├── View/
│   ├── Base/           — BaseVisual, IEntityView
│   ├── Components/     — переиспользуемые UI-компоненты (GuiText)
│   ├── Bindings/       — расширения для data bindings (BindingToExtensions)
│   ├── *Visual.cs      — визуальные представления сущностей
│   └── *ViewModel.cs   — вместе с Visual в одном файле
├── Application/
│   ├── Leaderboard/    — сервис таблицы лидеров, прокси-интерфейсы и Unity-реализации
│   ├── Screens/        — экраны (GameScreen, TitleScreen, AbstractScreen)
│   ├── Game.cs         — игровая логика и оркестрация
│   ├── Application.cs  — точка входа приложения (не MonoBehaviour)
│   ├── ApplicationEntry.cs — MonoBehaviour-обёртка над Application
│   ├── EntitiesCatalog.cs  — реестр сущностей, фабрики и их связи
│   ├── ModelFactory.cs
│   └── ViewFactory.cs
├── Configs/            — ScriptableObject конфиги (GameData и вложенные structs)
├── Input/              — PlayerInput, PlayerActions (включая сгенерированный код)
│   └── Generated/      — авто-сгенерированные файлы Input System
└── Utils/              — GameObjectPool, GameUtils, CoroutineResult
```

### Один класс — один файл
Правило соблюдается повсеместно. Исключение: ViewModel и Visual одной сущности объединены в один файл (например, `ShipVisual.cs` содержит `ShipViewModel` и `ShipVisual`).

### Namespace
Единственный namespace для всего проекта: `SelStrom.Asteroids`.
Исключение — компоненты модели используют отдельный `namespace Model.Components` (`MoveComponent.cs`, `GunComponent.cs`, `LaserComponent.cs`, `ThrustComponent.cs`).
Конфиги — `namespace SelStrom.Asteroids.Configs`.
Биндинги — `namespace SelStrom.Asteroids.Bindings`.
Namespace папок `Model`, `View`, `Application` пропущены в DotSettings ReSharper (`NamespaceFoldersToSkip`).

---

## Code Style

### Фигурные скобки
Открывающая скобка для методов, свойств и классов — на **новой строке** (Allman/BSD style).
Открывающая скобка для `if`, `else`, `switch`, `for`, `foreach`, `while` — **на той же строке** (K&R), что закреплено в `.editorconfig` (`csharp_new_line_before_open_brace = control_blocks,local_functions,methods`).
Тело `case` в `switch` без фигурных скобок.

```csharp
// Методы — скобка на новой строке
public void Update(float deltaTime)
{
    // ...
}

// Управляющие конструкции — скобка на новой строке
if (_newEntities.Any())
{
    // ...
}
foreach (var system in _systems) 
{
    system.Update(deltaTime);
}
```

### Отступы
4 пробела (настройка `.editorconfig`).

### Паттерн Connect/Dispose
Классы НЕ используют конструкторы для внедрения зависимостей от Unity-объектов. Вместо этого — метод `Connect(...)` для инициализации и `Dispose()` / `CleanUp()` для очистки. Конструкторы используются только для pure C# зависимостей.

```csharp
// Правильно
public void Connect(GameData configs, ModelFactory modelFactory, ViewFactory viewFactory) { ... }
public void Dispose() { ... }
public void CleanUp() { ... }
```

### Паттерн null-сброс при Dispose
В методах `Dispose()` поля явно обнуляются:
```csharp
public void Dispose()
{
    _catalog = null;
    _gameObjectPool = null;
    _model = null;
    // ...
}
```

### Event подписка/отписка
Всегда симметричная: подписка через `+=`, отписка через `-=`, обязательно в парном методе.

### Работа с `Action`-делегатами как поля
В компонентах модели (`GunComponent`, `LaserComponent`) `Action`-поля объявляются как **публичные поля** (не свойства, не события):
```csharp
public Action<GunComponent> OnShooting;
```

### switch expressions (C# 8+)
Используются в одном месте (`EntitiesCatalog.CreateAsteroid`):
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

### Логирование
`Debug.Log` / `Debug.LogError` напрямую. Логи обёрнуты в префикс `[ClassName]`:
```csharp
Debug.LogError($"[LeaderboardService] Initialization failed: {result.Error}");
Debug.Log($"[LeaderboardService] Signed in. PlayerId: {_auth.PlayerId}");
```

---

## Documentation

### Комментарии
Docstring-комментарии (XML `/// <summary>`) в проекте **не используются**.
Используются строчные `//` комментарии только для:
1. TODO-пометок с автором: `// TODO @a.shatalov: model pool`
2. Пояснений к нетривиальным решениям: `//TODO theoretically it can be added during update`
3. Маркировки намеренно пустых методов: `//empty`

```csharp
protected virtual void OnConnected()
{
    //empty
}
```

### Атрибуты Unity
`[SerializeField]` — для инспекторных полей MonoBehaviour.
`[Space]` и `[Header]` — для структурирования GameData в инспекторе.
`[CreateAssetMenu]` — для ScriptableObject конфигов.
`[PublicAPI]` (JetBrains) — для явной пометки публичного API.

---

## Patterns to Follow

### MVVM (через пакет `com.shtl.mvvm`)
Каждая сущность имеет:
- **Model** — pure C# данные, `IGameEntityModel` (`ShipModel`, `AsteroidModel`)
- **ViewModel** — `AbstractViewModel` с `ReactiveValue<T>` / `ObservableValue<T>` полями (`ShipViewModel`, `HudData`)
- **View** — `AbstractWidgetView<TViewModel>` или `BaseVisual<TData>` MonoBehaviour (`ShipVisual`, `HudVisual`)

### ECS-подобные системы
Системы наследуют `BaseModelSystem<TNode>`. TNode — компонент или tuple компонентов. Регистрация через `Model.RegisterSystem<TSystem>()`.

### Visitor-паттерн для dispatch сущностей
`IGroupVisitor` с перегрузками `Visit(XxxModel)`. Каждая модель реализует `AcceptWith(IGroupVisitor visitor)`. Используется для регистрации сущностей в системах.

### Object Pool
`GameObjectPool` — для переиспользования GameObject. Доступ через `ViewFactory.Get<TView>(prefab)` / `ViewFactory.Release(view)`.

### Proxy-интерфейсы для внешних сервисов
Внешние Unity-сервисы (Auth, Leaderboard) скрыты за интерфейсами `IAuthProxy`, `ILeaderboardProxy`. Реальные реализации: `UnityAuthProxy`, `UnityLeaderboardProxy`.

### CoroutineResult для async-операций
Вместо async/await — корутины с передачей `CoroutineResult` / `CoroutineResult<T>` как out-параметр:
```csharp
var result = new CoroutineResult<List<LeaderboardEntry>>();
yield return _leaderboardService.GetTopScores(result);
if (!result.IsSuccess) { ... }
```

### Конфиги через ScriptableObject
Все игровые параметры — в `GameData` (`Assets/Scripts/Configs/GameData.cs`). Вложенные `[Serializable] struct` для группировки: `BulletData`, `ShipData`, `LaserData`.

### Struct для передачи данных в View
Данные для экранов передаются через struct: `GameScreenData`.
