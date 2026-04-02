# Code Structure

**Дата анализа:** 2026-03-26

## Directory Layout

```
Assets/
├── Editor/                         # Unity Editor-утилиты
│   ├── LeaderboardPrefabCreator.cs # Инструмент создания prefab'ов лидерборда
│   └── AsteroidsEditor.asmdef      # Сборка только для Editor
│
├── Input/
│   └── PlayerActions.inputactions  # Конфиг Unity Input System (не в Scripts)
│
├── Media/
│   ├── configs/                    # ScriptableObject-ассеты с данными
│   │   ├── GameData.asset          # Главный конфиг (ссылки на все субконфиги)
│   │   ├── AsteroidBigData.asset
│   │   ├── AsteroidMediumData.asset
│   │   ├── AsteroidSmallData.asset
│   │   ├── UfoBigData.asset
│   │   ├── UfoData.asset
│   │   ├── UserGunData.asset
│   │   └── UfoGunData.asset
│   ├── effects/                    # VFX-ассеты (взрывы и т.п.)
│   ├── prefabs/                    # Prefab'ы игровых объектов
│   │   ├── asteroid_big.prefab
│   │   ├── asteroid_medium.prefab
│   │   ├── asteroid_small.prefab
│   │   ├── bullet.prefab
│   │   ├── bullet_enemy.prefab
│   │   ├── ship.prefab
│   │   ├── ufo.prefab
│   │   ├── ufo_big.prefab
│   │   └── gui/
│   │       ├── gui_text.prefab
│   │       └── leaderboard_entry.prefab
│   └── sprites/                    # Спрайты
│
├── Resources/                      # Ресурсы, загружаемые через Resources.Load
│
├── Scenes/
│   └── Main.unity                  # Единственная сцена
│
├── Scripts/                        # Весь C#-код игры
│   ├── Application/                # Слой приложения и точки входа
│   │   ├── ApplicationEntry.cs     # MonoBehaviour, точка входа Unity
│   │   ├── Application.cs          # Корневой C#-объект приложения
│   │   ├── Game.cs                 # Игровой процесс (старт/стоп/рестарт)
│   │   ├── EntitiesCatalog.cs      # Фабрика+реестр сущностей
│   │   ├── ModelFactory.cs         # Фабрика моделей
│   │   ├── ViewFactory.cs          # Фабрика вью (через пул)
│   │   ├── IApplicationComponent.cs
│   │   ├── Leaderboard/
│   │   │   ├── LeaderboardService.cs
│   │   │   ├── IAuthProxy.cs
│   │   │   ├── ILeaderboardProxy.cs
│   │   │   ├── LeaderboardEntry.cs
│   │   │   ├── UnityAuthProxy.cs
│   │   │   └── UnityLeaderboardProxy.cs
│   │   └── Screens/
│   │       ├── AbstractScreen.cs
│   │       ├── GameScreen.cs
│   │       └── TitleScreen.cs
│   │
│   ├── Configs/                    # ScriptableObject-классы конфигов
│   │   ├── BaseGameEntityData.cs   # Абстрактный SO (Score)
│   │   ├── AsteroidData.cs
│   │   ├── BulletData.cs
│   │   ├── GameData.cs             # Главный конфиг-SO
│   │   ├── GunData.cs
│   │   └── UfoData.cs
│   │   └── Configs.asmdef          # Сборка «Conf»
│   │
│   ├── Input/
│   │   ├── PlayerInput.cs          # C#-обёртка над Unity Input System
│   │   ├── PlayerActions.cs        # Ручная часть partial-класса PlayerActions
│   │   └── Generated/
│   │       └── PlayerActions.cs    # Авто-генерированный класс Input System
│   │
│   ├── Model/                      # ECS-ядро, чистый C# (без UnityEngine кроме Vector2)
│   │   ├── Model.cs                # Реестр систем и сущностей
│   │   ├── ActionScheduler.cs      # Планировщик таймеров
│   │   ├── Components/             # Компоненты (данные)
│   │   │   ├── IModelComponent.cs
│   │   │   ├── MoveComponent.cs
│   │   │   ├── RotateComponent.cs
│   │   │   ├── ThrustComponent.cs
│   │   │   ├── GunComponent.cs
│   │   │   ├── LaserComponent.cs
│   │   │   ├── LifeTimeComponent.cs
│   │   │   ├── MoveToComponent.cs
│   │   │   └── ShootToComponent.cs
│   │   ├── Entities/               # Сущности (наборы компонентов)
│   │   │   ├── IGameEntityModel.cs
│   │   │   ├── IGroupVisitor.cs
│   │   │   ├── ShipModel.cs
│   │   │   ├── AsteroidModel.cs
│   │   │   ├── BulletModel.cs
│   │   │   └── UfoBigModel.cs      # Содержит также UfoModel (вложен в файл)
│   │   └── Systems/                # Системы (логика обновления компонентов)
│   │       ├── BaseModelSystem.cs  # + interface IModelSystem
│   │       ├── MoveSystem.cs
│   │       ├── RotateSystem.cs
│   │       ├── ThrustSystem.cs
│   │       ├── GunSystem.cs
│   │       ├── LaserSystem.cs
│   │       ├── LifeTimeSystem.cs
│   │       ├── MoveToSystem.cs
│   │       └── ShootToSystem.cs
│   │
│   ├── Utils/
│   │   ├── CoroutineResult.cs      # Типизированный результат корутины
│   │   ├── GameObjectPool.cs       # Пул GameObject по prefab-id
│   │   └── GameUtils.cs            # Вычисление позиций спауна
│   │
│   └── View/                       # MVVM-слой отображения
│       ├── Base/
│       │   ├── BaseVisual.cs       # Абстрактный MonoBehaviour-вью
│       │   └── IEntityView.cs      # Интерфейс для вью игровых сущностей
│       ├── Bindings/
│       │   └── BindingToExtensions.cs  # Extension-методы для биндингов
│       ├── Components/
│       │   └── GuiText.cs
│       ├── AsteroidVisual.cs       # AsteroidViewModel + AsteroidVisual
│       ├── BulletVisual.cs
│       ├── EffectVisual.cs
│       ├── HudVisual.cs            # HudData + HudVisual
│       ├── LeaderboardEntryVisual.cs
│       ├── ScoreVisual.cs          # ScoreViewModel + ScoreVisual
│       ├── ShipVisual.cs           # ShipViewModel + ShipVisual
│       ├── TitleScreenView.cs      # TitleScreenViewModel + TitleScreenView
│       └── UfoVisual.cs
│
├── TextMesh Pro/                   # Ассеты TextMeshPro (шрифты, материалы)
│
└── Asteroids.asmdef                # Главная сборка игры
```

