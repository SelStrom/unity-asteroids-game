# Phase 12: Bridge & Lifecycle - Research

**Researched:** 2026-04-05
**Domain:** Unity ECS -> GameObject bridge, визуал ракеты, lifecycle интеграция
**Confidence:** HIGH

## Summary

Фаза 12 создает визуальное представление ракеты и связывает ECS-данные с GameObject через существующий bridge-слой. Все необходимые ECS-компоненты (RocketTag, MoveData, LifeTimeData, RocketTargetData) уже созданы в Phase 10. Существующие системы (GameObjectSyncSystem, DeadEntityCleanupSystem, ShootEventProcessorSystem) уже обрабатывают аналогичные entity (пули, астероиды) и требуют минимального расширения для поддержки ракет.

Ключевой технический момент -- ракета НЕ имеет `RotateData` (это решение D-02), поэтому вторая ветка `GameObjectSyncSystem` (без RotateData) уже синхронизирует позицию. Однако для вращения спрайта по направлению полёта нужна **третья ветка**, фильтрующая по `RocketTag` и вычисляющая rotation из `MoveData.Direction`. Все паттерны (CreateBullet, BulletVisual, GunShootEvent) имеют прямые аналоги в кодовой базе.

**Primary recommendation:** Следовать существующим паттернам CreateBullet/BulletVisual/GunShootEvent точно, добавляя минимальные отличия: третью ветку в GameObjectSyncSystem для rotation по Direction и RocketShootEvent для триггера спавна.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Добавить третью ветку в `GameObjectSyncSystem` для entity с `RocketTag` + `MoveData` + `GameObjectRef` без `RotateData` -- синхронизировать rotation из `MoveData.Direction` через `math.atan2(dir.y, dir.x)`
- **D-02:** Не добавлять `RotateData` на rocket entity -- это вызвало бы конфликт с `EcsRotateSystem`, которая обрабатывает RotateData по TargetDirection от ввода игрока
- **D-03:** Позиция синхронизируется как обычно: `MoveData.Position` -> `Transform.position`
- **D-04:** Новый метод `EntitiesCatalog.CreateRocket()` по аналогии с `CreateBullet()` -- создаёт ViewModel, Visual, привязки коллизий, GameObjectRef, регистрация в CollisionBridge и AddToCatalog
- **D-05:** Расширить enum `EntityType` значением `Rocket`
- **D-06:** `GameObjectRef` обязателен -- без него `DeadEntityCleanupSystem` не подхватит уничтожение ракеты и GameObject останется в сцене
- **D-07:** Новый `RocketShootEvent` как `DynamicBuffer<IBufferElementData>` -- единообразно с `GunShootEvent` и `LaserShootEvent`
- **D-08:** Обработка в `ShootEventProcessorSystem` -- добавить ветку для `RocketShootEvent`, вызывающую `EntitiesCatalog.CreateRocket()`
- **D-09:** `EcsRocketAmmoSystem` генерирует `RocketShootEvent` при запуске ракеты (аналогично тому как GunSystem генерирует GunShootEvent)
- **D-10:** Создать минимальный `RocketVisual` (аналог `BulletVisual`) -- хранит `Collider2D`, пробрасывает `OnCollisionEnter2D` через ViewModel.OnCollision
- **D-11:** Отдельный префаб ракеты с уменьшенным спрайтом корабля (VIS-01: `ShipData.MainSprite` в уменьшенном масштабе)
- **D-12:** Конфигурация `RocketData` в `GameData` -- ссылка на префаб, как для всех остальных entity
- **D-13:** Интеграционные тесты покрывают полный lifecycle: спавн entity + GameObjectRef -> наведение (GuidanceSystem обновляет Direction) -> коллизия (DeadTag) -> cleanup (DeadEntityCleanupSystem уничтожает GameObject)
- **D-14:** Тесты в EditMode используя существующий `AsteroidsEcsTestFixture` -- проверка что GameObjectRef корректно создаётся и уничтожается

