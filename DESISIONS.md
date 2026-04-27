# DESISIONS — Самонаводящиеся ракеты

Файл фиксирует исходный промт, дизайн фичи, ключевые архитектурные решения и оценку токенов работы. Создан в начале сессии (имя файла соответствует орфографии задачи пользователя).

## 1. Исходный промт

> Сомонаводящиеся ракеты. Нужно сделать новую фичу. Добавим самонаводящиеся ракеты. У игрока есть одна ракета, которую можно запустить, нажав кнопку R. Ракета летит по дуге в ближайшую цель. Если ракета по пути врежется не в выбранную цель, это тоже считается. После запуска ракет включается счётчик на респавн ракет. Количество ракет у игрока и время респавна должно быть задано в конфигах. Ракета коллайдится с астероидами и UFO. В качестве визала ракеты можно использовать уменьшенный спрайт корабля. Инверсионный след можно сделать из спрайта частиц. Ракета должна быть создана и вписана в текущую архитектуру ECS + визуал. Количество доступных ракет и время респавна должно выводиться в HUD-е игрока. Фича должна быть разработана в парадигме TDD, весь функционал покрыт тестами. Проверка функционала при помощи юнит, интеграционных тестов и MCP. Human validation в исключительных случаях, когда текущих инструментов недостаточно и невозможно написать новый функционал для MCP.

Дополнительные ограничения:
- Перед выполнением убедиться, что MCP работает.
- Все решения, оценку по токенам и промт записать в `DESISIONS.md`.

## 2. Контекст и проверка готовности

- **MCP проверен** — Unity Editor 6000.3.13f1 (Unity 6.3 LTS), `editor-application-get-state` отвечает корректно. MCP готов.
- **Архитектура** — milestone v1.0 (Unity 6.3 + URP + DOTS Entities) уже завершён. Используем DOTS:
  - Components: `IComponentData` структуры в `AsteroidsECS.asmdef`.
  - Systems: `ISystem` (Burst-compiled) для логики, `SystemBase` для bridge с GameObject.
  - Tags: `PlayerBulletTag`, `EnemyBulletTag`, `ShipTag`, `AsteroidTag`, `UfoTag`, `UfoBigTag`, `DeadTag`.
  - Bridge: `CollisionBridge` (GO↔Entity), `ShootEventProcessorSystem`, `EcsCollisionHandlerSystem`, `DeadEntityCleanupSystem`.
- **Тестовая инфра** — `AsteroidsEcsTestFixture` (NUnit, EditMode), 30+ существующих тестов: `EcsGunSystemTests`, `EcsLaserSystemTests`, `ShootToSystemTests`, `MoveSystemTests` и др. Полностью готова к TDD.
- **Свободные клавиши** — `R` свободна для запуска ракеты (Restart=Space, Attack=Space, Rotate=A/D, Accelerate=W, Laser=Q, Back=Esc).

## 3. Ключевые решения

### 3.1 Поведение наведения

Ракета летит **с непрерывным повторным выбором ближайшей цели** (continuous reacquire). Если первичная цель уничтожена — ракета сразу выбирает новую ближайшую. Если ни одной цели в радиусе `SeekRange` нет — ракета летит прямо.

**Дуга** — естественный результат **ограниченной угловой скорости** (`TurnRateDegPerSec`). Меньше turn rate → больше радиус разворота. Не используются Bezier-сплайны: они дают неестественное движение для homing missile и сложнее тестировать.

Алгоритм каждого кадра в `EcsHomingMissileSystem`:
1. Найти ближайший entity с `AsteroidTag | UfoTag | UfoBigTag` без `DeadTag` в радиусе `SeekRange`.
2. Если найден: `desiredDir = normalize(targetPos - missilePos)`, повернуть текущий `MoveData.Direction` к `desiredDir` с шагом не больше `TurnRateRadPerSec * dt`.
3. Если не найден: оставить `MoveData.Direction` без изменений.

Это покрывается чистыми unit-тестами без Unity.PhysX.

### 3.2 Коллизии

