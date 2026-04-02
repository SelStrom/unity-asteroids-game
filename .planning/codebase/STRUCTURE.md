# Code Structure

**Дата анализа:** 2026-04-02

## Directory Layout

```
Assets/
├── Editor/                         # Unity Editor-утилиты
│   ├── LeaderboardPrefabCreator.cs # Инструмент создания prefab'ов лидерборда
│   └── AsteroidsEditor.asmdef      # Сборка только для Editor
│
├── Input/
│   └── PlayerActions.inputactions  # Конфиг Unity Input System
│
├── Media/
│   ├── configs/                    # ScriptableObject-ассеты
│   │   ├── GameData.asset          # Главный конфиг
│   │   ├── AsteroidBigData.asset / AsteroidMediumData.asset / AsteroidSmallData.asset
│   │   ├── UfoBigData.asset / UfoData.asset
│   │   └── UserGunData.asset / UfoGunData.asset
│   ├── effects/                    # VFX-ассеты (взрывы)
│   ├── prefabs/                    # Prefab'ы игровых объектов
│   │   ├── asteroid_big/medium/small.prefab
│   │   ├── bullet.prefab / bullet_enemy.prefab
│   │   ├── ship.prefab / ufo.prefab / ufo_big.prefab
│   │   └── gui/ (gui_text.prefab, leaderboard_entry.prefab)
│   └── sprites/                    # Спрайты
│
├── Scenes/
│   └── Main.unity                  # Единственная сцена
│
├── Scripts/                        # Весь C#-код
│   ├── Application/                # Слой приложения
│   │   ├── ApplicationEntry.cs     # MonoBehaviour, точка входа
│   │   ├── Application.cs          # Корневой C#-объект
│   │   ├── Game.cs                 # Игровой процесс
│   │   ├── IApplicationComponent.cs # Интерфейс жизненного цикла
│   │   ├── EntitiesCatalog.cs      # Фабрика+реестр Model↔View
│   │   ├── ModelFactory.cs         # Создание моделей
│   │   ├── ViewFactory.cs          # Создание View из пула
│   │   ├── Leaderboard/            # Подсистема лидерборда
│   │   │   ├── LeaderboardService.cs       # Оркестратор Auth+Leaderboard
│   │   │   ├── LeaderboardEntry.cs         # Readonly struct записи
│   │   │   ├── IAuthProxy.cs               # Интерфейс аутентификации
│   │   │   ├── ILeaderboardProxy.cs        # Интерфейс лидерборда
│   │   │   ├── UnityAuthProxy.cs           # UGS реализация Auth
│   │   │   └── UnityLeaderboardProxy.cs    # UGS реализация Leaderboard
│   │   └── Screens/                # UI-экраны (MVVM)
│   │       ├── AbstractScreen.cs   # Базовый класс с EventBindingContext
│   │       ├── TitleScreen.cs      # Стартовый экран
│   │       └── GameScreen.cs       # HUD + EndGame + Leaderboard
│   │
│   ├── Model/                      # Модельный слой (ECS-ядро)
│   │   ├── Model.cs                # Центральный класс: сущности + системы
│   │   ├── ActionScheduler.cs      # Отложенные действия по таймеру
│   │   ├── Components/             # Данные (ECS-компоненты)
│   │   │   ├── IModelComponent.cs      # Маркерный интерфейс
│   │   │   ├── MoveComponent.cs        # Position (Observable), Speed (Observable), Direction
│   │   │   ├── RotateComponent.cs      # Rotation (Observable), TargetDirection, 90°/сек
│   │   │   ├── ThrustComponent.cs      # IsActive (Observable), UnitsPerSecond, MaxSpeed
│   │   │   ├── GunComponent.cs         # MaxShoots, ReloadDuration, CurrentShoots, Shooting flag
│   │   │   ├── LaserComponent.cs       # MaxShoots, UpdateDuration, CurrentShoots/ReloadRemaining (Observable)
│   │   │   ├── LifeTimeComponent.cs    # TimeRemaining (float)
│   │   │   ├── MoveToComponent.cs      # Ship ref, Every (interval), ReadyRemaining (timer)
│   │   │   └── ShootToComponent.cs     # Ship ref, Every, ReadyRemaining
│   │   ├── Entities/               # Модели сущностей
│   │   │   ├── IGameEntityModel.cs     # IsDead(), Kill(), AcceptWith()
│   │   │   ├── IGroupVisitor.cs        # Visitor для регистрации в системах
│   │   │   ├── ShipModel.cs            # Rotate+Thrust+Move+Gun+Laser, ShootPoint
│   │   │   ├── AsteroidModel.cs        # Move, Age (3→2→1), Data
│   │   │   ├── BulletModel.cs          # Move+LifeTime, IsDead = timeout || killed
│   │   │   └── UfoBigModel.cs          # Move+ShootTo+Gun; UfoModel extends: +MoveTo
│   │   └── Systems/                # Логика обновления (ECS-системы)
│   │       ├── BaseModelSystem.cs      # Базовый: Dictionary<Model,Node> + Update + Remove
│   │       ├── MoveSystem.cs           # Перемещение + тороидальный wrap
│   │       ├── RotateSystem.cs         # Кватернионное вращение Vector2
│   │       ├── ThrustSystem.cs         # Физика ускорения/торможения
│   │       ├── GunSystem.cs            # Перезарядка + стрельба (batch reload)
│   │       ├── LaserSystem.cs          # Перезарядка + стрельба (incremental reload)
│   │       ├── LifeTimeSystem.cs       # Обратный отсчёт времени жизни
│   │       ├── ShootToSystem.cs        # Предиктивное прицеливание UFO
│   │       └── MoveToSystem.cs         # Перехват корабля малым UFO
│   │
│   ├── Configs/                    # ScriptableObject определения
│   │   ├── GameData.cs                 # Главный конфиг (вложенные struct: ShipData, BulletData, LaserData)
│   │   ├── BaseGameEntityData.cs       # Абстрактный: Score
│   │   ├── AsteroidData.cs             # Prefab, SpriteVariants[]
│   │   ├── UfoData.cs                  # Prefab, Speed, GunData ref
│   │   ├── GunData.cs                  # MaxShoots, ReloadDurationSec
│   │   └── BulletData.cs               # ⚠ Dead code (не используется, GameData.BulletData struct вместо)
│   │
│   ├── Input/                      # Ввод
│   │   ├── PlayerInput.cs              # Обёртка: события OnAttack/OnRotate/OnThrust/OnLaser/OnBack
│   │   ├── PlayerActions.cs            # Auto-generated Input System wrapper
│   │   └── Generated/
│   │       └── PlayerActions.cs        # Дубликат (auto-generated)
│   │
│   ├── View/                       # Визуалы (MVVM-Views)
│   │   ├── Base/
│   │   │   ├── IEntityView.cs          # Dispose() + gameObject
│   │   │   └── BaseVisual.cs           # MonoBehaviour + generic BaseVisual<TData>
│   │   ├── Bindings/
│   │   │   └── BindingToExtensions.cs  # ReactiveValue<Vector2> → Transform.position
│   │   ├── Components/
│   │   │   └── GuiText.cs              # TMP_Text wrapper
│   │   ├── ShipVisual.cs               # Sprite switch + rotation → angle + collision relay
│   │   ├── AsteroidVisual.cs           # Position bind + sprite variant
│   │   ├── BulletVisual.cs             # Position bind + collider enable + collision relay
│   │   ├── UfoVisual.cs                # Position bind + collision relay (no rotation)
│   │   ├── EffectVisual.cs             # ParticleSystem play + OnStopped callback
│   │   ├── HudVisual.cs                # 5 TMP_Text полей + visibility toggle
│   │   ├── ScoreVisual.cs              # End-game: score, name input, leaderboard list, restart
│   │   ├── LeaderboardEntryVisual.cs   # Rank + Name + Score + color highlight
│   │   └── TitleScreenView.cs          # Start button binding
│   │
│   └── Utils/                      # Утилиты
│       ├── GameObjectPool.cs           # Пул GameObject по prefab InstanceID
│       ├── GameUtils.cs                # Позиционирование спауна (asteroid/UFO)
│       └── CoroutineResult.cs          # Результат корутины (Error + generic Value)
│
├── TextMesh Pro/                   # TMP шрифты и ассеты
└── Plugins/
    └── Shtl.Mvvm.dll              # MVVM-библиотека (скомпилированная)
```

