# Architecture Patterns

**Domain:** Самонаводящиеся ракеты для Asteroids (гибридный DOTS)
**Researched:** 2026-04-05
**Confidence:** HIGH (основано на анализе существующей кодовой базы, установленные паттерны)

## Recommended Architecture

Ракеты интегрируются в существующий гибридный DOTS по тому же паттерну, что пули и лазер: ECS-компоненты для данных, ISystem для логики, GameObjectRef для визуальной синхронизации, CollisionBridge для физики, MVVM для HUD. Никаких новых архитектурных решений -- только расширение существующих паттернов.

### Архитектурная схема

```
Input (R key)
    |
    v
Game.OnRocket() --- читает ShipTag entity, проверяет RocketAmmoData
    |                  устанавливает RocketAmmoData.Shooting = true
    v
EcsRocketAmmoSystem (ISystem)
    |--- перезарядка: ReloadRemaining -= dt, CurrentShoots += 1
    |--- стрельба: Shooting && CurrentShoots > 0 -> RocketLaunchEvent
    v
ShootEventProcessorSystem (SystemBase, расширяется)
    |--- ProcessRocketEvents(): читает DynamicBuffer<RocketLaunchEvent>
    |--- catalog.CreateRocket(position, direction)
    v
EntitiesCatalog.CreateRocket()
    |--- EntityFactory.CreateRocket(em, pos, dir, speed, turnRate, lifeTime)
    |--- viewFactory.Get<RocketVisual>(prefab)
    |--- collisionBridge.RegisterMapping()
    v
EcsRocketHomingSystem (ISystem)
    |--- Для каждой entity с RocketHomingData + MoveData:
    |    1. Найти ближайшую цель (Asteroid/Ufo/UfoBig без DeadTag)
    |    2. Рассчитать желаемое направление к цели
    |    3. Плавно повернуть MoveData.Direction (turnRate * dt)
    v
EcsMoveSystem (существующий, Burst) --- двигает ракету по MoveData
    v
GameObjectSyncSystem (модифицируется) --- синхронизирует Transform + Rotation для ракет
    v
CollisionBridge -> CollisionEventData -> EcsCollisionHandlerSystem
    |--- PlayerRocketTag + Enemy -> DeadTag обоим + Score
    v
DeadEntityCleanupSystem --- удаляет entity, возвращает GO в пул
```

### Component Boundaries

| Компонент | Ответственность | Взаимодействует с |
|-----------|----------------|-------------------|
| `RocketAmmoData` (IComponentData) | Боезапас ракет на корабле: CurrentShoots, MaxShoots, ReloadDurationSec, ReloadRemaining, Shooting, Direction, ShootPosition | EcsRocketAmmoSystem, ObservableBridgeSystem |
| `RocketHomingData` (IComponentData) | Параметры наведения ракеты: TurnRateDegPerSec | EcsRocketHomingSystem |
| `RocketLaunchEvent` (IBufferElementData) | Событие запуска ракеты: ShooterEntity, Position, Direction | EcsRocketAmmoSystem -> ShootEventProcessorSystem |
| `PlayerRocketTag` (IComponentData, tag) | Маркер принадлежности ракеты игроку | EcsCollisionHandlerSystem |
| `EcsRocketAmmoSystem` (ISystem) | Перезарядка + генерация RocketLaunchEvent | ShipTag entity с RocketAmmoData |
| `EcsRocketHomingSystem` (ISystem) | Поиск цели + поворот MoveData.Direction | Все entities с RocketHomingData, все вражеские entities |
| `RocketVisual` (MonoBehaviour) | Спрайт + ParticleSystem (инверсионный след) + OnCollisionEnter2D | GameObjectSyncSystem |

### Data Flow

