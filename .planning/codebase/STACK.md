# Tech Stack

**Дата анализа:** 2026-04-02

## Primary Language & Runtime

**Язык:**
- C# 9.0 (указан в `<LangVersion>9.0</LangVersion>` во всех `.csproj` файлах)

**Целевой фреймворк:**
- .NET Standard 2.1 / .NET Framework 4.7.1
  - `<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>` — формально в `.csproj` (генерируется Rider)
  - Фактически активны define-символы `NET_STANDARD_2_1`, `NETSTANDARD2_1` — Unity использует .NET Standard 2.1 профиль
  - Scripting backend: **Mono** (`ENABLE_MONO` в define-символах)

**Среда выполнения:**
- Mono (встроенный в Unity)
- Целевые платформы согласно `.asmdef`: `Editor`, `WebGL`, `WindowsStandalone64`

---

## Framework / Engine

**Игровой движок:**
- **Unity 2022.3.60f1** (LTS)
  - Файл версии: `ProjectSettings/ProjectVersion.txt`
  - Редактор установлен по пути: `/Applications/Unity/Hub/Editor/2022.3.60f1/`
  - Разрядность: 64-bit (`UNITY_64`, `PLATFORM_ARCH_64`)
  - Render pipeline: встроенный (Built-in Render Pipeline, не URP/HDRP)
  - Рендеринг 2D: Physics2D, Sprites, Tilemap, Particle System

---

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

---

## Build & Package Management

**Менеджер пакетов:**
- **Unity Package Manager (UPM)**
  - Файл зависимостей: `Packages/manifest.json`
  - Lockfile: `Packages/packages-lock.json` (присутствует, зафиксированы версии)
  - Реестр по умолчанию: `https://packages.unity.com`
  - Один git-пакет: `com.shtl.mvvm` подключён напрямую через git URL

**Сборочная система:**
- MSBuild (генерируется Unity/Rider), формат `.csproj` — ToolsVersion 4.0
- Файл решения: `asteroids.sln`
- Source generators: `Unity.SourceGenerators.dll`, `Unity.Properties.SourceGenerator.dll` (подключены как `<Analyzer>`)
- Unsafe-код: запрещён (`<AllowUnsafeBlocks>False</AllowUnsafeBlocks>`)
- Предупреждения 0169, 0649 подавлены

**Определённые сборки (Assembly Definition Files):**
- `Assets/Asteroids.asmdef` — сборка `Asteroids` (основная логика + сервисы)
- `Assets/Scripts/Configs/Configs.asmdef` — сборка `Conf` (данные конфигурации)
- `Assets/Editor/AsteroidsEditor.asmdef` — сборка `AsteroidsEditor` (только Editor)

---

## Development Tools

**IDE:**
- JetBrains Rider (основной — `com.unity.ide.rider` 3.0.39; файл настроек `Asteroids.csproj.DotSettings`)
- VS Code (дополнительный — `com.unity.ide.vscode` 1.2.5)

**Анализаторы кода:**
- Unity Source Generators (`Unity.SourceGenerators.dll`, `Unity.Properties.SourceGenerator.dll`)

**Контроль версий:**
- Unity Version Control / Plastic SCM (`com.unity.collab-proxy` 2.7.1)
- Git (файлы `.meta` указывают на использование Git для внешних пакетов)

**Профилировщик памяти:**
- Unity Memory Profiler (пакет присутствует как `.csproj` файлы: `Unity.MemoryProfiler.Editor.csproj`, `Unity.MemoryProfiler.csproj`)

**Input Actions:**
- Файл `Assets/Input/player_actions.inputactions` — описание входных действий
- Сгенерированный C#-класс: `Assets/Scripts/Input/Generated/PlayerActions.cs`

---

## Алгоритмы и ключевые реализации

### ECS-подобная система (Entity-Component-System)

Проект реализует собственную ECS-подобную архитектуру (не Unity ECS / DOTS), построенную на словарях и паттерне Visitor.

**Базовый класс систем:** `Assets/Scripts/Model/Systems/BaseModelSystem.cs`
- Каждая система хранит `Dictionary<IGameEntityModel, TNode>`, где `TNode` — компонент или кортеж компонентов.
- Обновление: итерация по `Values` словаря с вызовом абстрактного `UpdateNode(TNode, float deltaTime)`.
- Удаление: по ключу `IGameEntityModel`.