> "Если ракета по пути врежется не в выбранную цель, это тоже считается."

Поэтому коллизия ракеты обрабатывается **одинаково для любого врага**: уничтожение врага + начисление очков + уничтожение ракеты. Это идентично правилам PlayerBullet, только с новым тэгом.

Решение: новый `PlayerMissileTag : IComponentData`. В `EcsCollisionHandlerSystem` добавляется case по аналогии с PlayerBullet. Существующие правила PlayerBullet не трогаются — изоляция фичи.

### 3.3 Запас ракет и перезарядка

Аналог `LaserData`: на корабле висит `MissileLauncherData` с инкрементальной перезарядкой (по 1 ракете каждые `ReloadDurationSec`). Это позволяет переиспользовать паттерн HUD/таймера (laser counter + reload visible) и переиспользовать тестовую структуру `EcsLaserSystemTests`.

```csharp
public struct MissileLauncherData : IComponentData {
    public int MaxShoots;             // из конфига
    public float ReloadDurationSec;   // из конфига
    public int CurrentShoots;
    public float ReloadRemaining;
    public bool Shooting;             // взводится Game.OnMissile()
    public float2 Direction;
    public float2 ShootPosition;
}
```

### 3.4 Конфиг

Новая структура в `GameData.cs`:
```csharp
[Serializable]
public struct MissileLauncherConfig {
    public GameObject Prefab;
    public int LifeTimeSeconds;
    public float Speed;
    public float TurnRateDegPerSec;
    public float SeekRange;
    public int MaxShoots;
    public float ReloadDurationSec;
}
public MissileLauncherConfig Missile;
```

Default values для GameData asset: `MaxShoots=1, ReloadDurationSec=10, Speed=8, TurnRateDegPerSec=180, SeekRange=20, LifeTimeSeconds=5`.

### 3.5 Ввод

В `Assets/Input/player_actions.inputactions` добавляется action `Missile` (Button, биндинг `<Keyboard>/r`). В `PlayerInput.cs` — `OnMissileAction` event.

Перегенерация `PlayerActions.cs` через Unity делается автоматически при сохранении `.inputactions` файла. На время перегенерации код может не компилироваться, поэтому: сначала пишу тесты на ECS-систему (не зависят от input), параллельно правлю `.inputactions`, потом проверяю компиляцию через MCP.

### 3.6 HUD

В `HudData` добавляются поля по аналогии с лазером:
- `ReactiveValue<string> MissileShootCount`
- `ReactiveValue<string> MissileReloadTime`
- `ReactiveValue<bool> IsMissileReloadTimeVisible`

В `HudVisual` добавляются `[SerializeField]` для `_missileShootCount` и `_missileReloadTime`. Биндинги в `OnConnected`. Префаб `Assets/Media/prefabs/HUD.prefab` дополняется через Unity MCP (создание новых TMP_Text и привязка через `gameobject-component-modify`).

`ObservableBridgeSystem` — расширяется чтением `MissileLauncherData` с корабля и заполнением соответствующих полей `HudData`. По аналогии с `_laserMaxShoots` хранит `_missileMaxShoots`.

### 3.7 Ракета как entity

`EntityFactory.CreateMissile(em, position, direction, speed, lifeTime, turnRateRad, seekRange)` создаёт entity с компонентами:
- `MissileTag` (общий маркер)
- `PlayerMissileTag` (для коллизионных правил)
- `MoveData { Position, Speed, Direction }`
- `RotateData { Rotation = Direction, TargetDirection = 0 }` — направление спрайта обновляется отдельной маленькой системой `EcsMissileRotateSyncSystem`, которая копирует `MoveData.Direction → RotateData.Rotation` (вращение визуала следует за направлением полёта).
- `HomingMissileData { TurnRateRadPerSec, SeekRange }`
- `LifeTimeData { TimeRemaining }` — переиспользуется существующая `EcsLifeTimeSystem` + `EcsDeadByLifeTimeSystem`.

В `EntitiesCatalog` добавляется `CreateMissile(position, direction)`. `EntityType.Missile` — новое значение enum.