### Claude's Discretion
- Конкретный масштаб уменьшения спрайта ракеты (0.3x-0.5x от оригинала -- решить при создании префаба)
- Размер и форма Collider2D ракеты
- Порядок обработки RocketShootEvent в ShootEventProcessorSystem (до или после GunShootEvent)
- Формулировка Assert-сообщений в интеграционных тестах

### Deferred Ideas (OUT OF SCOPE)
- Инверсионный след (ParticleSystem trail) -- Phase 14
- Взрыв VFX при попадании -- Phase 14
- Input (кнопка R) для запуска ракеты -- Phase 13
- ScriptableObject конфигурация параметров -- Phase 14
- HUD отображение боезапаса -- Phase 15
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| VIS-01 | Ракета отображается как уменьшенный спрайт корабля | RocketVisual + префаб с ShipData.MainSprite в масштабе 0.4x, паттерн BulletVisual |
| VIS-03 | Спрайт ракеты вращается по направлению полёта | Третья ветка GameObjectSyncSystem: RocketTag + MoveData.Direction -> math.atan2 -> Transform.rotation |
| TEST-02 | Интеграционные тесты на lifecycle ракеты (спавн -> наведение -> коллизия -> уничтожение) | AsteroidsEcsTestFixture + GameObjectRef + DeadEntityCleanupSystem тесты |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- Язык документации и комментариев: русский
- Фигурные скобки обязательны в `if`/`else`/`for`/`while`, даже для однострочных тел
- Никаких однострочников
- Namespace: `SelStrom.Asteroids` (основной), `SelStrom.Asteroids.ECS` (компоненты/системы), `SelStrom.Asteroids.Configs` (конфиги)
- Naming: `PascalCase` для классов/методов, `_camelCase` для приватных полей, `[SerializeField]` поля с `_`
- Без `switch expressions`, без ternary/null-coalescing
- Тесты обязательны при изменении поведения
- C# 9.0, .NET Standard 2.1, Unity 2022.3.60f1

## Architecture Patterns

### Рекомендуемая структура файлов
```
Assets/Scripts/
├── ECS/
│   ├── Components/
│   │   └── RocketShootEvent.cs          # IBufferElementData -- событие запуска ракеты
│   └── Systems/
│       └── GameObjectSyncSystem.cs      # ИЗМЕНИТЬ -- добавить третью ветку для RocketTag
├── Bridge/
│   └── ShootEventProcessorSystem.cs     # ИЗМЕНИТЬ -- добавить ProcessRocketEvents()
├── View/
│   └── RocketVisual.cs                  # НОВЫЙ -- RocketViewModel + RocketVisual
├── Application/
│   └── EntitiesCatalog.cs               # ИЗМЕНИТЬ -- добавить CreateRocket(), EntityType.Rocket
└── Configs/
    └── GameData.cs                      # ИЗМЕНИТЬ -- добавить RocketData struct

Assets/Tests/EditMode/ECS/
├── RocketLifecycleTests.cs              # НОВЫЙ -- интеграционные тесты lifecycle
└── AsteroidsEcsTestFixture.cs           # ИЗМЕНИТЬ -- добавить CreateRocketShootEventSingleton()
```