**Регистрация сущностей в системах (Visitor):** `Assets/Scripts/Model/Model.cs:10-53`
- Паттерн Double Dispatch: каждая сущность реализует `AcceptWith(IGroupVisitor)`, вызывая `visitor.Visit(this)`.
- `GroupCreator` (вложенный класс `Model`) привязывает компоненты сущности к соответствующим системам.
- Например, `ShipModel` регистрируется в 5 системах: `MoveSystem`, `RotateSystem`, `GunSystem`, `LaserSystem`, `ThrustSystem`.
- `UfoModel` регистрируется в 4 системах: `MoveSystem`, `GunSystem`, `ShootToSystem`, `MoveToSystem`.

**Порядок обновления систем:** `Assets/Scripts/Model/Model.cs:124-154`
1. `ActionScheduler.Update(deltaTime)` — отложенные действия
2. Новые сущности из `_newEntities` переносятся в `_entities` и регистрируются через Visitor
3. Все системы обновляются в порядке регистрации: `RotateSystem` -> `ThrustSystem` -> `MoveSystem` -> `LifeTimeSystem` -> `GunSystem` -> `LaserSystem` -> `ShootToSystem` -> `MoveToSystem`
4. Мёртвые сущности (`IsDead() == true`) удаляются из всех систем и уничтожаются

### Алгоритм движения (MoveSystem)

**Файл:** `Assets/Scripts/Model/Systems/MoveSystem.cs`

Прямолинейное движение с заворачиванием (wrapping) по игровой области:
```
position = oldPosition + direction * speed * deltaTime
PlaceWithinGameArea(ref position.x, gameArea.x)
PlaceWithinGameArea(ref position.y, gameArea.y)
```

**Wrapping-алгоритм:** `Assets/Scripts/Model/Model.cs:156-167`
- Если позиция выходит за `+side/2`, переносится в `−side + position`.
- Если позиция выходит за `−side/2`, переносится в `+side − position`.
- Применяется отдельно для X и Y, создавая тороидальную топологию.

### Алгоритм вращения (RotateSystem)

**Файл:** `Assets/Scripts/Model/Systems/RotateSystem.cs`

- Вращение через Quaternion: `Quaternion.Euler(0, 0, 90 * deltaTime * direction) * currentRotation`
- Скорость: фиксированная `90` градусов/сек (`RotateComponent.DegreePerSecond` в `Assets/Scripts/Model/Components/RotateComponent.cs:8`)
- `TargetDirection` = -1 (по часовой), +1 (против часовой), 0 (нет вращения)
- Результат хранится как `ObservableValue<Vector2>` (единичный вектор направления), по умолчанию `Vector2.right`

### Алгоритм тяги (ThrustSystem)

**Файл:** `Assets/Scripts/Model/Systems/ThrustSystem.cs`

**При активной тяге:**
```
acceleration = unitsPerSecond * deltaTime
velocity = direction * speed + rotationVector * acceleration
direction = velocity.normalized
speed = Min(velocity.magnitude, maxSpeed)
```
Тяга добавляет вектор ускорения в направлении текущего поворота к текущему вектору скорости. Скорость ограничена `MaxSpeed`.

**При неактивной тяге (инерционное торможение):**
```
speed = Max(speed - unitsPerSecond / 2 * deltaTime, 0.0)
```
Замедление в 2 раза медленнее, чем ускорение. Минимальная скорость `0.0` (`ThrustComponent.MinSpeed`).

### Алгоритм стрельбы пушкой (GunSystem)

**Файл:** `Assets/Scripts/Model/Systems/GunSystem.cs`

Система перезарядки с пулом выстрелов:
1. Если `CurrentShoots < MaxShoots`, уменьшаем `ReloadRemaining` на `deltaTime`.
2. Когда `ReloadRemaining <= 0`, обнуляем таймер (`= ReloadDurationSec`) и восстанавливаем ВСЕ выстрелы (`CurrentShoots = MaxShoots`).
3. Если `Shooting == true` и `CurrentShoots > 0` — расходуем один выстрел, вызываем `OnShooting` callback.
4. Флаг `Shooting` сбрасывается в `false` каждый кадр.

Таким образом, перезарядка восстанавливает сразу все выстрелы разом, а не по одному.

### Алгоритм лазера (LaserSystem)

**Файл:** `Assets/Scripts/Model/Systems/LaserSystem.cs`

Отличие от пушки: лазер восстанавливает выстрелы **по одному** (`CurrentShoots += 1`) и использует `ObservableValue` для реактивного обновления HUD.