**Запуск ракеты (аналог Gun/Laser):**
1. `PlayerInput.OnRocketAction` -> `Game.OnRocket()`
2. `Game` находит ShipTag entity через `TryGetShipEntity()`, устанавливает `RocketAmmoData.Shooting = true`, заполняет `Direction` из `RotateData.Rotation`, `ShootPosition` из `MoveData.Position`
3. `EcsRocketAmmoSystem.OnUpdate()` проверяет `CurrentShoots > 0`, генерирует `RocketLaunchEvent` в DynamicBuffer, уменьшает `CurrentShoots`
4. `ShootEventProcessorSystem.ProcessRocketEvents()` вызывает `EntitiesCatalog.CreateRocket()`
5. `EntitiesCatalog.CreateRocket()` создает ECS entity (MoveData + LifeTimeData + RocketHomingData + PlayerRocketTag) + GameObject visual

**Наведение (каждый кадр):**
1. `EcsRocketHomingSystem` итерирует все entities с `RocketHomingData + MoveData`
2. Для каждой ракеты ищет ближайшую вражескую entity (AsteroidTag|UfoTag|UfoBigTag, без DeadTag) через отдельный query
3. Рассчитывает угол к цели, поворачивает `MoveData.Direction` на `TurnRateDegPerSec * dt` через RotateTowards
4. Если врагов нет -- ракета летит прямо (Direction не меняется)
5. `EcsMoveSystem` двигает ракету по обновленному `MoveData.Direction * Speed`
6. `GameObjectSyncSystem` синхронизирует Transform.position из MoveData.Position + Transform.rotation из MoveData.Direction

**Коллизия (существующий паттерн):**
1. `RocketVisual.OnCollisionEnter2D` -> `CollisionBridge.ReportCollision()`
2. `CollisionBridge` добавляет `CollisionEventData` в buffer entity
3. `EcsCollisionHandlerSystem.ProcessCollision()` обрабатывает: `PlayerRocketTag + Enemy -> DeadTag + Score`
4. `DeadEntityCleanupSystem` уничтожает entity, callback возвращает GO в пул

**HUD обновление (расширение ObservableBridgeSystem):**
1. `ObservableBridgeSystem` уже читает ShipTag entities -- добавляется чтение `RocketAmmoData`
2. Обновляет `HudData.RocketCount` и `HudData.RocketReloadTime`

## Новые файлы (создать)

### ECS Components

| Файл | Путь | Тип | Поля |
|------|------|-----|------|
| `RocketAmmoData.cs` | `ECS/Components/` | `IComponentData` | `int MaxShoots`, `float ReloadDurationSec`, `int CurrentShoots`, `float ReloadRemaining`, `bool Shooting`, `float2 Direction`, `float2 ShootPosition` |
| `RocketHomingData.cs` | `ECS/Components/` | `IComponentData` | `float TurnRateDegPerSec` |
| `RocketLaunchEvent.cs` | `ECS/Components/` | `IBufferElementData` | `Entity ShooterEntity`, `float2 Position`, `float2 Direction` |
| `PlayerRocketTag.cs` | `ECS/Components/Tags/` | `IComponentData` | -- (пустой struct) |

### ECS Systems

| Файл | Путь | Тип | Burst | System Ordering |
|------|------|-----|-------|-----------------|
| `EcsRocketAmmoSystem.cs` | `ECS/Systems/` | `ISystem` | Возможен (аналог EcsGunSystem) | `[UpdateAfter(EcsLaserSystem)]` |
| `EcsRocketHomingSystem.cs` | `ECS/Systems/` | `ISystem` | Нет (EntityQuery iteration для поиска целей) | `[UpdateBefore(EcsMoveSystem)]` |

### Visual

| Файл | Путь | Тип | Назначение |
|------|------|-----|------------|
| `RocketVisual.cs` | `View/` | `BaseVisual` + `RocketViewModel` | Спрайт (уменьшенный корабль) + ParticleSystem trail + OnCollisionEnter2D -> CollisionBridge |

## Модифицируемые файлы (существующие)

