<!-- GSD:project-start source:PROJECT.md -->
## Project

**Asteroids**

Классическая аркадная игра Asteroids на Unity. Корабль, астероиды, НЛО, стрельба пулями и лазером, тороидальный экран, лидерборд через Unity Gaming Services. Текущая реализация — ECS-подобная архитектура на MonoBehaviour + MVVM (shtl-mvvm) для UI.

**Core Value:** Играбельная классическая механика Asteroids с онлайн-лидербордом — фундамент для технической миграции на современный стек Unity.

### Constraints

- **Порядок миграции:** Unity 6.3 → URP → DOTS — каждый шаг на стабильной базе предыдущего
- **Обратная совместимость shtl-mvvm:** Фикс TMP должен работать начиная с Unity 2022.3
- **Функциональная эквивалентность:** Геймплей 1:1 после каждого этапа миграции
- **Гибридный DOTS:** Entities для логики/физики, GameObjects для UI и визуала
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Primary Language & Runtime
- C# 9.0 (указан в `<LangVersion>9.0</LangVersion>` во всех `.csproj` файлах)
- .NET Standard 2.1 / .NET Framework 4.7.1
- Mono (встроенный в Unity)
- Целевые платформы согласно `.asmdef`: `Editor`, `WebGL`, `WindowsStandalone64`
## Framework / Engine
- **Unity 2022.3.60f1** (LTS)
## Key Libraries & Packages
### Прямые зависимости (из `Packages/manifest.json`)
| Пакет | Версия | Назначение |
|---|---|---|
| `com.shtl.mvvm` | git (hash `c7bda1c`) | MVVM-фреймворк собственной разработки ([SelStrom/shtl-mvvm](https://github.com/SelStrom/shtl-mvvm.git)), реактивные привязки `ObservableValue`, `ReactiveValue`, `EventBindingContext` |
| `com.unity.inputsystem` | 1.19.0 | Новая система ввода Unity; генерируются классы в `Assets/Scripts/Input/Generated/` |
| `com.unity.textmeshpro` | 3.0.9 | Текстовые компоненты UI |
| `com.unity.ugui` | 1.0.0 | Стандартный UI (uGUI) |
| `com.unity.timeline` | 1.7.7 | Timeline-анимация |
| `com.unity.feature.2d` | 2.0.1 | Метапакет 2D-инструментов |
| `com.unity.services.core` | 1.16.0 | Базовый SDK Unity Gaming Services |
| `com.unity.services.authentication` | 3.6.0 | Анонимная аутентификация через UGS |
| `com.unity.services.leaderboards` | 2.3.3 | Таблица лидеров UGS |
| `com.unity.test-framework` | 1.1.33 | Тестирование (NUnit внутри Unity) |
| `com.unity.collab-proxy` | 2.7.1 | Unity Version Control (Plastic SCM) |
| `com.unity.ide.rider` | 3.0.39 | Интеграция с JetBrains Rider |
| `com.unity.ide.vscode` | 1.2.5 | Интеграция с VS Code |
### Транзитивные зависимости (из `Packages/packages-lock.json`)
| Пакет | Версия | Назначение |
|---|---|---|
| `com.unity.nuget.newtonsoft-json` | 3.2.2 | Json.NET (требуется `com.shtl.mvvm`) |
| `com.unity.burst` | 1.8.19 | Компилятор Burst (транзитивно через 2D) |
| `com.unity.collections` | 1.2.4 | Native-коллекции (транзитивно) |
| `com.unity.mathematics` | 1.2.6 | Математическая библиотека (транзитивно) |
| `com.unity.2d.animation` | 9.1.3 | 2D-анимация костей |
| `com.unity.2d.aseprite` | 1.1.8 | Импорт Aseprite-файлов |
| `com.unity.2d.pixel-perfect` | 5.0.3 | Pixel-perfect камера |
| `com.unity.2d.psdimporter` | 8.0.5 | Импорт PSD |
| `com.unity.2d.spriteshape` | 9.0.5 | SpriteShape |
| `com.unity.2d.tilemap.extras` | 3.1.3 | Расширения Tilemap |
## Build & Package Management
- **Unity Package Manager (UPM)**
- MSBuild (генерируется Unity/Rider), формат `.csproj` — ToolsVersion 4.0
- Файл решения: `asteroids.sln`
- Source generators: `Unity.SourceGenerators.dll`, `Unity.Properties.SourceGenerator.dll` (подключены как `<Analyzer>`)
- Unsafe-код: запрещён (`<AllowUnsafeBlocks>False</AllowUnsafeBlocks>`)
- Предупреждения 0169, 0649 подавлены
- `Assets/Asteroids.asmdef` — сборка `Asteroids` (основная логика + сервисы)
- `Assets/Scripts/Configs/Configs.asmdef` — сборка `Conf` (данные конфигурации)
- `Assets/Editor/AsteroidsEditor.asmdef` — сборка `AsteroidsEditor` (только Editor)
## Development Tools
- JetBrains Rider (основной — `com.unity.ide.rider` 3.0.39; файл настроек `Asteroids.csproj.DotSettings`)
- VS Code (дополнительный — `com.unity.ide.vscode` 1.2.5)
- Unity Source Generators (`Unity.SourceGenerators.dll`, `Unity.Properties.SourceGenerator.dll`)
- Unity Version Control / Plastic SCM (`com.unity.collab-proxy` 2.7.1)
- Git (файлы `.meta` указывают на использование Git для внешних пакетов)
- Unity Memory Profiler (пакет присутствует как `.csproj` файлы: `Unity.MemoryProfiler.Editor.csproj`, `Unity.MemoryProfiler.csproj`)
- Файл `Assets/Input/player_actions.inputactions` — описание входных действий
- Сгенерированный C#-класс: `Assets/Scripts/Input/Generated/PlayerActions.cs`
## Алгоритмы и ключевые реализации
### ECS-подобная система (Entity-Component-System)
- Каждая система хранит `Dictionary<IGameEntityModel, TNode>`, где `TNode` — компонент или кортеж компонентов.
- Обновление: итерация по `Values` словаря с вызовом абстрактного `UpdateNode(TNode, float deltaTime)`.
- Удаление: по ключу `IGameEntityModel`.
- Паттерн Double Dispatch: каждая сущность реализует `AcceptWith(IGroupVisitor)`, вызывая `visitor.Visit(this)`.
- `GroupCreator` (вложенный класс `Model`) привязывает компоненты сущности к соответствующим системам.
- Например, `ShipModel` регистрируется в 5 системах: `MoveSystem`, `RotateSystem`, `GunSystem`, `LaserSystem`, `ThrustSystem`.
- `UfoModel` регистрируется в 4 системах: `MoveSystem`, `GunSystem`, `ShootToSystem`, `MoveToSystem`.
### Алгоритм движения (MoveSystem)
- Если позиция выходит за `+side/2`, переносится в `−side + position`.
- Если позиция выходит за `−side/2`, переносится в `+side − position`.
- Применяется отдельно для X и Y, создавая тороидальную топологию.
### Алгоритм вращения (RotateSystem)
- Вращение через Quaternion: `Quaternion.Euler(0, 0, 90 * deltaTime * direction) * currentRotation`
- Скорость: фиксированная `90` градусов/сек (`RotateComponent.DegreePerSecond` в `Assets/Scripts/Model/Components/RotateComponent.cs:8`)
- `TargetDirection` = -1 (по часовой), +1 (против часовой), 0 (нет вращения)
- Результат хранится как `ObservableValue<Vector2>` (единичный вектор направления), по умолчанию `Vector2.right`
### Алгоритм тяги (ThrustSystem)
### Алгоритм стрельбы пушкой (GunSystem)
### Алгоритм лазера (LaserSystem)
### Алгоритм упреждающей стрельбы врагов (ShootToSystem)
- Скорость пули захардкожена как `20` (`Assets/Scripts/Model/Systems/ShootToSystem.cs:17`)
- Стреляет каждый кадр, когда есть доступные выстрелы (`CurrentShoots > 0`)
### Алгоритм упреждающего преследования (MoveToSystem)
### Алгоритм спавна врагов
- Каждые `SpawnNewEnemyDurationSec` секунд через `ActionScheduler` спавнится один враг.
- Тип врага выбирается случайно с равной вероятностью (1/3): астероид, малый UFO, большой UFO.
- При старте создаётся `AsteroidInitialCount` астероидов.
- Случайная позиция в пределах игровой области.
- Если расстояние до корабля меньше `SpawnAllowedRadius`, позиция корректируется, чтобы отодвинуться от корабля.
- X-координата = 0 (левый край, минус половина ширины).
- Y-координата случайная в пределах высоты.
- Вертикальная коррекция, если UFO слишком близко к кораблю.
### Алгоритм дробления астероидов
### Алгоритм подсчёта очков
### Алгоритм обработки коллизий
- **Корабль:** `ShipVisual.OnCollisionEnter2D` -> `Game.OnShipCollided` -> корабль уничтожается, игра завершается.
- **Пуля игрока:** `BulletVisual.OnCollisionEnter2D` -> `Game.OnUserBulletCollided`:
- **Пуля врага:** `Game.OnEnemyBulletCollided` — пуля уничтожается, коллайдер цели отключается.
- **UFO:** `UfoVisual.OnCollisionEnter2D` -> `Game.OnUfoCollided` -> UFO уничтожается.
### ActionScheduler — планировщик отложенных действий
- Оптимизированный таймерный планировщик с `_nextUpdateDuration` для раннего выхода (fast-path).
- Удаление элементов: swap-and-pop (перемещение последнего элемента на место удалённого, O(1)).
- Используется для: спавна врагов, удаления визуальных эффектов лазера.
### Object Pooling
- Пул по prefab Instance ID (`Dictionary<string, Stack<GameObject>>`).
- При возврате: деактивация объекта, перемещение под pool-контейнер.
- При получении: если есть в стеке — повторное использование, иначе `Instantiate`.
- Обратное отображение `_gameObjectToPrefabId` для O(1) возврата.
- Пулинга моделей пока нет (TODO в коде) — каждый раз создаётся `new TModel()`.
### Система ввода (Input System)
| Действие | Тип | Клавиша | Описание |
|---|---|---|---|
| Attack | Button | Space | Выстрел из пушки |
| Rotate | Value (Axis) | A/D | Вращение корабля (1DAxis: A=negative, D=positive, инвертирован: min=1, max=-1) |
| Accelerate | Value | W | Активация тяги |
| Laser | Button | Q | Выстрел лазером |
| Back | Button | Escape | Выход из игры (не на WebGL) |
| Restart | Button | Space | Рестарт игры |
### MVVM-привязки (Shtl.Mvvm)
- `ObservableValue<T>` — наблюдаемое значение в Model-слое
- `ReactiveValue<T>` — наблюдаемое значение в ViewModel-слое
- `ReactiveList<T>` — наблюдаемый список (используется для записей лидерборда)
- `EventBindingContext` — контекст привязок с методом `CleanUp()` для отписки
- `AbstractViewModel` — базовый ViewModel
- `AbstractWidgetView<T>` — базовый View, параметризованный ViewModel
- `To(Transform)` — связывает `ReactiveValue<Vector2>` с `transform.position` (копирует X/Y, оставляя Z).
## Platform Requirements
- Unity 2022.3.60f1 LTS
- macOS (текущая конфигурация), Windows (поддерживается через WindowsStandalone64)
- WebGL (поддержка через `#if !UNITY_WEBGL` для отключения Escape-выхода)
- Windows Standalone 64-bit
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

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
## File Organization
### Структура директорий
### Один класс -- один файл
- ViewModel и Visual одной сущности объединены в один файл (например, `Assets/Scripts/View/ShipVisual.cs` содержит `ShipViewModel` и `ShipVisual`).
- `UfoModel` и `UfoBigModel` объединены в `Assets/Scripts/Model/Entities/UfoBigModel.cs` (наследование `UfoModel : UfoBigModel`).
### Namespace
- Основной namespace: `SelStrom.Asteroids`
- Компоненты модели: `namespace Model.Components` (`Assets/Scripts/Model/Components/`)
- Конфиги: `namespace SelStrom.Asteroids.Configs` (`Assets/Scripts/Configs/`)
- Биндинги: `namespace SelStrom.Asteroids.Bindings` (`Assets/Scripts/View/Bindings/`)
- Папки `Model`, `View`, `Application` пропущены в namespace через `Asteroids.csproj.DotSettings` (`NamespaceFoldersToSkip`).
- **Внимание:** `Model.Components` -- это НЕ `SelStrom.Asteroids.Model.Components`, а просто `Model.Components`. Файлы в этом namespace используют `using SelStrom.Asteroids;` для доступа к `IGameEntityModel` и пр.
## Code Style
### Фигурные скобки
### Отступы
### default-инициализация [SerializeField]
## Patterns to Follow
### MVVM (через пакет `com.shtl.mvvm`)
- **Model** -- pure C# данные: `ShipModel`, `AsteroidModel`, `BulletModel`, `UfoBigModel`, `UfoModel`
- **ViewModel** -- `AbstractViewModel` с `ReactiveValue<T>` полями: `ShipViewModel`, `AsteroidViewModel`, `BulletViewModel`, `UfoViewModel`, `HudData`, `ScoreViewModel`, `LeaderboardEntryViewModel`
- **View** -- `AbstractWidgetView<TViewModel>` MonoBehaviour: `ShipVisual`, `AsteroidVisual`, `BulletVisual`, `UfoVisual`, `HudVisual`, `ScoreVisual`, `LeaderboardEntryVisual`
### Паттерн Connect/Dispose
### Паттерн null-сброс при Dispose
### Event подписка/отписка
### Action-делегаты как публичные поля
### ECS-подобные системы
- Хранит `Dictionary<IGameEntityModel, TNode>` для маппинга entity -> node
- `Add(model, node)` / `Remove(model)` для регистрации
- `Update(deltaTime)` итерирует все nodes и вызывает `UpdateNode(node, deltaTime)`
- `TNode` может быть как одиночный компонент, так и **ValueTuple** нескольких компонентов
- Одиночный: `BaseModelSystem<MoveComponent>`, `BaseModelSystem<GunComponent>`, `BaseModelSystem<LaserComponent>`
- Tuple: `BaseModelSystem<(ThrustComponent, MoveComponent, RotateComponent)>`, `BaseModelSystem<(MoveComponent, GunComponent, ShootToComponent)>`, `BaseModelSystem<(MoveComponent, MoveToComponent)>`
### Visitor-паттерн для dispatch сущностей
### Object Pool
- Ключ пула -- `prefab.GetInstanceID().ToString()` (строковый ID инстанса префаба)
- `Stack<GameObject>` для каждого типа префаба
- При `Get`: если есть в стеке -- Pop + SetParent + SetActive(true); иначе -- `Object.Instantiate`
- При `Release`: SetActive(false) + SetParent(poolContainer) + Push в стек
- `_gameObjectToPrefabId` -- обратный маппинг для Release
- При Release незнакомого объекта -- `throw new Exception`
### Proxy-интерфейсы для внешних сервисов
### CoroutineResult для async-операций
### Конфиги через ScriptableObject
- Вложенные `[Serializable] struct` для группировки: `BulletData`, `ShipData`, `LaserData`
- Отдельные ScriptableObject для данных сущностей: `AsteroidData`, `UfoData`, `GunData` -- наследуют `BaseGameEntityData` (содержит `Score`)
- `[Space]` и `[Header]` для группировки в инспекторе
### Struct для immutable data
## Algorithm Patterns
### Движение с телепортацией через границу (toroidal wrapping)
### Движение: velocity = direction * speed
### Ускорение корабля (thrust)
- **При ускорении**: суммирует текущий velocity (`direction * speed`) с вектором ускорения (`rotation * acceleration`), нормализует, clamp до `MaxSpeed`:
- **При отпускании газа**: линейное замедление со скоростью `UnitsPerSecond / 2`, до `MinSpeed` (0.0):
### Поворот корабля (rotate)
- Константная скорость: `DegreePerSecond = 90` градусов/сек
- Поворот через `Quaternion.Euler(0, 0, angle) * currentRotation`:
- `TargetDirection` -- float, приходит от Input System как ось: -1 (вправо, по часовой), 0 (нет поворота), +1 (влево, против часовой). Инвертировано через `1DAxis(minValue=1,maxValue=-1)` в InputActions.
- `Rotation` хранится как `Vector2` (не как угол), начальное значение `Vector2.right`.
### Перезарядка оружия (gun / laser)
### Стрельба пули
### Лазер (raycast)
### Предиктивное прицеливание AI (lead targeting)
- `ShootToSystem` использует жёстко закодированную скорость пули `20` в формуле `(20 - ship.Move.Speed.Value)`
- `MoveToSystem` использует разницу скоростей UFO и корабля
- `MoveToSystem` перезаходит каждые `Every` секунд (3 сек для маленького UFO)
- `ShootToSystem` стреляет каждый кадр при наличии патронов (без cooldown кроме GunSystem reload)
### Дробление астероидов
- Астероид имеет `Age` (3 = большой, 2 = средний, 1 = маленький)
- При уничтожении: `age = asteroidModel.Age - 1`
- Если `age > 0` -- создаются **2 новых астероида** меньшего размера на той же позиции
- Скорость осколков: `Math.Min(asteroidModel.Move.Speed.Value * 2, 10f)` -- удвоенная скорость родителя, но не более 10
- Направление осколков: `Random.insideUnitCircle` (случайное)
### Спавн врагов
- Периодический спавн через `ActionScheduler.ScheduleAction` с интервалом `SpawnNewEnemyDurationSec`
- Рекурсивное перепланирование: после каждого спавна ставится следующий
- Равновероятный выбор типа: `Random.Range(0, 3)` -- астероид / маленький UFO / большой UFO
### Спавн позиций (safe spawn)
- Случайная позиция в пределах GameArea
- Если слишком близко к кораблю (< `SpawnAllowedRadius`) -- сдвигает позицию **от** корабля на недостающее расстояние
- Спавн на **левом краю** экрана (`x = 0 - gameArea.x * 0.5`)
- Случайная y-позиция
- Если вертикальное расстояние до корабля < `SpawnAllowedRadius` -- сдвигает по вертикали
### Большой UFO: горизонтальное движение
### Маленький UFO: преследование
### Время жизни пуль
### Расчёт GameArea из камеры
### ActionScheduler: отложенное выполнение
- Оптимизация: `_nextUpdateDuration` -- минимальная задержка до ближайшего действия; если время не пришло, `Update` возвращается рано
- Удаление по swap-with-last: при исполнении действия -- меняется местами с последним элементом, удаляется последний (O(1) удаление из List)
- `_secondsSinceLastUpdate` накапливается между обновлениями, при обработке сбрасывается в 0
- Действия, добавленные через `ScheduleAction`, учитывают уже прошедшее время: `nextUpdate = durationSec + _secondsSinceLastUpdate`
### Leaderboard: best-score submit
- Перед отправкой запрашивает текущий результат игрока с сервера
- Отправляет `Math.Max(serverBestScore, currentScore)` -- только если текущий результат лучше
- Проверка stale ViewModel: `if (_score.ViewModel != viewModel) yield break;` -- защита от race condition при быстром рестарте
### Collision handling: отключение коллайдера
### VFX: эффект взрыва через пул
- В `OnConnected()` запускает ParticleSystem
- По событию `OnParticleSystemStopped` вызывает callback, который возвращает эффект в пул
## Documentation
### Комментарии
### Атрибуты Unity
- `[SerializeField]` -- для инспекторных полей MonoBehaviour
- `[Space]` и `[Header]` -- для группировки в инспекторе (`Assets/Scripts/Configs/GameData.cs`, `Assets/Scripts/View/ScoreVisual.cs`)
- `[CreateAssetMenu]` -- для ScriptableObject конфигов
- `[PublicAPI]` (JetBrains) -- для подавления предупреждений о неиспользуемых приватных методах (см. `PlayerInput.cs:35,42,48,54,59,64`)
### Логирование
## Testing Policy
При исправлении бага — обязателен регрессионный тест на исправленный сценарий. Тест на баг — часть фикса, не «лишний код». Без теста фикс не считается завершённым.

## Anti-Patterns to Avoid
### Жёстко закодированные магические числа
- `ShootToSystem.cs:17`: скорость пули `20` -- жёстко в формуле, не из конфига
- `MoveToComponent`: `Every = 3f` задаётся в `EntitiesCatalog.cs:149`, не в конфиге
- `Game.cs:185`: максимальная скорость осколков `10f` -- жёстко в коде
- `Game.cs:115`: начальный размер астероида `3` -- жёстко в коде
- `LaserSystem/GunSystem`: буфер raycast `30` -- жёстко в `Game.cs:220`
### switch expressions
### Ternary / null-coalescing
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Общий дизайн
- Игровая логика полностью отделена от Unity: модели и системы — чистые C#-классы без `MonoBehaviour`.
- Единственная точка входа в Unity-стек — `ApplicationEntry` (MonoBehaviour). Все остальные объекты создаются вручную через `new`.
- Связывание модель↔вью реализовано через реактивные значения (`ObservableValue`, `ReactiveValue`) из библиотеки `Shtl.Mvvm`.
- Конфигурация хранится в ScriptableObject-ассетах.
## Архитектурные слои
```
```
## Основные системы
### 1. Слой приложения (`Assets/Scripts/Application/`)
- Единственный `MonoBehaviour` в игровом коде (не считая визуалов).
- Реализует `IApplicationComponent`: пробрасывает `Update`, `OnPause`, `OnResume` как C#-события.
- Создаёт `Application`, `LeaderboardService`, `GameScreen`, `TitleScreen` в `Awake`.
- `[SerializeField] GameData _configs` — точка вброса конфига из сцены.
- Корневой объект приложения (чистый C#).
- Владеет `GameObjectPool`, `EntitiesCatalog`, `Model`, `Game`, экранами.
- Вычисляет GameArea из камеры: `sceneWidth = camera.aspect * orthographicSize * 2`.
- Подписывается на `IApplicationComponent.OnUpdate` и вызывает `Model.Update(deltaTime)` каждый кадр.
- Управляет игровым процессом: старт, стоп, рестарт.
- Спаунит корабль, астероиды, UFO через `EntitiesCatalog`.
- Обрабатывает коллизии, уничтожение сущностей, логику разбивания астероидов на части.
- Расписывает таймер появления новых врагов через `ActionScheduler`.
- Фабрика и реестр всех игровых сущностей.
- Хранит двунаправленные словари: модель↔вью, `GameObject`↔модель, модель↔bindings.
- При создании каждой сущности создаёт модель (`ModelFactory`), вью (`ViewFactory`) и `EventBindingContext` с привязками реактивных значений.
- При уничтожении (`Release`) снимает привязки и возвращает объекты в пул.
### 2. Модельный слой — ECS-ядро (`Assets/Scripts/Model/`)
```
```
- `ShipModel` → MoveSystem, RotateSystem, GunSystem, LaserSystem, ThrustSystem
- `AsteroidModel` → MoveSystem
- `BulletModel` → MoveSystem, LifeTimeSystem
- `UfoBigModel` → MoveSystem, GunSystem, ShootToSystem
- `UfoModel` → MoveSystem, GunSystem, ShootToSystem, MoveToSystem
### 3. Системы компонентов — подробно
#### ThrustSystem (`ThrustSystem.cs`) — физика ускорения корабля
#### RotateSystem (`RotateSystem.cs`) — вращение корабля
#### MoveSystem (`MoveSystem.cs`) — перемещение + тороидальный экран
```csharp
```
#### GunSystem (`GunSystem.cs`) — механика перезарядки и стрельбы
#### LaserSystem (`LaserSystem.cs`) — лазер с инкрементальной перезарядкой
- `CurrentShoots` и `ReloadRemaining` — `ObservableValue<>` (реактивные, связаны с HUD)
- Перезарядка восстанавливает **по одному** выстрелу за период (`CurrentShoots += 1`)
- Стрельба тратит **по одному** (`CurrentShoots -= 1`)
#### ShootToSystem (`ShootToSystem.cs`) — предиктивное прицеливание UFO
#### MoveToSystem (`MoveToSystem.cs`) — перехват корабля малым UFO
#### LifeTimeSystem (`LifeTimeSystem.cs`) — время жизни пуль
### 4. ActionScheduler — отложенные действия
### 5. Управление жизненным циклом сущностей
### 6. Коллизии и подсчёт очков
- ⚠ **Баг:** `ReceiveScore` вызывается, но `Kill` для UFO **НЕ вызывается**. UFO остаётся бессмертным после попадания пули.
- Kill корабля + Stop (конец игры)
- Не различает тип столкновения (астероид/UFO/пуля)
### 7. Спаун врагов
- Малый UFO: `Random.insideUnitCircle.normalized` — в любую сторону
- Большой UFO: `(Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized` — преимущественно горизонтально (y-компонента подавлена в 10×)
### 8. Машина состояний игры
```
```
### 9. MVVM-связывание
- Thrust sprite: `From(model.Thrust.IsActive).To(viewModel.Sprite, ...)` — переключение спрайта по булевому значению через лямбда-адаптер
- Rotation: View сам конвертирует Vector2 → угол через `Atan2` и устанавливает `Quaternion.Euler`
- Position: расширение `BindingToExtensions.To(transform)` — копирует x,y из Vector2 в Transform.position
### 10. Leaderboard — лучший результат
<!-- GSD:architecture-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd:quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd:debug` for investigation and bug fixing
- `/gsd:execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd:profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