**Механика лазерного луча:** `Assets/Scripts/Application/Game.cs:210-238`
1. Создаётся визуальный эффект `LineRenderer` из пулa объектов.
2. Эффект позиционируется на корабле, поворачивается по направлению корабля.
3. Эффект удаляется через `ActionScheduler` после `BeamEffectLifetimeSec`.
4. Физический рейкаст: `Physics2D.RaycastNonAlloc` с буфером на 30 хитов.
5. Маска слоёв: `"Asteroid"`, `"Enemy"`.
6. Максимальная длина луча: `gameArea.magnitude` (диагональ игровой области).
7. Все попавшие сущности уничтожаются и засчитываются в очки.

### Алгоритм упреждающей стрельбы врагов (ShootToSystem)

**Файл:** `Assets/Scripts/Model/Systems/ShootToSystem.cs`

UFO используют предиктивное прицеливание:
```
time = distance_to_ship / (bullet_speed - ship_speed)
pendingPosition = shipPosition + shipDirection * shipSpeed * time
direction = (pendingPosition - ufoPosition).normalized
```
- Скорость пули захардкожена как `20` (`Assets/Scripts/Model/Systems/ShootToSystem.cs:17`)
- Стреляет каждый кадр, когда есть доступные выстрелы (`CurrentShoots > 0`)

### Алгоритм упреждающего преследования (MoveToSystem)

**Файл:** `Assets/Scripts/Model/Systems/MoveToSystem.cs`

Малый UFO (`UfoModel`) использует периодическое обновление курса (каждые `Every` секунд, захардкожено 3.0 в `EntitiesCatalog.cs:149`):
```
time = distance_to_ship / (ufo_speed - ship_speed)
pendingPosition = shipPosition + shipDirection * shipSpeed * time
direction = (pendingPosition - ufoPosition).normalized
```
Алгоритм аналогичен `ShootToSystem`, но вычисляет направление движения, а не стрельбы.

### Алгоритм спавна врагов

**Файл:** `Assets/Scripts/Application/Game.cs:77-115`

- Каждые `SpawnNewEnemyDurationSec` секунд через `ActionScheduler` спавнится один враг.
- Тип врага выбирается случайно с равной вероятностью (1/3): астероид, малый UFO, большой UFO.
- При старте создаётся `AsteroidInitialCount` астероидов.

**Позиция спавна астероидов:** `Assets/Scripts/Utils/GameUtils.cs:24-37`
- Случайная позиция в пределах игровой области.
- Если расстояние до корабля меньше `SpawnAllowedRadius`, позиция корректируется, чтобы отодвинуться от корабля.

**Позиция спавна UFO:** `Assets/Scripts/Utils/GameUtils.cs:9-22`
- X-координата = 0 (левый край, минус половина ширины).
- Y-координата случайная в пределах высоты.
- Вертикальная коррекция, если UFO слишком близко к кораблю.

**Направление больших UFO:** `Assets/Scripts/Application/Game.cs:105`
```
(Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized
```
Вектор с сильным горизонтальным перекосом — BigUfo летят преимущественно горизонтально.

**Направление малых UFO:** `Random.insideUnitCircle.normalized` — равномерное случайное направление.

### Алгоритм дробления астероидов

**Файл:** `Assets/Scripts/Application/Game.cs:170-195`

При уничтожении астероида:
1. Уменьшается `Age` на 1 (начальный = 3, выбирается в `SpawnAsteroid`).
2. Если `Age <= 0`, осколков нет.
3. Иначе создаются 2 новых астероида:
   - Того же возраста.
   - На той же позиции.
   - Скорость: `Min(originalSpeed * 2, 10.0)` — удвоение с верхним лимитом 10.
   - Направление: `Random.insideUnitCircle` (новое случайное направление).
4. Размер визуала определяется конфигом: `Age 3` -> `AsteroidBig`, `Age 2` -> `AsteroidMedium`, `Age 1` -> `AsteroidSmall`.
5. Спрайт выбирается случайно из `SpriteVariants[]` конфига.

### Алгоритм подсчёта очков

**Файл:** `Assets/Scripts/Model/Model.cs:108-122`

Очки начисляются из поля `Score` соответствующего `ScriptableObject` конфига (`AsteroidData`, `UfoData`). Каждый тип врага имеет индивидуальную стоимость. При отправке в лидерборд берётся `Max(текущий_счёт, лучший_счёт_с_сервера)` (`GameScreen.cs:233`).

### Алгоритм обработки коллизий