### Pattern 1: Entity с визуалом (CreateBullet-паттерн)
**What:** Фабричный метод в EntitiesCatalog создает ECS entity через EntityFactory, затем добавляет GameObjectRef и регистрирует в CollisionBridge
**When to use:** Для всех entity с визуальным представлением в сцене
**Example:**
```csharp
// Источник: Assets/Scripts/Application/EntitiesCatalog.cs:127-158
public void CreateRocket(Vector2 position, Vector2 direction)
{
    var viewModel = new RocketViewModel();
    var bindings = new EventBindingContext();
    bindings.InvokeAll();

    var view = _viewFactory.Get<RocketVisual>(_configs.Rocket.Prefab);
    view.Connect(viewModel);

    var entity = EntityFactory.CreateRocket(
        _entityManager,
        new float2(position.x, position.y),
        speed: 8f,                    // TODO: из конфига в Phase 14
        new float2(direction.x, direction.y),
        lifeTime: 5f,                 // TODO: из конфига в Phase 14
        turnRateDegPerSec: 180f       // TODO: из конфига в Phase 14
    );
    _entityManager.AddComponentObject(entity, new GameObjectRef
    {
        Transform = view.transform,
        GameObject = view.gameObject
    });
    _collisionBridge.RegisterMapping(view.gameObject, entity);

    viewModel.OnCollision.Value = col =>
    {
        _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
    };

    AddToCatalog(view.gameObject, entity, EntityType.Rocket, bindings);
}
```
[VERIFIED: Assets/Scripts/Application/EntitiesCatalog.cs -- CreateBullet паттерн]

### Pattern 2: Синхронизация вращения по Direction (третья ветка)
**What:** Третья ветка в GameObjectSyncSystem для entity с RocketTag -- синхронизация rotation из MoveData.Direction
**When to use:** Entity без RotateData, но с необходимостью вращения визуала по направлению движения
**Example:**
```csharp
// Источник: GameObjectSyncSystem.cs + решение D-01
// Третья ветка: entity с RocketTag, MoveData, GameObjectRef, без RotateData
foreach (var (move, goRef) in
         SystemAPI.Query<RefRO<MoveData>, GameObjectRef>()
             .WithAll<RocketTag>()
             .WithNone<RotateData>())
{
    var pos = move.ValueRO.Position;
    goRef.Transform.position = new Vector3(pos.x, pos.y, goRef.Transform.position.z);

    var dir = move.ValueRO.Direction;
    var angle = math.atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    goRef.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
}
```
[VERIFIED: GameObjectSyncSystem.cs -- существующая структура двух веток]

**ВАЖНО:** Вторая ветка (без RotateData) использует `.WithNone<RotateData>()`. Ракета тоже не имеет RotateData, поэтому она попадёт во вторую ветку. Третья ветка должна быть с `.WithAll<RocketTag>()`, а вторая ветка должна получить `.WithNone<RocketTag>()` для исключения ракет из неё.

### Pattern 3: DynamicBuffer ShootEvent
**What:** IBufferElementData для передачи событий из ECS-системы в bridge-слой
**When to use:** Когда ECS-система должна создать entity с визуалом
**Example:**
```csharp
// Источник: Assets/Scripts/ECS/Components/GunShootEvent.cs
public struct RocketShootEvent : IBufferElementData
{
    public Entity ShooterEntity;
    public float2 Position;
    public float2 Direction;
}
```
[VERIFIED: GunShootEvent.cs -- точная структура IBufferElementData]

### Pattern 4: RocketVisual (аналог BulletVisual)
**What:** Минимальный Visual с Collider2D, пробрасывающий коллизии
**When to use:** Entity-снаряды с физическими коллизиями
**Example:**
```csharp
// Источник: Assets/Scripts/View/BulletVisual.cs -- паттерн для RocketVisual
public class RocketViewModel : AbstractViewModel
{
    public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
}

public class RocketVisual : AbstractWidgetView<RocketViewModel>, IEntityView
{
    [SerializeField] private Collider2D _collider = default;

    protected override void OnConnected()
    {
        _collider.enabled = true;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        ViewModel.OnCollision.Value?.Invoke(col);
    }
}
```
[VERIFIED: BulletVisual.cs -- точная копия паттерна]