## Пространства имён

```
SelStrom.Asteroids              # Основной namespace (Application, Model, View, Utils)
SelStrom.Asteroids.Configs      # ScriptableObject конфиги
SelStrom.Asteroids.Bindings     # Extension methods для биндингов
Model.Components                # ⚠ Отдельный namespace (GunComponent, LaserComponent, etc.)
Shtl.Mvvm                       # Внешняя MVVM библиотека
```

**Несогласованность:** `Model.Components` не следует паттерну `SelStrom.Asteroids.*`.

## Иерархия классов

### Модели сущностей
```
IGameEntityModel
├── ShipModel          (Rotate + Thrust + Move + Gun + Laser)
├── AsteroidModel      (Move + Age)
├── BulletModel        (Move + LifeTime)
└── UfoBigModel        (Move + ShootTo + Gun)
    └── UfoModel       (+ MoveTo)  ← наследование, sealed
```

### Системы
```
IModelSystem
└── BaseModelSystem<TNode>    (Dictionary<IGameEntityModel, TNode>)
    ├── MoveSystem            <MoveComponent>
    ├── RotateSystem          <RotateComponent>
    ├── ThrustSystem          <(ThrustComponent, MoveComponent, RotateComponent)>
    ├── GunSystem             <GunComponent>
    ├── LaserSystem           <LaserComponent>
    ├── LifeTimeSystem        <LifeTimeComponent>
    ├── ShootToSystem         <(MoveComponent, GunComponent, ShootToComponent)>
    └── MoveToSystem          <(MoveComponent, MoveToComponent)>
```