Коллизии обрабатываются через Unity Physics2D + `OnCollisionEnter2D`:
- **Корабль:** `ShipVisual.OnCollisionEnter2D` -> `Game.OnShipCollided` -> корабль уничтожается, игра завершается.
- **Пуля игрока:** `BulletVisual.OnCollisionEnter2D` -> `Game.OnUserBulletCollided`:
  - Пуля уничтожается.
  - Коллайдер цели отключается (`col.otherCollider.enabled = false`).
  - Ищется модель по `GameObject` через `EntitiesCatalog.TryFindModel<T>`.
  - Начисляются очки, цель уничтожается.
- **Пуля врага:** `Game.OnEnemyBulletCollided` — пуля уничтожается, коллайдер цели отключается.
- **UFO:** `UfoVisual.OnCollisionEnter2D` -> `Game.OnUfoCollided` -> UFO уничтожается.

Пули имеют ограниченное время жизни (`LifeTimeSystem`), автоматически уничтожаются когда `TimeRemaining <= 0`.

### ActionScheduler — планировщик отложенных действий

**Файл:** `Assets/Scripts/Model/ActionScheduler.cs`

- Оптимизированный таймерный планировщик с `_nextUpdateDuration` для раннего выхода (fast-path).
- Удаление элементов: swap-and-pop (перемещение последнего элемента на место удалённого, O(1)).
- Используется для: спавна врагов, удаления визуальных эффектов лазера.

### Object Pooling

**Файл:** `Assets/Scripts/Utils/GameObjectPool.cs`

- Пул по prefab Instance ID (`Dictionary<string, Stack<GameObject>>`).
- При возврате: деактивация объекта, перемещение под pool-контейнер.
- При получении: если есть в стеке — повторное использование, иначе `Instantiate`.
- Обратное отображение `_gameObjectToPrefabId` для O(1) возврата.

**Фабрика моделей:** `Assets/Scripts/Application/ModelFactory.cs`
- Пулинга моделей пока нет (TODO в коде) — каждый раз создаётся `new TModel()`.

### Система ввода (Input System)

**Файл:** `Assets/Scripts/Input/PlayerInput.cs`

Input Actions (из `Assets/Input/player_actions.inputactions`):

| Действие | Тип | Клавиша | Описание |
|---|---|---|---|
| Attack | Button | Space | Выстрел из пушки |
| Rotate | Value (Axis) | A/D | Вращение корабля (1DAxis: A=negative, D=positive, инвертирован: min=1, max=-1) |
| Accelerate | Value | W | Активация тяги |
| Laser | Button | Q | Выстрел лазером |
| Back | Button | Escape | Выход из игры (не на WebGL) |
| Restart | Button | Space | Рестарт игры |

Обёртка `PlayerInput` подписывается на `performed` и `canceled` события и транслирует их в C#-события (`Action<>`, `Action<float>`, `Action<bool>`).

### MVVM-привязки (Shtl.Mvvm)

Фреймворк `com.shtl.mvvm` используется на 3 уровнях:

1. **Model -> ViewModel (EntitiesCatalog):** `EventBindingContext` связывает `ObservableValue<T>` из модели с `ReactiveValue<T>` во ViewModel.
   ```csharp
   bindings.From(model.Move.Position).To(viewModel.Position);
   ```

2. **ViewModel -> View (Visual-компоненты):** Bind-расширения связывают `ReactiveValue<T>` с UI-компонентами.
   ```csharp
   Bind.From(ViewModel.Position).To(transform);
   Bind.From(ViewModel.Score).To(_scoreText);
   ```

3. **Screen-level привязки:** `AbstractScreen` содержит `EventBindingContext` для привязок экранных данных.

**Ключевые типы Shtl.Mvvm:**
- `ObservableValue<T>` — наблюдаемое значение в Model-слое
- `ReactiveValue<T>` — наблюдаемое значение в ViewModel-слое
- `ReactiveList<T>` — наблюдаемый список (используется для записей лидерборда)
- `EventBindingContext` — контекст привязок с методом `CleanUp()` для отписки
- `AbstractViewModel` — базовый ViewModel
- `AbstractWidgetView<T>` — базовый View, параметризованный ViewModel

**Кастомное расширение привязок:** `Assets/Scripts/View/Bindings/BindingToExtensions.cs`
- `To(Transform)` — связывает `ReactiveValue<Vector2>` с `transform.position` (копирует X/Y, оставляя Z).

---

## Platform Requirements

**Development:**
- Unity 2022.3.60f1 LTS
- macOS (текущая конфигурация), Windows (поддерживается через WindowsStandalone64)

**Production:**
- WebGL (поддержка через `#if !UNITY_WEBGL` для отключения Escape-выхода)
- Windows Standalone 64-bit

---

*Анализ стека: 2026-04-02*