### 3.8 Bridge-обработчики

- `ShootEventProcessorSystem.ProcessMissileEvents` — читает буфер `MissileShootEvent`, для каждого события вызывает `_catalog.CreateMissile`.
- `EcsCollisionHandlerSystem` — case PlayerMissile + Enemy → MarkDead обоих + AddScore.
- `Application.cs.InitializeEcsSingletons` — добавляется буфер `MissileShootEvent` (singleton).
- `Application.cs.OnDeadEntity` — для `EntityType.Missile` играть VFX взрыва (как для UFO/Asteroid).
- `Game.cs.ClearEcsEventBuffers` — очищать буфер MissileShootEvent при рестарте.
- `Game.cs.Start` — подписка на `_playerInput.OnMissileAction += OnMissile`.
- `Game.cs.OnMissile()` — взводит `MissileLauncherData.Shooting = true`.

### 3.9 Запуск через ECS-систему

Новая система `EcsMissileLauncherSystem : ISystem` (UpdateAfter `EcsLaserSystem`). Логика — как у `EcsLaserSystem`: инкрементальная перезарядка, при `Shooting && CurrentShoots > 0` декремент + добавление `MissileShootEvent` в буфер. Полная аналогия структурно.

### 3.10 Префаб ракеты

Создаётся через MCP (Unity tools):
- Базовый префаб `Assets/Media/prefabs/missile.prefab` с компонентами:
  - `Transform` (scale 0.5 относительно ship — "уменьшенный спрайт корабля")
  - `SpriteRenderer` (sprite ship.MainSprite, layer "Bullet" или новый, depth = bullet depth)
  - `Rigidbody2D` (kinematic, как у пули)
  - `CircleCollider2D` (small radius)
  - `MissileVisual` script (новый, копия `BulletVisual` для ECS-bridge)
  - Дочерний GameObject с `ParticleSystem` (инверсионный след)

Префаб создаётся через `assets-prefab-create` + `gameobject-component-add` MCP. Если prefab edit-mode/`gameobject-component-modify` для ParticleSystem окажется недоступным или ограниченным — это исключительный случай для **human validation** (явно разрешено в промте).

Назначение префаба в GameData asset — через `assets-modify` с прокидыванием GUID. Если асс-API не сможет настроить вложенные `[Serializable]` поля — fallback на ручную правку YAML asset (`Read` + `Write` файла с пересчётом GUID/fileID).

## 4. Альтернативы (отклонены)

| Вариант | Почему отклонён |
|---|---|
| **Bezier/spline-дуга** через предвычисленные waypoints | Невозможно реагировать на движение цели; неестественный полёт; сложнее тестировать; больше кода. |
| **Reuse `BulletTag` + `PlayerBulletTag`** для ракеты | Невозможно дифференцировать визуал/VFX/score-логику; смешение коллизионных правил. |
| **Расширить существующий `GunData`** новым параметром "homing" | Перегружает гибкую структуру, ломает изоляцию тестов GunSystem; Single Responsibility сломан. |
| **MoveTo + ShootTo systems как для UFO** | Эти системы предсказывают позицию цели по корабельной скорости — для ракеты, которая сама — снаряд, нужна другая математика (turn rate clamp). |

## 5. Верификация

1. **Unit-тесты (EditMode, NUnit):**
   - `EcsMissileLauncherSystemTests` — 5 тестов: reload increment, no exceed max, shoot decrements, shoot does nothing on no ammo, shoot resets Shooting flag (паттерн `EcsLaserSystemTests`).
   - `EcsHomingMissileSystemTests` — 4 теста:
     - Selects nearest target out of two candidates.
     - Steers direction toward target by at most `TurnRate * dt` per frame (clamp).
     - Keeps direction when no targets exist within `SeekRange`.
     - Ignores DeadTag entities when picking target.
   - `EntityFactoryTests` (расширение) — Missile entity содержит ожидаемые компоненты.