### Anti-Patterns to Avoid
- **Добавление RotateData на ракету:** Вызовет конфликт с EcsRotateSystem, которая управляет RotateData по TargetDirection от ввода игрока (D-02)
- **Забыть GameObjectRef:** Без него DeadEntityCleanupSystem не вызовет callback и GameObject останется в сцене (D-06)
- **Забыть .WithNone<RocketTag>() во второй ветке GameObjectSyncSystem:** Ракета попадёт во вторую ветку (только позиция, без rotation) И в третью (позиция + rotation), что приведет к двойной обработке
- **Забыть CollisionBridge.RegisterMapping:** Коллизии ракеты не будут обработаны ECS-системой

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Пулинг GameObject ракеты | Свой пул | `GameObjectPool` + `ViewFactory` | Уже работает для пуль, астероидов, UFO -- единый путь |
| Уничтожение ракеты при DeadTag | Свой cleanup | `DeadEntityCleanupSystem` | Автоматически подхватит любую entity с GameObjectRef + DeadTag |
| Маппинг GameObject <-> Entity | Свой словарь | `EntitiesCatalog._gameObjectToEntity` + `CollisionBridge` | Единый реестр с корректным cleanup |
| Синхронизация позиции | Свой Update() на MonoBehaviour | `GameObjectSyncSystem` | Централизованная синхронизация всех entity в PresentationSystemGroup |

## Common Pitfalls

### Pitfall 1: Двойная обработка в GameObjectSyncSystem
**What goes wrong:** Ракета без RotateData попадает во вторую ветку (position only) И в третью ветку (position + rotation от Direction)
**Why it happens:** Вторая ветка фильтрует `.WithNone<RotateData>()` -- ракета удовлетворяет этому условию
**How to avoid:** Добавить `.WithNone<RocketTag>()` во вторую ветку ИЛИ переместить третью ветку перед второй и сделать её с `.WithAll<RocketTag>().WithNone<RotateData>()`
**Warning signs:** Ракета синхронизирует позицию, но не вращается (вторая ветка перезаписывает rotation)
[VERIFIED: GameObjectSyncSystem.cs строки 24-31 -- вторая ветка .WithNone<RotateData>()]

### Pitfall 2: RocketShootEvent буфер не создан на ship entity
**What goes wrong:** `ShootEventProcessorSystem` не находит `DynamicBuffer<RocketShootEvent>` для обработки
**Why it happens:** `EntityFactory.CreateShip` не добавляет буфер `RocketShootEvent` на ship entity
**How to avoid:** Добавить `em.AddBuffer<RocketShootEvent>(entity)` в `EntityFactory.CreateShip`
**Warning signs:** Ракеты не появляются при запуске, хотя ammo расходуется
[VERIFIED: EntityFactory.cs строки 8-62 -- CreateShip не создаёт буферы ShootEvent; проверить где они создаются]

### Pitfall 3: Параметры ракеты захардкожены
**What goes wrong:** Speed, lifeTime, turnRate захардкожены в CreateRocket вместо конфига
**Why it happens:** Phase 14 (CONF-01) отложена, но нужны разумные значения сейчас
**How to avoid:** Использовать временные константы в EntitiesCatalog.CreateRocket() с TODO-комментарием для Phase 14
**Warning signs:** Не проблема сейчас, но станет при балансировке

### Pitfall 4: Порядок обработки ShootEvents
**What goes wrong:** RocketShootEvent обрабатывается после того, как entity уже уничтожена
**Why it happens:** `ShootEventProcessorSystem` обновляется в `LateSimulationSystemGroup`, `UpdateBefore(DeadEntityCleanupSystem)` -- это правильно, но ракетный event должен обрабатываться в том же кадре
**How to avoid:** `ProcessRocketEvents()` вызывать в `OnUpdate()` наряду с `ProcessGunEvents()` и `ProcessLaserEvents()`
[VERIFIED: ShootEventProcessorSystem.cs строки 37-46 -- OnUpdate вызывает ProcessGunEvents/ProcessLaserEvents]

### Pitfall 5: Префаб ракеты без физического слоя
**What goes wrong:** Коллизии ракеты не срабатывают с врагами
**Why it happens:** Неправильный Physics Layer на префабе
**How to avoid:** Установить layer "PlayerBullet" на префабе ракеты (или отдельный "Rocket" -- решение из Phase 11 STATE.md)
**Warning signs:** Ракета пролетает сквозь врагов