### View-слой
```
MonoBehaviour
├── AbstractWidgetView<TViewModel>  (Shtl.Mvvm — из DLL)
│   ├── ShipVisual
│   ├── AsteroidVisual
│   ├── BulletVisual
│   ├── UfoVisual
│   ├── HudVisual
│   ├── ScoreVisual
│   ├── LeaderboardEntryVisual
│   └── TitleScreenView
├── BaseVisual → BaseVisual<TData>
│   └── EffectVisual
└── ApplicationEntry (IApplicationComponent)
```

## Зависимости между компонентами

```
ApplicationEntry ──→ Application ──→ Model
                 ──→ GameScreen      ├─→ Systems (8 штук)
                 ──→ TitleScreen     ├─→ Entities
                 ──→ LeaderboardService  └─→ ActionScheduler
                           │
                 Application ──→ EntitiesCatalog ──→ ModelFactory ──→ Model
                                                 ──→ ViewFactory ──→ GameObjectPool

Game ──→ EntitiesCatalog (создание/уничтожение)
     ──→ Model (подписка OnEntityDestroyed)
     ──→ PlayerInput (подписка на ввод)
     ──→ GameScreen (переключение состояний)
     ──→ GameData (конфигурация)
```

## Ключевые метрики

| Метрика | Значение |
|---------|----------|
| Файлов C# (без Generated) | ~50 |
| Строк кода (без Generated) | ~2200 |
| Сущностей (моделей) | 5 (Ship, Asteroid, Bullet, UfoBig, Ufo) |
| ECS-систем | 8 |
| ViewModels | 7 |
| Scenes | 1 (Main.unity) |
| ScriptableObject ассетов | ~8 |
| Тестов | 0 |