| Файл | Изменение | Паттерн-образец |
|------|-----------|-----------------|
| **EntityFactory.cs** | Добавить `CreateRocket(em, pos, dir, speed, turnRate, lifeTime)` -- entity с MoveData + LifeTimeData + RocketHomingData + PlayerRocketTag | `CreateBullet()` |
| **EntityFactory.cs** | Модифицировать `CreateShip()` -- добавить `RocketAmmoData` параметры и `em.AddComponentData(entity, new RocketAmmoData{...})` | Аналогично GunData/LaserData в CreateShip |
| **EntitiesCatalog.cs** | Добавить `EntityType.Rocket` в enum. Добавить метод `CreateRocket()` -- ViewModel + ViewFactory.Get + EntityFactory.CreateRocket + GameObjectRef + CollisionBridge.RegisterMapping | `CreateBullet()` |
| **EcsCollisionHandlerSystem.cs** | Добавить метод `IsPlayerRocket(ref EntityManager em, Entity entity)` -> `HasComponent<PlayerRocketTag>`. Добавить 2 блока в `ProcessCollision()`: PlayerRocket + Enemy -> MarkDead обоим + AddScore | `IsPlayerBullet()` блоки |
| **ShootEventProcessorSystem.cs** | Добавить `ProcessRocketEvents()` -- чтение `DynamicBuffer<RocketLaunchEvent>`, вызов `_catalog.CreateRocket()`. Вызвать из `OnUpdate()` | `ProcessGunEvents()` |
| **ObservableBridgeSystem.cs** | Добавить чтение `RocketAmmoData` из ShipTag query (расширить существующий foreach). Записать в `_hudData.RocketCount` и `_hudData.RocketReloadTime` | Аналогично чтению LaserData |
| **GameObjectSyncSystem.cs** | Добавить третий query: `MoveData + RocketHomingData + GameObjectRef, WithNone<RotateData>` -- sync position + вычисление rotation из MoveData.Direction | Первый query (MoveData + RotateData) |
| **HudData.cs** | Добавить `ReactiveValue<string> RocketCount`, `ReactiveValue<string> RocketReloadTime`, `ReactiveValue<bool> IsRocketReloadTimeVisible` | Аналогично LaserShootCount/LaserReloadTime |
| **HudVisual.cs** | Добавить `[SerializeField] TMP_Text _rocketCount`, `_rocketReloadTime`. Bind в `OnConnected()` | Аналогично laser bindings |
| **GameData.cs** | Добавить `[Serializable] struct RocketData { GameObject Prefab; float Speed; float TurnRateDegPerSec; float LifeTimeSec; int MaxShoots; float ReloadDurationSec; }` + поле `public RocketData Rocket;` | `BulletData` / `LaserData` struct |
| **Game.cs** | Добавить `OnRocket()` handler (аналог `OnLaser()`). Подписка/отписка в `Start()`/`Stop()`. Очистка `RocketLaunchEvent` buffer в `ClearEcsEventBuffers()` | `OnLaser()` + `ClearEcsEventBuffers()` |
| **PlayerInput.cs** | Добавить `event Action OnRocketAction` + `OnRocket(InputAction.CallbackContext)` handler | `OnLaserAction` |
| **player_actions.inputactions** | Добавить действие `Rocket` (Button, binding: R key) в PlayerControls action map | `Laser` action |

## Patterns to Follow

### Pattern 1: Ammo System (GunData/LaserData -> RocketAmmoData)

**Что:** `RocketAmmoData` -- IComponentData на ShipTag entity. Структура идентична GunData/LaserData: MaxShoots, ReloadDurationSec, CurrentShoots, ReloadRemaining, Shooting, Direction, ShootPosition.
**Когда:** Всегда -- установленный паттерн проекта для каждого типа оружия.
**Обоснование:** Gun и Laser используют одинаковую схему "ammo + reload + shooting flag + event buffer". Ракета -- третий тип оружия, та же схема.

```csharp
public struct RocketAmmoData : IComponentData
{
    public int MaxShoots;
    public float ReloadDurationSec;
    public int CurrentShoots;
    public float ReloadRemaining;
    public bool Shooting;
    public float2 Direction;
    public float2 ShootPosition;
}
```