2. **Интеграция:** `tests-run` через MCP по EditMode — все существующие 30+ тестов должны остаться зелёными.
3. **Compilation gate:** `editor-application-get-state` после изменений — `IsCompiling=false` без ошибок.
4. **Runtime smoke:** через MCP `editor-application-set-state` (playmode) проверить, что нажатие `R` запускает ракету (через `console-get-logs` + `screenshot-game-view`). Это для финальной валидации после кода.
5. **Human validation:** только если MCP не позволит настроить `ParticleSystem` trail префаба или назначить prefab в `GameData` asset.

## 6. Оценка токенов

Грубая оценка работы (входные + выходные токены). Не учитывает кэш.

| Этап | Tool calls | Tokens (вход + выход) |
|---|---|---|
| Brainstorming + Explore агенты | 2 параллельных Explore + ~15 read tool calls | ~80K |
| DESISIONS.md (этот файл) | 1 Write | ~12K |
| TDD red — написание тестов (3 файла) | ~5 Write/Edit | ~25K |
| Реализация ECS компонентов и систем (~10 файлов) | ~15 Write/Edit | ~45K |
| Bridge / Конфиг / Ввод / HUD / Catalog | ~10 Edit | ~30K |
| MCP-операции для префаба | ~10–20 MCP tool calls | ~25K |
| Прогон тестов + диагностика | ~5–10 итераций | ~30K |
| **Итого** | | **~250K** (~$3–5 при текущем тарифе Opus 4.7) |

## 7. План исполнения (в порядке)

1. ✅ MCP проверен.
2. ✅ Контекст собран.
3. ✅ DESISIONS.md записан.
4. **TDD red:** написать `EcsMissileLauncherSystemTests`, `EcsHomingMissileSystemTests`. Они должны падать, потому что компонентов и систем ещё нет.
5. **TDD green:** реализовать `MissileTag`, `PlayerMissileTag`, `MissileShootEvent`, `MissileLauncherData`, `HomingMissileData`, `EcsMissileLauncherSystem`, `EcsHomingMissileSystem`, `EcsMissileRotateSyncSystem`, `EntityFactory.CreateMissile`. Тесты зелёные.
6. **Bridge:** обработка событий и коллизий.
7. **Конфиг + Ввод + HUD + Catalog:** интеграция.
8. **Префаб через MCP.**
9. **Прогон всех EditMode тестов.**
10. **Smoke playmode через MCP** (опционально, по результатам).

## 8. Пост-релизный инцидент: цикл в графе ECS-систем

**Симптом (playmode):** `IndexOutOfRangeException` в `Unity.Entities.ComponentSystemSorter.FindExactCycleInSystemGraph` при автоматическом старте мира (`AutomaticWorldBootstrap`). EditMode/PlayMode unit-тесты не ловили проблему — тестовый `World` не запускает `DefaultWorldInitialization`, поэтому атрибуты `[UpdateBefore]/[UpdateAfter]` на нём не сортируются.

**Корневая причина:** в `EcsHomingMissileSystem` стояли одновременно `[UpdateAfter(EcsShipPositionUpdateSystem)]` и `[UpdateBefore(EcsMoveSystem)]`. С учётом существующего `EcsMoveSystem [UpdateBefore(EcsShipPositionUpdateSystem)]` это формирует цикл: `HomingMissile → after ShipPositionUpdate → after MoveSystem → after HomingMissile`.

**Фикс:** убран `[UpdateAfter(EcsShipPositionUpdateSystem)]`. Эта зависимость была лишней — система самонаведения работает с астероидами/UFO, не с положением корабля. Достаточно `[UpdateBefore(EcsMoveSystem)]`, чтобы коррекция направления применялась в том же кадре.

**Верификация через MCP после фикса:** `assets-refresh` (компиляция чистая) → `editor-application-set-state isPlaying=true` → `console-get-logs (Error/Exception, last 2 min)` — пусто → `screenshot-game-view` — отображается title screen без артефактов → playmode остановлен.

**Урок:** при добавлении новой ISystem с атрибутами порядка обязателен реальный playmode-прогон через MCP, а не только unit-тесты на изолированном `World`.