## Code Examples

### RocketShootEvent (новый компонент)
```csharp
// Assets/Scripts/ECS/Components/RocketShootEvent.cs
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketShootEvent : IBufferElementData
    {
        public Entity ShooterEntity;
        public float2 Position;
        public float2 Direction;
    }
}
```
[VERIFIED: GunShootEvent.cs, LaserShootEvent.cs -- точный паттерн]

### RocketData в GameData (расширение конфига)
```csharp
// Добавить в GameData.cs
[Serializable]
public struct RocketData
{
    public GameObject Prefab;
}

// Добавить поле в класс GameData:
[Space]
public RocketData Rocket;
```
[VERIFIED: GameData.cs -- паттерн BulletData/LaserData]

### ProcessRocketEvents (расширение ShootEventProcessorSystem)
```csharp
// Добавить в ShootEventProcessorSystem.cs
private readonly List<RocketShootEvent> _pendingRocketEvents = new();

private void ProcessRocketEvents()
{
    _pendingRocketEvents.Clear();

    foreach (var buffer in SystemAPI.Query<DynamicBuffer<RocketShootEvent>>())
    {
        if (buffer.Length == 0)
        {
            continue;
        }

        for (int i = 0; i < buffer.Length; i++)
        {
            _pendingRocketEvents.Add(buffer[i]);
        }

        buffer.Clear();
    }

    for (int i = 0; i < _pendingRocketEvents.Count; i++)
    {
        var evt = _pendingRocketEvents[i];
        var position = new Vector2(evt.Position.x, evt.Position.y);
        var direction = new Vector2(evt.Direction.x, evt.Direction.y);
        _catalog.CreateRocket(position, direction);
    }
}
```
[VERIFIED: ShootEventProcessorSystem.cs строки 50-77 -- точный паттерн ProcessGunEvents]

### Интеграционный тест lifecycle
```csharp
// Assets/Tests/EditMode/ECS/RocketLifecycleTests.cs
[Test]
public void RocketLifecycle_SpawnToCleanup()
{
    // 1. Создаём ракету с GameObjectRef
    var go = CreateTestGameObject("Rocket");
    var entity = CreateRocketEntity(
        float2.zero, 8f, new float2(1f, 0f), 5f, 180f);
    m_Manager.AddComponentObject(entity, new GameObjectRef
    {
        Transform = go.transform,
        GameObject = go
    });

    // 2. Проверяем entity существует и имеет компоненты
    Assert.IsTrue(m_Manager.Exists(entity));
    Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
    Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
    Assert.IsTrue(m_Manager.HasComponent<GameObjectRef>(entity));

    // 3. Добавляем DeadTag (имитация коллизии)
    m_Manager.AddComponentData(entity, new DeadTag());

    // 4. Запускаем cleanup
    _cleanupSystem.Update();

    // 5. Проверяем entity уничтожена
    Assert.IsFalse(m_Manager.Exists(entity));
    Assert.AreEqual(1, _callbackResults.Count);
    Assert.AreEqual(go, _callbackResults[0].GameObject);
}
```
[VERIFIED: DeadEntityCleanupSystemTests.cs -- паттерн тестирования lifecycle]

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | `Assets/Tests/EditMode/EditMode.asmdef` |
| Quick run command | `unity -runTests -testPlatform EditMode -testFilter RocketLifecycle` |
| Full suite command | `unity -runTests -testPlatform EditMode` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| VIS-01 | Ракета отображается с уменьшенным спрайтом корабля | manual-only (визуальная проверка) | -- | -- |
| VIS-03 | Спрайт вращается по направлению полёта | unit | `GameObjectSyncSystemTests` -- добавить тест для RocketTag | Частично (нет теста для RocketTag) |
| TEST-02 | Интеграционные тесты lifecycle | integration | `RocketLifecycleTests` | Wave 0 |