### Pattern 2: Event Buffer -> ShootEventProcessor (GunShootEvent -> RocketLaunchEvent)

**Что:** ECS-система записывает событие в `DynamicBuffer<RocketLaunchEvent>`. `ShootEventProcessorSystem` (managed SystemBase) читает буфер и создает entity через `EntitiesCatalog`.
**Почему managed:** Создание entity + GameObject требует ViewFactory, GameObjectPool, CollisionBridge -- все managed-объекты. Нельзя в Burst.
**Образец:** `EcsGunSystem` записывает `GunShootEvent`, `ShootEventProcessorSystem.ProcessGunEvents()` создает пулю.

```csharp
public struct RocketLaunchEvent : IBufferElementData
{
    public Entity ShooterEntity;
    public float2 Position;
    public float2 Direction;
}
```

### Pattern 3: Homing через поворот MoveData.Direction

**Что:** `EcsRocketHomingSystem` НЕ создает свой компонент движения. Вместо этого плавно поворачивает `MoveData.Direction` к цели. `EcsMoveSystem` затем двигает entity по обновленному направлению. Тороидальная телепортация -- автоматически.
**Почему:** Переиспользует EcsMoveSystem (Burst, тороидальная обертка) и GameObjectSyncSystem (sync Transform).

```csharp
// В EcsRocketHomingSystem.OnUpdate:
var toTarget = math.normalizesafe(targetPos - rocketPos);
var currentAngle = math.atan2(dir.y, dir.x);
var targetAngle = math.atan2(toTarget.y, toTarget.x);
var maxTurn = math.radians(homing.ValueRO.TurnRateDegPerSec) * deltaTime;
var newAngle = MoveAngleTowards(currentAngle, targetAngle, maxTurn);
move.ValueRW.Direction = new float2(math.cos(newAngle), math.sin(newAngle));
```

### Pattern 4: Visual rotation из MoveData.Direction для ракет

**Что:** GameObjectSyncSystem имеет 2 query: (1) MoveData + RotateData (корабль, UFO -- rotation из RotateData), (2) MoveData без RotateData (астероиды, пули -- только позиция). Ракетам нужна позиция + rotation из Direction.
**Решение:** Третий query: `MoveData + RocketHomingData + GameObjectRef, WithNone<RotateData>` -- вычисляет rotation из `MoveData.Direction`.
**Почему не RotateData:** `RotateData` привязана к Input-управлению (TargetDirection от игрока). Ракета управляется HomingSystem, не Input.

```csharp
// Третий query в GameObjectSyncSystem:
foreach (var (move, homing, goRef) in
         SystemAPI.Query<RefRO<MoveData>, RefRO<RocketHomingData>, GameObjectRef>())
{
    var pos = move.ValueRO.Position;
    goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);
    var dir = move.ValueRO.Direction;
    var angle = math.atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    goRef.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
}
```

### Pattern 5: Lifecycle через LifeTimeData + DeadTag

**Что:** Ракета имеет `LifeTimeData` -- если не попала ни в кого, `EcsDeadByLifeTimeSystem` ставит `DeadTag`. При коллизии `EcsCollisionHandlerSystem` ставит `DeadTag`. `DeadEntityCleanupSystem` уничтожает entity.
**Образец:** `BulletTag` entity с `LifeTimeData` -- идентичный паттерн.

### Pattern 6: Collision через CollisionBridge -> EcsCollisionHandlerSystem

**Что:** `RocketVisual.OnCollisionEnter2D` вызывает `CollisionBridge.ReportCollision()`. Это добавляет `CollisionEventData` в buffer entity. `EcsCollisionHandlerSystem` обрабатывает: `IsPlayerRocket && IsEnemy -> MarkDead + AddScore`.
**Образец:** Полностью идентично пулям (`IsPlayerBullet + IsEnemy`).

## Anti-Patterns to Avoid