## Ключевые пространства имён / сборки

| Сборка (.asmdef) | Namespace | Описание |
|-----------------|-----------|----------|
| `Asteroids` (`Assets/Asteroids.asmdef`) | `SelStrom.Asteroids` | Весь игровой код (Application, View, Model, Input, Utils) |
| `Conf` (`Assets/Scripts/Configs/Configs.asmdef`) | `SelStrom.Asteroids.Configs` | ScriptableObject-конфиги |
| `AsteroidsEditor` (`Assets/Editor/AsteroidsEditor.asmdef`) | — | Editor-утилиты (только Editor) |

**Зависимости сборок:**
- `Asteroids` зависит от: `Conf`, `Shtl.Mvvm`, `Unity.Services.Core`, `Unity.Services.Authentication`, `Unity.Services.Leaderboards`, Unity Input System.
- `Conf` не имеет зависимостей (кроме UnityEngine).
- `AsteroidsEditor` зависит от: `Asteroids`, `Unity.TextMeshPro`, `Shtl.Mvvm`.

**Namespace внутри сборки `Asteroids`:**
- `SelStrom.Asteroids` — все классы Application, View, Model, Utils.
- `Model.Components` — компоненты модели (отдельный namespace, несмотря на ту же сборку).
- `SelStrom.Asteroids.Bindings` — расширения биндингов.

## Точки входа

**Главная точка входа:**
- `Assets/Scripts/Application/ApplicationEntry.cs` — `MonoBehaviour`, размещён на GameObject в сцене `Assets/Scenes/Main.unity`.
- `Awake()`: создаёт `LeaderboardService`, `GameScreen`, `TitleScreen`, вызывает `Application.Connect(...)`.
- `Start()`: вызывает `Application.Start()` → вычисляет размер сцены по камере, создаёт `Model`, `EntitiesCatalog`, `Game`.

**Запуск игрового цикла:**
- `TitleScreen.Connect(onStart)` регистрирует коллбэк.
- Нажатие кнопки старта → `onStart()` → `Game.Start()`.
- `Game.Start()` спаунит корабль + астероиды, подписывается на ввод, переключает экран.