### Sampling Rate
- **Per task commit:** Запуск тестов в Unity Editor (EditMode)
- **Per wave merge:** Полный EditMode suite
- **Phase gate:** Все тесты зелёные перед `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/ECS/RocketLifecycleTests.cs` -- covers TEST-02 (lifecycle спавн -> cleanup)
- [ ] Тест синхронизации rotation для RocketTag в `GameObjectSyncSystemTests.cs` -- covers VIS-03

## Security Domain

Фаза не затрагивает аутентификацию, сетевые запросы, пользовательский ввод или криптографию. Все изменения внутриигровые (ECS <-> GameObject bridge).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Масштаб спрайта ракеты 0.4x от корабля визуально подходит | Architecture Patterns | Низкий -- легко изменить в префабе |
| A2 | Временные значения speed=8, lifeTime=5, turnRate=180 разумны для начала | Pitfall 3 | Низкий -- Phase 14 вынесет в конфиг |
| A3 | Physics Layer "PlayerBullet" подходит для ракеты | Pitfall 5 | Средний -- может потребоваться отдельный layer если логика коллизий отличается от пуль |
| A4 | GunShootEvent/LaserShootEvent буферы создаются где-то за пределами EntityFactory.CreateShip | Pitfall 2 | Средний -- нужно найти где создаются буферы и добавить RocketShootEvent |

## Open Questions (RESOLVED)

1. **Где создаются DynamicBuffer<GunShootEvent> и DynamicBuffer<LaserShootEvent>?**
   - **RESOLVED:** Singleton entity с буферами создаётся в Application.cs при старте ECS-мира. Plan 12-02 Task 2 добавляет `DynamicBuffer<RocketShootEvent>` в то же место (Application.cs singleton pattern).

2. **Physics Layer для ракеты: PlayerBullet или отдельный Rocket?**
   - **RESOLVED:** Использовать "PlayerBullet" -- коллизионная матрица идентична пулям (Phase 11 D-05: ракета коллидирует только с врагами). Отдельный layer не нужен.

## Sources

### Primary (HIGH confidence)
- `Assets/Scripts/ECS/Systems/GameObjectSyncSystem.cs` -- текущая структура синхронизации (2 ветки)
- `Assets/Scripts/Bridge/ShootEventProcessorSystem.cs` -- паттерн обработки ShootEvent
- `Assets/Scripts/Bridge/DeadEntityCleanupSystem.cs` -- паттерн lifecycle cleanup
- `Assets/Scripts/Application/EntitiesCatalog.cs` -- паттерн CreateBullet для CreateRocket
- `Assets/Scripts/View/BulletVisual.cs` -- паттерн минимального Visual
- `Assets/Scripts/ECS/EntityFactory.cs` -- CreateRocket уже существует
- `Assets/Scripts/Configs/GameData.cs` -- структура конфигурации
- `Assets/Scripts/ECS/Components/GunShootEvent.cs` -- паттерн IBufferElementData
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` -- тестовый fixture
- `Assets/Tests/EditMode/ECS/DeadEntityCleanupSystemTests.cs` -- паттерн тестирования lifecycle
- `Assets/Tests/EditMode/ECS/GameObjectSyncSystemTests.cs` -- паттерн тестирования синхронизации

### Secondary (MEDIUM confidence)
- `.planning/phases/12-bridge-lifecycle/12-CONTEXT.md` -- решения пользователя D-01..D-14

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все паттерны существуют в кодовой базе, нужно только расширение
- Architecture: HIGH -- прямые аналоги (BulletVisual, CreateBullet, GunShootEvent) уже реализованы
- Pitfalls: HIGH -- выявлены через анализ кода (двойная обработка в GameObjectSyncSystem, отсутствие буферов)

**Research date:** 2026-04-05
**Valid until:** 2026-05-05 (стабильный паттерн, зависит только от внутреннего кода)