### Anti-Pattern 1: Отдельная система движения для ракет

**Что:** Создание `EcsRocketMoveSystem` с дублированием тороидальной телепортации.
**Почему плохо:** DRY. EcsMoveSystem уже обрабатывает все entities с MoveData.
**Вместо:** Ракета имеет MoveData -- EcsMoveSystem двигает автоматически. EcsRocketHomingSystem только поворачивает Direction.

### Anti-Pattern 2: Хранение ссылки на цель в компоненте

**Что:** `RocketHomingData.TargetEntity` с кешированной ссылкой на цель.
**Почему плохо:** Цель может получить DeadTag в любой момент (другая ракета/пуля попала раньше). Нужна проверка валидности каждый кадр. Entity может быть уничтожена `DeadEntityCleanupSystem`.
**Вместо:** Каждый кадр искать ближайшую цель через query. При 20 ракетах и 50 врагах = 1000 distance-проверок -- незаметно для производительности. Нет stale-ссылок.

### Anti-Pattern 3: Создание entity из Game.OnRocket() напрямую

**Что:** Вызов `EntitiesCatalog.CreateRocket()` из input handler, минуя Event -> System -> Processor.
**Почему плохо:** (1) Нарушает установленный паттерн Gun/Laser; (2) Structural changes в непредсказуемый момент; (3) Нет проверки ammo в ECS-системе.
**Вместо:** `Game.OnRocket()` устанавливает только `RocketAmmoData.Shooting = true`. Весь остальной pipeline через системы.

### Anti-Pattern 4: ScoreValue на ракете

**Что:** Добавление `ScoreValue` компонента на entity ракеты.
**Почему плохо:** `ScoreValue` -- очки за уничтожение entity (носитель -- враг). `AddScore` в CollisionHandler читает ScoreValue с вражеской entity, не с projectile.
**Вместо:** Ракета без ScoreValue. Очки начисляются за врага, как при попадании пулей.

## Scalability Considerations

| Concern | Текущий масштаб (1-5 ракет) | Потенциальное масштабирование |
|---------|-----|-----|
| Поиск ближайшей цели | Линейный перебор O(N*M). 5 ракет * 50 врагов = 250 проверок -- незаметно | При 100+ ракет рассмотреть spatial hash |
| ParticleSystem trail | 1 система частиц на ракету. 5 систем -- нормально | При 50+ рассмотреть Trail Renderer |
| Collision layers | Ракеты на слое PlayerBullet -- Physics2D обрабатывает | Отдельный layer "Rocket" если нужна иная collision matrix |
| EcsRocketHomingSystem без Burst | Main thread query. При 5 ракетах не bottleneck | Burst + ComponentLookup для 100+ |

## Suggested Build Order

Порядок учитывает зависимости: каждый шаг тестируем изолированно. Аналогия с тем, как были построены существующие системы.

### Phase 1: ECS Core (данные + логика без визуала)

**Порядок внутри фазы:**
1. **RocketAmmoData** (IComponentData) -- struct по образцу GunData/LaserData
2. **RocketLaunchEvent** (IBufferElementData) -- по образцу GunShootEvent
3. **EcsRocketAmmoSystem** (ISystem) -- перезарядка + генерация события. TDD: тесты перезарядки, списания, Shooting flag
4. **RocketHomingData** (IComponentData) -- struct с TurnRateDegPerSec
5. **PlayerRocketTag** (IComponentData) -- пустой tag
6. **EcsRocketHomingSystem** (ISystem) -- поиск цели + поворот Direction. TDD: поворот к цели, выбор ближайшей, нет целей = прямо
7. **EntityFactory.CreateRocket()** -- сборка entity. TDD: проверка компонентов
8. **EntityFactory.CreateShip()** -- добавить RocketAmmoData. TDD: проверка наличия

**Зависимости:** Нет внешних -- чистый ECS, EditMode тесты.

### Phase 2: Collision Integration