**Перезапуск:**
- `GameScreen` → кнопка Restart → `Game.Restart()` → `Model.CleanUp()` + `Game.Start()`.

## Границы модулей

### Application (слой оркестрации)
- **Содержит:** `ApplicationEntry`, `Application`, `Game`, `EntitiesCatalog`, фабрики, экраны, лидерборд.
- **Знает о:** Model, View, Configs, Utils, Input.
- **Не знает о:** конкретных системах Unity (кроме `ApplicationEntry`).

### Model (игровая логика)
- **Содержит:** `Model`, системы, сущности, компоненты, `ActionScheduler`.
- **Зависимости:** только `UnityEngine.Vector2` (математика), `Shtl.Mvvm` (для `ObservableValue`).
- **Не знает о:** Unity-сцене, GameObject, MonoBehaviour, View.
- **Правило:** любая новая система регистрируется в конструкторе `Model` и регистрирует сущности через `IGroupVisitor.Visit`.

### View (отображение)
- **Содержит:** `*Visual` MonoBehaviour-классы, `*ViewModel` классы, биндинги.
- **Зависит от:** `Shtl.Mvvm`, Unity UI/TextMeshPro.
- **Не содержит игровой логики:** реагирует только на изменения ViewModel.

### Configs (конфигурация)
- **Содержит:** только ScriptableObject-классы с данными.
- **Правило:** не содержит логики, только данные. Все ссылки на prefab хранятся здесь.

### Input (ввод)
- **Содержит:** `PlayerInput`, обёртку над Unity Input System.
- **Публикует:** C#-события, не Unity Messages.
- **Не зависит:** от Model или View.

## Именование файлов

| Тип | Паттерн | Пример |
|-----|---------|--------|
| Модель сущности | `{Entity}Model.cs` | `ShipModel.cs` |
| Вью сущности (ViewModel + Visual в одном файле) | `{Entity}Visual.cs` | `ShipVisual.cs` |
| Система | `{Behaviour}System.cs` | `MoveSystem.cs` |
| Компонент | `{Behaviour}Component.cs` | `MoveComponent.cs` |
| Конфиг (ScriptableObject) | `{Entity}Data.cs` | `AsteroidData.cs` |
| Конфиг-ассет | `{Entity}Data.asset` | `AsteroidBigData.asset` |
| Экран | `{Name}Screen.cs` | `GameScreen.cs` |
| Вью экрана | `{Name}View.cs` | `TitleScreenView.cs` |

## Куда добавлять новый код

**Новая игровая сущность (например, `MineModel`):**
1. Компоненты (если нужны новые): `Assets/Scripts/Model/Components/`
2. Модель: `Assets/Scripts/Model/Entities/MineModel.cs` (реализовать `IGameEntityModel`)
3. Добавить `Visit(MineModel)` в `IGroupVisitor` (`Model/Entities/IGroupVisitor.cs`)
4. Реализовать `Visit(MineModel)` в `Model.GroupCreator` (`Model/Model.cs`)
5. ViewModel + Visual: `Assets/Scripts/View/MineVisual.cs`
6. Метод `CreateMine(...)` в `EntitiesCatalog` (`Application/EntitiesCatalog.cs`)
7. Prefab: `Assets/Media/prefabs/mine.prefab`
8. Данные (если нужны): `Assets/Scripts/Configs/MineData.cs` + ассет в `Assets/Media/configs/`

**Новая система:**
1. Создать `Assets/Scripts/Model/Systems/{Name}System.cs`, наследовать `BaseModelSystem<TNode>`
2. Зарегистрировать в конструкторе `Model` через `RegisterSystem<{Name}System>()`
3. В `Model.GroupCreator.Visit` добавить `_owner.GetSystem<{Name}System>().Add(model, node)`

**Новый экран:**
1. ViewModel-класс + Visual (MonoBehaviour): `Assets/Scripts/View/{Name}View.cs`
2. Screen-класс (чистый C#): `Assets/Scripts/Application/Screens/{Name}Screen.cs` (наследовать `AbstractScreen`)
3. Создать экран в `ApplicationEntry.Awake()` и передать в `Application.Connect`

**Новый конфиг:**
1. Класс в `Assets/Scripts/Configs/` (наследовать `BaseGameEntityData` для игровых объектов или `ScriptableObject` напрямую)
2. Добавить ссылку в `GameData.cs`
3. Создать `.asset` через меню Unity или `[CreateAssetMenu]`

---

*Анализ структуры: 2026-03-26*