1. **EcsCollisionHandlerSystem** -- добавить `IsPlayerRocket()` + обработку `PlayerRocketTag + Enemy`. TDD: все комбинации коллизий (Rocket+Asteroid, Rocket+Ufo, Rocket+UfoBig, Rocket+Ship=ignore, Rocket+Bullet=ignore)

**Зависимости:** Phase 1 (PlayerRocketTag).

### Phase 3: Bridge Layer + Entity Lifecycle

1. **ShootEventProcessorSystem.ProcessRocketEvents()** -- чтение буфера, вызов catalog
2. **EntitiesCatalog.CreateRocket()** + **EntityType.Rocket** -- полный lifecycle
3. **GameObjectSyncSystem** -- третий query для RocketHomingData entities (position + rotation)
4. **RocketVisual + RocketViewModel** -- MonoBehaviour с OnCollisionEnter2D, ParticleSystem trail

**Зависимости:** Phase 1 + 2.

### Phase 4: Input + Game Integration

1. **player_actions.inputactions** -- действие Rocket (R key)
2. **PlayerInput.cs** -- `OnRocketAction` event
3. **Game.cs** -- `OnRocket()` handler, подписка/отписка в Start/Stop
4. **Game.cs:ClearEcsEventBuffers()** -- очистка RocketLaunchEvent buffer при Restart

**Зависимости:** Phase 3 (EntitiesCatalog.CreateRocket).

### Phase 5: Config + Prefab

1. **GameData.RocketData** struct + поле в GameData
2. **Prefab** -- GameObject с SpriteRenderer + Collider2D + ParticleSystem + RocketVisual
3. **ScriptableObject** -- заполнение значений в инспекторе

**Зависимости:** Phase 4.

### Phase 6: HUD

1. **HudData** -- RocketCount, RocketReloadTime, IsRocketReloadTimeVisible
2. **HudVisual** -- TMP_Text bindings
3. **ObservableBridgeSystem** -- чтение RocketAmmoData, запись в HudData
4. **Prefab HUD** -- добавить текстовые элементы на Canvas

**Зависимости:** Phase 1 (RocketAmmoData) + Phase 5 (визуальная сцена).

## Key Architecture Decision: Burst-совместимость EcsRocketHomingSystem

**Проблема:** Поиск ближайшей вражеской entity требует итерации по всем врагам с проверкой наличия тегов и чтением MoveData.Position.

**Вариант A (рекомендуется): partial struct ISystem без [BurstCompile]**
- Использует `SystemAPI.Query<>()` для итерации по ракетам
- Отдельный foreach через `SystemAPI.Query<RefRO<MoveData>>().WithAny<AsteroidTag, UfoTag, UfoBigTag>().WithNone<DeadTag>()` для поиска целей
- Простой, читаемый код. Без Burst, но при 5 ракетах и 50 врагах это 250 distance-проверок на main thread -- незаметно

**Вариант B: [BurstCompile] ISystem с ComponentLookup**
- NativeArray целей предсобирается в OnUpdate через query
- Ракеты итерируют NativeArray для поиска ближайшей
- Burst-совместимо, но сложнее в реализации и тестировании

**Рекомендация:** Вариант A. При масштабе Asteroids Burst для homing не нужен. Если потребуется оптимизация -- переход на B локализован в одной системе.

## Sources

- Кодовая база проекта: EntityFactory.cs, EcsCollisionHandlerSystem.cs, EcsGunSystem.cs, EcsLaserSystem.cs, ShootEventProcessorSystem.cs, ObservableBridgeSystem.cs, GameObjectSyncSystem.cs, DeadEntityCleanupSystem.cs, EntitiesCatalog.cs, Game.cs, HudVisual.cs, HudData.cs, PlayerInput.cs, CollisionBridge.cs, GameObjectRef.cs, MoveData.cs, GunData.cs, LaserData.cs, LifeTimeData.cs
- PROJECT.md: архитектурные constraint, milestone context
- Confidence: HIGH -- все паттерны подтверждены работающим кодом в v1.1.0
